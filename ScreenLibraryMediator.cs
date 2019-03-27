using System;
using System.Collections.Generic;
using UnityEngine;
using Dragonlands.Gui;
using Dragonlands.Core.Config;
using GameInput;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[MediatorUgui(alpha:0)]
[PlatformSpecificView(RunPlatform.All, typeof(ScreenLibraryMediator), "ScreenLibrary")]
class ScreenLibraryMediator : ScreenMapObjectUguiMediator, IBlockerInputHandler
{
    private const string LibraryLocale = "library_button";
    private const string MoveLocale = "move_button";
    private const string InfoLocale = "info_button";
    private const string HeaderLocale = "library_name";

    [SerializeField]
    private Button _libraryButton;
    [SerializeField]
    private Button _moveButtton;
    [SerializeField]
    private Button _infoButton;
    [SerializeField]
    private Text _libraryLabel;
    [SerializeField]
    private Text _moveLabel;
    [SerializeField]
    private Text _infoLabel;
    [SerializeField]
    private Text _headerLabel;

    public override void Init(IMapObjectView mapObjectView)
    {
        base.Init(mapObjectView);
        _libraryButton.onClick.AddListener(OpenLibrary);
        _moveButtton.onClick.AddListener(OnMoveClick);
        _infoButton.onClick.AddListener(OnInfoClick);
        Localize();
    }

    private void Localize()
    {
        var locale = Use<ILocale>();
        _libraryLabel.text = locale.Get(LibraryLocale);
        _moveLabel.text = locale.Get(MoveLocale);
        _infoLabel.text = locale.Get(InfoLocale);
        _headerLabel.text = locale.Get(HeaderLocale);
    }

    private void OpenLibrary()
    {
        var scenario = Use<IGuiManager>().CreateView<LibraryOpenDialogScenario>();
        Use<IScenarioService>().StartScenario(scenario);
    }


    public void OnInput(BlockerInputEventArgs e)
    {
        switch (e.Type)
        {
            case EventTriggerType.PointerDown:
                Use<IGuiManager>().AllowedInput = ScreenInput.Ngui;
            break;
            case EventTriggerType.Scroll:
                Use<IInputAdapter>().GenerateScrollEvent(e.EventData.scrollDelta.y / 10f);
            break;
        }
        
    }
}

