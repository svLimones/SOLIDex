using System;
using System.Collections.Generic;
using System.Linq;

public class OrScenarioStep : IScenarioStep
{
    public List<IScenarioStep> Steps;

    public OrScenarioStep(params IScenarioStep[] steps )
    {
        Steps = steps.ToList();
    }

    public bool IsStepAffected( StepAffectCondition condition )
    {
        return Steps.Any( a => a.IsStepAffected(condition));
    }

    public void SetViewForCondition( StepAffectCondition condition, IView view )
    {
        foreach( var scenarioStep in Steps.Where( scenarioStep => scenarioStep.IsStepAffected( condition ) ) )
        {
            scenarioStep.SetViewForCondition( condition, view );
        }
    }

    [Obsolete("dont use view for multiple conditional step")]
    public IView View { get { return null; } }

}