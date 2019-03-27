using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Gui.CrossPromo;
using Common.Models;
using GuildChat;
using Guilds.Models;
using Guilds.Models.Messages;
using Guilds.Models.Messages.Chat;
using Guilds.Models.Services;
using Guilds.SharedLogic;
using Parsing;
using uGUI.Carousel;
using UnityEngine;

public class ScreenGuildChatViewModel : ViewModelBase
{
    public List<IData> Messages
    {
        get
        {
            var m = InChatTab? GetChatMessages() : GetIncomingMessages();
            _messages = m.ToList();
            _messages.Sort();
            return _messages;
        }
    }
    private List<IData> _messages;
    private List<Message> _deletingMessages = new List<Message>();
    private const double durationDeletingAnimationMs = 3000f;

    public List<Id<Message>> AddedAnimationsMessagesId { get; set; }
    public List<Id<Message>> DeletedAnimationsMessagesId { get; set; }

    public bool InGuild { get { return Use<IDataCenter>().Guild.CurrentGuild != null && Use<IDataCenter>().Guild.CurrentGuild.IsReady; } }

    public string GuildName { get { return guildModel.Name; } }

    public bool InChatTab
    {
        get { return _inChatTab; }
        private set
        {
            _inChatTab = value;
            PropertyChanged(() => Messages);
            PropertyChanged(() => InChatTab);
            PropertyChanged(() => ChatCounter);
            PropertyChanged(() => IncomingCounter);
            PropertyChanged(() => EmblemIsVisible);
            PropertyChanged(() => BtnOpenGuildIsVisible);
            PropertyChanged(() => InputPanelIsVisible);
            PropertyChanged(() => TitleText);
            PropertyChanged(() => TabInfoText);
            PropertyChanged(() => LblTabInfoIsVisible);
        }
    }

    public IGuildInfo GuildModel { get { return guildModel; } }

    public List<UserId> GuildMembersId
    {
        get
        {
            return InGuild
                ? guildModel.GuildMembers.Keys.ToList()
                : new List<UserId>();
        }
    }

    public int ChatCounter { get { return counterService.CountNewChatMessage; } }
    public int IncomingCounter { get { return counterService.CountNewIncomingMessage; } }

    internal bool HelpRequestAvailable
    {
        get { return InGuild && (HelpCooldown == null || HelpCooldown.State == CooldownState.Off); }
    }

    internal double NextHelpRequestWaitTime
    {
        get { return HelpCooldown == null ? 0 : HelpCooldown.WaitTime; }
    }


    internal int NextHelpRequestSpeedupWaitingCost
    {
        get
        {
            return Mathf.RoundToInt(
                (float) (Use<IDataCenter>().Definitions.HelpRequestDefinition.CooldownSkipPrice* NextHelpRequestWaitTime / HelpCooldown.Duration));
        }
    }

    internal Cooldown HelpCooldown
    {
        get
        {
            if (Use<IDataCenter>().Guild.Local == null) return null;
                return Use<IDataCenter>().Guild.Local.HelpCooldown;
        }
    }
    internal IPlayerModel PlayerModel { get { return Use<IDataCenter>().PlayerModel; } }

    private IGuildChatService chatService { get { return Use<IGuildChatService>(); } }
    private Guild guildModel { get { return Use<IDataCenter>().Guild.CurrentGuild; } }
    private IChatCounterService counterService { get { return Use<IChatCounterService>(); } }
    public bool EmblemIsVisible { get { return InChatTab && InGuild; } }
    public bool BtnOpenGuildIsVisible { get { return InChatTab && !InGuild; } }
    public bool InputPanelIsVisible { get { return InChatTab && InGuild; } }
    public bool LblTabInfoIsVisible { get { return InChatTab ? !InGuild : !_messages.Any(); }  }

    public string TitleText
    {
        get
        {
            if(!InChatTab)
                return Use<ILocale>().Get("incomming");

            return InGuild ? GuildName : Use<ILocale>().Get("guild");
        }
    }
    public string TabInfoText { get { return Use<ILocale>().Get(InChatTab ? "create_your_guild" : "guild_chat_incoming_info"); } }

    private bool timeIsRun = false;
    private bool _inChatTab;
    


    public ScreenGuildChatViewModel(IServiceContainer container, IViewModelFactory factory) : base(container, factory)
    {
        InChatTab = true;
        DeletedAnimationsMessagesId = new List<Id<Message>>();
        AddedAnimationsMessagesId = new List<Id<Message>>();
        chatService.MessageArrived += OnChatMessageArrived;
        Binder.BindDictionary(
            Use<IDataCenter>().Guild.Chat,
            p => p.ChatMessages,
            OnChatMessagesChanged);

        Binder.BindDictionary(
            Use<IDataCenter>().Guild.Chat,
            p => p.IncomingMessages,
            OnIncomingMessagesChanged);

        Use<IInfoLoadingService>().GetUsersWithAvatarsFromCacheOrDownload(GuildMembersId);
        chatService.TryReconnect();
        counterService.onCounterChaged += OnChatCounterChanged;
        StartTimer();
    }

    internal void SpeedUpHelpRequestWaiting()
    {
        if (PlayerModel.RealBalance < NextHelpRequestSpeedupWaitingCost)
            return;
        var args = new ReleaseHelpCooldownArguments
        { ReleaseRequest = true };
        Use<IGuildSharedLogicCommands>().ReleaseHelpCooldown(args);
        PropertyChanged(() => NextHelpRequestWaitTime);
    }


    public IViewModel CreateMessageViewModel(Type vmType, IData data)
    {
        var vm = CreateChildViewModel(vmType);
        (vm as IReinitable).Data = data;
        return vm;
    }

    public void ShowChatMessage()
    {
        InChatTab = true;
    }

    public void ShowIncomingMessage()
    {
        InChatTab = false;
    }

    public void SendPublicMessage(string text)
    {
        var message = RemoveForbiddenSymbols(text);
        if (String.IsNullOrEmpty(message))
            return;
        chatService.SendPublicMessage(message);
    }

    private string RemoveForbiddenSymbols(string text)
    {
        var arr = text.ToCharArray();
        var locale = Use<ILocale>();
        var allowedChars = arr.Where(ch => locale.CurrentLanguage.IsCharInputAllowed(ch)).ToArray();
        return new string(allowedChars);
    }

    public void SetReadedMessages(List<IData> messages)
    {
        var list = messages.Select(t => (t as Message).Id.Value);
        counterService.SetReadedMessage(list);
    }

    private void StartTimer()
    {
        timeIsRun = true;
        Use<ITimersService>().Subscribe(Constants.TIMER_INTERVAL, TimerTick, false);
    }

    private void TimerTick()
    {
        PropertyChanged(() => NextHelpRequestWaitTime);
    }

    private void OnChatCounterChanged()
    {
        PropertyChanged(() => ChatCounter);
        PropertyChanged(() => IncomingCounter);
    }

    private void OnChatMessageArrived(Message obj)
    {
        if(!InChatTab)
            return;

        PropertyChanged(() => Messages);
    }

    private void OnChatMessagesChanged(DictionaryEvent @event = DictionaryEvent.Created, int key = 0, Message item = null)
    {
        if (!InChatTab)
          return;

        if (@event==DictionaryEvent.ItemUpdated)
            return;

        if (item is HelpRequestMessage)
        {
            if (@event == DictionaryEvent.ItemRemoved)
            {
                item.Lifetime.CanExpire = true;
                item.Lifetime.Duration = Use<ITimeProvider>().GetTime() - item.Lifetime.CreationTime +
                                         durationDeletingAnimationMs;

                _deletingMessages.Add(item);
                DeletedAnimationsMessagesId.Add(item.Id);
            }
            else if (@event == DictionaryEvent.ItemAdded)
            {
                AddedAnimationsMessagesId.Add(item.Id);
            }
        }

        chatService.TryReconnect();
        PropertyChanged(() => InGuild);
        PropertyChanged(() => InChatTab);
        PropertyChanged(() => EmblemIsVisible);

        PropertyChanged(() => Messages);
    }

    private void OnIncomingMessagesChanged(DictionaryEvent @event = DictionaryEvent.Created, int key = 0, Message item = null)
    {
        if (InChatTab)
            return;

        PropertyChanged(() => Messages);
    }

    private IEnumerable<IData> GetChatMessages()
    {
        if (InGuild)
        {
            return MergeChatAndGuildMessages();
        }
        return Enumerable.Empty<IData>();
    }

    private IEnumerable<IData> GetIncomingMessages()
    {
        var ms = Use<IDataCenter>().Guild.Chat.IncomingMessages;
        var messages = ms.Values.Where(t => !counterService.IsLifetimeIsOver(t) && counterService.ICanSee(t)).Cast<IData>().ToList();
        SubrcibeToUpdateLifetime(messages);
        return messages;
    }

    private IEnumerable<IData> MergeChatAndGuildMessages()
    {
        var pus = Use<IDataCenter>().Guild.Chat.ChatMessages;
        var guildMessages = pus.Values
            .Where(t => !counterService.IsLifetimeIsOver(t) && counterService.ICanSee(t))
            .Cast<IData>()
            .ToList();
        var chatMessages = chatService.Messages.Cast<IData>();
        _deletingMessages = _deletingMessages
            .Where(t => !counterService.IsLifetimeIsOver(t))
            .ToList();
        var deletedMessages = _deletingMessages.Cast<IData>().ToList();

        SubrcibeToUpdateLifetime(guildMessages);
        SubrcibeToUpdateLifetime(deletedMessages);

        var result = Enumerable.Empty<IData>();
        result = result.Concat(guildMessages);
        result = result.Concat(chatMessages);
        result = result.Concat(deletedMessages);
        return result;
    }

    private void SubrcibeToUpdateLifetime(IEnumerable<IData> messages)
    {
        var timer = Use<ITimersService>();
        timer.TryRelease(this);
        var timeNow = Use<ITimeProvider>().GetTime();
        if (timeIsRun) StartTimer();
        double minTime = double.MaxValue;
        foreach (var item in messages)
        {
            var data = item as Message;
            if(!data.Lifetime.CanExpire)
                continue;

            var time = data.Lifetime.CreationTime + data.Lifetime.Duration - timeNow;
            if (time < minTime) minTime = time;
        }
        timer.Subscribe(minTime, OnLifetimeIsOver, true);
    }

    protected virtual void OnLifetimeIsOver()
    {
        PropertyChanged(() => Messages);
    }

    protected override void OnDispose()
    {
        Binder.Release();
        chatService.MessageArrived -= OnChatMessageArrived;
        counterService.onCounterChaged -= OnChatCounterChanged;
        base.OnDispose();
    }
};