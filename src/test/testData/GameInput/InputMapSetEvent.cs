using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using _Car_Parking.Scripts.Other.Helpers;
using UnityEngine;

namespace _Car_Parking.Scripts.GameInput
{
    [CreateAssetMenu(fileName = "New InputMapSetEvent", menuName = "SOAP/InputMapsConfig")]
    public class InputMapSetEvent : ScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField, TextArea] protected string description;
#endif
        [SerializeField] protected bool debugLog;
        private InputGroupsEnum _currentInputGroup;
        public InputGroupsEnum CurrentInputGroup => _currentInputGroup;

        [Tooltip("DOES NOT UPDATE IN RUNTIME")]
        [SerializeField] private List<InputMapGroup> inputGroups = new();

        private InputGroupsEnum _previousInputGroup;
        public InputGroupsEnum PreviousInputGroup => _previousInputGroup;
        private readonly Dictionary<InputGroupsEnum, IEnumerable<InputMapEnum>> _inputMapGroups = new();

        private event Action<InputGroupsEnum> OnRaised;

        public void Subscribe(Action<InputGroupsEnum> listener) => OnRaised += listener;
        public void Unsubscribe(Action<InputGroupsEnum> listener) => OnRaised -= listener;

        private void Raise(InputGroupsEnum value)
        {
            LogRaise(value);
            OnRaised?.Invoke(value);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private void LogRaise(InputGroupsEnum value)
        {
            if (debugLog)
                WDebug.Log($"[SOAP] {name} raised with {value}", Color.cyan);
        }

        private void OnDisable() => OnRaised = null;

        public void Initialize()
        {
            _inputMapGroups.Clear();
            foreach (InputMapGroup group in inputGroups)
                _inputMapGroups.Add(group.GroupName, group.GetAllMaps());

            _currentInputGroup = InputGroupsEnum.None;
            _previousInputGroup = InputGroupsEnum.None;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public bool TrySetGroup(InputGroupsEnum newGroup)
        {
            if (newGroup == _currentInputGroup)
                return false;

            if (!_inputMapGroups.ContainsKey(newGroup))
            {
                WDebug.Log($"[GameInputManager] _inputMapGroups doesn't contain raised {newGroup}");
                return false;
            }

            _previousInputGroup = _currentInputGroup;
            _currentInputGroup = newGroup;

            Raise(newGroup); // GameInputManager receives this

            return true;
        }

        // Maps that differ between previous and current group
        public IEnumerable<InputMapEnum> GetChangedMaps()
        {
            var current  = _inputMapGroups.TryGetValue(_currentInputGroup,  out var curr) 
                ? curr 
                : Enumerable.Empty<InputMapEnum>();
            var previous = _inputMapGroups.TryGetValue(_previousInputGroup, out var prev) 
                ? prev 
                : Enumerable.Empty<InputMapEnum>();

            return current.Except(previous)   // newly added → enable
                .Union(previous.Except(current)); // removed → disable
        }

        public IEnumerable<InputMapEnum> GetMapsToEnable()  => 
            _inputMapGroups.TryGetValue(_currentInputGroup,  out var curr) &&
            _inputMapGroups.TryGetValue(_previousInputGroup, out var prev)
                ? curr.Except(prev)
                : _inputMapGroups[_currentInputGroup];

        public IEnumerable<InputMapEnum> GetMapsToDisable() => 
            _inputMapGroups.TryGetValue(_previousInputGroup, out var prev) &&
            _inputMapGroups.TryGetValue(_currentInputGroup,  out var curr)
                ? prev.Except(curr)
                : Enumerable.Empty<InputMapEnum>();

        public bool TryGetCurrentMaps(out IEnumerable<InputMapEnum> maps)
        {
            return _inputMapGroups.TryGetValue(_currentInputGroup, out maps);
        }
    }
}