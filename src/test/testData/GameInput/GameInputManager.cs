using System.Collections.Generic;
using System.Linq;
using _Car_Parking.Scripts.Other.Helpers;
using _Car_Parking.Scripts.SOAP;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Zenject;

namespace _Car_Parking.Scripts.GameInput
{
    public partial class GameInputManager : MonoBehaviour
    {
        [SerializeField] private EventRegistry mainRegistry;

        private EventRegistryService _eventRegistryService;

        private GameInputController _controls;
        private UnityEngine.EventSystems.EventSystem _lastEventSystem;
        private bool _initialized;

        private bool _isInputActive;
        private bool _isCameraActive;
        private int _lastGroupSwitchFrame = -1;

        private bool debug = true;

        private InputField _chatInputField;

        [Inject]
        public void Construct(
            EventRegistryService eventRegistryService)
        {
            _eventRegistryService = eventRegistryService;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void TrySetInput(InputGroupsEnum inputGroup)
        {
            WDebug.Log($"[GameInputManager] TrySetInput to {inputGroup}", Color.orange, this);
            _lastGroupSwitchFrame = Time.frameCount;
            V.OnOutOfCarChanged -= OnOutOfCarChanged;

            List<InputMapEnum> mapsToDisable = _inputMapSetEvent.GetMapsToDisable().ToList();
            List<InputMapEnum> mapsToEnable = _inputMapSetEvent.GetMapsToEnable().ToList();
            if (debug)
            {
                WDebug.Log($"[GameInputManager] input maps enabled are {mapsToEnable.Beautify()}, maps to disable are  {mapsToDisable.Beautify()}");
            }

            SwitchMapsAsync(mapsToDisable, mapsToEnable).Forget();

            bool isStickEnabled = _inputMapSetEvent.CurrentInputGroup == InputGroupsEnum.Ui || _inputMapSetEvent.CurrentInputGroup == InputGroupsEnum.Garage;
            SetUINavigation(isStickEnabled);
        }

        private async UniTask SwitchMapsAsync(List<InputMapEnum> toDisable, List<InputMapEnum> toEnable)
        {
            await UniTask.Yield();

            foreach (var map in toDisable)
            {
                CleanupDisabledMap(map);
            }

            await UniTask.Yield();

            foreach (var map in toEnable)
            {
                SetupEnabledMap(map);
            }

            if (_inputMapSetEvent.TryGetCurrentMaps(out var maps) && maps.Contains(InputMapEnum.Camera) && !_isCameraActive)
            {
                ToggleMap(InputMapEnum.Camera, true);
                _isCameraActive = true;
            }
        }
        
        private void SetupEnabledMap(InputMapEnum inputMap)
        {
            switch (inputMap)
            {
                case InputMapEnum.UI:
                    AssignInputToActiveEventSystem();
                    break;
                case InputMapEnum.UiPointer:
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    break;
            }

            ToggleMap(inputMap, true);
        }
        
        private void CleanupDisabledMap(InputMapEnum previousMap)
        {
            switch (previousMap)
            {
                case InputMapEnum.UiPointer:
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    break;
                case InputMapEnum.Camera:
                case InputMapEnum.CameraGarage:
                case InputMapEnum.DroneCamera: 
                    _lookHandle.Get(_eventRegistryService)?.Reset();
                    _zoomHandle.Get(_eventRegistryService)?.Reset();
                    _onLookHandle.Get(_eventRegistryService)?.Raise(false);
                    break;
                case InputMapEnum.CircleMenu:
                    _circleMenuMoveHandle.Get(_eventRegistryService)?.Reset();
                    break;
                case InputMapEnum.Shifter:
                    _shifterMoveHandle.Get(_eventRegistryService)?.Reset();
                    break;
                case InputMapEnum.CarBase:
                    _throttleHandle.Get(_eventRegistryService)?.Reset();
                    _brakeHandle.Get(_eventRegistryService)?.Reset();
                    _steerHandle.Get(_eventRegistryService)?.Reset();
                    _handbrakeHandle.Get(_eventRegistryService)?.Reset();
                    _clutchHandle.Get(_eventRegistryService)?.Reset();
                    break;
                case InputMapEnum.UiThrottle:
                    _throttleHandle.Get(_eventRegistryService)?.Reset();
                    break;
                case InputMapEnum.Person:
                    _movePersonHandle.Get(_eventRegistryService)?.Reset();
                    break;
                case InputMapEnum.MapUi:
                    _mapMoveHandle.Get(_eventRegistryService)?.Reset();
                    break;
                case InputMapEnum.PaintSelectionMade:
                    _positionChangeHandle.Get(_eventRegistryService)?.Reset();
                    break;
                case InputMapEnum.PaintSize:
                    _sizeChangeHandle.Get(_eventRegistryService)?.Reset();
                    _turnLeftRightHandle.Get(_eventRegistryService)?.Reset();
                    break;
                case InputMapEnum.PaintColor:
                    _opacityChangeHandle.Get(_eventRegistryService)?.Reset();
                    _colorChangeHandle.Get(_eventRegistryService)?.Reset();
                    break;
            }
            
            ToggleMap(previousMap, false);
        }
        private void AssignInputToActiveEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
                return;

            if (UnityEngine.EventSystems.EventSystem.current.TryGetComponent<InputSystemUIInputModule>(out var uiModule) 
                && UnityEngine.EventSystems.EventSystem.current != _lastEventSystem)
            {
                _lastEventSystem = UnityEngine.EventSystems.EventSystem.current;
                uiModule.actionsAsset = _controls.asset;
                uiModule.move = InputActionReference.Create(_controls.UI.Navigate);
                // uiModule.submit = InputActionReference.Create(_controls.UIAcceptCancel.Submit); // removed because steamdeckkeyboard uses the action
                uiModule.cancel = InputActionReference.Create(_controls.UIAcceptCancel.Cancel);
                uiModule.point = InputActionReference.Create(_controls.UiPointer.Point);
                uiModule.leftClick = InputActionReference.Create(_controls.UiPointer.Click);
                uiModule.scrollWheel = InputActionReference.Create(_controls.UiPointer.ScrollWheel);
                uiModule.rightClick = InputActionReference.Create(_controls.UiPointer.RightClick);
                uiModule.middleClick = InputActionReference.Create(_controls.UiPointer.MiddleClick);
                uiModule.trackedDevicePosition = InputActionReference.Create(_controls.UiPointer.TrackedDevicePosition);
                uiModule.trackedDeviceOrientation = InputActionReference.Create(_controls.UiPointer.TrackedDeviceOrientation);
            }
        }

        private void SetUINavigation(bool isStickEnabled = false)
        {
            InputAction navigateAction = _controls.UI.Navigate; 

            for (int i = 0; i < navigateAction.bindings.Count; i++) 
            {
                if (!navigateAction.bindings[i].path.Contains("leftStick", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                if (isStickEnabled)
                {
                    WDebug.Log("[GameInputManager] SetUINavigation RemoveBindingOverride", Color.orange, transform);
                    navigateAction.RemoveBindingOverride(i);
                    return; 
                }
        
                WDebug.Log($"[GameInputManager] SetUINavigation ApplyBindingOverride", Color.orange, transform);
                navigateAction.ApplyBindingOverride(i, new InputBinding { overridePath = "" });
                return; 
            }
        }
    }
}
