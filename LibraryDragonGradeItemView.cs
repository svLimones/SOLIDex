using System;
using System.Linq;
using UnityEngine;
using Dragonlands.Core.Config;
using Dragonlands.Gui;
using GameData.Definitions.LibraryUpgradesDefinition;
using UnityEngine.UI;

[MediatorUgui]
[PlatformSpecificView(RunPlatform.All, typeof(LibraryDragonGradeItemView), "LibraryDragonGradeItem")]
public class LibraryDragonGradeItemView : View<LibraryDragonGradeItemViewModel>
{
    [SerializeField] private Image Icon;
    [SerializeField] private GameObject SafeIcon;
    [SerializeField] private Text LevelLabel;
    [SerializeField] private Image LevelIcon;
    [SerializeField] private Text ParameterValueLabel;
    [SerializeField] private UGUIAtlas Atlas;
    [SerializeField] private float borderHeightActive = 115f;
    [SerializeField] private float borderHeightInactive = 108f;
    [SerializeField] private Image SelectedBorderImage;
    [SerializeField] private LayoutElement SelectedBorder;
    [SerializeField] private Image Flame;

    public override ViewType ViewType { get { return ViewType.ChildComponent;} }

    protected bool IsSafeLevel { get { throw new NotImplementedException(); }  set { SafeIcon.SetActive(value); } }
    protected string IconName { get { throw new NotImplementedException(); }  set { Icon.overrideSprite = Atlas.GetSpriteByName(value); } }
    protected Color IconColor { get { throw new NotImplementedException(); }  set { LevelIcon.color = value; }}
    protected Color FlameColor { get{ throw new NotImplementedException(); }  set { Flame.color = value; }}
    protected string ParameterValue { get { throw new NotImplementedException(); } set { ParameterValueLabel.text = value; } }
    protected bool IsCurrentGrade { get { throw new NotImplementedException(); } set { ShowSelectedBorder(value); } }
    protected bool ShowLevel { get { throw new NotImplementedException(); } set { LevelIcon.gameObject.SetActive(value); } }
    protected int Level { get { throw new NotImplementedException(); } set { LevelLabel.text = "+"+value;} }


    protected override void PrepareBindings()
    {
        base.PrepareBindings();
        Bind(() => IsSafeLevel, vm => vm.IsSafeLevel);
        Bind(() => IconName, vm => vm.IconName);
        Bind(() => IconColor, vm => vm.IconColor);
        Bind(() => FlameColor, vm => vm.FlameColor);
        Bind(() => ParameterValue, vm => vm.ParameterValue);
        Bind(() => IsCurrentGrade, vm => vm.IsCurrentGrade);
        Bind(() => ShowLevel, vm => vm.ShowLevel);
        Bind(() => Level, vm => vm.Level);
    }

   

    public void ShowSelectedBorder(bool active=true)
    {
        if (SelectedBorder == null)
            return;

        SelectedBorder.minHeight = active ? borderHeightActive : borderHeightInactive;
        SelectedBorderImage.enabled = active;
    }
};

public class LibraryDragonGradeItemViewModel : ViewModelBase
{
    public DragonObjectModel Model { get { return model; } set { model = value; ReInit(); } }
    public LibraryUpgradeParameter GradeParameter { get { return gradeParameter; } set { gradeParameter = value; ReInit(); } }
    public int Level { get { return level; } set { level = Math.Min(value,MaxLevel); ReInit(); }  }

    private int MaxLevel
    {
        get
        {
            return _maxLevel > 0
                ? _maxLevel
                : _maxLevel =
                    Use<IDataCenter>().Definitions.LibraryUpgrades.Parameters[GradeParameter].Levels.Keys.Max();
        }

    }

    internal string IconName { get { return Def.IconName; } }
    internal bool IsSafeLevel { get { return Level > 0 && Def.IsSafe; } }
    internal bool ShowLevel { get { return Level > 0; } }
    internal Color IconColor { get { return Def.Color; } }
    internal Color FlameColor { get { return Def.FlameColor; } }
    internal string ParameterValue { get { return CalculateParametervalue().ToString(); } }
    internal bool IsCurrentGrade { get { return Model != null && Level == Model.LibraryUpgrades[GradeParameter].Level; } }

    private LibraryUpgradeLevelDefinition Def { get { return Use<IDataCenter>().Definitions.LibraryUpgrades.Parameters[GradeParameter].Levels[Level]; }  }
    private DragonObjectModel model;
    private bool hideLevel;
    private int level = 0;
    private LibraryUpgradeParameter gradeParameter = LibraryUpgradeParameter.LibraryUpgradeAttack;
    private int _maxLevel;

    public LibraryDragonGradeItemViewModel(IServiceContainer container, IViewModelFactory factory) : base(container, factory) { }


    private void ReInit()
    {
        PropertyChanged(() => IsSafeLevel);
        PropertyChanged(() => IconColor);
        PropertyChanged(() => FlameColor);
        PropertyChanged(() => IconName);
        PropertyChanged(() => ParameterValue);
        PropertyChanged(() => IsCurrentGrade);
        PropertyChanged(() => Level);
        PropertyChanged(() => ShowLevel);
    }

    public int CalculateParametervalue()
    {
        if (Model == null)
            return 0;

        var baseValue = GetBaseParamaterValue();
        var value = Model.GetLibraryModifier(GradeParameter, Level);
        return (int)(baseValue*value);
    }

    private double GetBaseParamaterValue()
    {
        if(GradeParameter == LibraryUpgradeParameter.LibraryUpgradeHealth)
        {
            return Model.HealthBase* Model.HealthAmuletMultiplier * Model.HealthTokenMultiplier;
        }
        else
        {
            return Model.AttackBase * Model.AttackAmuletMultiplier * Model.AttackTokenMultiplier;
        }
    }
};
