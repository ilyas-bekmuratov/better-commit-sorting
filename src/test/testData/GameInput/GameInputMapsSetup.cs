namespace _Car_Parking.Scripts.GameInput
{
    public partial class GameInputManager
    {
        public void Setup(out GameInputController.SteeringWheelActions steeringWheelActions)
        {
            if (_initialized)
            {
                steeringWheelActions = _controls.SteeringWheel;
                return;
            }

            _controls = new GameInputController();
            steeringWheelActions = _controls.SteeringWheel;

            _eventRegistryService.Provide(mainRegistry);

            _inputMapSetEvent = _eventRegistryService.GetInputMapSetEvent<InputMapSetEvent>();
            _inputMapSetEvent.Initialize();
            _inputMapSetEvent.Subscribe(TrySetInput);

            _setActionsActiveHandle.Get(_eventRegistryService)?.Subscribe(SetInputActive);
            _isInputActive = true;
            _controls.UiPointer.Enable();
            SubscribeToActions();

            _initialized = true;
        }

        private void SetInputActive(bool enable)
        {
            _isInputActive = enable;
        }

        private void OnDestroy()
        {
            if (_inputMapSetEvent != null)
            {
                _inputMapSetEvent.Unsubscribe(TrySetInput);
            }

            if (_controls != null)
            {
                UnsubscribeToActions();
                _controls.Disable(); 
                _controls.Dispose();
            }
        }
    }
}
