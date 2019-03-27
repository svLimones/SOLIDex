using System;
using UnityEngine;
using UnityEngine.UI;

public class DragonInfoView : View<DragonInfoViewModel>
{
    [SerializeField] private RawImage AvatarImage;
    [SerializeField] private Text NameLabel;
    [SerializeField] private Text DragonTypeLabel;
    [SerializeField] private Text LevelLabel;
    [SerializeField] private GameObject Throbber;
    public override ViewType ViewType { get { return ViewType.ChildComponent; } }

    private string Name { get { throw new NotImplementedException(); } set { if (NameLabel != null) NameLabel.text = value; } }
    private string DragonType { get { throw new NotImplementedException(); } set { if (DragonTypeLabel == null) return; DragonTypeLabel.text = Use<ILocale>().Get(value); } }
    private int Level { get { throw new NotImplementedException(); } set { if (LevelLabel != null) LevelLabel.text = value.ToString(); } }

    public string Avatar
    {
        get { throw new NotImplementedException(); }
        set
        {
            if (Throbber) Throbber.SetActive(true);
            AvatarImage.texture = null;
            if (string.IsNullOrEmpty(value))
                return;

            Use<IResourceStorage>().Get<Texture>(value, this, OnAvatarLoaded);
        }
    }


    protected override void PrepareBindings()
    {
        base.PrepareBindings();
        Bind(() => Name, vm => vm.Name);
        Bind(() => DragonType, vm => vm.DragonType);
        Bind(() => Level, vm => vm.Level);
        Bind(() => Avatar, vm => vm.Avatar);
    }

    private void OnAvatarLoaded(Texture tex)
    {
        if (Throbber) Throbber.SetActive(false);
        AvatarImage.texture = tex;
    }
};



public class DragonInfoViewModel : ViewModelBase
{
    public DragonObjectModel Model { get { return _model; } set { _model = value; ReInit(); } }
    
    internal string DragonType { set { throw new NotImplementedException(); } get { return Def.Name; } }
    internal string Name { set { throw new NotImplementedException(); }  get { return Model.Name; } }
    internal int Level { set { throw new NotImplementedException(); } get { return Model.Level; } }
    internal string Avatar {  set { throw new NotImplementedException(); } get { return Def.IconNameId; } }

    private DragonObjectModel _model;
    private DragonDefinition Def { get { return Model.Definition; }
    }


    public DragonInfoViewModel(IServiceContainer container, IViewModelFactory factory) : base(container, factory)
    {
        _model = Use<IDataCenter>().ModelsPool.Dragons[0];
    }

    private void ReInit()
    {
        PropertyChanged(() => DragonType);
        PropertyChanged(() => Name);
        PropertyChanged(() => Level);
        PropertyChanged(() => Avatar);
    }


};
