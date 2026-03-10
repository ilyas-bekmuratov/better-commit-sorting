using System;
using System.Collections.Generic;
using System.Linq;
using _Car_Parking;
using _Car_Parking.Scripts.Car;
using _Car_Parking.Scripts.Car.CarSetter;
using _Car_Parking.Scripts.Car.Configs;
using _Car_Parking.Scripts.DataStorageService;
using _Car_Parking.Scripts.Drag;
using _Car_Parking.Scripts.EngineTuning;
using _Car_Parking.Scripts.EngineTuning.DataTypes;
using _Car_Parking.Scripts.EngineTuning.TuningTimer;
using _Car_Parking.Scripts.GameInput;
using _Car_Parking.Scripts.Infrastructure;
using _Car_Parking.Scripts.Interior;
using _Car_Parking.Scripts.Localization;
using _Car_Parking.Scripts.MenuUI;
using _Car_Parking.Scripts.MenuUI.VisualTuning;
using _Car_Parking.Scripts.Other.Helpers;
using _Car_Parking.Scripts.SOAP;
using _Car_Parking.Scripts.VinylsAi;
using _Car_Parking.Scripts.VisualTuning.Wheels;
using _Car_Parking.Scripts.VisualTuningFavourite;
using Addler.Runtime.Core.LifetimeBinding;
using CPM.Database;
using CPM.Database.Constructors;
using CPM.Scripts.Garage;
using CPM.Scripts.VinylDrawer;
using CPMEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Flags;
using Racing.CameraManagement;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;


public class VisualTuningWindow : WindowScript, IBackableWindow
{
    private const float OperationDelay = 0.01f;
    private const string WheelDiskTexturesConfigPath = "WheelDiskTexturesConfig";
    
    public event Action OnTuningValuesChanged;
    
    public CanvasGroup visualTuningCanvasGroup;
    public VisualTuningElementSpawner VisualTuningElementSpawner;
    public ToggleGroup toggleGroup;
    public UiScrollRectOcclusiion UiScrollRectOcclusiion;
    public GameObject spawner;
    public Transform content;
    public GameObject controlButtonsPanel;
    
    [HideInInspector] public MonoBehaviour spawnerScript;
    
    [SerializeField] private VisualTuningToggle[] _visualTuningToggles;
    [SerializeField] private Transform ScrollRectPlace;
    [SerializeField] private ScrollRect ScrollRectPrefab;
    [SerializeField] VisualItemElement VisualItemElementPrefab;
    [SerializeField] private DownloadAssetButton DownloadAssetButtonPrefab;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button discardButton;

    [SerializeField] private GameObject SideMunuParent;
    [SerializeField] private Button SetDefaultButton;
    [SerializeField] private TextMeshProUGUI axleSideText;
    [SerializeField] private Button leftArrow;
    [SerializeField] private Button rightArrow;
    [SerializeField] private GameObject LeftRightArrowsParent;
    [SerializeField] private GameObject BrakesControlPanel;
    [SerializeField] private WheelUIChangerPanel WheelUIChangerPanel;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject colorPanel;
    [SerializeField] private Button GoToVynils;
    [SerializeField] private VisualColorPanel viusalColorPanel;
    [SerializeField] private VisualTuningBodyKitsPanel visualTuningBodyKitsPanel;
    [SerializeField] private VisualPolicePanel VisualPolicePanel;
    [SerializeField] private CanvasGroup CanvasGroup;
    [SerializeField] private BrakesViusalTuningPanel BrakesViusalTuningPanel;
    [SerializeField] private CalipersTuningPanel CalipersVisualTuningPanel;
    [SerializeField] private RectTransform LayoutGroup;
    [SerializeField] private GameObject LeftPanel;
    [SerializeField] private VinylsEditor VinylsPanel;
    [SerializeField] private FavouriteWheelsController FavouriteWheelsController;
    [SerializeField] private Texture InteriorIcon;
    
    [SerializeField] private Sprite coinIcon;
    [SerializeField] private Sprite moneyIcon;
    [SerializeField] private Button rideCarButton;
    
    private ScrollRect _scrollRect;
    private FlagsDataSO _flagsDataSo;
    private VisualTuningTypes _currentVisualTuningTypes;
    private VisualItemElement _selectedVisualItemElement;
    private ICarInfoService _carInfoService;
    private CarInfo _carInfo;
    private VisualTuningPricesService _visualTuningPricesService;
    private ExteriorTuning _exteriorTuning;
    private BrakeTuningConfig _brakeTuningConfig;
    private MenuControl _menuControl;
    private IPaintDataService _paintDataService;
    private PlayerVinylCensorChecker _playerVinylCensorChecker;
    private GarageController _garageController;
    private bool _mustSpawnFavourite;
    private bool _operationSkip;
    private bool _isDestroyed;
    private string _lastScope;

    private List<VisualItemElement> _cachedVisualTuningElements;
    private VehicleData _vehicleData;
    private Powertrain _powertrain;
    private Tween _tween;
    private IVisualTuningViewModelComplexOne _visualTuningViewModelComplexOne;
    private IVisualTuningViewModelComplexParams _visualTuningViewModelComplexParams;
    private IVisualTuningUpdateAction _visualTuningUpdateAction;
    private IWheelsDataService _wheelsDataService;
    private GraphicRaycaster _graphicRaycaster;
    private ITimerTuningService _timerTuningService;
    private CameraManager _cameraManager;
    private VisualItemElement _lastVisualtuningToggle;
    private WheelsDiskTuningConfig _wheelsDiskTuningConfig;
    private IPrefabFactory _prefabFactory;
    private WheelDiskTexturesConfig _wheelDiskTexturesConfig;
    private EventRegistryService _eventRegistryService;
    private InputMapSetEvent _inputMapSetEvent;
    private RegistryHandle<BoolEvent> _onNextPrevious = new RegistryHandle<BoolEvent>(EventRegistryIDs.OnNextPrevious);
    private RegistryHandle<ScriptableEvent> _toggleUiHandle = new(EventRegistryIDs.ToggleUi);
    private List<VisualTuningToggle> _actualToggles = new()
    {
        // VisualTuningTypes.BodyKits,
        // VisualTuningTypes.Colors,
        // VisualTuningTypes.Wheels,
        // VisualTuningTypes.Interiors,
        // VisualTuningTypes.Flags,
        // VisualTuningTypes.Brakes,
        // VisualTuningTypes.Police,
        // VisualTuningTypes.Vinyls,
    };

    private InteriorsVisualTuningPart _interiorsTuning;
    
    public PoliceVisualScope PoliceVisualScope
    {
        get
        {
            VisualTuningScope visualTuningScope = _visualTuningViewModelComplexOne.GetScope(VisualTuningTypes.Police);
            if (visualTuningScope == null)
            {
                return null;
            }
            return visualTuningScope as PoliceVisualScope;
        }
    }
    
    public BodyKitsVisualScope BodyKitsVisualScope
    {
        get
        {
            VisualTuningScope visualTuningScope = _visualTuningViewModelComplexOne.GetScope(VisualTuningTypes.BodyKits);
            return visualTuningScope as BodyKitsVisualScope;
        }
    }
    
    public VisualTuningTypes VisualTuningTypes => _currentVisualTuningTypes;
    private VisualTuningScope CurrentVisualScope => _visualTuningViewModelComplexOne.GetScope(VisualTuningTypes);
        
    [Inject][UnityEngine.Scripting.Preserve]
    public void Construct(WheelsDiskTuningConfig wheelsDiskTuningConfig,
        FlagsDataSO flagsDataSo,
        IPrefabFactory prefabFactory,
        IVisualTuningViewModelComplexOne visualTuningViewModelComplexOne,
        IVisualTuningViewModelComplexParams visualTuningViewModelComplexParams,
        IVisualTuningUpdateAction visualTuningUpdateAction,
        ManagerUI managerUI, ICarInfoService carInfoService,
        VisualTuningPricesService visualTuningPricesService, ExteriorTuning exteriorTuning,
        MenuControl menuControl, BrakeTuningConfig brakeTuningConfig, IPaintDataService paintDataService,
        IWheelsDataService wheelsDataService, ITimerTuningService timerTuningService, CameraManager cameraManager,
        EventRegistryService eventRegistryService,
        PlayerVinylCensorChecker playerVinylCensorChecker, GarageController garageController)
    {
        _cameraManager = cameraManager;
        _timerTuningService = timerTuningService;
        _wheelsDataService = wheelsDataService;
        _paintDataService = paintDataService;
        _menuControl = menuControl;
        _exteriorTuning = exteriorTuning;
        _visualTuningPricesService = visualTuningPricesService;
        _carInfoService = carInfoService;
        _visualTuningViewModelComplexOne = visualTuningViewModelComplexOne;
        _visualTuningViewModelComplexParams = visualTuningViewModelComplexParams;
        _visualTuningUpdateAction = visualTuningUpdateAction;
        _flagsDataSo = flagsDataSo;
        _wheelsDiskTuningConfig = wheelsDiskTuningConfig;
        _prefabFactory = prefabFactory;
        _managerUI = managerUI;
        _brakeTuningConfig = brakeTuningConfig;
        _eventRegistryService = eventRegistryService;
        _inputMapSetEvent = _eventRegistryService.GetInputMapSetEvent<InputMapSetEvent>();
        _playerVinylCensorChecker = playerVinylCensorChecker;
        _garageController = garageController;
    }

#if ALTER_INPUT
    private void OnEnable()
    {
        _onNextPrevious.Get(_eventRegistryService).Subscribe(NextPreviousTuning);
        _toggleUiHandle.Get(_eventRegistryService).Subscribe(OnToggleUi);
    }

    private void OnDisable()
    {
        _onNextPrevious.Get(_eventRegistryService).Unsubscribe(NextPreviousTuning);
        _toggleUiHandle.Get(_eventRegistryService).Unsubscribe(OnToggleUi);
    }

    private void NextPreviousTuning(bool isNext)
    {
        if (_actualToggles.Count == 0)
            return;

        int index = -1;
        int current;
        for (current = 0; current < _actualToggles.Count; current++)
        {
            if(_actualToggles[current].togglePanelState == _currentVisualTuningTypes)
            {
                index = isNext 
                    ? (current + 1) % _actualToggles.Count
                    : (current - 1 + _actualToggles.Count) % _actualToggles.Count;
                break;
            }
        }

        if (index == -1)
        {
            WDebug.Log("[PaintInput] _currentVisualTuningTypes not found in actual toggles");
            return;
        }

        _actualToggles[current].SetIsOnWithoutNotify(false);
        _actualToggles[index].SetIsOnWithoutNotify(true);

        ChangePanel(_actualToggles[index].togglePanelState);
    }

    private void OnToggleUi()
    {
        bool hide = visualTuningCanvasGroup.alpha > 0f;
        visualTuningCanvasGroup.alpha = hide ? 0f : 1f;
        visualTuningCanvasGroup.interactable = !hide;
        visualTuningCanvasGroup.blocksRaycasts = !hide;
    }

#endif
    protected override void Start()
    {
        _cameraManager.garageCam.ResetCameraForVisualTuning();
        if (V.Car.TryGetComponent(out _vehicleData))
        {
            _vehicleData.racingState = RacingState.OnVisualTuning;
            _garageController.UpdateCarPhysics(_vehicleData, true);
        }
       
        if (V.Car.TryGetComponent(out _powertrain))
        {
            _powertrain.SetAlignCar(true);
            _powertrain.ResetTransmission(false);
            
        }
        _graphicRaycaster = _managerUI.GetComponent<GraphicRaycaster>();
        InitBackButton();
        _menuControl.tempTarget.position = V.Car.transform.position;
        WheelUIChangerPanel.currentWheelAxleType = WheelTuningAxleType.Both;
        AddButtonsListeners();
        HideUnusedTuningToggles();
        SetFirstTuningType();
        
        _visualTuningUpdateAction.OnValuesChanged += OnVisualTuningValuesChanged;
        FavouriteWheelsController.OnFavouriteWheelsShow += ShowFavouriteWheels;
        FavouriteWheelsController.OnFavouriteWheelsUpdated += UpdateFavouriteWheels;
        FlagsStorage.OnVisualTuning = true;
        FlagsStorage.IsCarInService = true;
        LeftPanel.SetActive(true);
        VinylsPanel.SetActive(false);
        LoadWheelDiskTexturesConfig().Forget();
        _interiorsTuning = _prefabFactory.Create<InteriorsVisualTuningPart>();
        _interiorsTuning.Init(this, _vehicleData, VisualItemElementPrefab, InteriorIcon, DownloadAssetButtonPrefab);
    }

    protected override void OnDestroy()
    {
        _visualTuningUpdateAction.OnValuesChanged -= OnVisualTuningValuesChanged;
        FavouriteWheelsController.OnFavouriteWheelsShow -= ShowFavouriteWheels;
        FavouriteWheelsController.OnFavouriteWheelsUpdated -= UpdateFavouriteWheels;
        FlagsStorage.OnVisualTuning = false;
        _isDestroyed = true;
        _cameraManager.garageCam.ResetCamToDefault();
    }

    private void HideUnusedTuningToggles()
    {
        _actualToggles.Clear();
        foreach (var tuningToggle in _visualTuningToggles)
        {
            if (CarIdStorage.IsCarWithoutBodyWheelTuning(_vehicleData.CarId) && 
                VisualTuningExclude.IsF1ExcludedType(tuningToggle.togglePanelState) ||
                _vehicleData.HasBuiltInInterior && tuningToggle.togglePanelState == 
                VisualTuningTypes.Interiors)
            {
                tuningToggle.SetActive(false);
                var navigation = tuningToggle.navigation;
                navigation.mode = Navigation.Mode.None;
                tuningToggle.navigation = navigation;
            }
            else
            {
                _actualToggles.Add(tuningToggle);
            }
        }
    }

    private async UniTask LoadWheelDiskTexturesConfig()
    {
        AsyncOperationHandle<WheelDiskTexturesConfig> asyncOperationHandle =
            Addressables.LoadAssetAsync<WheelDiskTexturesConfig>(WheelDiskTexturesConfigPath).BindTo(gameObject);

        await asyncOperationHandle.Task;

        if (_isDestroyed)
            return;

        _wheelDiskTexturesConfig = asyncOperationHandle.Result;
    }

    private void CreateContentPanel(float pivot, bool offsetByY = false)
    {
        DestroyContentPanel();
        _scrollRect = _prefabFactory.Instantiate(ScrollRectPrefab, ScrollRectPlace);
        _scrollRect.content.pivot = new Vector2(pivot, _scrollRect.content.pivot.y);
        content = _scrollRect.content;
        if (offsetByY)
        {
            var rect = content.GetComponent<RectTransform>();
            var pos = rect.anchoredPosition;
            rect.anchoredPosition = new Vector2(pos.x, pos.y + 10);
        }
        UiScrollRectOcclusiion = _scrollRect.gameObject.GetComponent<UiScrollRectOcclusiion>();
        toggleGroup = content.GetComponent<ToggleGroup>();
    }

    public void DestroyContentPanel()
    {
        if(_scrollRect)
            Destroy(_scrollRect.gameObject);
    }

    public void SetFirstTuningType()
    {
        foreach (var tuningToggle in _visualTuningToggles)
        {
            if (tuningToggle.gameObject.activeSelf)
            {
                tuningToggle.SetIsOnWithoutNotify(true);
                ChangePanel(tuningToggle.togglePanelState);
                break;
            }
        }
    }

    private void Init(VisualTuningTypes tuningTypes)
    {
        SetDefaultButton.gameObject.SetActive(false);
        FavouriteWheelsController.OnFavouriteSwitchPanelEnabled?.Invoke(tuningTypes == VisualTuningTypes.Wheels);
        RevertCurrentCategory();
        SetActiveControlButtons(false);
        LeftRightArrowsParent.SetActive(false);
        BrakesControlPanel.SetActive(false);
        CalipersVisualTuningPanel.SetActive(false);
        VisualPolicePanel.Disable();
        CreateContentPanel(tuningTypes == VisualTuningTypes.Colors ? 0.5f:0f, tuningTypes == VisualTuningTypes.Wheels);
        WDebug.Log($"Set Tuning type:{tuningTypes}", new Color(1f, 0.67f, 0.96f));
        bool isWheel = tuningTypes == VisualTuningTypes.Wheels;
        WheelUIChangerPanel.transform.parent.gameObject.SetActive(isWheel);
        
        if (tuningTypes == VisualTuningTypes.Vinyls)
        {
            LeftPanel.SetActive(false);
            VinylsPanel.SetActive(true);
            VinylsPanel.ShowVinylsWindow();
            _visualTuningToggles.First().SetIsOnWithoutNotify(true);
            _visualTuningToggles.Last().SetIsOnWithoutNotify(false);
#if ALTER_INPUT
            _inputMapSetEvent.TrySetGroup(InputGroupsEnum.PaintSelection);
        }
        else
        {
            _inputMapSetEvent.TrySetGroup(InputGroupsEnum.VisualTuning);
#endif
        }

        colorPanel.SetActive(tuningTypes == VisualTuningTypes.Colors);
        //  spawner.gameObject.SetActive(tuningTypes != VisualTuningTypes.Colors);
        _carInfo = _carInfoService.GetFromSlot(V.SlotID);

        if (tuningTypes != VisualTuningTypes.Vinyls)
            VisualScopesInits(tuningTypes);
        
        _currentVisualTuningTypes = tuningTypes;
        if (VisualTuningTypes.Colors == tuningTypes)
            viusalColorPanel.colorPicker.enabled = true;
    }

    private void RevertCurrentCategory()
    {
        if(_currentVisualTuningTypes == VisualTuningTypes.Interiors)
            _interiorsTuning.ClosePanel();
        
        if(_currentVisualTuningTypes == VisualTuningTypes.Brakes || _currentVisualTuningTypes == VisualTuningTypes.Calipers)
            OnClickExecute(_currentVisualTuningTypes, "OnChangeScope");
        else
            OnClickExecute(_currentVisualTuningTypes, "Revert");
    }

    private List<WheelDiskData> TryInitWheelsForF1(List<WheelDiskData> wheelsToSpawn)
    {
        VehicleStockTuningData carData = _vehicleData ? CarTuningFactory.GetDefaultCarTuningDataConfig(_vehicleData.CarId) : null;
        
        if(carData == null || carData.GetClass != VehicleClass.F1)
            return wheelsToSpawn;
        
        WheelDiskData wheelDiskData = WheelsTuningScope.GetWheelData(carData.Tuning.FrontWheelData.RimModelId, 
            _wheelsDiskTuningConfig.AllWheels);

        wheelDiskData.IsInteractableUiItem = false;
        wheelDiskData.ShowPrice = false;
        WheelUIChangerPanel.gameObject.SetActive(false);
        SideMunuParent.SetActive(false);
        
        VisualTuningFavouriteControllerBase favouriteController = GetComponent<VisualTuningFavouriteControllerBase>();
        favouriteController.OnFavouriteSwitchPanelEnabled?.Invoke(false);

        return new List<WheelDiskData>() {wheelDiskData};
    }
    
    private void VisualScopesInits(VisualTuningTypes tuningTypes)
    {
        ClearCollections();
        OnClickExecute(tuningTypes, "Init");
        switch (tuningTypes)
        {
            case VisualTuningTypes.Wheels:
                SetDefaultButton.SetActive(true);
                LeftRightArrowsParent.SetActive(true);
                WheelUIChangerPanel.Init(_carInfoService.GetFromSlot(V.SlotID).TuningData.FrontWheelData, _visualTuningViewModelComplexOne);
                ChangeSpawner();
                SetActiveControlButtons(false);
                List<WheelDiskData> favouriteWheels = GetFavouriteWheels();
                List<WheelDiskData> wheelsToSpawn = _mustSpawnFavourite ? favouriteWheels : _wheelsDiskTuningConfig.AllWheels.ToList();
                wheelsToSpawn = TryInitWheelsForF1(wheelsToSpawn);
                VisualTuningElementSpawner.SpawnList(wheelsToSpawn, VisualItemElementPrefab, InitWheels);
                FavouriteWheelsController.CurrentFavouriteCount = favouriteWheels.Count;
                break;
        case VisualTuningTypes.Flags:
                VisualTuningElementSpawner.SpawnList(_flagsDataSo.flags.ToList(), VisualItemElementPrefab, InitFlags);
                break;
            case VisualTuningTypes.Colors:
                viusalColorPanel.DisableOrEnableWheelUIElements(false);
                viusalColorPanel.Init(_carInfo, ColorsScope.ColorsType.Car);
                break;
            case VisualTuningTypes.BodyKits:
                VisualTuningElementSpawner.ResetVisualTuningElementsCollection();
                visualTuningBodyKitsPanel.Init();
                DontForgetTodoHelper.DontForgetFixIt();
                break;
            case VisualTuningTypes.Brakes:
                BrakesViusalTuningPanel.gameObject.SetActive(true);
                BrakesControlPanel.SetActive(true);
                SetActiveControlButtons(false);
                break;
            case VisualTuningTypes.Police:
                VisualPolicePanel.Init(this, _vehicleData);
                break;
            case VisualTuningTypes.Interiors:
                _interiorsTuning.OpenPanel();
                break;
            default:
                Debug.LogError($"Unkown visual scope type {tuningTypes}");
                break;
        }
    }

    private List<WheelDiskData> GetFavouriteWheels()
    {
        List<WheelDiskData> favouriteWheels = new List<WheelDiskData>();
        foreach (var wheel in _wheelsDiskTuningConfig.AllWheels)
        {
            if (FavouriteWheelsController.IsItemFavourite(wheel.index))
            {
                favouriteWheels.Add(wheel);
            }
        }

        return favouriteWheels;
    }
    
    private void ShowFavouriteWheels(bool showFavourite)
    {
        _mustSpawnFavourite = showFavourite;
        Init(VisualTuningTypes.Wheels);
    }

    private void UpdateFavouriteWheels()
    {
        if (_mustSpawnFavourite)
            Init(VisualTuningTypes.Wheels);
    }

    private void AddButtonsListeners()
    {
        applyButton.onClick.AddListener(TryApply);
        discardButton.onClick.AddListener(Discard);
        leftArrow.onClick.AddListener((() => ChangeSpawner(WheelUIChangerPanel.OnArrowPressed(true))));
        rightArrow.onClick.AddListener((() => ChangeSpawner(WheelUIChangerPanel.OnArrowPressed(false))));
        rideCarButton.onClick.AddListener(RideCar);
        SetDefaultButton.onClick.AddListener(SetDefault);
    }

    private void TryApply()
    {
        if (CanShowConfirmationWindow())
        {
            visualTuningCanvasGroup.alpha = 0;
            string confirmText = new LocalizedString("VisualTuningWindow.Buythisitemfor");
            string priceText = _selectedVisualItemElement.elementPrice.text;
            if (CurrentVisualScope != null)
            {
                CombinedPrice combinedPrice = CurrentVisualScope.GetBoughtItemPrice();
                if (combinedPrice != null && combinedPrice.Coin > 0) 
                {
                    priceText = ((int)combinedPrice.Coin).ToString();
                }
            }
            _managerUI.ShowConfirmWindow(Apply, new LocalizedString("Playerprofilewindow.Message"), 
                string.Format(confirmText, priceText),() => visualTuningCanvasGroup.alpha = 1).Forget();
        }
        else
        {
            Apply();
        }
    }

    private bool CanShowConfirmationWindow()
    {
        if (CurrentVisualScope != null)
        {
            CombinedPrice combinedPrice = CurrentVisualScope.GetBoughtItemPrice();
            if (combinedPrice != null) 
            {
                return combinedPrice.Coin > 0;
            }
        }
        int bodyKitCoin = 0;
        if (_selectedVisualItemElement)
        {
            bodyKitCoin = BodyKitsService.GetCoinPriceForKit(_selectedVisualItemElement.bodyKitType, _selectedVisualItemElement.index);
        }

        bool isbodyKitCoin = _selectedVisualItemElement && 
                             _currentVisualTuningTypes is VisualTuningTypes.BodyKits or VisualTuningTypes.Interiors  && 
                             bodyKitCoin != 0;
        
        bool isWheelCoin = _selectedVisualItemElement && _currentVisualTuningTypes == VisualTuningTypes.Wheels && 
                           WheelsTuningScope.GetWheelData(_selectedVisualItemElement.index, _wheelsDiskTuningConfig.AllWheels).isCoinPrice;
        
        bool isNeedShowConfirmationCategory = _currentVisualTuningTypes == VisualTuningTypes.Calipers ||
                                              _currentVisualTuningTypes == VisualTuningTypes.Brakes ||
                                              _currentVisualTuningTypes == VisualTuningTypes.Colors ||
                                              _currentVisualTuningTypes == VisualTuningTypes.Flags ||
                                              _currentVisualTuningTypes == VisualTuningTypes.Police ||
                                              isWheelCoin || isbodyKitCoin;
        if (_currentVisualTuningTypes == VisualTuningTypes.Colors &&
            viusalColorPanel.currentColorType == ColorsScope.ColorsType.Smoke)
        {
            isNeedShowConfirmationCategory = false;
        }

        bool isSelectedElementNotBought = _selectedVisualItemElement && !_selectedVisualItemElement.IsBought;
        return isNeedShowConfirmationCategory && isSelectedElementNotBought;
    }

    private void Apply()
    {
        visualTuningCanvasGroup.alpha = 1;
        OnClickExecute(_currentVisualTuningTypes, "Apply");
        
        if(_currentVisualTuningTypes == VisualTuningTypes.Interiors)
            _interiorsTuning.Apply();
        
        SetActiveControlButtons(false);
    }

    private void Discard()
    {
        OnClickExecute(_currentVisualTuningTypes, nameof(VisualTuningScope.Discard));
        _managerUI.ShowNotificationWindow(new LocalizedString("VisualTuningWindow.Detailreset!"), 3).Forget();
        if(_currentVisualTuningTypes == VisualTuningTypes.Colors)
            viusalColorPanel.RevertColor();
        SetActiveControlButtons(false);
        RevertUIElements();
    }

    private void SetDefault()
    {
        OnClickExecute(_currentVisualTuningTypes, nameof(VisualTuningScope.SetDefault));
        RevertUIElements();
    }
    private void RevertUIElements() // if we need reset some ui elements like sliders
    {
        SetActiveControlButtons(false);
        switch (_currentVisualTuningTypes)
        {
            case VisualTuningTypes.Wheels:
                switch (WheelUIChangerPanel.currentWheelAxleType)
                {
                    //ignore both
                    case WheelTuningAxleType.Front:
                        WheelUIChangerPanel.InitWithoutNotify(_carInfoService.GetFromSlot(V.SlotID).TuningData.FrontWheelData);
                        break;
                    case WheelTuningAxleType.Rear:
                        WheelUIChangerPanel.InitWithoutNotify(_carInfoService.GetFromSlot(V.SlotID).TuningData.RearWheelData);
                        break;
                }
                break;
            case VisualTuningTypes.Brakes:
                BrakesViusalTuningPanel.InitWithoutNotify();
                break;
            case VisualTuningTypes.Calipers:
                CalipersVisualTuningPanel.InitWithoutNotify();
                break;            
        }
    }
    
    private void OnVisualTuningValuesChanged()
    {
        CheckBoughtElement();
        OnTuningValuesChanged?.Invoke();
    }

    private void CheckBoughtElement()
    {
        if (!_selectedVisualItemElement)
            return;
        
        switch (_currentVisualTuningTypes)
        {
            case VisualTuningTypes.Wheels:
                if (WheelUIChangerPanel.GetWheelPaid(_selectedVisualItemElement.index))
                {
                    _selectedVisualItemElement.elementPrice.text = "0";
                    SetActiveControlButtons(false);
                    _selectedVisualItemElement.IsBought = true; 
                }

                break;
            case VisualTuningTypes.Flags:
                if (FlagConstructor.FlagsBought.ContainsKey(_selectedVisualItemElement.index))
                {
                    _selectedVisualItemElement.elementPrice.text = "0";
                    SetActiveControlButtons(false);
                    _selectedVisualItemElement.IsBought = true; 
                }

                break;
            case VisualTuningTypes.Colors:
                if (_selectedVisualItemElement is VisualItemColorElement colorElement)
                    if (_paintDataService.CheckPaintBought(colorElement.carMatType))
                    {
                        _selectedVisualItemElement.elementPrice.text = "0";
                        SetActiveControlButtons(false);
                        _selectedVisualItemElement.IsBought = true; 
                    }

                break;
            case VisualTuningTypes.BodyKits:
                if (_carInfo.BoughtBodyKits.GetBodyKitPaidStatus(_selectedVisualItemElement.bodyKitType,
                        _selectedVisualItemElement.index))
                {
                    _selectedVisualItemElement.elementPrice.text = "0";
                    SetActiveControlButtons(false);
                    _selectedVisualItemElement.IsBought = true; 
                }

                break;
            case VisualTuningTypes.Interiors:
                if (_carInfo.BoughtBodyKits.GetBodyKitPaidStatus(_selectedVisualItemElement.bodyKitType, 
                        _selectedVisualItemElement.index))
                {
                    _selectedVisualItemElement.elementPrice.text = "0";
                    SetActiveControlButtons(false);
                    _selectedVisualItemElement.IsBought = true; 
                }
                break;
            case VisualTuningTypes.Brakes:
                UpdateBrakePriceLabel(_selectedVisualItemElement, WheelsTuningScope.GetBrakeDataData(_selectedVisualItemElement.index, _brakeTuningConfig.AllBrakes));
                break;
            case VisualTuningTypes.Calipers:
                if(_wheelsDataService.IsCaliperBought(_selectedVisualItemElement.index))
                {
                    _selectedVisualItemElement.elementPrice.text = "";
                    SetActiveControlButtons(false);
                    _selectedVisualItemElement.IsBought = true; 
                    _selectedVisualItemElement.elementPrice.gameObject.SetActive(false);
                    break;
                }
                
                if (CalipersVisualTuningPanel.GetCalipersPaid(_selectedVisualItemElement.index))
                {
                    _selectedVisualItemElement.elementPrice.text = "";
                    SetActiveControlButtons(false);
                    _selectedVisualItemElement.IsBought = true; 
                    _selectedVisualItemElement.elementPrice.gameObject.SetActive(false);
                }
                break;
        }
    }

    public CarInfo GetCarInfo()
    {
        
        return _carInfo;
    }

    public void ChangeColor(Color color)
    {
        SetActiveControlButtons(true);
        float iridescenceThickness = 0f;
        float iridescencePower = 0f;
        bool isIridescent = viusalColorPanel.currentColorMatType is CarMatType.Iridescent or CarMatType.ChromeIridescent or CarMatType.MatteIridescent;
        if (viusalColorPanel.currentColorType == ColorsScope.ColorsType.Smoke)
        {
            isIridescent = false;
        }
        viusalColorPanel.ChangeSliderStates(isIridescent);
        viusalColorPanel.ChangeAlphaStates(viusalColorPanel.currentColorType == ColorsScope.ColorsType.Windows);

        if (isIridescent)
        {
            (float, float) thicknessAndPowerower = viusalColorPanel.GetThicknessAndPower();
            iridescenceThickness = thicknessAndPowerower.Item1;
            iridescencePower = thicknessAndPowerower.Item2;
        }

        if (viusalColorPanel.elementImage)
            viusalColorPanel.elementImage.color = color;

        CarPaintColor carColor = new CarPaintColor()
        {
            CarMatType = viusalColorPanel.currentColorMatType,
            Color = color,
            IridescenceThickness = iridescenceThickness,
            IridescencePower = iridescencePower
        };
        switch (viusalColorPanel.currentColorType)
        {
            case ColorsScope.ColorsType.Car:
                OnClickExecute(VisualTuningTypes.Colors, nameof(ColorsScope.SelectCarColor), carColor);
                break;
            case ColorsScope.ColorsType.Wheels:
                OnClickExecuteWithParams(VisualTuningTypes.Colors, nameof(ColorsScope.SelectWheelColor), carColor,
                    viusalColorPanel.currentWheelsColorType);
                break;
            case ColorsScope.ColorsType.Windows:
                carColor.Color.a = viusalColorPanel.WindowAlphaFromSlider(viusalColorPanel.GetAlpha());
                OnClickExecute(VisualTuningTypes.Colors, nameof(ColorsScope.SelectWindowColor), carColor);
                break;
            case ColorsScope.ColorsType.Smoke:
                OnClickExecute(VisualTuningTypes.Colors, nameof(ColorsScope.SelectSmokeColor), carColor);
                break;
            case ColorsScope.ColorsType.BodyKits:
                OnClickExecute(VisualTuningTypes.Colors, nameof(ColorsScope.SelectBodyKitColor), carColor);
                break;
        }
    }

    public void DestroySpawnerScript()
    {
        if (spawnerScript)
        {
            Destroy(spawnerScript);
        }
    }

    public void ChangeSpawner(int index = 2)
    {
        WheelUIChangerPanel.currentWheelAxleType = (WheelTuningAxleType)index;
        WheelUIChangerPanel.IgnoreExecute = true;
        axleSideText.text = new LocalizedString("VisualColorPanel." + WheelUIChangerPanel.currentWheelAxleType);

        if (WheelUIChangerPanel.currentWheelAxleType == WheelTuningAxleType.Front ||
            WheelUIChangerPanel.currentWheelAxleType == WheelTuningAxleType.Both)
            WheelUIChangerPanel.Init(_carInfoService.GetFromSlot(V.SlotID).TuningData.FrontWheelData,
                _visualTuningViewModelComplexOne);
        else if (WheelUIChangerPanel.currentWheelAxleType == WheelTuningAxleType.Rear)
            WheelUIChangerPanel.Init(_carInfoService.GetFromSlot(V.SlotID).TuningData.RearWheelData,
                _visualTuningViewModelComplexOne);
        OnClickExecute(VisualTuningTypes.Wheels, "SetAxleType", WheelUIChangerPanel.currentWheelAxleType);
        
    }

    private void ChangeMaterialType(CarMatType carMatType, RawImage image)
    {
        viusalColorPanel.currentColorMatType = carMatType;
        viusalColorPanel.elementImage = image;
        ChangeColor(viusalColorPanel.colorPicker.color);
    }

    public void ChangeIridescent(float value)
    {
        ChangeColor(viusalColorPanel.colorPicker.color);
    }

    public void ChangeAlpha(float arg0)
    {
        ChangeColor(viusalColorPanel.colorPicker.color);
    }

    public void ResetColor(VehicleStockTuningData vehicleStockTuningData, ColorsScope.WheelsColorType wheelsColorType)
    {
        OnClickExecuteWithParams(VisualTuningTypes.Colors,  nameof(ColorsScope.SetDefaultColor), vehicleStockTuningData, wheelsColorType);
    }

    public void Revert()
    {
        OnClickExecute(_currentVisualTuningTypes, "Revert");
        SetActiveControlButtons(false);
    }

    public void PlaySound(uint key)
    {
        SoundEventsDataHelper.PlayUISound(key,gameObject);
    }

    public void SetSelectedVisualItemElement(VisualItemElement element)
    {
        _selectedVisualItemElement = element;
    }

    public void InitColorMatType(GameObject go, object carMatTypes)
    {
        CarMatType castedCarMatType = (CarMatType)carMatTypes;
        VisualItemColorElement element = go.GetComponent<VisualItemColorElement>();
        element.colorMatName.gameObject.SetActive(true);
        if (_paintDataService.CheckPaintBought(castedCarMatType))
        {
            element.elementPrice.text = "0";
            element.IsBought = true;
        }
        else
        {
            element.elementPrice.text = _visualTuningPricesService.GetColorPrice().ToString();
        }

        element.carMatType = castedCarMatType;
        element.colorMatName.text = new LocalizedString("Colors." + carMatTypes);
        element.Button.group = toggleGroup;
        element.index = (int)castedCarMatType;
        element.Button.onValueChanged.AddListener((x) =>
        {
            if (x)
            {
                PlaySound(SoundEventsDataHelper.TuningSelectPaintSwitch);
                SetActiveControlButtons(true);
                _selectedVisualItemElement = element;
                ChangeMaterialType(castedCarMatType, element.elementImage);
            }
        });
        bool isSelected = castedCarMatType == viusalColorPanel.currentColorMatType;
        if (isSelected)
        {
            viusalColorPanel.elementImage = element.elementImage;
            element.elementImage.color = viusalColorPanel.colorPicker.color;
            _selectedVisualItemElement = element;
        }

        element.Button.SetIsOnWithoutNotify(isSelected);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        element.name = element.name.Replace(")", $"{(int)castedCarMatType})");
#endif
    }

    public void InitWheels(GameObject go, object data)
    {
        WheelDiskData castedDiskData = (WheelDiskData)data;
        VisualItemElement element = go.GetComponent<VisualItemElement>();
        element.Button.interactable = castedDiskData.IsInteractableUiItem;
        element.elementPrice.gameObject.SetActive(castedDiskData.ShowPrice);
        element.SpecialStickerImage.gameObject.SetActive(castedDiskData.isTwoPartDisk);
        element.moneyIcon.sprite = castedDiskData.isCoinPrice ? coinIcon : moneyIcon;

        if (_wheelDiskTexturesConfig)
        {
            TextureData textureData = _wheelDiskTexturesConfig.AllTextureData
                .FirstOrDefault(d => d.ID == castedDiskData.index);
            if (textureData != null)
            {
                element.elementImage.texture = textureData.Texture;
            }
            else
            {
                WDebug.Log($"No texture found for {castedDiskData.index}");
            }
        }
        else
        {
            WDebug.Log("Wheel disk texture config doesn't exist");
        }
        
        element.Button.group = toggleGroup;
        element.index = castedDiskData.index;

        var price = _visualTuningPricesService.GetWheelPrice(castedDiskData.index);
        element.elementPrice.text = price.Price + "";
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        element.elementPrice.text += $"\n(ID{castedDiskData.index})";
        element.name = element.name.Replace(")", $"{castedDiskData.index})");
#endif

        element.Button.onValueChanged.AddListener((x) =>
        {
            if (x)
            {
                FavouriteWheelsController.OnSelectItem(castedDiskData.index);
                PlaySound(SoundEventsDataHelper.TuningSelectRimSwitch);
                SetActiveControlButtons(true);
                _selectedVisualItemElement = element;
                OnClickExecuteWithDelay(VisualTuningTypes.Wheels, "Select", castedDiskData.index);
            }
        });

        element.Button.SetIsOnWithoutNotify(castedDiskData.index == WheelUIChangerPanel.GetCurrentWheelId());
    }

    public void InitCalipersMesh(GameObject caliperGO, object caliperModel)
    {
        BrakeDiskItemData castedDiskItemData = (BrakeDiskItemData)caliperModel;
        VisualItemElement element = caliperGO.GetComponent<VisualItemElement>();
        element.moneyIcon.sprite = castedDiskItemData.isCoinPrice ? coinIcon : moneyIcon;
        element.elementImage.texture = castedDiskItemData.PreviewTexture;
        var price = _visualTuningPricesService.GetCalipersPrice(castedDiskItemData.index);
        element.IsBought = price.Price == 0;
        element.elementPrice.text = element.IsBought ? "" : price.Price.ToString();
        element.elementPrice.gameObject.SetActive(!element.IsBought);
        element.Button.group = toggleGroup;
        element.index = castedDiskItemData.index;
        element.Button.onValueChanged.AddListener((x) =>
        {
            if (x)
            {
                PlaySound(SoundEventsDataHelper.TuningSelectBrakesSwitch);
                if (BrakesViusalTuningPanel.CurrentAxleType == WheelTuningAxleType.Both)
                {
                    if (_carInfo.TuningData.FrontWheelData.BrakesData.CaliperMeshIndex !=
                        _carInfo.TuningData.RearWheelData.BrakesData.CaliperMeshIndex)
                    {
                        SetActiveControlButtons(true);
                    }
                    else
                    {
                        ShowControlPanel(element.index, _carInfo.TuningData.FrontWheelData.BrakesData.CaliperMeshIndex);
                    }
                }
                else
                {
                    bool isFront =
                        BrakesViusalTuningPanel.CurrentAxleType is WheelTuningAxleType.Front
                            or WheelTuningAxleType.Both;

                    int calculatedIndex = isFront
                        ? _carInfo.TuningData.FrontWheelData.BrakesData.CaliperMeshIndex
                        : _carInfo.TuningData.RearWheelData.BrakesData.CaliperMeshIndex;
                    
                    ShowControlPanel(element.index,calculatedIndex);
                }
               
                _selectedVisualItemElement = element;
                OnClickExecuteWithParams(VisualTuningTypes.Calipers, "SelectCaliperMesh", (int) castedDiskItemData.index,false);
            }
        });

        bool isFront = BrakesViusalTuningPanel.CurrentAxleType is WheelTuningAxleType.Front or WheelTuningAxleType.Both;
        int needIndex = isFront
            ? _carInfo.TuningData.FrontWheelData.BrakesData.CaliperMeshIndex
            : _carInfo.TuningData.RearWheelData.BrakesData.CaliperMeshIndex;
        element.Button.SetIsOnWithoutNotify(castedDiskItemData.index == needIndex);
        CheckToggleState(element, isFront, _carInfo.TuningData.FrontWheelData.BrakesData.CaliperMeshIndex
            , _carInfo.TuningData.RearWheelData.BrakesData.CaliperMeshIndex);
        _cachedVisualTuningElements.Add(element);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        element.name = element.name.Replace(")", $"{castedDiskItemData.index})");
#endif
    }
    public void InitBrakesDisc(GameObject caliperGO, object brakesDisc)
    {
        BrakeDiskItemData castedDiskItemData = (BrakeDiskItemData)brakesDisc;
        VisualItemElement element = caliperGO.GetComponent<VisualItemElement>();
        element.moneyIcon.sprite = castedDiskItemData.isCoinPrice ? coinIcon : moneyIcon;
        element.elementImage.texture = castedDiskItemData.PreviewTexture;
        UpdateBrakePriceLabel(element, castedDiskItemData);
        element.titleText.transform.parent.gameObject.SetActive(true);
        element.titleText.text = castedDiskItemData.BrakeDiscType.ToString();
        element.Button.group = toggleGroup;
        element.index = castedDiskItemData.index;
        element.Button.onValueChanged.AddListener((x) =>
        {
            if (x)
            {
                PlaySound(SoundEventsDataHelper.TuningSelectBrakesSwitch);
                if (BrakesViusalTuningPanel.CurrentAxleType == WheelTuningAxleType.Both)
                {
                    if (_carInfo.TuningData.FrontWheelData.BrakesData.DiscType !=
                        _carInfo.TuningData.RearWheelData.BrakesData.DiscType)
                    {
                        SetActiveControlButtons(true);
                    }
                    else
                    {
                        ShowControlPanel(element.index,  (int)_carInfo.TuningData.FrontWheelData.BrakesData.DiscType);
                    }
                }
                else
                {
                    bool isFront = BrakesViusalTuningPanel.CurrentAxleType == WheelTuningAxleType.Front;
                    int calculatedIndex = isFront
                        ? (int)_carInfo.TuningData.FrontWheelData.BrakesData.DiscType
                        : (int)_carInfo.TuningData.RearWheelData.BrakesData.DiscType;
                    ShowControlPanel(element.index, calculatedIndex);
                }
                
                _selectedVisualItemElement = element;
                OnClickExecuteWithParams(VisualTuningTypes.Brakes, "SelectDiscTexture",(int) castedDiskItemData.BrakeDiscType,false);
            }
        }); 
        _cachedVisualTuningElements.Add(element);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        element.name = element.name.Replace(")", $"{castedDiskItemData.index})");
#endif

    }

    private void ShowControlPanel(int index, int selectedIndex)
    {
        SetActiveControlButtons(index != selectedIndex);
    }

    public void CheckInstalledElement(List<VisualItemElement> visualTuningElements,bool isFront,int frontIndex, int rearIndex)
    {
        foreach (var element in visualTuningElements)
        {
            CheckToggleState(element, isFront, frontIndex, rearIndex);
        }
    }

    public void ClearCollections()
    {
        _cachedVisualTuningElements = new List<VisualItemElement>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(LayoutGroup);
    }
    
    public List<VisualItemElement> GetCachedElements()
    {
        return _cachedVisualTuningElements;
    }
    
    public void CheckToggleState(VisualItemElement element,bool isFront, int frontIndex, int rearIndex)
    {
        int needIndex = isFront ? frontIndex : rearIndex;
        element.Button.SetIsOnWithoutNotify(element.index == needIndex);
    }

    public void InitFlags(GameObject go, object data)
    {
        FlagItem castedData = (FlagItem)data;
        VisualItemElement element = go.GetComponent<VisualItemElement>();
        element.elementImage.texture = castedData.texture;
        element.Button.group = toggleGroup;
        element.index = castedData.data.id;
        bool isBought = castedData.data.id == 0 || FlagConstructor.FlagsBought.ContainsKey(element.index);
        int price = isBought ? 0 : castedData.price;
        element.IsBought = isBought;
        element.elementPrice.text = price.ToString();
        element.Button.onValueChanged.AddListener((x) =>
        {
            if (x)
            {
                PlaySound(SoundEventsDataHelper.SmallClickSwitch);
                SetActiveControlButtons(true);
                _selectedVisualItemElement = element;
                OnClickExecuteWithDelay(VisualTuningTypes.Flags, "Select", castedData.data.id);
            }
        });
        element.Button.SetIsOnWithoutNotify(_carInfo.AdditionalData.FlagId == castedData.data.id);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        element.name = element.name.Replace(")", $"{castedData.data.id})");
#endif
    }

    public void OnClickExecute(VisualTuningTypes scopeType, string method, object argument = null)
    {
        //Debug.Log("Exrcute " + scopeType + " " + argument);
        _visualTuningViewModelComplexOne.Execute(scopeType, method, argument);
    }

    public async UniTaskVoid OnClickExecuteWithDelay(VisualTuningTypes scopeType, string method, object argument = null)
    {
        //Debug.Log("Exrcute " + scopeType + " " + argument);
        if(_operationSkip)
            return;
        _graphicRaycaster.enabled = false;
        _operationSkip = true;
        _visualTuningViewModelComplexOne.Execute(scopeType, method, argument);
        await UniTask.Delay(TimeSpan.FromSeconds(OperationDelay));
        _operationSkip = false;
        _graphicRaycaster.enabled = true;
    }

    public void OnClickExecuteWithParams(VisualTuningTypes scopeType, string method, params object[] argument)
    {
        _visualTuningViewModelComplexParams.Execute(scopeType, method, argument);
    }
    
    public async UniTaskVoid OnClickExecuteWithParamsWithDelay(VisualTuningTypes scopeType, string method, params object[] argument)
    {
        if(_operationSkip)
            return;
        _operationSkip = true;
        _graphicRaycaster.enabled = false;
        _visualTuningViewModelComplexParams.Execute(scopeType, method, argument);
        await UniTask.Delay(TimeSpan.FromSeconds(OperationDelay));
        _operationSkip = false;
        _graphicRaycaster.enabled = true;
    }

    private void ChangeSelectedItemPrice()
    {
        if (_selectedVisualItemElement)
            _selectedVisualItemElement.elementPrice.text = "0";
    }

    public void Close()
    {
        if (_vehicleData)
            _vehicleData.racingState = RacingState.None;
        _graphicRaycaster.enabled = true;
        _managerUI.ShowCarTuningWindow();
        OnClickExecute(_currentVisualTuningTypes, "Revert");
        FlagsStorage.IsCarInService = false;
        _powertrain.SetAlignCar(false);
        base.Close();
    }

    private void RideCar()
    {
        if (_timerTuningService.IsCarBlocked(V.SlotID))
        {
            _ = _managerUI.ShowNotificationWindow(new LocalizedString("VisualTuningWindow.Yourcarcurrentlyisinservice"), 3);
            return;
        }
        _exteriorTuning.ExitVisualTuning(false);
    }

    public void InitBackButton()
    {
        _managerUI.OnBackButtonActivated?.Invoke(true);
        _managerUI.SetCurrentWindow(this);
    }

    public void Back()
    {
        _playerVinylCensorChecker.CheckCurrentCar();
        FlagsStorage.OnVisualTuning = false;
        _exteriorTuning.ExitVisualTuning();
        _interiorsTuning.ClosePanel();
        Close();
    }
    public void ChangePanel(VisualTuningTypes togglePanelState)
    {
        _mustSpawnFavourite = false;
        FavouriteWheelsController.ResetToAllView();
        Init(togglePanelState);
    }

    public void SetCurrentVisualTuningTypes(VisualTuningTypes tuningType)
    {
        _currentVisualTuningTypes = tuningType;
    }

    private void UpdateBrakePriceLabel(VisualItemElement element, BrakeDiskItemData castedDiskItemData)
    {
        if (!element || castedDiskItemData == null)
        {
            return;
        }
        
        bool isStockBrake = _wheelsDataService.IsBrakeStock(castedDiskItemData.BrakeDiscType, _carInfo.CarID);
        GlobalCost price = new GlobalCost();
        if (isStockBrake)
        {
            element.IsBought = true;
        }
        else
        {
            price = _visualTuningPricesService.GetBrakePrice(castedDiskItemData.index);
            element.IsBought = price.Price == 0;
        }
        
        element.elementPrice.text = price.Price.ToString();
        element.elementPrice.SetActive(!element.IsBought);
    }

    public void SetActiveControlButtons(bool isActive)
    {
        controlButtonsPanel.SetActive(isActive);
    }
}