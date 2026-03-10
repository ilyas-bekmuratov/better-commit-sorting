using System;
using System.Diagnostics;
using _Car_Parking.Scripts.Other.Helpers;
using UnityEngine;

namespace _Car_Parking.Scripts.SOAP
{
    public abstract class ScriptableEvent<T> : ScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField, TextArea] protected string description;
#endif
        [SerializeField] protected bool debugLog;

        private event Action<T> _onRaised;

        public void Subscribe(Action<T> listener) => _onRaised += listener;
        public void Unsubscribe(Action<T> listener) => _onRaised -= listener;

        public void Raise(T value)
        {
            LogRaise(value);
            _onRaised?.Invoke(value);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private void LogRaise(T value)
        {
            if (debugLog)
                WDebug.Log($"[SOAP] {name} raised with {value}", Color.cyan);
        }

        private void OnDisable() => _onRaised = null;
    }
}
