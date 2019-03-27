using System;
using System.Linq;
using UnityEngine;
using Dragonlands.Core.Config;
using Dragonlands.Gui;
using GameData.Definitions.LibraryUpgradesDefinition;
using UnityEngine.UI;

[MediatorUgui]
[PlatformSpecificView(RunPlatform.All, typeof(LibraryDragonGradePanelView), "LibraryDragonGradePanel")]
public class LibraryDragonGradePanelView : View<LibraryDragonGradePanelViewModel>
{
    [SerializeField] private DragonInfoView DragonInfo;
    [SerializeField] private LibraryGradeListPopupView GradeListPopup;
    [SerializeField] private Button SelectDragonButton;
    [SerializeField] private Button InfoButton;
    [SerializeField] private LibraryDragonGradeItemView Grade0;
    [SerializeField] private LibraryDragonGradeItemView Grade1;
    [SerializeField] private LibraryDragonGradeItemView Grade2;
    [SerializeField] private GameObject MaxLevelLabel;
    [SerializeField] private GameObject Arrow0;
    [SerializeField] private GameObject Arrow1;

    public override ViewType ViewType { get { return ViewType.ChildComponent; } }
    public event Action onClickChangeDragon = delegate { };

    protected bool IsMaxGradeLevel
    {
        get { throw new NotImplementedException(); }
        set
        {
            Grade1.gameObject.SetActive(!value);
            MaxLevelLabel.gameObject.SetActive(value);
            Arrow0.SetActive(!value);
        }
    }

    protected bool IsLastGradeLevel
    {
        get { throw new NotImplementedException(); }
        set
        {
            Grade2.gameObject.SetActive(!value);
            Arrow1.SetActive(!value);
        }
    }

    

    public override void OnCreate()
    {
        base.OnCreate();
        SelectDragonButton.onClick.AddListener(OpenSelectDragonWindow);
        InfoButton.onClick.AddListener(ShowInfoWindow);
        ViewModel.ReInit();
        GradeListPopup.Hide();
    }

    protected override void PrepareBindings()
    {
        base.PrepareBindings();
        Bind(() => IsLastGradeLevel, vm => vm.IsLastGradeLevel);
        Bind(() => IsMaxGradeLevel, vm => vm.IsMaxGradeLevel);
    }

    private void OpenSelectDragonWindow()
    {
        onClickChangeDragon();
    }

    public void ShowInfoWindow()
    {
        GradeListPopup.Show();
    }
};


public class LibraryDragonGradePanelViewModel : ViewModelBase
{
    public DragonObjectModel Model
    {
        get { return model; }
        set
        {
            model = value;
            Binder.Release();
            Binder.BindBranch(Model, p => p.LibraryUpgrades, ReInit);
            ReInit();
        }
    }

    public LibraryUpgradeParameter GradeParamater { get { return gradeParamater; } set { gradeParamater = value; ReInit(); } }

    internal bool IsMaxGradeLevel { get { return CurrentGradeIndex == gradesCount - 1; } }
    internal bool IsLastGradeLevel { get { return CurrentGradeIndex >= gradesCount - 2; } }
    private DragonObjectModel model = null;
    private LibraryUpgradeParameter gradeParamater = LibraryUpgradeParameter.LibraryUpgradeAttack;
    private LibraryUpgradeParameterDefinition gradeListDef { get { return Use<IDataCenter>().Definitions.LibraryUpgrades.Parameters[GradeParamater]; } }
    private int gradesCount { get { return gradeListDef.Levels.Count; } }
    private int CurrentGradeIndex { get { return Model.LibraryUpgrades[GradeParamater].Level; } }
    private int NextGradeIndex { get { return Model.LibraryUpgrades[GradeParamater].NextLevel; } }

    public LibraryDragonGradePanelViewModel(IServiceContainer container, IViewModelFactory factory) : base(container, factory)
    {
        model = Use<IDataCenter>().ModelsPool.Dragons[0];
    }

    public void ReInit()
    {
        PropertyChanged(() => IsMaxGradeLevel);
        PropertyChanged(() => IsLastGradeLevel);

        var isViewNotReady = !Children.Any();
        if (isViewNotReady)
            return;

        UpdateDragonInfoVm();
        UpdateGradeListVm();
        UpdateGradeItemsVm();
    }

    private void UpdateDragonInfoVm()
    {
        var dragonInfoVm = Children.OfType<DragonInfoViewModel>().First();
        dragonInfoVm.Model = Model;
    }

    private void UpdateGradeListVm()
    {
        var gradeListVm = Children.OfType<LibraryGradeListPopupViewModel>().First();
        gradeListVm.Model = Model;
        gradeListVm.GradeParamater = GradeParamater;
    }

    private void UpdateGradeItemsVm()
    {
        var isMaxGradeLevel = IsMaxGradeLevel;
        var gradeItems = Children.OfType<LibraryDragonGradeItemViewModel>().ToList();
        gradeItems[0].Model = Model;
        gradeItems[0].GradeParameter = GradeParamater;
        gradeItems[0].Level = CurrentGradeIndex;

        if (isMaxGradeLevel)
            return;

        gradeItems[1].Model = Model;
        gradeItems[1].GradeParameter = GradeParamater;
        gradeItems[1].Level = NextGradeIndex;

        if (IsLastGradeLevel)
            return;

        gradeItems[2].Model = Model;
        gradeItems[2].GradeParameter = GradeParamater;
        gradeItems[2].Level = gradeListDef.Levels.Count - 1;
    }

};