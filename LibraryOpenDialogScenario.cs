using System.Collections.Generic;
using Scripts.Gui.Dialog.Wrappers;


class LibraryOpenDialogScenario : ScenarioBase<LibraryOpenDialogScenarioViewModel>
{
    private IDragonElementData _selectedDragon;

    public override IEnumerable<IScenarioStep> Scenario()
    {
        var vm = ViewModel.CreateDragonSelectionVm();
        var selectionDialog = Use<IGuiManager>().CreateView<DialogDragonSelectionMediator>(null, null, vm);
        selectionDialog.OnSelected += OnDragonSelected;
        yield return new SimpleScenarioStep<DialogDragonSelectionMediator>(StepCondition.ViewClosed);
        if (_selectedDragon != null)
        {
            OpenDialogLibrary();
        }
    }

    private void OnDragonSelected(DialogDragonSelectionMediator mediator)
    {
        _selectedDragon = mediator.SelectedItem;
    }

    private void OpenDialogLibrary()
    {
        var vm = ViewModel.CreateChildViewModel<DialogLibraryViewModel>();
        vm.DragonModel = DataCenter.ModelsPool.GetObjectModel(_selectedDragon.Id) as DragonObjectModel;
        Use<IScenarioService>().StartScenario<LibraryTutorialScenario>();
        Use<IGuiManager>().CreateView<DialogLibraryView>(null, null, vm);
    }
}
