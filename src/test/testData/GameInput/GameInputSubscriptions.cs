namespace _Car_Parking.Scripts.GameInput
{
    public partial class GameInputManager
    {
        public void SubscribeToActions()
        {
            _controls.UIAcceptCancel.Cancel.performed += OnCancel;

            _controls.Camera.Look.started += OnLook;
            _controls.Camera.Look.canceled += OnLook;
            _controls.CameraGarage.Look.started += OnLook;
            _controls.CameraGarage.Look.canceled += OnLook;
            _controls.DroneCamera.Look.started += OnLook;
            _controls.DroneCamera.Look.canceled += OnLook;

            _controls.ChangeCamera.ChangeCamera.performed += OnChangeCamera;

            _controls.Headlights.Headlights.performed += HeadlightsOnPerformed;

            _controls.CarBase.Horn.started += OnHorn;
            _controls.CarBase.Horn.canceled += OnHorn;

            _controls.CarBase.StartEngine.performed += OnStartEngine;
            _controls.CarBase.StartEngine.canceled += OnStartEngine;

            _controls.Door.OpenDoor.started += OnDoorStart;
            _controls.Door.OpenDoor.canceled += OnDoorCanceled;

            _controls.Shifter.ShiftDown.started += ShiftDown;
            _controls.Shifter.ShiftUp.started += ShiftUp;
            _controls.Shifter.ShiftM.started += ShiftM;
            _controls.Shifter.ShiftPRND.started += ShiftA;
            _controls.Shifter.ToggleMA.performed += ToggleShiftMA;

            _controls.CarBase.SetGearVisible.started += CloseCircleMenu;
            _controls.CarBase.SetGearVisible.performed += SetGearsVisible;
            _controls.CarBase.SetGearVisible.canceled += SetGearsInvisible;

            _controls.OpenCircleMenu.OpenCircleMenu.performed += ToggleCircleMenu;
            _controls.OpenCircleMenu.OpenCircleMenu.performed += SetGearsInvisible;

            _controls.CircleMenu.CircleMenuClose.performed += ToggleCircleMenu;
            _controls.CircleMenu.CircleMenuScroll.performed += OnCircleMenuScroll;

            _controls.ShowMap.Map.performed += OnToggleMapOn;
            _controls.Menu.Menu.performed += OpenMenu;
            _controls.PhoneVoice.Phone.performed += OpenPhone;
            _controls.PhoneVoice.VoiceChat.started += OnVoice;
            _controls.PhoneVoice.VoiceChat.canceled += OnVoiceCanceled;

            _controls.Person.Animations.performed += OpenAnimations;

            _controls.MapUi.Expand.performed += ExpandMapOptions;
            _controls.MapUi.HideShow.performed += HideShowOnMap;

            _controls.MapUi.CloseMap.performed += OnToggleMapOff;

            _controls.UiNextPrevious.Next.performed += Next;
            _controls.UiNextPrevious.Previous.performed += Previous;

            _controls.PaintBase.BlockCam.performed += BlockCamera;
            _controls.PaintBase.DeleteAll.performed += DeleteAll;
            _controls.PaintBase.DeleteAll.canceled += DeleteSelected;
            _controls.PaintBase.FavoritesToggle.performed += FavoritesToggle;
            _controls.PaintBase.ToggleUi.performed += ToggleUi;

            _controls.PaintColor.ColorCode.performed += SetColorCode;
            _controls.PaintColor.ColorToggle.performed += ColorToggle;
            _controls.PaintColor.TintToggle.performed += TintToggle;
            _controls.PaintSize.SetSize.performed += SetSize;
            _controls.PaintSize.Mirror.performed += Mirror;

            _controls.PaintCopy.Copy.performed += Copy;
            _controls.PaintFavorite.Favorite.performed += Favorite;
            _controls.PaintMultiSelect.MultiSelect.performed += MultiSelect;

            _controls.PaintApplyCancel.ApplyChange.performed += ApplyChange;
            _controls.PaintApplyCancel.CancelChange.performed += CancelChange;

            _controls.PaintSelectNew.SelectNew.performed += SelectNew;
            _controls.PaintSelectSet.SelectSetVinyl.performed += SelectSetVinyl;
            _controls.PaintSelectionMade.SetSize.performed += ToggleToSize;
            _controls.PaintSelectionMade.SetColor.performed += ToggleToColor;
            _controls.PaintSelectionMade.LayerUpDown.performed += LayerUpDown;
        }

        public void UnsubscribeToActions()
        {
            _controls.UIAcceptCancel.Cancel.performed -= OnCancel;

            _controls.Camera.Look.started -= OnLook;
            _controls.Camera.Look.canceled -= OnLook;
            _controls.CameraGarage.Look.started -= OnLook;
            _controls.CameraGarage.Look.canceled -= OnLook;
            _controls.DroneCamera.Look.started -= OnLook;
            _controls.DroneCamera.Look.canceled -= OnLook;

            _controls.ChangeCamera.ChangeCamera.performed -= OnChangeCamera;

            _controls.Headlights.Headlights.performed -= HeadlightsOnPerformed;

            _controls.CarBase.Horn.started -= OnHorn;
            _controls.CarBase.Horn.canceled -= OnHorn;

            _controls.CarBase.StartEngine.performed -= OnStartEngine;
            _controls.CarBase.StartEngine.canceled -= OnStartEngine;

            _controls.Door.OpenDoor.started -= OnDoorStart;
            _controls.Door.OpenDoor.canceled -= OnDoorCanceled;

            _controls.Shifter.ShiftDown.started -= ShiftDown;
            _controls.Shifter.ShiftUp.started -= ShiftUp;
            _controls.Shifter.ShiftM.started -= ShiftM;
            _controls.Shifter.ShiftPRND.started -= ShiftA;
            _controls.Shifter.ToggleMA.performed -= ToggleShiftMA;

            _controls.CarBase.SetGearVisible.started -= CloseCircleMenu; //pressing clutch hides circle menu
            _controls.CarBase.SetGearVisible.performed -= SetGearsVisible; // performing this action shows gears
            _controls.CarBase.SetGearVisible.canceled -= SetGearsInvisible; //releasing it hides the gears

            _controls.OpenCircleMenu.OpenCircleMenu.performed -= ToggleCircleMenu;
            _controls.OpenCircleMenu.OpenCircleMenu.performed -= SetGearsInvisible;

            _controls.CircleMenu.CircleMenuClose.performed -= ToggleCircleMenu;
            _controls.CircleMenu.CircleMenuScroll.performed -= OnCircleMenuScroll;

            _controls.ShowMap.Map.performed -= OnToggleMapOn;
            _controls.Menu.Menu.performed -= OpenMenu;
            _controls.PhoneVoice.Phone.performed -= OpenPhone;
            _controls.PhoneVoice.VoiceChat.started -= OnVoice;
            _controls.PhoneVoice.VoiceChat.canceled -= OnVoiceCanceled;

            _controls.Person.Animations.performed -= OpenAnimations;

            _controls.MapUi.Expand.performed -= ExpandMapOptions;
            _controls.MapUi.HideShow.performed -= HideShowOnMap;

            _controls.MapUi.CloseMap.performed -= OnToggleMapOff;
            
            _controls.PaintBase.BlockCam.performed -= BlockCamera;
            _controls.PaintBase.DeleteAll.performed -= DeleteAll;
            _controls.PaintBase.DeleteAll.canceled -= DeleteSelected;
            _controls.PaintBase.FavoritesToggle.performed -= FavoritesToggle;
            _controls.PaintBase.ToggleUi.performed -= ToggleUi;

            _controls.PaintColor.ColorCode.performed -= SetColorCode;
            _controls.PaintColor.ColorToggle.performed -= ColorToggle;
            _controls.PaintColor.TintToggle.performed -= TintToggle;
            _controls.PaintSize.SetSize.performed -= SetSize;
            _controls.PaintSize.Mirror.performed -= Mirror;

            _controls.PaintCopy.Copy.performed -= Copy;
            _controls.PaintFavorite.Favorite.performed -= Favorite;
            _controls.PaintMultiSelect.MultiSelect.performed -= MultiSelect;

            _controls.PaintApplyCancel.ApplyChange.performed -= ApplyChange;
            _controls.PaintApplyCancel.CancelChange.performed -= CancelChange;

            _controls.PaintSelectNew.SelectNew.performed -= SelectNew;
            _controls.PaintSelectSet.SelectSetVinyl.performed -= SelectSetVinyl;
            _controls.PaintSelectionMade.SetSize.performed -= ToggleToSize;
            _controls.PaintSelectionMade.SetColor.performed -= ToggleToColor;
            _controls.PaintSelectionMade.LayerUpDown.performed -= LayerUpDown;
        }
    }
}
