using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Gui.CrossPromo;
using Assets.Scripts.Gui.Dialog;
using Dragonlands.Core.Config;
using Dragonlands.Gui;
using GuildChat;
using Guilds;
using Guilds.Models.Messages.Chat;
using Holoville.HOTween;
using Holoville.HOTween.Core;
using SocialQuantum.DragonLands.Guild.HelpRequest;
using uGUI.Carousel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UWP;


[MediatorUgui(alpha:0)]
[PlatformSpecificView(RunPlatform.All ^ RunPlatform.Wp ^ RunPlatform.Web, typeof(ScreenGuildChatUguiView), "ScreenGuildChatUgui")]
[PlatformSpecificView(RunPlatform.Wp | RunPlatform.Web, typeof(ScreenGuildChatUguiView), "ScreenGuildChatUgui_forDesktop")]
public class ScreenGuildChatUguiView : View<ScreenGuildChatViewModel>, ICarouselItemFactory, IBackButtonHandler, IBlockerInputHandler
{
    private const string HelpRequestWaitingMessageKey = "help_request_waiting_message";

    [SerializeField] private RectTransform window;
    [SerializeField] public EventTrigger btnClose;
    [SerializeField] private InputField inputField;
    [SerializeField] private CarouselScrollListWidget carousel;
    [SerializeField] private Text lblTabInfo;
    [SerializeField] public Toggle btnChatTab;
    [SerializeField] public Toggle btnIncomingTab;
    [SerializeField] private GuildIconView guildIcon;
    [SerializeField] private Text Title;
    [SerializeField] private Button btnOpenGuildDialog;
    [SerializeField] private Button btnRequestHelp;
    [SerializeField] private GameObject InputPanel;
    [SerializeField] private CounterUguiWidget chatCounter;
    [SerializeField] private CounterUguiWidget incomingCounter;
    [SerializeField] private GameObject timerIcon;
    [SerializeField] private GameObject timerPanel;
    [SerializeField] private TimerLabelWidget lblTimer;
    [SerializeField] private Button btnClearMessage;

    public event Action OnHide = delegate { };
    public List<IData> Messages { get { throw new NotImplementedException(); } set { carousel.DataList = value; OnNewMessageArrive(); } }

    public int ChatCounter { get { throw new NotImplementedException(); } set { chatCounter.Count = value; } }
    public int IncomingCounter { get { throw new NotImplementedException(); } set { incomingCounter.Count = value; }}
    public string TitleText { get { throw new NotImplementedException(); } set { Title.text = value; } }
    public string TabInfoText { get { throw new NotImplementedException(); } set { lblTabInfo.text = value; } }
    public bool BtnOpenGuildIsVisible { get { throw new NotImplementedException(); } set { btnOpenGuildDialog.gameObject.SetActive(value); } }
    public bool InChatTab { get { throw new NotImplementedException(); } set { carousel.GotoLastPackMessage(); } }
    public bool LblTabInfoIsVisible { get { throw new NotImplementedException(); } set { lblTabInfo.gameObject.SetActive(value); } }
    public bool InputPanelIsVisible { get { throw new NotImplementedException(); }
        set
        {
            InputPanel.gameObject.SetActive(value);
            scrollRect.offsetMin = new Vector2(scrollRect.offsetMin.x, value ? scrollWithInput : scrollWithoutInput);
        }
    }


    public bool InGuild
    {
        get { throw new NotImplementedException(); }
        set
        {
            btnRequestHelp.gameObject.SetActive(value);
            guildIcon.ViewModel.Guild = ViewModel.GuildModel;
        }
    }

    private double NextHelpRequestWaitTime
    {
        get { throw new NotImplementedException(); }
        set
        {
            var active = value > 0.5f;
            timerIcon.SetActive(active);
            timerPanel.SetActive(active);
            lblTimer.SetTime(value);
        }
    }

    public override ViewType ViewType { get { return ViewType.Dialog; } }


    private float _hidePos = -500;
    private const float animDuratation = 0.0009f;
    private const float delayMsToShowOpenButtton = 0.45f;
    private const float scrollWithInput = 61.5f;
    private const float scrollWithoutInput = 3f;
    private const float percentPosToOpen = 0.3f;
    private RectTransform scrollRect;
    private MessagePool pool;
    
    private bool isHidinig = false;
    private bool emulateDrag = false;
    private bool isDraging = false;
    private float _aspect;
    private bool inputIsDeselected;

    private void Awake()
    {
        scrollRect = carousel.transform as RectTransform;
        _aspect = 640f/Screen.height;

        //#303617 Баг юнити. В WSA билде колесо мыши инвертировано 
#if UNITY_WSA && !UNITY_EDITOR
        var scroller = carousel.gameObject.GetComponent<ScrollRect>();
        scroller.scrollSensitivity *= -1;
#endif
    }

    public override void OnCreate()
    {
        base.OnCreate();
        pool = new MessagePool();

        carousel.Factory = this;
        carousel.onVisibleElementsChanged += OnVisibleElementsChanged;
        
        btnRequestHelp.gameObject.SetActive(ViewModel.InGuild);
		btnRequestHelp.onClick.AddListener(OnHelpRequestButtonClicked);
		btnChatTab.onValueChanged.AddListener(OnClickChatTab);
        btnIncomingTab.onValueChanged.AddListener(OnClickIncomingTab);
        btnOpenGuildDialog.onClick.AddListener(() => OpenGuildDialog(DialogGuildsViewModel.EContentCases.Join));
        
        _hidePos = window.anchoredPosition.x;
        InitCloseButton();
        InitInputField();

        var scenario = Use<IGuiManager>().CreateView<WaitingOpenMarketScreenScenario>();
        scenario.dialogToClose = this;
        Use<IScenarioService>().StartScenario(scenario);

        ViewModel.ShowChatMessage();
        carousel.GotoLastPackMessage();
    }

    protected override void PrepareBindings()
    {
        base.PrepareBindings();
        Bind(() => Messages, vm => vm.Messages);
        Bind(() => InGuild, vm => vm.InGuild);
        Bind(() => InChatTab, vm => vm.InChatTab);
        Bind(() => ChatCounter, vm => vm.ChatCounter);
        Bind(() => IncomingCounter, vm => vm.IncomingCounter);
        Bind(() => NextHelpRequestWaitTime, vm => vm.NextHelpRequestWaitTime);
        Bind(() => EmblemIsVisible, vm => vm.EmblemIsVisible);
        Bind(() => TitleText, vm => vm.TitleText);
        Bind(() => TabInfoText, vm => vm.TabInfoText);
        Bind(() => BtnOpenGuildIsVisible, vm => vm.BtnOpenGuildIsVisible);
        Bind(() => InputPanelIsVisible, vm => vm.InputPanelIsVisible);
        Bind(() => LblTabInfoIsVisible, vm => vm.LblTabInfoIsVisible);
        
    }

    public bool EmblemIsVisible
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            guildIcon.gameObject.SetActive(value);
        }
    }


    public void Show()
    {
        StartShowHideAnimation(true);
    }

    public void Hide()
    {
        if(isHidinig)
            return;

        isHidinig = true;
        var time = Mathf.Abs(delayMsToShowOpenButtton * (window.anchoredPosition.x - _hidePos));
        Use<ITimersService>().Subscribe(time, () => { OnHide(); }, true);
        StartShowHideAnimation(false, Close);
    }

    public void ShowWithEmulateDrag()
    {
        if(emulateDrag)
            return;

        emulateDrag = true;
        

        var pointer = new PointerEventData(EventSystem.current);
        pointer.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        btnClose.OnDrag(pointer);
    }

    public override void Update()
    {
        base.Update();

        if (!emulateDrag)
            return;

        if (Input.GetMouseButton(0))
        {
            var pointer = new PointerEventData(EventSystem.current);
            pointer.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            Drag(pointer);
        }

        if (Input.GetMouseButtonUp(0))
        {
            OnDragEnd(null);
        }
    }

    #region ICarouselItemFactory
    public IReinitable CreateCarouselItem(IData data)
    {
        IReinitable view = TryGetMessageFromPool(data) ?? CreateNewMessage(data);
        AnimateMessage(view);
        return view;
    }

    public void DisposeCarouselItem(IReinitable item)
    {
        pool.Put(item, item.Data.GetType());
    }

    public void OnCarouselItemUpdate(IReinitable item)
    {
        AnimateMessage(item);
    }
    #endregion


    private IReinitable TryGetMessageFromPool(IData data)
    {
        IReinitable view = pool.Get(data.GetType());
        if (view != null)
        {
            view.Data = data;
            var trans = (view as MonoBehaviour).transform;
            trans.SetParent(carousel.ScrollingPanel.transform);
        }
        return view;
    }

    private IReinitable CreateNewMessage(IData data)
    {
        IReinitable view = null;
        Type viewType = typeof(BlankMessageView);
        var attribute = data.GetType().GetAttribute<TypeMapAttribute>();
        if (attribute != null && attribute.Type != null)
        {
            viewType = attribute.Type;
        }
        var vmType = viewType.BaseType().GetGenericArguments().First();

        var vm = ViewModel.CreateMessageViewModel(vmType, data);
        view = Use<IGuiManager>().CreateView(viewType, carousel.ScrollingPanel, null, vm) as IReinitable;
        return view;
    }

    private void AnimateMessage(IReinitable view)
    {
        var v = view as IAnimatedMessage;
        if(v==null)
            return;

        var data = view.Data;
        
        if (ViewModel.AddedAnimationsMessagesId.Contains(data.Id))
        {
            v.StartShowAnimation();
            ViewModel.AddedAnimationsMessagesId.Remove(data.Id);
        }
        else
        if (ViewModel.DeletedAnimationsMessagesId.Contains(data.Id))
        {
            v.StartHideAnimation();
            ViewModel.DeletedAnimationsMessagesId.Remove(data.Id);
        }
    }

    private void InitInputField()
    {
        if (btnClearMessage != null) btnClearMessage.onClick.AddListener(ClearMessage);
        inputField.onEndEdit.AddListener(SendPublicMessage);

        var inputFieldFixed = inputField as InputFieldFixed;
        if (inputFieldFixed != null)
        {
            inputFieldFixed.onDeselect += () => { inputIsDeselected = true; };
        }
    }

    private void InitCloseButton()
    {
        var trigger = btnClose.gameObject.GetComponent<EventTrigger>();
        var entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((eventData) => { if(!isDraging && !emulateDrag) Hide(); });
        trigger.triggers.Add(entry);

        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.EndDrag;
        entry.callback.AddListener(OnDragEnd);
        trigger.triggers.Add(entry);

        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.Drag;
        entry.callback.AddListener(Drag);
        trigger.triggers.Add(entry);
    }

    public void OnInput(BlockerInputEventArgs eventData)
    {
        if (eventData.Type == EventTriggerType.PointerClick) Hide();
    }

    private void Drag(BaseEventData arg)
    {
        var data = arg as PointerEventData;
        var delta = 30;
        var mousePosX = data.position.x * _aspect;
        var newPos = mousePosX - window.sizeDelta.x + delta;
        newPos = Mathf.Min(0, newPos);
        window.anchoredPosition = new Vector2(newPos, window.anchoredPosition.y);
        isDraging = true;
    }

    private void OnDragEnd(BaseEventData arg0)
    {
        var size = window.sizeDelta.x;
        var pos = Mathf.Abs(window.anchoredPosition.x);
        var needOpen = emulateDrag ? 1f - pos/size > percentPosToOpen : pos/size < percentPosToOpen;
        emulateDrag = false;
        isDraging = false;

        if (needOpen)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void OnVisibleElementsChanged(List<IData> obj)
    {
        ViewModel.SetReadedMessages(obj);
    }

    private void OpenGuildDialog(DialogGuildsViewModel.EContentCases tab)
    {
        var v = Use<IGuiManager>().CreateView<DialogGuildsMediator>();
        v.ViewModel.ContentCases = tab;
        Hide();
    }

    private void OnNewMessageArrive()
    {
        carousel.AddItem();
    }

    private void OnClickChatTab(bool value)
    {
        if (!value) return;

        ViewModel.ShowChatMessage();
    }

    private void OnClickIncomingTab(bool value)
    {
        if (!value) return;

        ViewModel.ShowIncomingMessage();
    }

    private void OnHelpRequestButtonClicked()
    {
        if (ViewModel.HelpRequestAvailable)
            CreateHelpRequestDialog();
        else
            CreateHelpRequestWaitingDialog();
    }

    private void StartShowHideAnimation(bool show, TweenDelegate.TweenCallback OnCompliteAction = null)
    {
        var finishPos = show ? 0f : _hidePos;
        var time = Mathf.Abs(animDuratation*(window.anchoredPosition.x - finishPos));
        HOTween.To(window, time, new TweenParms().Prop("anchoredPosition", new Vector2(finishPos, 0)).OnComplete(OnCompliteAction));
    }


    private void ClearMessage()
    {
        inputField.text = string.Empty;
    }

    private void SendPublicMessage(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;
        if (inputField.wasCanceled)
            return;
        if (inputIsDeselected)
        {
            inputIsDeselected = false;
            return;
        }

        ViewModel.SendPublicMessage(text);
        ClearMessage();
    }

    private void CreateHelpRequestWaitingDialog()
    {
        var speedUpCompatible = new SimplySpeedUpCompatible
        {
            Cost = ViewModel.NextHelpRequestSpeedupWaitingCost,
            WaitTime = TimeSpan.FromMilliseconds(ViewModel.NextHelpRequestWaitTime)
        };
       SpeedUpHelper.Start(HelpRequestWaitingMessageKey, speedUpCompatible, ViewModel.PlayerModel,
            (e) => { ViewModel.SpeedUpHelpRequestWaiting();
                       CreateHelpRequestDialog(); });
    }

    private void CreateHelpRequestDialog()
    {
        Use<IGuiManager>().CreateView<HelpRequestMediator>();
    }

    protected override void OnDispose()
    {
        OnHide();
        pool.Dispose();
        base.OnDispose();
    }

    public override void Close()
    {
        base.Close();
        Dispose();
        if(gameObject) Destroy(gameObject);
    }

    public void OnBackButton()
    {
        Hide();
    }
};


public class WaitingOpenMarketScreenScenario : ScenarioBase<EmptyFakeVM>
{
    public IView dialogToClose;
    private OrScenarioStep openStep = new OrScenarioStep(new SimpleScenarioStep<DialogMarketMediatorBase>(StepCondition.ViewCreated), new SimpleScenarioStep<DialogGuildsMediator>(StepCondition.ViewCreated));

    public override IEnumerable<IScenarioStep> Scenario()
    {
        yield return new OrScenarioStep(
            new SimpleScenarioStep<ScreenGuildChatUguiView>(StepCondition.ViewClosed),
            openStep);

        
        if (dialogToClose != null && !dialogToClose.IsDisposed)
            dialogToClose.Close();
    }
}

