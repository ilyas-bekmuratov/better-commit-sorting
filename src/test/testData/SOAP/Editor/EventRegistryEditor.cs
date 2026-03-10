#if UNITY_EDITOR
using UnityEditor;

namespace _Car_Parking.Scripts.SOAP.Editor
{
    [CustomEditor(typeof(EventRegistry))]
    public class EventRegistryEditor : UnityEditor.Editor
    {
        private bool _autoRefresh = false;
        public override void OnInspectorGUI()
        {
            DrawValidationWarnings();
        }

        private void DrawValidationWarnings()
        {
            SerializedProperty regionsProp = serializedObject.FindProperty("entryRegions");
            if (regionsProp == null || !regionsProp.isArray) return;

            string errorMessages = "";
            bool hasErrors = false;

            // Iterate through all regions
            for (int i = 0; i < regionsProp.arraySize; i++)
            {
                SerializedProperty region = regionsProp.GetArrayElementAtIndex(i);
            
                // Check if region is actually used (optional: remove this check if you want to validate unused regions too)
                SerializedProperty isUsedProp = region.FindPropertyRelative("isUsed");
                if (isUsedProp != null && !isUsedProp.boolValue) continue;

                SerializedProperty entriesProp = region.FindPropertyRelative("entries");
                if (entriesProp == null || !entriesProp.isArray) continue;

                // Iterate through all entries in this region
                for (int j = 0; j < entriesProp.arraySize; j++)
                {
                    SerializedProperty entry = entriesProp.GetArrayElementAtIndex(j);
                    SerializedProperty idProp = entry.FindPropertyRelative("ID");
                    SerializedProperty assetProp = entry.FindPropertyRelative("Asset");

                    bool missingID = string.IsNullOrWhiteSpace(idProp.stringValue);
                    bool missingAsset = assetProp.objectReferenceValue == null;

                    if (missingID || missingAsset)
                    {
                        hasErrors = true;
                        string location = $"Region {i + 1}, Entry {j + 1}";
                    
                        if (missingID && missingAsset)
                            errorMessages += $"• {location}: Missing ID and Asset\n";
                        else if (missingID)
                            errorMessages += $"• {location}: Missing ID (Asset: {assetProp.objectReferenceValue?.name})\n";
                        else if (missingAsset)
                            errorMessages += $"• {location}: Missing Asset (ID: '{idProp.stringValue}')\n";
                    }
                }
            }

            if (hasErrors)
            {
                // Remove the last newline for clean formatting
                errorMessages = errorMessages.TrimEnd();
                EditorGUILayout.HelpBox($"MISSING DATA DETECTED:\n{errorMessages}", MessageType.Error);
                EditorGUILayout.Space(5);
            }
        }
    }
}
#endif