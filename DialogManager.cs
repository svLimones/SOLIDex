#region

using System;
using System.Collections.Generic;
using System.Linq;
using DebugLogs;
using UnityEngine;
using UWP;
using ViewCreation;
using Object = UnityEngine.Object;

#endregion

/// <summary>
///     Метод показа диалога
/// </summary>
public enum DialogShowMethod
{
    AddFront,
    CloseOthers,
    Queued
}

public interface IBlockerInputHandler
{
    void OnInput(BlockerInputEventArgs eventData);
};


public interface IDialogManager : IGuiMediatorManager
{

    void SetCurrentDialog(IView dialog);

    /// <summary>
    ///     Закрыть все отображаемые диалоги
    /// </summary>
    void CloseAll(bool clearQueue = true);

    /// <summary>
    ///     Закрыть диалог с медиатором <paramref name="dialogToClose" />
    /// </summary>
    /// <param name="dialogToClose">Медиатор закрываемого диалога</param>
    void CloseDialog(IView dialogToClose);

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T Find<T>() where T :class, IView;

    IView Find(Type type);
}

/// <summary>
///     Класс для работы с диалогами.
///     Предоставляет API для показа и удаления диалогов
/// </summary>
public class DialogManager : IDialogManager
{
    private GameObject _layerRootObject;
    private GameObject _layerRootObject3D;
    private IViewFactory _viewFactory;
    private GameObject _uGuiCanvas;

    private IGuiMediatorManager _screensManager;

    public event Action<IView> CurrentChanged;
    public event Action<IGuiMediatorManager, IView> OnOpenGuiMediator;
    public event Action<IGuiMediatorManager, IView> OnCloseGuiMediator;

    private Dictionary<IView, List<IView>> _screenOwnedDialogs = new Dictionary<IView, List<IView>>();
    private Queue<QueueData> _dialogQueue = new Queue<QueueData>();
    private UGuiBlockerView _uguiBlocker;
    private Stack<IView> _mediators3DStack = new Stack<IView>();
    private IView _current;



    public DialogManager(IViewFactory viewFactory, GameObject layerRootObject, GameObject layerRootObject3D, GameObject uGuiCanvas, IGuiMediatorManager screenManager)
    {
        _viewFactory = viewFactory;
        _layerRootObject = layerRootObject;
        _layerRootObject3D = layerRootObject3D;
        _uGuiCanvas = uGuiCanvas;

        _screensManager = screenManager;
        _screensManager.OnCloseGuiMediator += OnScreenClose;
    }


    public IView Current
    {
        get { return _current; }
        private set
        {
            _current = value;
            if (CurrentChanged != null) CurrentChanged(value);
        }
    }

    public void OnUguiMissClick(BlockerInputEventArgs eventData)
    {
        var current = Current as IBlockerInputHandler;
        if (current == null)
            return;

        current.OnInput(eventData);
    }

    /// <summary>
    ///     Показать диалог с медиатором TDialogClass
    /// </summary>
    /// <typeparam name="T">Медиатор показываемого диалога</typeparam>
    /// <param name="viewType">Тип вьюмодели</param>
    /// <param name="owner">открывающий скрин. Может быть null</param>
    /// <param name="method">Способ отображения диалога</param>
    /// <param name="onShown"> </param>
    /// <returns>Инстанс медиатора диалога</returns>
    public T ShowDialog<T>(ViewType viewType, IView owner = null, IViewModel viewModel = null, DialogShowMethod method = DialogShowMethod.AddFront,
        Action<T> onShown = null) where T :class, IView
    {
        Action<IView> onShowAction = null;
        if (onShown != null)
            onShowAction = a => onShown(a as T);
        return ShowDialog(typeof (T), viewType, owner, viewModel, method, onShowAction) as T;
    }

    public IView ShowDialog(Type type, ViewType viewType, IView owner = null, IViewModel viewModel = null, DialogShowMethod method = DialogShowMethod.AddFront, Action<IView> onShown = null)
    {
        D.Log(LoggingTags.Events, "Open Dialog: " + type);
        IView dialog = null;
        if (Current == null && method == DialogShowMethod.Queued)
        {
            method = DialogShowMethod.AddFront;
        }

        switch (method)
        {
            case DialogShowMethod.AddFront:
                dialog = CreateDialog(type, owner, viewModel);
                if (Current != null)
                {
                    dialog.Next = Current;
                }

                var currentScreen = _screensManager.Current;
                if (currentScreen != null)
                {
                    currentScreen.OnBlur();
                }

                break;
            case DialogShowMethod.CloseOthers:
                dialog = CreateDialog(type, owner, viewModel);
                if (Current != null)
                {
                    CloseAll();
                }
                break;
            case DialogShowMethod.Queued:
                if (Current != null)
                {
                    _dialogQueue.Enqueue(new QueueData
                    {
                        mOpenDialogAction = () => ShowDialog(type, viewType, owner, viewModel, DialogShowMethod.AddFront, onShown),
                        mOwnerScreen = owner
                    });
                    return null;
                }
                dialog = CreateDialog(type, owner, viewModel);
                break;
        }

        AfterCreateDialog(viewType, onShown, dialog);

        return dialog;
    }

    public void SetCurrentDialog(IView dialog)
    {
        dialog.Next = Current;
        Current = dialog;
        dialog.CloseHandler -= CloseDialog;
        dialog.CloseHandler += CloseDialog;
    }

    private void AfterCreateDialog(ViewType viewType, Action<IView> onShown, IView dialog )
    {
        if (viewType == ViewType.DropDownDialog)
        {
            Current = dialog;
        }
        else
        {
            if (dialog != null && IsMediator3D(dialog.GetType()))
                ShowDialog3D(dialog);
            Current = dialog;
        }

        if (OnOpenGuiMediator != null)
        {
            OnOpenGuiMediator(this, dialog);
        }

        if (onShown != null)
        {
            onShown(dialog);
        }
    }

    private void ShowDialog3D(IView mediator)
    {
        if (_mediators3DStack.Count > 0)
        {
            var currentMediator = _mediators3DStack.Peek();
            currentMediator.Component.gameObject.SetActive(false);
        }

        _mediators3DStack.Push(mediator);
    }

    private void HideCurrent3dMediator()
    {
        if (_mediators3DStack.Count == 0)
            return;

        _mediators3DStack.Pop();
        if (_mediators3DStack.Count == 0)
            return;
        _mediators3DStack.Peek().Component.gameObject.SetActive(true);
    }


    private void ClearStack3D()
    {
        if (_mediators3DStack.Count > 0)
            _mediators3DStack.Clear();
    }

    private void CloseAllUguiDialogs()
    {
        if (_uGuiCanvas == null)
            return;

        for (var i = 0; i < _uGuiCanvas.transform.childCount; i++)
        {
            var child = _uGuiCanvas.transform.GetChild(i).gameObject;
            if (child.tag == "DontDestroy")
                continue;

            Object.Destroy(child);
        }
    }
    
    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private IView CreateDialog(Type type, IView owner, IViewModel viewModel = null)
    {
        var root = IsMediator3D(type) ? _layerRootObject3D : _layerRootObject;
        if (type.HasAttribute<MediatorUguiAttribute>())
        {
            root = _uGuiCanvas;
        }

        var dialog = _viewFactory.Create(
            type,
            root,
            viewModel,
            null,
            CreationOptions.GuiCreation | CreationOptions.CreateGameObject
            );

        dialog.ClearCloseHandler();
        dialog.CloseHandler += CloseDialog;

        if (owner != null)
        {
            if (!_screenOwnedDialogs.ContainsKey(owner))
            {
                _screenOwnedDialogs.Add(owner, new List<IView>());
            }
            _screenOwnedDialogs[owner].Add(dialog);
        }

        return dialog;
    }

    private bool IsMediator3D(Type type)
    {
        var mediator3DAttributeType = typeof (Mediator3D);
        var attributes = type.GetCustomAttributes(mediator3DAttributeType, false);
        return attributes.Any();
    }
    
    /// <summary>
    ///     Закрыть все отображаемые диалоги
    /// </summary>
    public void CloseAll(bool clearQueue = true)
    {
        if (clearQueue)
            _dialogQueue.Clear();

        var dialog = Current;
        while (dialog != null)
        {
            var next = dialog.Next;
            if (dialog is ILoadingDialog == false)
            {
                CloseDialog(dialog);
            }
            dialog = next;
        }

        ClearStack3D();
        CloseAllUguiDialogs();

    }

    /// <summary>
    ///     Закрыть диалог с медиатором <paramref name="dialogToClose" />
    /// </summary>
    /// <param name="dialogToClose">Медиатор закрываемого диалога</param>
    public void CloseDialog(IView dialogToClose)
    {
        // Обязательно переписать
        if (IsMediator3D(dialogToClose.GetType()))
        {
            HideCurrent3dMediator();
        }
        // Обязательно переписать

        if (Current == dialogToClose)
        {
            Current = dialogToClose.Next;
        }
        else
        {
            // Склеиваем очередь диалогов если закрыт не последний
            IView prevWindow;
            IView window;
            if (Current != null)
                for (window = Current.Next, prevWindow = Current;
                    window != null;
                    prevWindow = window, window = window.Next)
                    if (window == dialogToClose)
                        prevWindow.Next = window.Next;
        }
        
        
        if (OnCloseGuiMediator != null)
            OnCloseGuiMediator(this, dialogToClose);
        foreach (var screenDialogs in _screenOwnedDialogs)
            screenDialogs.Value.Remove(dialogToClose);
        _viewFactory.DestroyView(dialogToClose);

        
        var currentScreen = _screensManager.Current;
        if (currentScreen != null)
        {
            if (Current == null)
            {
                currentScreen.OnFocuse();
            }
        }

        if (Current == null)
        {
            while (_dialogQueue.Count > 0)
            {
                // case 1 есть еще диалоги в очереди
                //try find
                var data = _dialogQueue.Dequeue();
                if (data.mOwnerScreen == null || data.mOwnerScreen == _screensManager.Current)
                {
                    if (data.mOpenDialogAction != null)
                        data.mOpenDialogAction();
                    if (Current == null)
                        continue;
                    break;
                }
            }
        }

        UIDrawCall.ReleaseInactive();
    }

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T Find<T>() where T : class, IView
    {
        var window = Current;
        while (window != null)
        {
            if (window is T)
                return (T) window;
            window = window.Next;
        }
        return null;
    }

    public IView Find(Type type)
    {
        if (Current == null)
            return null;
        var window = Current;
        while (window != null)
        {
            if (window.Component.GetComponent(type) != null)
                return window;
            window = window.Next;
        }
        return null;
    }


    private void OnScreenClose(IGuiMediatorManager source, IView screen)
    {
        if (!_screenOwnedDialogs.ContainsKey(screen))
            return;
        _screenOwnedDialogs[screen].ForEach(CloseDialog);
        _screenOwnedDialogs.Remove(screen);
    }

    private class QueueData
    {
        public Action mOpenDialogAction;
        public IView mOwnerScreen;
    }
}
