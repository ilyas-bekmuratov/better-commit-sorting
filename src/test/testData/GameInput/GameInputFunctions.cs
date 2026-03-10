using _Car_Parking.Scripts.Other.Helpers;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Car_Parking.Scripts.GameInput
{
    public partial class GameInputManager
    {
#region Car
        private void HeadlightsOnPerformed(InputAction.CallbackContext ctx)
        {
            if(!_isInputActive)
                return;
            _onHeadlightsHandle.Get(_eventRegistryService)?.Raise();
        }

        private void OnStartEngine(InputAction.CallbackContext ctx)
        {
            if(!_isInputActive)
                return;
            bool isCancelled = ctx.canceled;
            _onStartEngineHandle.Get(_eventRegistryService)?.Raise(isCancelled);
        }

        private void OnHorn(InputAction.CallbackContext ctx)
        {
            if(!_isInputActive)
                return;
            bool isStarted = ctx.started;
            _onHornHandle.Get(_eventRegistryService)?.Raise(isStarted);
        }

        private void OnThrottle(InputAction.CallbackContext ctx)
        {
            bool isStarted = ctx.started;
            _onThrottleHandle.Get(_eventRegistryService)?.Raise(isStarted);
        }

        private void ShiftUp(InputAction.CallbackContext ctx)
        {
            if (!_isInputActive)
                return;
            _shifterUpDownHandle.Get(_eventRegistryService)?.Raise(true);
        }

        private void ShiftDown(InputAction.CallbackContext ctx)
        {
            if (!_isInputActive)
                return;
            _shifterUpDownHandle.Get(_eventRegistryService)?.Raise(false);
        }

        private void ShiftM(InputAction.CallbackContext ctx)
        {
            if (!_isInputActive)
                return;
            _shifterMAHandle.Get(_eventRegistryService)?.Raise(true);
        }

        private void ShiftA(InputAction.CallbackContext ctx)
        {
            if (!_isInputActive)
                return;
            _shifterMAHandle.Get(_eventRegistryService)?.Raise(false);
        }

        private void ToggleShiftMA(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount || !_isInputActive)
                return;
            _lastGroupSwitchFrame = Time.frameCount;
            _shiftToggleMAHandle.Get(_eventRegistryService)?.Raise();
        }

        private void SetGearsVisible(InputAction.CallbackContext ctx)
        {
            if (!_isInputActive)
                return;

            if (_inputMapSetEvent.CurrentInputGroup == InputGroupsEnum.CarCircleMenu)
                return;

            _shifterMoveStartedHandle.Get(_eventRegistryService)?.Raise(true);
            ResetLookNextFrame().Forget();
        }

        private void SetGearsInvisible(InputAction.CallbackContext ctx)
        {
            if (!_isInputActive)
                return;

            _shifterMoveStartedHandle.Get(_eventRegistryService)?.Raise(false);
            ResetLookNextFrame().Forget();
        }
#endregion

#region Camera
        private async UniTask ResetLookNextFrame()
        {
            await UniTask.Yield();
            _lookHandle.Get(_eventRegistryService)?.Reset();
            _zoomHandle.Get(_eventRegistryService)?.Reset();
            _onLookHandle.Get(_eventRegistryService)?.Raise(false);
        }

        private void OnChangeCamera(InputAction.CallbackContext ctx)
        {
            if (!_isInputActive)
                return;
            _changeCameraHandle.Get(_eventRegistryService)?.Raise();
        }

        private void OnLook(InputAction.CallbackContext ctx)
        {
            bool isStarted = ctx.started;
            _onLookHandle.Get(_eventRegistryService)?.Raise(isStarted);
        }
#endregion

#region Person
        private void OnDoorCanceled(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount)
                return;

            _doorStartCancelHandle.Get(_eventRegistryService)?.Raise(false);
        }

        private void OnDoorStart(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            V.OnOutOfCarChanged -= OnOutOfCarChanged;
            V.OnOutOfCarChanged += OnOutOfCarChanged;

            _doorStartCancelHandle.Get(_eventRegistryService)?.Raise(true);
            if (!_isInputActive && Time.frameCount - _lastGroupSwitchFrame <= 1)
            {
                return;
            }

            var _clutch = _clutchHandle.Get(_eventRegistryService);

            float previousValue = 0f;
            if (_clutch)
            {
                previousValue = _clutch.Value;
            }

            if(previousValue > 0f)
                _closeCircleMenuHandle.Get(_eventRegistryService)?.Raise();
            WDebug.Log("Вызывается CloseCircleMenu");
        }

        private void OnOutOfCarChanged(bool isOutOfCar)
        {
            if (isOutOfCar)
            {
                _inputMapSetEvent.TrySetGroup(InputGroupsEnum.Person);
                return;
            }

            InputGroupsEnum group;

            if (FlagsStorage.IsPassenger)
            {
                group = InputGroupsEnum.CarPassenger;
            }
            else
            {
                // Driver entering — pick group based on active game mode
                group = FlagsStorage.RacingGameMode
                    ? FlagsStorage.SingeDragRacingMode
                        ? InputGroupsEnum.CarDrag
                        : InputGroupsEnum.CarRacing
                    : InputGroupsEnum.CarFree;
            }

            _inputMapSetEvent.TrySetGroup(group);
        }
#endregion

#region Menus and Other actions
        private void OpenAnimations(InputAction.CallbackContext ctx)
        {
        }

        private void OnVoiceCanceled(InputAction.CallbackContext ctx)
        {
        }

        private void OnVoice(InputAction.CallbackContext ctx)
        {
        }

        private void OpenMenu(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _onMenuHandle.Get(_eventRegistryService)?.Raise();
        }

        private void OpenPhone(InputAction.CallbackContext ctx)
        {
            if (!_isInputActive)
                return;
            _openPhoneHandle.Get(_eventRegistryService)?.Raise();
        }

        private void OnConfirm(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount)
                return;

            if (_chatInputField
                && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != _chatInputField.gameObject)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(_chatInputField.gameObject); // the rest of the logic is handled in the steamdeckkeyboard
            }
            
            //accept invites
        }

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            GameObject selectedObj = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            if (selectedObj != null && selectedObj.TryGetComponent<TMP_InputField>(out _))
                return;

            _cancelHandle.Get(_eventRegistryService)?.Raise();
            _closeCircleMenuHandle.Get(_eventRegistryService)?.Raise();
        }
#endregion

#region Drone and Photomoode
        private void OpenPhotoMode(InputAction.CallbackContext ctx)
        {
            if (!_isInputActive)
                return;
            // CircuitRacingUIControl.CircuitPlayWindow.OpenPhotoMode();
        }
#endregion

#region Map
        private void OnToggleMapOn(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _toggleMapHandle.Get(_eventRegistryService)?.Raise(true);
            // FreeDriveProvider.FreeDriveControl.ShowBigMap();
        }
        private void OnToggleMapOff(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _toggleMapHandle.Get(_eventRegistryService)?.Raise(false);
            // FreeDriveProvider.FreeDriveControl.HideBigMap();
        }
        private void ExpandMapOptions(InputAction.CallbackContext ctx)
        {
            //throw new NotImplementedException();
        }

        private void OnNavigateMapOptions(InputAction.CallbackContext ctx)
        {
            //throw new NotImplementedException();
        }

        private void HideShowOnMap(InputAction.CallbackContext ctx)
        {
            //throw new NotImplementedException();
        }
#endregion

#region Paint
        private void Next(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _onNextPreviousHandle.Get(_eventRegistryService)?.Raise(true);
        }
        private void Previous(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _onNextPreviousHandle.Get(_eventRegistryService)?.Raise(false);
        }
        private void BlockCamera(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _blockCameraHandle.Get(_eventRegistryService)?.Raise();
        }
        private void DeleteAll(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _deleteAllHandle.Get(_eventRegistryService)?.Raise();
        }
        private void DeleteSelected(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _deleteSelectedHandle.Get(_eventRegistryService)?.Raise();
        }
        private void FavoritesToggle(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _toggleFavoritesHandle.Get(_eventRegistryService)?.Raise();
        }
        private void ToggleUi(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _toggleUiHandle.Get(_eventRegistryService)?.Raise();
        }
        private void SetColorCode(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _setColorHandle.Get(_eventRegistryService)?.Raise();
        }
        private void ColorToggle(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _onStartColorMoveHandle.Get(_eventRegistryService)?.Raise();
        }
        private void TintToggle(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _onStartTintMoveHandle.Get(_eventRegistryService)?.Raise();
        }
        private void SetSize(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _setSizeHandle.Get(_eventRegistryService)?.Raise();
        }
        private void Mirror(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _mirrorHandle.Get(_eventRegistryService)?.Raise();
        }
        private void Copy(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _copyHandle.Get(_eventRegistryService)?.Raise();
        }
        private void Favorite(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _addFavoriteHandle.Get(_eventRegistryService)?.Raise();
        }
        private void MultiSelect(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _toggleMultiSelectHandle.Get(_eventRegistryService)?.Raise();
        }
        private void ApplyChange(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _onApplyChangesHandle.Get(_eventRegistryService)?.Raise();
        }
        private void CancelChange(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _onCancelChangesHandle.Get(_eventRegistryService)?.Raise();
        }
        private void SelectNew(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            bool navigateRight = ctx.ReadValue<float>() > 0;

            _onNavigateNewHandle.Get(_eventRegistryService)?.Raise(navigateRight);
        }
        private void SelectSetVinyl(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            bool navigateUp = ctx.ReadValue<float>() > 0;

            _onNavigateExistingHandle.Get(_eventRegistryService)?.Raise(navigateUp);
        }
        private void ToggleToSize(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _toggleToSizeHandle.Get(_eventRegistryService)?.Raise();
        }
        private void ToggleToColor(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            _toggleToColorHandle.Get(_eventRegistryService)?.Raise();
        }
        private void LayerUpDown(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
                return;

            bool isUp = ctx.ReadValue<float>() > 0;
            _onLayerUpDownHandle.Get(_eventRegistryService)?.Raise(isUp);
        }
#endregion


#region  CircleMenu
        private void ToggleCircleMenu(InputAction.CallbackContext ctx)
        {
            if (!_isInputActive)
            {
                return;
            }

            bool isGamepad = ctx.control.device is Gamepad;

            if (Time.frameCount - _lastGroupSwitchFrame <= 1)
            {
                return;
            }

            _circleMenuHandle.Get(_eventRegistryService)?.Raise(isGamepad);
            
            ResetLookNextFrame().Forget();
        }

        private void OnCircleMenuScroll(InputAction.CallbackContext ctx)
        {
            if (_lastGroupSwitchFrame == Time.frameCount && !_isInputActive)
            {
                return;
            }

            bool isForward = ctx.ReadValue<float>() > 0;
            _circleMenuScrollHandle.Get(_eventRegistryService)?.Raise(isForward);
        }

        private void CloseCircleMenu(InputAction.CallbackContext ctx)
        {
            if (Time.frameCount - _lastGroupSwitchFrame <= 1)
            {
                return;
            }

            if (ctx.ReadValue<float>() > 0)
            {
                _closeCircleMenuHandle.Get(_eventRegistryService)?.Raise();
            }
        }
#endregion
    }
}
