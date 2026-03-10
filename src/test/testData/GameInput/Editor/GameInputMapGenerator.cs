// Parses GameInputController.cs to extract action map property names,
// then generates InputMapEnum.cs and GameInputMapToggle.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace _Car_Parking.Scripts.GameInput.Editor
{
    public static class GameInputMapGenerator
    {
        private const string PrefAuto  = "GIG_AutoMapToggle";
        private const string Namespace = "_Car_Parking.Scripts.GameInput";

        // Legacy/unused maps to skip from generation
        private static readonly HashSet<string> SkipProperties = new()
        {
            "Car", "SteeringWheel"
        };

        private static string InputMapEnumPath =>
            "Assets/_Car Parking/Scripts/GameInput/InputMapEnum.cs";
        private static string TogglePath =>
            "Assets/_Car Parking/Scripts/GameInput/GameInputMapToggle.cs";

        private class EnumItem
        {
            public string Name;
            public int Value;
            public bool IsCommented;
        }

        // ── Menu items ────────────────────────────────────────────────────────
        [MenuItem("CPMInstruments/Game Input/Generate InputMapEnum + MapToggle")]
        public static void GenerateFromMenu()
        {
            string path = FindGICPath();
            if (path == null)
            {
                Debug.LogError("[InputMapGen] Could not find GameInputController.cs.");
                return;
            }
            Generate(path);
        }

        [MenuItem("CPMInstruments/Game Input/Toggle Auto-refresh: InputMapEnum + MapToggle")]
        public static void ToggleAutoRefresh()
        {
            bool next = !EditorPrefs.GetBool(PrefAuto, false);
            EditorPrefs.SetBool(PrefAuto, next);
            Debug.Log($"[InputMapGen] Auto-refresh set to: {next}");
        }

        [MenuItem("CPMInstruments/Game Input/Toggle Auto-refresh: InputMapEnum + MapToggle", true)]
        public static bool ValidateToggle()
        {
            Menu.SetChecked("CPMInstruments/Game Input/Toggle Auto-refresh: InputMapEnum + MapToggle",
                EditorPrefs.GetBool(PrefAuto, false));
            return true;
        }

        public static void Generate(string gicAssetPath)
        {
            string src = File.ReadAllText(Path.GetFullPath(gicAssetPath));

            // Parse: public UIActions @UI => ...  →  propertyName = "UI"
            // The @ is the verbatim identifier prefix Unity generates.
            var entries = new List<string>(); // property names e.g. "UI", "Car", "MapUi"
            var matches = Regex.Matches(src, @"public \w+Actions @(\w+) =>");
            foreach (Match m in matches)
            {
                string prop = m.Groups[1].Value;
                if (!SkipProperties.Contains(prop))
                    entries.Add(prop);
            }

            if (entries.Count == 0)
            {
                Debug.LogError("[InputMapGen] No action map properties found in GameInputController.cs.");
                return;
            }

            GenerateInputMapEnum(entries);
            GenerateMapToggle(entries);
            EditorApplication.delayCall += AssetDatabase.Refresh;
        }

        private static void GenerateInputMapEnum(List<string> currentEntries)
        {
            string fullPath = Path.GetFullPath(InputMapEnumPath);
            List<EnumItem> enumItems = new List<EnumItem>();
            int maxValue = 0;

            // 1. Parse the existing enum file if it exists
            if (File.Exists(fullPath))
            {
                string[] lines = File.ReadAllLines(fullPath);
                // Regex to find: (optional //) Name = Value
                Regex regex = new Regex(@"^\s*(?<comment>//)?\s*(?<name>[A-Za-z0-9_]+)\s*=\s*(?<val>\d+)");
                
                foreach (string line in lines)
                {
                    Match m = regex.Match(line);
                    if (m.Success)
                    {
                        string name = m.Groups["name"].Value;
                        int val = int.Parse(m.Groups["val"].Value);
                        bool isCommented = m.Groups["comment"].Success;

                        enumItems.Add(new EnumItem { Name = name, Value = val, IsCommented = isCommented });
                        
                        if (val > maxValue) 
                            maxValue = val;
                    }
                }
            }

            // 2. Ensure "None = 0" always exists at the top
            if (enumItems.Count == 0 || enumItems[0].Name != "None")
            {
                enumItems.Insert(0, new EnumItem { Name = "None", Value = 0, IsCommented = false });
            }

            // 3. Compare existing items against the new GameInputController entries
            HashSet<string> entriesSet = new HashSet<string>(currentEntries);

            foreach (var item in enumItems)
            {
                if (item.Name == "None") continue;

                if (entriesSet.Contains(item.Name))
                {
                    item.IsCommented = false; // It's in the controller, ensure it's active
                    entriesSet.Remove(item.Name); // Mark as handled
                }
                else
                {
                    item.IsCommented = true; // No longer in the controller, comment it out
                }
            }

            foreach (string newEntry in currentEntries)
            {
                if (entriesSet.Contains(newEntry))
                {
                    maxValue++;
                    enumItems.Add(new EnumItem { Name = newEntry, Value = maxValue, IsCommented = false });
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("// THIS FILE IS AUTO-GENERATED by GameInputMapGenerator.cs");
            sb.AppendLine($"namespace {Namespace}");
            sb.AppendLine("{");
            sb.AppendLine("    [System.Serializable]");
            sb.AppendLine("    public enum InputMapEnum // actual input maps in GameInputController");
            sb.AppendLine("    {");

            foreach (var item in enumItems)
            {
                string prefix = item.IsCommented ? "        // " : "        ";
                sb.AppendLine($"{prefix}{item.Name} = {item.Value},");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            WriteIfChanged(InputMapEnumPath, sb.ToString(), "InputMapEnum");
        }

        private static void GenerateMapToggle(List<string> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// THIS FILE IS AUTO-GENERATED by GameInputMapGenerator.cs");
            sb.AppendLine("using _Car_Parking.Scripts.Other.Helpers;");
            sb.AppendLine();
            sb.AppendLine($"namespace {Namespace}");
            sb.AppendLine("{");
            sb.AppendLine("    public partial class GameInputManager");
            sb.AppendLine("    {");
            sb.AppendLine("        private void ToggleMap(InputMapEnum inputMap, bool toggleOn)");
            sb.AppendLine("        {");
            sb.AppendLine("            switch (inputMap)");
            sb.AppendLine("            {");
            foreach (string prop in entries)
            {
                sb.AppendLine($"                case InputMapEnum.{prop}:");
                sb.AppendLine($"                    if (toggleOn) _controls.{prop}.Enable(); else _controls.{prop}.Disable();");
                sb.AppendLine("                    break;");
            }
            sb.AppendLine("                default:");
            sb.AppendLine("                    WDebug.LogError($\"[GameInputManager] Unhandled InputMapEnum: {inputMap}\");");
            sb.AppendLine("                    break;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            WriteIfChanged(TogglePath, sb.ToString(), "GameInputMapToggle");
        }

        private static string FindGICPath()
        {
            foreach (string guid in AssetDatabase.FindAssets("GameInputController t:MonoScript"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith("GameInputController.cs")) return p;
            }
            return null;
        }

        private static void WriteIfChanged(string assetPath, string content, string label)
        {
            string fullPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            if (File.Exists(fullPath) && File.ReadAllText(fullPath) == content)
            {
                Debug.Log($"[InputMapGen] No changes: {label}");
                return;
            }
            File.WriteAllText(fullPath, content);
            Debug.Log($"[InputMapGen] Written: {label}");
        }

        private class AutoRefreshPostprocessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(
                string[] imported, string[] _d, string[] _m, string[] _mf)
            {
                if (!EditorPrefs.GetBool(PrefAuto, false)) return;
                foreach (string path in imported)
                {
                    if (path.EndsWith("GameInputController.cs"))
                    {
                        EditorApplication.delayCall += () => Generate(path);
                        return;
                    }
                }
            }
        }
    }
}
#endif