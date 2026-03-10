using _Car_Parking.Scripts.Other.Helpers;
using UnityEngine;

namespace _Car_Parking.Scripts.SOAP
{
    public class EventRegistryService
    {
        private EventRegistry _registry;

        public void Provide(EventRegistry registry)
        {
            _registry = registry;
            _registry.Initialize();
        }

        public T GetInputMapSetEvent<T>() where T : ScriptableObject
        {
            if (_registry == null)
            {
                WDebug.LogError("[Service] Registry not initialized! Check GameInputManager.");
                return null;
            }
            return _registry.GetInputMapSetEvent<T>();
        }

        // Hard Get
        // ReSharper disable Unity.PerformanceAnalysis
        public T Get<T>(string id) where T : ScriptableObject
        {
            if (_registry == null)
            {
                WDebug.LogError("[Service] Registry not initialized! Check GameInputManager.");
                return null;
            }
            return _registry.Get<T>(id);
        }

        // Soft Get
        public bool TryGet<T>(string id, out T asset) where T : ScriptableObject
        {
            if (_registry == null)
            {
                asset = null;
                return false;
            }
            return _registry.TryGet(id, out asset);
        }
    }
}