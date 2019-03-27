using System;
using Assets.Scripts.Gui.Dialog;
using Dragonlands.Core.Config;
using Dragonlands.Gui;
using Guilds.Models.Messages;
using uGUI.Carousel;
using UnityEngine;
using UnityEngine.UI;

namespace Guilds
{
    [MediatorUgui]
    [PlatformSpecificView(RunPlatform.All, typeof(SystemMessageView), "SystemMessage")]
    public class SystemMessageView : MessageBaseView<SystemMessageViewModel, SystemMessage>
    {
        [SerializeField] private Text messageLabel;

        public string MessageText { get { throw new NotImplementedException(); } set { messageLabel.text = value; } }

        protected override void PrepareBindings()
        {
            base.PrepareBindings();
            Bind(() => MessageText, vm => vm.MessageText);
        }
    };


    public class SystemMessageViewModel : MessageBaseViewModel<SystemMessage>
    {
        public string MessageText { get { return data.Text; }  }


        public SystemMessageViewModel(IServiceContainer container, IViewModelFactory factory) : base(container, factory)
        {
            data = new SystemMessage();
        }

        protected override void Init(IData _data)
        {
            if (_data == data)
                return;

            base.Init(_data);
            PropertyChanged(() => MessageText);
        }
    };


    [TypeMap(typeof(SystemMessageView))]
    public class SystemMessage : Message
    {
        public string Text;
    };
}