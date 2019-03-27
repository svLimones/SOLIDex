using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UWP;
using ViewCreation;

public interface IGuiMediatorManager
{
    IView Current { get; }
    event Action<IGuiMediatorManager, IView> OnOpenGuiMediator;
    event Action<IGuiMediatorManager, IView> OnCloseGuiMediator;
    void OnUguiMissClick(BlockerInputEventArgs eventData);
};

[CreateMonoObject("GuiManager")]
public class GuiManager : ServiceBaseMonoBehaviour, IGuiManager
{
    private const float DefaultHeight = 640f;
    private const float DefaultWidth = 960f;
    private const string UiRootPrefabName = "UiRootView";
    private static IServiceContainer _container;
    private DialogManager _dialogManager;
    private ILoadingDialog _loadingDialog;
    private ScreensManager _screensManager;
    private InputBlockingManager _inputBlockingManager;
    private UguiCanvasDigest _uguiCanvasDigest;
    private UiRootDigest _uiRootDigest;
    private Camera3dDigest _camera3DDigest;

    //для эффекта фокуса
    private GameObject _focusEffectGameObject;


    private IViewInput ViewInput
    {
        get { return Use<IViewInput>(); }
    }

    private IViewFactory Factory
    {
        get { return Use<IViewFactory>(); }
    }

    public static Camera Camera { get; private set; }

    public float YRate { get { return GetVerticalGuiScale(); } }

    public Vector2 NGuiScreenSize
    {
        get
        {
            var widget = _uiRootDigest.BlockerSprite.GetComponent<UIWidget>();
            return new Vector2(widget.width, widget.height);
        }
    }

    /// <summary>
    /// </summary>
    public static IGuiManager I
    {
        get { return _container.Use<IGuiManager>(); }
    }

    /// <summary>
    ///     device width (largest dimension)
    /// </summary>
    public static int Width
    {
        get
        {
            // позволим в едиторе выставлять какое угодно разрешение, не переворачивая экран
            if (Application.isEditor)
            {
                return UnityEngine.Screen.width;
            }
            return UnityEngine.Screen.width > UnityEngine.Screen.height
                ? UnityEngine.Screen.width
                : UnityEngine.Screen.height;
        }
    }

    /// <summary>
    /// device height (shortest dimension)
    /// </summary>
    public static int Height
    {
        get
        {
            if (Application.isEditor)
            {
                return UnityEngine.Screen.height;
            }
            return UnityEngine.Screen.width > UnityEngine.Screen.height
                ? UnityEngine.Screen.height
                : UnityEngine.Screen.width;
        }
    }


    public bool LockBackButton { get; set; }

    public TutorialObjectRegister TutorialObjectRegister { get; private set; }

    public GameObject Camera3d
    {
        get { return _uiRootDigest.DialogLayer; }
    }


    /// <summary>
    ///     deinitialization
    /// </summary>
    public void Reset()
    {
        _screensManager.CloseCurrent();
        _dialogManager.CloseAll();
        LockBackButton = false;
    }

    public void OnSwitchRoomLoadingStart()
    {
        Use<ToolManager>().CancelTool();

        _screensManager.CloseAll();
        _dialogManager.CloseAll();

        UIDrawCall.ReleaseAll();
    }

    /// <summary>
	/// <summary>
	/// gui scale modifier
	/// </summary>
	private float GetVerticalGuiScale()
    {
        float yRate = Height / DefaultHeight;
        float xRate = Width / DefaultWidth;
        float aspectRate = (((float)Width) / Height) / (DefaultWidth / DefaultHeight);
        float rate = (aspectRate < 1) ? xRate : yRate;

        return rate;
    }

    public override void OnContainerSet()
    {
        _container = Container;
        base.OnContainerSet();
        CreateUiRoot();
        CreateComposedObjects();

        ViewInput.ClickHandler.InputMissed += MisclickHandler;
        TutorialObjectRegister = new TutorialObjectRegister();
        TutorialObjectRegister.RegisterObject(string.Empty, GuiConstants.Ids.ModalBlockerId, _uiRootDigest.BlockerSprite);
        Use<ISwitchRoomController>().onSwitchRoomLoadingStart += OnSwitchRoomLoadingStart;
    }

    public T ShowScreen<T>() where T : class, IView
    {
        var res = _screensManager.ShowScreen<T>();

        OnViewCreated(res);
        return res;
    }

    public T ShowDialog<T>(IView owner = null, IViewModel viewModel = null, DialogShowMethod method = DialogShowMethod.AddFront, Action<T> onShown = null) where T : class, IView
    {
        Action<T> newOnShown = dialog =>
        {
            if (onShown != null) onShown(dialog);
            OnViewCreated(dialog);
        };
        var result = _dialogManager.ShowDialog(GetViewTypeByView(typeof(T)), owner,  viewModel, method, newOnShown);
        return result;
    }

    public T CreateView<T>(GameObject parent = null, GameObject prefab = null, IViewModel viewModel = null)
        where T : class, IView
    {
        return CreateView(typeof (T), parent, prefab, viewModel) as T;
    }

    public IView CreateView(Type type, GameObject parent = null, GameObject prefab = null, IViewModel viewModel = null)
    {
        IView res = null;
        var _parent = parent;

        var viewType = GetViewTypeByView(type);
        switch (viewType)
        {
            case ViewType.Screen:
                res = _screensManager.ShowScreen(type, viewModel);
                break;

            case ViewType.Dialog:
                IView owner = null;
                if (parent != null)
                {
                    owner = parent.GetComponent<IView>();
                }

                var isUgui = _parent == null && type.HasAttribute<MediatorUguiAttribute>();
                if (isUgui)
                {
                    _parent = _uguiCanvasDigest.CanvasGroup.gameObject;
                    res = Factory.Create(type, _parent, viewModel, prefab,
                        CreationOptions.GuiCreation | CreationOptions.CreateGameObject);
                    _dialogManager.SetCurrentDialog(res);
                    break;
                }
                res = _dialogManager.ShowDialog(type, viewType, owner, viewModel);
                break;

            case ViewType.DropDownDialog:
                res = _dialogManager.ShowDialog(type, viewType, null, viewModel);
                break;

            case ViewType.ChildComponent:
                isUgui = _parent == null && type.HasAttribute<MediatorUguiAttribute>();
                if (isUgui)
                    _parent = _uguiCanvasDigest.CanvasGroup.gameObject;
                res = Factory.Create(type, _parent, viewModel, prefab,
                    CreationOptions.GuiCreation | CreationOptions.CreateGameObject);
                break;

            case ViewType.Scenario:
                res = Factory.Create(type, parent, viewModel, prefab, CreationOptions.Empty);
                break;

            case ViewType.Effect:
            case ViewType.SceneObject:
                throw new ArgumentException("Should be created with ViewFactory:" + viewType);
            default:
                throw new ArgumentOutOfRangeException("Cannot create viewtype:" + viewType);
        }
        OnViewCreated(res);
        return res;
    }

    public void RegisterTutorialObject(string prefix, string key, GameObject target)
    {
        if (TutorialObjectRegister != null)
        {
            TutorialObjectRegister.RegisterObject(prefix, key, target);
        }
    }

    private void CreateComposedObjects()
    {
        _screensManager = new ScreensManager(Factory, _uiRootDigest.ScreenLayer, _uguiCanvasDigest.CanvasGroup.gameObject);
        _screensManager.CurrentChanged += OnCurrentChanged;

        _dialogManager = new DialogManager(Factory, _uiRootDigest.DialogLayer, _camera3DDigest.DialogLayer3D, _uguiCanvasDigest.CanvasGroup.gameObject, _screensManager);
        _dialogManager.CurrentChanged += OnCurrentChanged;

        TutorGUI = new TutorialGUIManager(_uiRootDigest.UiCamera, _uiRootDigest.TutorialLayer);

        _inputBlockingManager = new InputBlockingManager(_uguiCanvasDigest, _uiRootDigest, Use<ICameraMoverSwitcher>(), _camera3DDigest);
        _inputBlockingManager.HitBlocker += OnMissClick;
    }

    private void OnMissClick(BlockerInputEventArgs e)
    {
        switch (e.InputType)
        {
            case ScreenInput.Ugui:
                if (GetSublineIndex(_dialogManager.Current as Component) > GetSublineIndex(_screensManager.Current as Component))
                {
                    _dialogManager.OnUguiMissClick(e);
                }
                else
                {
                    _screensManager.OnUguiMissClick(e);
                }
                break;
            case ScreenInput.Ngui:
            default:
                break;
        }
    }

    private int GetSublineIndex(Component view)
    {
        if (view == null)
            return 0;

        return view.transform.GetSiblingIndex();
    }

    private void OnCurrentChanged(IView newCurrent)
    {
        IView current = _dialogManager.Current ?? _screensManager.Current;
        var currentComponent = current as Component;
        if (currentComponent != null) BringForward(currentComponent.gameObject);

        _inputBlockingManager.SetAppropriateInput(current);
        if (current!=null && current.ViewType == ViewType.DropDownDialog)
            return;

        _inputBlockingManager.HandleBlockerLayers(current);
        _inputBlockingManager.NormalizePanelDepths();
    }

    public void SetInputToCurrentView()
    {
        var current = _dialogManager.Current ?? _screensManager.Current;
        _inputBlockingManager.SetAppropriateInput(current, true);
    }

    //Public method for implementing miss click outside when it needed
    public void EmulateMissClick()
    {
        MisclickHandler();
    }

    /// <summary>
    ///     <see cref="ScreensManager" />
    /// </summary>
    public IScreensManager Screen
    {
        get { return _screensManager; }
    }

    /// <summary>
    ///     <see cref="DialogManager" />
    /// </summary>
    public IDialogManager Dialog
    {
        get { return _dialogManager; }
    }

    /// <summary>
    ///     <see cref="DialogManager" />
    /// </summary>
    public HintsView HintsLayer
    {
        get { return _uiRootDigest.HintsView; }
    }

    /// <summary>
    ///     <see cref="TutorialLayerManager" />
    /// </summary>
    public TutorialGUIManager TutorGUI { get; private set; }

    /// <summary>
    ///     Контейнер для ngui-элементов которые рендерятся до старта игры (например, лоты в магазине)
    /// </summary>
    public GameObject PrerenderLayer
    {
        get { return _uiRootDigest.PrerenderLayer; }
    }

    public RenderTexture CreateRenderTexture()
    {
        return new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32) {name = "genericRenderTexture"};
    }

    /// <summary>
    /// </summary>
    /// <typeparam name="TMediator"></typeparam>
    /// <param name="prefab"></param>
    /// <returns></returns>
    public TMediator AddToDialog<TMediator>(GameObject prefab) where TMediator : Component
    {
        if (prefab != null)
        {
            PlayerPreferences.SaveRecentGui(prefab.name, Container);
        }

        var instance = _uiRootDigest.DialogLayer.AddChild(prefab);

        return instance.GetComponent<TMediator>();
    }


    /// <summary>
    ///     Gets the tutorial object.
    /// </summary>
    /// <param name="prefix">_prefix.</param>
    /// <param name="key">_key.</param>
    /// <param name="callback">_callback.</param>
    public void GetTutorialObject(string prefix, string key, Action<GameObject> callback)
    {
        TutorialObjectRegister.Get(prefix, key, callback);
    }

    public void ClearTutorialCallbacks()
    {
        if (TutorialObjectRegister != null)
            TutorialObjectRegister.ClearCallbacks();
    }

    public ScreenInput AllowedInput
    {
        get { return _inputBlockingManager.AllowedInput; }
        set { _inputBlockingManager.AllowedInput = value; }
    }

    public void HideScreenLayer(bool smoothly = false)
    {
        if (smoothly)
        {
            throw new NotImplementedException();
        }
        NGUITools.SetActive(_uiRootDigest.ScreenLayer, false);
    }

    public void ShowScreenLayer()
    {
        NGUITools.SetActive(_uiRootDigest.ScreenLayer, true);
    }

    public void EnableFocuseEffect()
    {
        //загружаем префаб для эффекта фокуса
        ResourceStorage.Instance.Get<GameObject>("FocusEffect", this,
            go =>
            {
                _focusEffectGameObject = Use<Instantiator>()
                    .InstantiateAtParent(_uiRootDigest.DialogLayer.transform, go);
            });
        NGUITools.SetActive(_uiRootDigest.ScreenLayer, false);
    }

    public void DisableFocuseEffect()
    {
        if (_focusEffectGameObject)
        {
            //TODO: может надо удалять объект с выгрузкой из ресурсной системы?
            Destroy(_focusEffectGameObject);
        }
    }

    public void CloseLoadingDialog()
    {
        if (_loadingDialog == null)
        {
            return;
        }

        _dialogManager.CloseDialog(_loadingDialog as IView);
        _loadingDialog = null;
    }

    public T CreateLoadingDialog<T>() where T : View<ViewModelBase>, ILoadingDialog
    {
        if ((_loadingDialog as MonoBehaviour) != null && !(_loadingDialog is T))
        {
            CloseLoadingDialog();
        }

        if ((_loadingDialog as MonoBehaviour) == null)
        {
            var t = typeof(T).GetCustomAttributes(typeof(ResourcePathAttribute), false);
            var attr = t.FirstOrDefault() as ResourcePathAttribute;

            if (attr == null)
            {
                throw new Exception("Can't create loading dialog: attribute resources not defined " +
                                    typeof(T).Name);
            }

            var prefab = attr.IsLocalResource
                ? Resources.Load<GameObject>(attr.Path)
                : ResourceStorage.Instance.Get<GameObject>(attr.Path);

            _loadingDialog = AddToDialog<T>(prefab);
            var go = ((Component)_loadingDialog).gameObject;
            NGUITools.AdjustDepth(go, GuiConstants.Depths.LoadingDialog);
            _dialogManager.SetCurrentDialog(_loadingDialog as IView);
        }

        return _loadingDialog as T;
    }

    /// <summary>
    ///     return viewtype by view, ex. TrololoMediator -> ViewType.Dialog
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    public ViewType GetViewTypeByView(Type view)
    {
        return Factory.GetViewType(view);
    }


    private void Update()
    {
        ProceedBackButton();
    }

    private void ProceedBackButton()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !LockBackButton)
        {
            OnBackButtonDown();
        }
    }

    public void OnBackButtonDown()
    {
        if (TutorialModel.InTutorialOld)
            return;

        var current = _dialogManager.Current ?? _screensManager.Current;
        var currentBackHandler = current as IBackButtonHandler;

        if (current == null)
            return;

        if (currentBackHandler!=null)
        {
            currentBackHandler.OnBackButton();
            return;
        }

        current.Close();
        
        //kludge: убрать и сделать GuiManager абстрактнее
        if (!TutorialModel.InTutorialOld && 
            Use<ToolManager>().CurrentTool is ClickTool && 
            _dialogManager.Current == null && 
            Use<IScenesController>().CurrentScene==GameScene.Main &&
            !(_screensManager.Current is DLFScreenLeagueMediator))
        {
            _screensManager.ShowMainScreen();
        }
    }

    private void OnViewCreated(IView res)
    {
        Use<IScenarioService>().OnViewCreated(res);
    }

    private void CreateUiRoot()
    {
        var resourceStorage = Use<IResourceStorage>();
        var _uiRootPrefab = resourceStorage.Get<GameObject>(UiRootPrefabName);
        var uiRoot = Instantiate(_uiRootPrefab);
        uiRoot.transform.SetParent(transform);

        _uiRootDigest = uiRoot.GetComponentInChildren<UiRootDigest>();
        _uguiCanvasDigest = uiRoot.GetComponentInChildren<UguiCanvasDigest>();
        _camera3DDigest = uiRoot.GetComponentInChildren<Camera3dDigest>();

        var root = _uiRootDigest.GetComponent<UIRoot>();
        var heightResolution = (int) Mathf.Round(Height / YRate);
        root.scalingStyle = UIRoot.Scaling.Constrained;
        root.manualHeight = heightResolution;
        _uiRootDigest.HintsView.Init();
        Camera = _uiRootDigest.Camera;

        var canvas = _uguiCanvasDigest.GetComponent<CanvasScaler>();
        canvas.referenceResolution = new Vector2(canvas.referenceResolution.x, heightResolution);
    }

    /// <summary>
    /// </summary>
    private void MisclickHandler()
    {
        if (!TutorialModel.InTutorialOld && Use<ToolManager>().CurrentTool is ClickTool && _dialogManager.Current == null && Use<IScenesController>().CurrentScene==GameScene.Main)
        {
            _screensManager.ShowMainScreen();
        }
    }

    /// <summary>
    ///     Bring all of the widgets on the specified object forward.
    /// </summary>
    public void BringForward(GameObject go)
    {
        NGUITools.AdjustDepth(go, GuiConstants.Depths.Forward);
    }

    public void NormalizePanelDepths()
    {
        _inputBlockingManager.NormalizePanelDepths();
    }
    
    public void NormalizeChildPanelDepths(GameObject go)
    {
        var panels = go.GetComponentsInChildren<UIPanel>(true);
        foreach (var item in panels)
        {
            item.UpdateDepth();
        }
    }
}

public static class GuiConstants
{
    public static class Ids
    {
        public const string ModalBlockerId = "ModalBlocker";
    }

    public static class Depths
    {
        public const int Forward = 1000;
        public const int ModalBlocker = 900;
        public const int ModalDialog = 1000;
        public const int LoadingDialog = 2000;
        public const int TransperetBlocker = 2100;
    }
}

public interface ILoadingDialog
{
    void Close();
}

public interface IBackButtonHandler
{
    void OnBackButton();
}

[Flags]
public enum ScreenInput
{
    None = 0,
    Ngui = 1<<1,
    Ugui = 1<<2
}

public interface IGuiManager : IViewService, IResettable
{
    IDialogManager Dialog { get; }
    IScreensManager Screen { get; }
    TutorialObjectRegister TutorialObjectRegister { get; }
    Vector2 NGuiScreenSize { get; }
    TutorialGUIManager TutorGUI { get; }
    bool LockBackButton { get; set; }

    T ShowDialog<T>(IView owner = null, IViewModel viewModel = null, DialogShowMethod method = DialogShowMethod.AddFront,
        Action<T> onShown = null) where T : class,IView;

    T ShowScreen<T>() where T : class, IView;

    TMediator AddToDialog<TMediator>(GameObject prefab) where TMediator : Component;

    T CreateView<T>(GameObject parent = null, GameObject prefab = null, IViewModel viewModel = null) where T : class, IView;
    T CreateLoadingDialog<T>() where T : View<ViewModelBase>, ILoadingDialog;
    void CloseLoadingDialog();
    IView CreateView(Type type, GameObject parent = null, GameObject prefab = null, IViewModel viewModel = null);

    RenderTexture CreateRenderTexture();
    void HideScreenLayer(bool smoothly = false);
    void ShowScreenLayer();
    ViewType GetViewTypeByView(Type view);
    void EmulateMissClick();

    void RegisterTutorialObject(string prefix, string key, GameObject target);
    void BringForward(GameObject go);
    void SetInputToCurrentView();
}