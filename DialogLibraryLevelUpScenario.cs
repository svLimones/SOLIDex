using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Gui.Dialog.Library;
using Common.Models;
using GameData.Definitions.LibraryUpgradesDefinition;
using LibraryUpgrades.Models;
using Tools;

public class DialogLibraryLevelUpScenario : ScenarioBase<DialogLibraryLevelUpScenarioViewModel>
{
    public DialogLibraryView TargetView { get; set; }

    public override IEnumerable<IScenarioStep> Scenario()
    {
        TargetView.ViewModel.Children.OfType<LibraryEventPanelVM>().ForEach(p=>p.ForceHide = true);
        
        var upgradeState = TargetView.ViewModel.DefineUpgradeState(TargetView.ViewModel.Quality);

        if (upgradeState == ErrorCode.NotEnoughResources)
        {
            // нужно больше ресурсов
            Use<IMarketController>().ShowNotEnoughCurrencyDialog(TargetView.ViewModel.LevelUpCurrency);
            yield break;
        }

        if (TargetView.ViewModel.Quality == LibraryUpgradeQuality.Splendid && PlayerPreferences.BuyConfirmation && ViewModel.TutorialFinished)
        {
            // для реала нужно вызвать диаложку подтверждения покупки
            var view = Use<IGuiManager>().CreateView<DialogYesNoMediator>();
            view.InitYesNo(
                Locale.Get("library_upgrade"),
                Locale.Get("library_upgrade_confirm_for_real"),
                Locale.Get("rerandom_reward_text_2"),
                TargetView.ViewModel.LevelUpForRealGameCurrency,
                Locale.Get("confirmation_ask_description"),
                true
            );
            yield return new SimpleScenarioStep<DialogYesNoMediator>(StepCondition.ViewClosed);
            if (view.Result == false)
            {
                Close();
                yield break;
            }
        }
        
        // показываем диалог по апгрейду
        var vm = TargetView.ViewModel.CreateDialogGrageResultVm();
        var v = Use<IGuiManager>().CreateView<DialogGradeResultView>(null, null, vm);

        // нужно ли автоматически пропустить окно по апдейту или нет?
        if (!PlayerPreferences.ShowLibraryUpgradeInfo)
        {
            // вызовем команду шаред логики по прокачке параметра
            TargetView.ViewModel.Upgrade();
            // покажем эффект, после которого диалог по апгрейду автоматически закроется
            v.StartAnimation();
            vm.ReInit();
        }
        
        yield return new SimpleScenarioStep<DialogGradeResultView>(StepCondition.ViewClosed);
        TargetView.ViewModel.ReInitEventPanels();
    }

    protected override void OnDispose()
    {
        TargetView.ViewModel.Children.OfType<LibraryEventPanelVM>().ForEach(p=>p.ForceHide = false);
        base.OnDispose();
    }
}
