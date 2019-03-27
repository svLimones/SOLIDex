using System;
using System.Linq;
using Assets.Scripts.Gui.CrossPromo;
using Assets.Scripts.Gui.Dialog.Library;
using Dragonlands.Core.Config;
using Dragonlands.Gui;
using GameData.Definitions.LibraryUpgradesDefinition;
using LibraryUpgrades.Models;
using LibraryUpgrades.SharedLogic;
using UnityEngine;
using UnityEngine.UI;

[MediatorUgui]
[PlatformSpecificView(RunPlatform.All, typeof(DialogGradeResultView), "DialogGradeResult")]
public class DialogGradeResultView : View<DialogGradeResultViewModel>
{
    [SerializeField] private Text TitleLabel;
    [SerializeField] private Button OkButton;
    [SerializeField] private Button YesButton;
    [SerializeField] private Button NoButton;
    [SerializeField] private Button CloseButton;
    [SerializeField] private GameObject PanelStage0;
    [SerializeField] private GameObject PanelStage1;
    [SerializeField] private LibraryDragonGradeItemView GradeFrom;
    [SerializeField] private LibraryDragonGradeItemView GradeTo;
    [SerializeField] private LibraryDragonGradeItemView ResultGrade;
    [SerializeField] private GameObject FxGradeWin;
    [SerializeField] private GameObject FxGradeFail;
    [SerializeField] private GameObject FxBlinkRoll;
    [SerializeField] private Text WarningLabel;

    [SerializeField] private Button DontAskBoxButton;
    [SerializeField] private GameObject DontAskMark;
    [SerializeField] private GameObject DontAskRoot;

    [SerializeField] private Animator _animator;
    

    protected string TitleText { get { throw new NotImplementedException(); } set { TitleLabel.text = value; } }

    protected string WarningText { get { throw new NotImplementedException(); } set { WarningLabel.text = value; } }

    protected bool DontAsk
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            DontAskMark.SetActive(value);
        }
    }
    
    
    public bool TutorialFinished 
    {        
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            DontAskRoot.SetActive(value);
        } 
    }

    public Button YesBtn { get { return YesButton; } }
    public Button OkBtn { get { return OkButton; } }

    public override ViewType ViewType { get { return ViewType.Dialog; } }

    public override void OnCreate()
    {
        base.OnCreate();

        _animator.enabled = false;

        CloseButton.onClick.AddListener(Close);
        OkButton.onClick.AddListener(Close);
        YesButton.onClick.AddListener(OnYesClick);
        NoButton.onClick.AddListener(Close);
        DontAskBoxButton.onClick.AddListener(OnDontAskClick);

        PanelStage0.SetActive(true);
        PanelStage1.SetActive(false);
        ViewModel.ReInit();
    }

    protected override void PrepareBindings()
    {
        base.PrepareBindings();
        Bind(() => TitleText, vm => vm.TitleText);
        Bind(() => WarningText, vm => vm.WarningText);
        Bind(() => DontAsk, vm => vm.DontAsk);
        Bind(() => TutorialFinished, vm => vm.TutorialFinished);
    }

    private void OnYesClick()
    {
        ViewModel.UpgradeParameter();
        StartAnimation();
    }

    private void OnDontAskClick()
    {
        ViewModel.OnDontAsk();
    }

    public void StartAnimation()
    {
        PanelStage1.SetActive(true);
        CloseButton.interactable = false;
        OkButton.interactable = false;
        NoButton.interactable = false;
        YesButton.interactable = false;

        _animator.SetBool("isSucess", ViewModel.IsGradeSuccess);
        _animator.enabled = true;

        // sound
        if (ViewModel.IsGradeSuccess)
        {
            Use<IFxSoundManager>().SoundManager.Play("library_grade_win");
        }
        else
        {
            Use<IFxSoundManager>().SoundManager.Play("library_grade_lose");
        }

        Use<IEventDispatcher>().Publish(new LibraryUpgradeAnimationStartedEvent());
    }

    #region вызываются из триггеров в анимации
    private void StartSucessFx()
    {
        FxGradeWin.SetActive(true);
        FxBlinkRoll.SetActive(true);
    }

    private void StartFailFx()
    {
        FxGradeFail.SetActive(true);
    }

    private void OnAnimationFinish()
    {
        CloseButton.interactable = true;
        OkButton.interactable = true;

        // для тутора
        Use<IEventDispatcher>().Publish(new LibraryParameterUpgradeFinishedEvent());
    }
    #endregion
};



public class DialogGradeResultViewModel : ViewModelBase
{
    public DragonObjectModel Model { get { return model; } set { model = value; ReInit(); } }
    public LibraryUpgradeParameter GradeParamater { get { return gradeParamater; } set { gradeParamater = value; ReInit(); } }
    public LibraryUpgradeQuality Quality { get; set; }
    
    public bool TutorialFinished
    {
        get { return Use<IDataCenter>().Library.TutorialProgress == TutorialState.Finished; }
    }
    

    public bool IsGradeSuccess
    {
        get
        {
            return lastGradeLevel < CurrentGradeIndex;
        }
    }

    internal string TitleText { get { return gradeParamater==LibraryUpgradeParameter.LibraryUpgradeAttack ? Use<ILocale>().Get("attack_label") : Use<ILocale>().Get("dlf_dragon_view_health_caption"); } }
    internal string WarningText { get { return Use<ILocale>().Get("library_grade_warning_message", Model.LibraryUpgrades[GradeParamater].SafeLevel); } }

    private DragonObjectModel model = null;
    private LibraryUpgradeParameter gradeParamater = LibraryUpgradeParameter.LibraryUpgradeAttack;
    private int CurrentGradeIndex { get { return Model.LibraryUpgrades[GradeParamater].Level; } }
    private int lastGradeLevel = 0;

    public bool DontAsk
    {
        get
        {
            return PlayerPreferences.ShowLibraryUpgradeInfo;
        }
    }

    public DialogGradeResultViewModel(IServiceContainer container, IViewModelFactory factory) : base(container, factory)
    {
    }

    public void ReInit()
    {
        var isViewNotReady = !Children.Any();
        if (isViewNotReady)
            return;

        lastGradeLevel = CurrentGradeIndex;
        UpdateGradeItemsVm();
    }

    public void UpgradeParameter()
    {
        Parents.OfType<DialogLibraryViewModel>().First().Upgrade();
        UpdateResultGradeItemVm();
       
    }

    private void UpdateGradeItemsVm()
    {
        var gradeItems = Children.OfType<LibraryDragonGradeItemViewModel>().ToList();

        //порядок вьюх(Sibling Index) важен;
        var currentGrade = gradeItems[0];
        currentGrade.Model = Model;
        currentGrade.GradeParameter = GradeParamater;
        currentGrade.Level = CurrentGradeIndex;

        var nextGrade = gradeItems[1];
        nextGrade.Model = Model;
        nextGrade.GradeParameter = GradeParamater;
        nextGrade.Level = CurrentGradeIndex+1;

        var resultGrade = gradeItems[2];
        resultGrade.Model = Model;
        resultGrade.GradeParameter = GradeParamater;
        resultGrade.Level = CurrentGradeIndex;
        
        var eventPanels = Children.OfType<LibraryEventPanelVM>().ToList();
        eventPanels[0].Init(GradeParamater, Quality, CurrentGradeIndex + 1);
    }

    private void UpdateResultGradeItemVm()
    {
        var gradeItems = Children.OfType<LibraryDragonGradeItemViewModel>().ToList();
        gradeItems[2].Level = CurrentGradeIndex;
    }

    public void OnDontAsk()
    {
        PlayerPreferences.ShowLibraryUpgradeInfo = !PlayerPreferences.ShowLibraryUpgradeInfo;
        PropertyChanged(() => DontAsk);
    }
};
public class LibraryParameterUpgradeFinishedEvent :IEvent { }
public class LibraryUpgradeAnimationStartedEvent : IEvent { }