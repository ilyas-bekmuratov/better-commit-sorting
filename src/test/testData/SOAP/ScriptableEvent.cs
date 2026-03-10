using System;
using System.Diagnostics;
using _Car_Parking.Scripts.Other.Helpers;
using UnityEngine;

namespace _Car_Parking.Scripts.SOAP
{
    [CreateAssetMenu(fileName = "New ScriptableEvent", menuName = "SOAP/ScriptableEvent")]
    public class ScriptableEvent : ScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField, TextArea] private string description;
#endif
        [SerializeField] private bool debugLog;

        private event Action _onRaised;

        public void Subscribe(Action listener) => _onRaised += listener;
        public void Unsubscribe(Action listener) => _onRaised -= listener;

        public void Raise()
        {
            LogRaise();
            _onRaised?.Invoke();
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private void LogRaise()
        {
            if (debugLog)
                WDebug.Log($"[SOAP] {name} raised", Color.cyan);
        }

#if UNITY_EDITOR
        [ContextMenu("Raise (Test)")]
        private void RaiseFromInspector() => Raise();
#endif

        private void OnDisable() => _onRaised = null;
    }
}
