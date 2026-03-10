using UnityEngine;
using UnityEngine.InputSystem;

namespace _Car_Parking.Scripts.GameInput
{
    public partial class GameInputManager
    {
        [SerializeField] private float _gamepadLookMultiplier = 50f;
        [SerializeField] private float shifterTransformScale = 500f;

        private void FixedUpdate()
        {
            if (!_isInputActive  && (Time.frameCount - _lastGroupSwitchFrame) <= 1f)
                return;

            if (_controls.CarBase.enabled)
            {
                _throttleHandle.Get(_eventRegistryService).Value = _controls.CarBase.Throttle.ReadValue<float>(); //if either of them is 0 they don't overwrite things
                _brakeHandle.Get(_eventRegistryService).Value = _controls.CarBase.Brake.ReadValue<float>();
                _steerHandle.Get(_eventRegistryService).Value = _controls.CarBase.Steering.ReadValue<float>();
                _handbrakeHandle.Get(_eventRegistryService).Value = _controls.CarBase.Handbrake.ReadValue<float>();
                _clutchHandle.Get(_eventRegistryService).Value = _controls.CarBase.Clutch.ReadValue<float>();
            }
            else if (_controls.UI.enabled)
            {
                // _throttleHandle.Get(_eventRegistryService).Value = UiActions.Throttle.ReadValue<float>();
            }

            if (_controls.Person.enabled)
            {
                _movePersonHandle.Get(_eventRegistryService).Value = _controls.Person.Move.ReadValue<Vector2>().normalized;
            }

            if (_controls.MapUi.enabled)
            {
                _mapMoveHandle.Get(_eventRegistryService).Value = _controls.MapUi.Move.ReadValue<Vector2>().normalized;
            }
        }

        private void Update()
        {
            if (!_isInputActive && (Time.frameCount - _lastGroupSwitchFrame) <= 1f)
                return;

            Vector2 rawLook = Vector2.zero;
            InputAction lookAction = null;

            if (_controls.Camera.enabled && _clutchHandle.Get(_eventRegistryService).Value < 0.5f)
            {
                rawLook = _controls.Camera.Look.ReadValue<Vector2>();
                lookAction = _controls.Camera.Look;
                _zoomHandle.Get(_eventRegistryService).Value = _controls.Camera.Zoom.ReadValue<float>();
            }
            else if (_controls.CameraGarage.enabled)
            {
                rawLook = _controls.CameraGarage.Look.ReadValue<Vector2>();
                lookAction = _controls.CameraGarage.Look;
                _zoomHandle.Get(_eventRegistryService).Value = _controls.CameraGarage.Zoom.ReadValue<float>();

            }
            else if (_controls.DroneCamera.enabled)
            {
                rawLook = _controls.DroneCamera.Look.ReadValue<Vector2>().normalized;
                lookAction = _controls.DroneCamera.Look;
                _zoomHandle.Get(_eventRegistryService).Value = _controls.DroneCamera.Zoom.ReadValue<float>();
            }
            else if (_controls.PaintBase.enabled)
            {
                _zoomHandle.Get(_eventRegistryService).Value = _controls.PaintBase.Zoom.ReadValue<float>();
            }
            if (lookAction?.activeControl?.device is Gamepad)
            {
                rawLook *= _gamepadLookMultiplier;
            }

            _lookHandle.Get(_eventRegistryService).Value = rawLook;

            if(_controls.CircleMenu.enabled)
                _circleMenuMoveHandle.Get(_eventRegistryService).Value = _controls.CircleMenu.CircleMenuMove.ReadValue<Vector2>();

            if(_controls.Shifter.enabled)
                _shifterMoveHandle.Get(_eventRegistryService).Value = shifterTransformScale * _controls.Shifter.ShifterMove.ReadValue<Vector2>().normalized;

            if (_controls.PaintColor.enabled)
            {
                _opacityChangeHandle.Get(_eventRegistryService).Value = _controls.PaintColor.AlphaValue.ReadValue<float>();
                _colorChangeHandle.Get(_eventRegistryService).Value = _controls.PaintColor.ColorMove.ReadValue<Vector2>().normalized;
            }

            if (_controls.PaintSize.enabled)
            {
                _sizeChangeHandle.Get(_eventRegistryService).Value = _controls.PaintSize.SizeMove.ReadValue<Vector2>().normalized;
                _turnLeftRightHandle.Get(_eventRegistryService).Value =_controls.PaintSize.Turn.ReadValue<float>();
            }

            if (_controls.PaintSelectionMade.enabled)
            {
                _positionChangeHandle.Get(_eventRegistryService).Value = _controls.PaintSelectionMade.Move.ReadValue<Vector2>().normalized;
            }
        }
    }
}
