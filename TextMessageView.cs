using System;
using Dragonlands.Core.Config;
using Dragonlands.Gui;
using GuildChat.uGUI;
using Guilds.Models;
using Guilds.Models.Messages.Chat;
using uGUI.Carousel;
using UnityEngine;
using UnityEngine.UI;

namespace Guilds
{
    [MediatorUgui]
    [RequireComponent(typeof(Button))]
    [PlatformSpecificView(RunPlatform.All, typeof(TextMessageView), "TextMessage")]
    public class TextMessageView : MessageBaseView<TextMessageViewModel, TextMessage>, IThrowClickable
    {
        [SerializeField] private Text messageLabel;
        [SerializeField] private UserPanelView userPanel;
        [SerializeField] private CanvasRenderer leftArrow;
        [SerializeField] private CanvasRenderer rightArrow;
        [SerializeField] private LayoutElement blank;

        public UserId UserId { get { throw new NotImplementedException(); } set { userPanel.ViewModel.UserId = value; userPanel.ViewModel.IsMember = true; } }
        public string MessageText {  get { throw new NotImplementedException(); } set { messageLabel.text = value; } }
        public bool IsMineMessage
        {
            get { throw new NotImplementedException(); }
            set
            {
                blank.minWidth = value? 15 : 0;
                leftArrow.SetAlpha(value ? 0f : 1f);
                rightArrow.SetAlpha(value ? 1f : 0f);
            }
        }
        



        public override void OnCreate()
        {
            base.OnCreate();
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        public void OnClick()
        {
            if (!ViewModel.IsMineMessage)
            {
                ShowContextMenu(); 
            }
        }


        private void ShowContextMenu()
        {
            var vm = ViewModel.CreateContextMenuViewModel();
            var menu = Use<IGuiManager>().CreateView<MessageContextMenuView>(null, null, vm);
            var rectTransform = transform as RectTransform;
            var menuTransform = menu.transform as RectTransform;
            Vector2 pos = new Vector2
            (
                rectTransform.anchoredPosition.x + 250,
                rectTransform.rect.y + rectTransform.rect.height/2f
            );
            var worldPos = rectTransform.TransformPoint(pos);
            var localPos = menuTransform.InverseTransformPoint(worldPos);
            menuTransform.anchoredPosition = localPos;
        }

        
        protected override void PrepareBindings()
        {
            base.PrepareBindings();
            Bind(() => UserId, vm => vm.UserId);
            Bind(() => MessageText, vm => vm.MessageText);
            Bind(() => Time, vm => vm.Time);
            Bind(() => IsMineMessage, vm => vm.IsMineMessage);
        }
    };



    public class TextMessageViewModel : MessageBaseViewModel<TextMessage>
    {
        public string MessageText { get { return data.Text; } }
        public bool IsMineMessage { get { return data.Sender.HybridId==Use<IConfig>().UserHybridId; } }
        public UserId UserId { set { throw new NotImplementedException(); } get { return data.Sender; } }


        public TextMessageViewModel(IServiceContainer container, IViewModelFactory factory) : base(container, factory)
        {
            data = new TextMessage();
        }

        protected override void Init(IData _data)
        {
            if (_data == data)
                return;

            base.Init(_data);
            PropertyChanged(() => MessageText);
            PropertyChanged(() => IsMineMessage);
            PropertyChanged(() => UserId);
        }

        public IViewModel CreateContextMenuViewModel()
        {
            var vm = CreateChildViewModel<MessageContextMenuViewModel>();
            vm.UserId = UserId;
            return vm;
        }

    };
}
