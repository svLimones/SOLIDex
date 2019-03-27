using System;
using System.Collections.Generic;
using System.Linq;
using DebugLogs;
using Guilds;
using Guilds.Models;
using Guilds.Models.Messages;
using Guilds.Models.Messages.Chat;
using Newtonsoft.Json;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using Sfs2X.Util;
using UnityEngine;

namespace GuildChat
{
    public interface IGuildChatService : IViewService
    {
        bool IsConnected { get; }
        void TryReconnect();
        void Connect();
        void Disconnect();
        void SendPublicMessage(string text);
        event Action<Message> MessageArrived;
        event Action<bool> ConnectionChanged;
        Queue<Message> Messages { get; }
    };

    [Serializable]
    public class RoomVariable
    {
        public string uid;
        public string m;
        public double t;
    };



    [CreateMonoObject("GuildChatService")]
    public class GuildChatService : ServiceBaseMonoBehaviour, IGuildChatService
    {
        private bool isConnected = false;
        public bool IsConnected
        {
            private set
            {
                isConnected = value;
                ConnectionChanged(isConnected);
            }
            get { return isConnected; }
        }

        private bool RoomChanged { get { return !( inGuild && lastRoomName == roomName && !String.IsNullOrEmpty(lastRoomName)); } }

        public Queue<Message> Messages { get; private set; }
        public event Action<Message> MessageArrived = delegate { };
        public event Action<bool> ConnectionChanged = delegate { };

        private SmartFox sfs;
        private string userId { get { return Use<IConfig>().UserHybridId; } }
        private SId<Guild> GuildId { get { return Use<IDataCenter>().Guild.CurrentGuild.GuildId; } }
        private string token {get {return Use<IDataCenter>().GlobalGuild.ChatRoomToken; } }
        private bool inGuild { get { return Use<IDataCenter>().Guild.CurrentGuild != null; } }
        private string roomName { get { return GuildId.Value; } }
        private string roomPassword { get { return Use<IDataCenter>().Guild.CurrentGuild.Password; } }

        private TimerWithInterval _timer;
        private int maxMessages { get { return Use<IDataCenter>().Definitions.GuildMessages.MaxTextMessages; } }
        private int pingRate = 1001;
        private const int minimalRate = 1000;
        private int frameCount;
        private bool isConnecting = false;
        private string lastRoomName;
        private ConfigData cfg;
        private bool haveConnectionProblem;


        public override void OnContainerSet()
        {
            base.OnContainerSet();

            Messages = new Queue<Message>(maxMessages);
            Application.logMessageReceived += OnException;

            var config = Use<IConfig>().GuildChat;
            cfg = new ConfigData();
            cfg.Host = config.host;
            cfg.Port = config.port;
            cfg.Zone = config.zone;
            cfg.Debug = true;
        }

        private void OnException(string condition, string stacktrace, LogType type)
        {
            if (!Use<IConfig>().LogErrorsInChat )
                return;

            if(type!=LogType.Error && type!=LogType.Exception)
                return;

            condition = condition.Remove(250);

            var message = new SystemMessage()
            {
                Lifetime = new Lifetime()
                {
                    CreationTime = Use<ITimeProvider>().GetTime(),
                    Duration = 100000
                },
                Text = condition
            };
            Messages.Enqueue(message);
            MessageArrived(message);
        }

        public void TryReconnect()
        {
            if (RoomChanged)
            {
                Disconnect();
            }

            if (!IsConnected && inGuild)
            {
                Connect();
            }
        }

        public void Connect()
        {
            if(!inGuild || isConnecting)
                return;

            Disconnect();
            isConnecting = true;

            sfs = new SmartFox();
            sfs.ThreadSafeMode = true;
            sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
            sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
            sfs.AddEventListener(SFSEvent.LOGIN, OnLogin);
            sfs.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
            sfs.AddEventListener(SFSEvent.ROOM_JOIN, OnRoomJoined);
            sfs.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);
            sfs.AddEventListener(SFSEvent.PUBLIC_MESSAGE, OnPublicMessage);
            sfs.Connect(cfg);
            D.Log(LoggingTags.Chat, "SFS reconnecting...");
        }


        public void Disconnect()
        {
            IsConnected = false;
            isConnecting = false;
            if(sfs==null)
                return;

            sfs.Disconnect();
            sfs = null;
        }

        protected override void OnDispose()
        {
            Disconnect();
            base.OnDispose();
        }

        public void SendPublicMessage(string text)
        {
            if (!IsConnected)
            {
                Connect();
                D.LogWarning(LoggingTags.Chat, "SFS is not connected");
                return;
            }

            sfs.Send(new PublicMessageRequest(text));
        }

        private void Update()
        {
            if(!inGuild)
                return;

            if (sfs != null)
            {
                sfs.ProcessEvents();
            }

            if (++frameCount%pingRate == 0)
            {
                frameCount = 0;

                if (!isConnected && inGuild && !isConnecting)
                {
                    Connect();
                }
               
            }
        }

        ////////////SFS Events
        private void OnConnection(BaseEvent evt)
        {
            if ((bool)evt.Params["success"])
            {
                var par = new SFSObject();
                par.PutUtfString("pass", token);
                sfs.Send(new LoginRequest(userId, "", Use<IConfig>().GuildChat.zone, par));
                pingRate = minimalRate;
            }
            else
            {
                Disconnect();
                D.LogWarning(LoggingTags.Chat, "Connection failed; is the server running at all?");

                pingRate *= 2;
                if (haveConnectionProblem)
                    return;

                var message = new SystemMessage()
                {
                    Id = new Id<Message>(0),
                    Text = Use<ILocale>().Get("chat_connection_lost"),
                    Lifetime = new Lifetime()
                    {
                        CreationTime = Use<ITimeProvider>().GetTime(),
                        Duration = -1000
                    }
                };

                Messages.Enqueue(message);
                haveConnectionProblem = true;
            }

        }

        private void OnConnectionLost(BaseEvent evt)
        {
            Disconnect();
            var reason = (string) evt.Params["reason"];
            D.LogWarning(LoggingTags.Chat, "Connection was lost; reason is: " + reason);

            pingRate *= 2;
            if (reason == "idle" || haveConnectionProblem)
                return;


            var message = new SystemMessage()
            {
                Id = new Id<Message>(0),
                Text = Use<ILocale>().Get("chat_connection_lost"),
                Lifetime = new Lifetime()
                {
                    CreationTime = Use<ITimeProvider>().GetTime(),
                    Duration = -1000
                }
            };

            Messages.Enqueue(message);
            MessageArrived(message);
            haveConnectionProblem = true;
        }

        private void OnLogin(BaseEvent evt)
        {
            lastRoomName = roomName;
            if (String.IsNullOrEmpty(roomName))
            {
                D.LogError(LoggingTags.Chat, "Room is null: " + roomName);
            }
            sfs.Send(new JoinRoomRequest(roomName, roomPassword));
        }

        private void OnLoginError(BaseEvent evt)
        {
            Disconnect();
            D.LogError(LoggingTags.Chat, "Login failed: " + (string)evt.Params["errorMessage"]);
        }

        private void OnRoomJoined(BaseEvent evt)
        {
            Message message;
            Messages = new Queue<Message>(maxMessages);
            IsConnected = true;
            isConnecting = false;
            haveConnectionProblem = false;

            var room = (Room)evt.Params["room"];
            var roomVariable = room.GetVariable("messages");
            if (roomVariable == null)
                return;

            var array = JsonConvert.DeserializeObject<List<RoomVariable>>(roomVariable.GetStringValue());           
            if (array == null)
                return;

            foreach (var item in array)
            {
                var time = new Lifetime();
                time.CreationTime = item.t * 1000;
                time.Duration = -1000;
                time.CanExpire = false;
                if (IsBannedUser(item.uid, time.CreationTime))
                    continue;

                unchecked
                {
                    message = new TextMessage()
                    {
                        Id = new Id<Message>((int)(time.CreationTime/ 1000)),
                        Sender = new UserId(item.uid),
                        Text = ToolHelper.Base64Decode(item.m),
                        Lifetime = time
                    };
                }

                if (Messages.Count >= maxMessages) Messages.Dequeue();
                Messages.Enqueue(message);
            }

            if(Messages.Any()) MessageArrived(Messages.Last());
            D.Log(LoggingTags.Chat, "Joined room is successfull:");
        }

        private void OnRoomJoinError(BaseEvent evt)
        {
            D.LogError(LoggingTags.Chat, "Room join failed: " + (string)evt.Params["errorMessage"]);
        }

        private void OnPublicMessage(BaseEvent evt)
        {
            User sender = (User)evt.Params["sender"];
            string text = (string)evt.Params["message"];
            double time = Use<ITimeProvider>().GetTime();

            if (IsBannedUser(sender.Name, time))
                return;
            unchecked
            {
                var message = new TextMessage()
                {
                    Id = new Id<Message>((int)(time/1000)),
                    Sender = new UserId(sender.Name),
                    Text = text,
                    Lifetime = new Lifetime()
                    {
                        CreationTime = time,
                        Duration = -1000,
                        CanExpire = false
                    }
                };
                if (Messages.Count >= maxMessages) Messages.Dequeue();
                Messages.Enqueue(message);
                MessageArrived(message);
            }
        }
        ////////SFS Events

        private bool IsBannedUser(string userHybrid, double time)
        {
            var blackList = Use<IDataCenter>().Guild.Local.BlackList;
            if (blackList.Muted==null || !blackList.Muted.ContainsKey(new UserId(userHybrid)))
                return false;

            var bantime = blackList.Muted[new UserId(userHybrid)].MutingTime;
            return time>bantime;
        }

    };
}