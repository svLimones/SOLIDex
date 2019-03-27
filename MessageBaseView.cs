using System;
using System.Collections.Generic;
using Guilds.Models;
using Guilds.Models.Messages;
using uGUI.Carousel;
using UnityEngine;
using UnityEngine.UI;

namespace Guilds
{
    public class MessageBaseView<T, TD> : View<T>, IReinitable where TD : Message, new() where T: MessageBaseViewModel<TD> 
    {
        [SerializeField] private Text timeLabel;

        public virtual IData Data { get { return ViewModel.Data; } set { ViewModel.Data = value; } }
        public string Time { get { throw new NotImplementedException(); } set { timeLabel.text = value; } }
        public override ViewType ViewType { get { return ViewType.ChildComponent; } }
        

        protected override void PrepareBindings()
        {
            base.PrepareBindings();
            Bind(() => Time, vm => vm.Time);
        }
    };


    public class MessageBaseViewModel<TD> : ViewModelBase, IReinitable where TD : Message, new()
    {
        public IData Data { get { return data; } set { Init(value); } }
        public string Time { get { return data.Lifetime != null ? ToolHelper.TimeStampToChatFormat(data.Lifetime.CreationTime) : ""; } }
        protected TD data;

        public MessageBaseViewModel(IServiceContainer container, IViewModelFactory factory) : base(container, factory)
        {
            data = new TD();
        }

        protected virtual void Init(IData _data)
        {
            data = _data as TD;
            PropertyChanged(()=>Time);
        }
    };

    public class GuildMessageBaseViewModel<TD> : MessageBaseViewModel<TD> where TD : Message, new()
    {
        public GuildInfo GuildInfo {get; protected set; }

        public GuildMessageBaseViewModel(IServiceContainer container, IViewModelFactory factory) : base(container, factory)
        {
            data = new TD();
        }

        protected void GetGuildInfo(SId<Guild> id)
        {
            GuildInfo = Use<IInfoLoadingService>().GetGuildFromCacheOrDownload(id);
            Binder.BindProperty(GuildInfo, g => g.Loaded, OnGuildInfoLoadedChanged);
            OnGuildInfoLoadedChanged(GuildInfo.Loaded, GuildInfo.Loaded);
        }

        protected void  OnGuildInfoLoadedChanged(bool oldValue, bool newValue)
        {
            if (!newValue)
                return;
            NotifyPropertyChangedOnGuildLoaded();
        }

        protected virtual void NotifyPropertyChangedOnGuildLoaded()
        {
            PropertyChanged(() => GuildInfo);
        }
    }
}
