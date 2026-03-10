using System;
using System.Diagnostics;
using _Car_Parking.Scripts.Other.Helpers;
using UnityEngine;

namespace _Car_Parking.Scripts.SOAP
{
    public abstract class ScriptableVariable<T> : ScriptableObject where T : struct
    {
#if UNITY_EDITOR
        [SerializeField, TextArea] protected string description;
#endif
        [SerializeField] protected bool debugLog;
        [SerializeField] private T initialValue;

#if UNITY_EDITOR
        [SerializeField]
#else
        [NonSerialized]
#endif
        private T _runtimeValue;

        public T Value
        {
            get => _runtimeValue;
            set
            {
                _runtimeValue = value;
                LogChange(value);
            }
        }

        /// <summary>
        /// Sets value without debug logging. Use for per-frame writes to avoid log spam.
        /// </summary>
        public void SetSilent(T value) => _runtimeValue = value;
        public void Reset() => _runtimeValue = initialValue;

        private void OnEnable() => _runtimeValue = initialValue;
        private void OnDisable() => _runtimeValue = initialValue;

        // ReSharper disable Unity.PerformanceAnalysis
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private void LogChange(T value)
        {
            if (debugLog)
                WDebug.Log($"[SOAP] {name} = {value}", Color.yellow);
        }
    }
}
