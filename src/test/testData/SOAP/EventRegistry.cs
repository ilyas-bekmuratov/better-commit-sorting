using System.Collections.Generic;
using System.Linq;
using _Car_Parking.Scripts.Other.Helpers;
using UnityEngine;

namespace _Car_Parking.Scripts.SOAP
{
    [CreateAssetMenu(fileName = "EventRegistry", menuName = "SOAP/Registry")]
    public class EventRegistry : ScriptableObject
    {
        [System.Serializable]
        public class EntryRegion
        {
#if UNITY_EDITOR
            [SerializeField, TextArea] private string description;
#endif
            [SerializeField] internal bool isUsed = true; 
            [SerializeField] internal List<Entry> entries = new List<Entry>();
        }

        [System.Serializable]
        public struct Entry
        {
            [Tooltip("ids are automatically stored in the EventRegistryIDs")]
            public string ID;
            [Tooltip("scriptable event must be of the exact type you need")]
            public ScriptableObject Asset; 
        }

        [Tooltip("the one and only controller of the currently active input maps")]
        [SerializeField] private ScriptableObject inputMapSetEvent;

        [Tooltip("Name of this mode (e.g. 'Driving', 'OnFoot', 'Menu')")]
        [SerializeField] private List<EntryRegion> entryRegions = new ();
        private readonly Dictionary<string, ScriptableObject> _lookup = new ();

        public void Initialize()
        {
            _lookup.Clear();
            foreach (var entry in entryRegions.Where(entryRegion => entryRegion.isUsed)
                         .SelectMany(entryRegion => entryRegion.entries
                             .Where(entry => !_lookup.ContainsKey(entry.ID))))
            {
                _lookup.Add(entry.ID, entry.Asset);
            }

            if (inputMapSetEvent == null)
            {
                WDebug.LogError("[Registry] ❌ CRITICAL: inputMapSetEvent not found in registry!");
            }
        }

        public T GetInputMapSetEvent<T>() where T : ScriptableObject
        {
            return (T)inputMapSetEvent;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public T Get<T>(string id) where T : ScriptableObject
        {
            if (!_lookup.TryGetValue(id, out var asset))
            {
                WDebug.LogError($"[Registry] ❌ CRITICAL: ID '{id}' not found in registry!");
                return null;
            }

            if (asset is T castedAsset)
            {
                return castedAsset;
            }

            WDebug.LogError($"[Registry] ⚠️ TYPE MISMATCH: ID '{id}' exists, but it is a [{asset.GetType().Name}], not a [{typeof(T).Name}]!");
            return null;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public bool TryGet<T>(string id, out T asset) where T : ScriptableObject
        {
            asset = null;

            if (!_lookup.TryGetValue(id, out var foundObj))
                return false;

            if (foundObj is T castedObj)
            {
                asset = castedObj;
                return true;
            }

            WDebug.LogWarning($"[Registry] Type mismatch for '{id}'. Found {foundObj.GetType().Name}.");
            return false;
        }
    }
}