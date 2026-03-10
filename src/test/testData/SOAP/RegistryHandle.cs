using UnityEngine;

namespace _Car_Parking.Scripts.SOAP
{
    public struct RegistryHandle<T> where T : ScriptableObject
    {
        private readonly string _id;
        private T _cachedAsset;

        public RegistryHandle(string id)
        {
            _id = id;
            _cachedAsset = null;
        }

        public T Get(EventRegistryService eventRegistryService)
        {
            if (_cachedAsset)
                return _cachedAsset;

            if (eventRegistryService.TryGet(_id, out T asset))
            {
                _cachedAsset = asset;
            }

            return _cachedAsset;
        }

        public static implicit operator bool(RegistryHandle<T> handle) => handle._cachedAsset != null;
    }
}