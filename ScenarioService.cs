using System;
using System.Collections.Generic;
using System.Linq;
using DebugLogs;
using Tools;


public interface IScenarioService : IViewService, IResettable
{
    void OnViewCreated(IView res);
    void OnViewDestroyed(IView dialogToClose);
    void StartScenario(IScenario view);
    void StartScenario<T>(IViewModel viewModle=null) where T :class, IView, IScenario;
    bool ScenarioIsReady(Type type);
    void CheckCustomCondition();
    void StopScenario<T>();
};


[CreateMonoObject("ScenarioService")]
public class ScenarioService : ServiceBaseMonoBehaviour, IScenarioService
{
    private readonly Dictionary<Type, IScenario>  _currentScenarios=new Dictionary<Type, IScenario>();
    private readonly Dictionary<Type, IEnumerator<IScenarioStep>> _currentScenarioSteps=new Dictionary<Type, IEnumerator<IScenarioStep>>();

    private string _currentScenarioName;

    public void StartScenario<T>(IViewModel viewModle = null) where T :class, IView, IScenario
    {
        var view= Use<IGuiManager>().CreateView<T>(null, null, viewModle);
        StartScenario(view);
    }
    public void StartScenario(IScenario view)
    {
        if (_currentScenarios.ContainsKey(view.GetType()))
        {
            D.LogWarning(LoggingTags.Services,
                string.Format("scenario {0} overriden by {1}", _currentScenarios.GetType(), view.GetType()));
            CleanScenario(view.GetType());
        }
        _currentScenarios[view.GetType()] = view;
        _currentScenarioSteps[view.GetType()] = view.Scenario().GetEnumerator();
        _currentScenarioName = view.GetType().ToString();
        D.Log(LoggingTags.Services, "=> Start scenario: " + _currentScenarioName);
        MoveNextStep(view.GetType());
    }
    public void OnViewCreated(IView view)
    {
        foreach (var key in _currentScenarios.Keys.ToList())
        {
            if (!ScenarioIsReady(key))
                continue;
            ProcessScenarioStep(key,view, StepCondition.ViewCreated);
        }
        
    }   

    public void OnViewDestroyed(IView view)
    {
        foreach (var key in _currentScenarios.Keys.ToList())
        {
            if (!ScenarioIsReady(key))
                continue;
            ProcessScenarioStep(key, view, StepCondition.ViewClosed);
        }
    }

    public bool ScenarioIsReady(Type type)
    {
        return _currentScenarios.ContainsKey(type)
               && !_currentScenarios[type].IsBusy
               && !_currentScenarios[type].IsDisposed
               && _currentScenarioSteps[type].Current != null;
    }

    public void CheckCustomCondition()
    {
        foreach (var key in _currentScenarios.Keys.ToList())
        {
            ProcessScenarioStep(key,null, StepCondition.Custom);
        }
    }

    private void Update()
    {
        foreach (var key in _currentScenarios.Keys.ToList())
        {
            ProcessScenarioStep(key, null, StepCondition.NextFrame);
        }
    }

    public void StopScenario<T>()
    {
        CleanScenario(typeof(T));
    }

    //private bool ViewIsScreenOrDialog( ViewBase view )
    //{
    //    return true;
    //    //return view.ViewType == ViewType.Dialog || view.ViewType == ViewType.Screen||view.ViewType==ViewType.ChildComponent; 
    //}
    
    private void ProcessScenarioStep(Type key, IView view, StepCondition condition)
    {
        var step = _currentScenarioSteps[key].Current;
        if (step == null)
        {
            // добавлена эта проверка для вот какого кейса:
            // если у нас работают 2 сценария одновременно, в одном нету елдов, другой сценарий ждет евенты и когда он их получает,
            // евенты вынуждают проверять текущий стип у всех сценариев, в сценарии где нету елдов стип будет нуллом, отсюда нуллреф
            return;
        }
        var stepAffectCondition = new StepAffectCondition(condition, view==null?null: view.GetType());
        if (step.IsStepAffected(stepAffectCondition) )
        {
            step.SetViewForCondition(stepAffectCondition, view);
            MoveNextStep(key);
        }

    }


    private void MoveNextStep(Type key)
    {
        try
        {
            _currentScenarios[key].IsBusy = true;
            var result = _currentScenarioSteps[key].MoveNext();
            if (!result)
            {
                CleanScenario(key);
            }
            else
            {
                _currentScenarios[key].IsBusy = false;
            }
        }
        catch (Exception e)
        {
            D.LogError(LoggingTags.Services,
                string.Format("scenario {0} execution error during one of steps. Internal exception: {1}",
                    _currentScenarios[key].GetType(), e));
            CleanScenario(key);
        }
    }


    private void CleanScenario(Type type)
    {
        D.Log(LoggingTags.Services, "<= Finish scenario: " + _currentScenarioName);
        _currentScenarioSteps[type].Dispose();
        _currentScenarios[type].Dispose();
        _currentScenarios.Remove(type);
        _currentScenarioSteps.Remove(type);
    }

    //public bool AllowDestroyGui(Type type)
    //{
    //    return _currentScenario == null || _currentScenario.CanDestroyView(type);
    //}
    public void Reset()
    {
        _currentScenarioSteps.ForEach(p=>p.Value.Dispose());
        _currentScenarios.ForEach(p => p.Value.Dispose());
        _currentScenarioSteps.Clear();
        _currentScenarios.Clear();
    }
}