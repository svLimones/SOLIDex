using System;
using System.Collections.Generic;
using Dragonlands.Core.Config;
using Dragonlands.Gui;
using Guilds.Models;
using uGUI;
using UnityEngine;
using UnityEngine.UI;

namespace GuildChat.uGUI
{
    [PlatformSpecificView(RunPlatform.All, typeof(UserPanelView), "UserPanel")]
    public class UserPanelView : View<UserPanelViewModel>
    {
        [SerializeField] private RawImage AvatarImage;
        [SerializeField] private Text NameLabel;
        [SerializeField] private Text RankLabel;
        [SerializeField] private Text LevelLabel;
        [SerializeField] private ImageFromNguiAtlas SocialIconImage;
        [SerializeField] private GameObject Throbber;
        public override ViewType ViewType { get { return ViewType.ChildComponent; } }

        public string Name { get {throw new NotImplementedException();} set { if(NameLabel!=null) NameLabel.text = value;} }
        public string Rank { get { throw new NotImplementedException(); } set { if (RankLabel != null) RankLabel.text = string.IsNullOrEmpty(value) ? "" : Use<ILocale>().Get(value); } }
        public int Level { get { throw new NotImplementedException(); } set { if (LevelLabel != null) LevelLabel.text = value.ToString(); } }

        public string SocialIcon
        {
            get { throw new NotImplementedException(); }
            set
            {
                if (SocialIconImage == null)
                    return;

                SocialIconImage.gameObject.SetActive(!string.IsNullOrEmpty(value));
                SocialIconImage.spriteName = value;
                SocialIconImage.SetSprite();
            }
        }

        public Texture Avatar
        {
            get { throw new NotImplementedException(); }
            set
            {
                if (AvatarImage == null)
                    return;

                AvatarImage.texture = value;
                AvatarImage.gameObject.SetActive(value);
            }
        }

        public bool ThrobberActive { get { throw new NotImplementedException(); }
            set
            {
                if (Throbber == null)
                    return;

                Throbber.SetActive(value);
                if (value) AvatarImage.gameObject.SetActive(false);
            }
        }


        protected override void PrepareBindings()
        {
            base.PrepareBindings();
            Bind(()=> Name, vm=>vm.Name);
            Bind(()=> Rank, vm=>vm.Rank);
            Bind(()=> Level, vm=>vm.Level);
            Bind(()=> SocialIcon, vm=>vm.SocialIconName);
            Bind(()=> Avatar, vm=>vm.Avatar);
            Bind(()=> ThrobberActive, vm=>vm.ThrobberActive);
        }
    };



    public class UserPanelViewModel : ViewModelBase
    {
        private UserId _userId;

        public UserId UserId
        {
            get { return _userId; }
            set
            {
                if (_userId == value)
                    return;

                _userId = value;
                Init();
            }
        }

        private bool isMember = false;

        public bool IsMember
        {
            get { return isMember; }
            set
            {
                isMember = value;
                PropertyChanged(()=>Rank);
            }
        }

        public bool ThrobberActive { get; set; }

        public string Rank
        {
            set { throw new NotImplementedException(); }
            get
            {
                if (IsMember && guildModel != null && !guildModel.GuildMembers.ContainsKey(_userId))
                    return "old_member";

                if (_userId.IsEmpty || _user==null || _user.GuildMemberInfo==null)
                    return "";

                var role = _user.GuildMemberInfo.GuildRole;
                return MessageAttribute.GetMessageByEnum(role);
            }
        }

        public string Name
        {
            set {  throw new NotImplementedException(); }
            get
            {
                return _user != null ? _user.UserName : "";
            }
        }

        public int Level
        {
            set {  throw new NotImplementedException(); }
            get
            {
                return _user != null ? _user.Level : 0;
            }
        }

        public Texture Avatar
        {
            set { throw new NotImplementedException(); }
            get
            {
                return _avatar != null ? _avatar.Item : null;
            }
        }

        public string SocialIconName
        {
            set { throw new NotImplementedException(); }
            get
            {
                return _user != null && _user.SocialNetwork!=null && _user.SocialNetwork.SocialImage != null
                    ? _user.SocialNetwork.SocialImage
                    : "";
            }
        }

        private Guild guildModel { get { return Use<IDataCenter>().Guild.CurrentGuild; } }
        private DelayedLoadable<UserInfo, Texture> _avatar;
        private UserInfo _user;


        public UserPanelViewModel(IServiceContainer container, IViewModelFactory factory) : base(container, factory)
        {
            ThrobberActive = false;
        }

        private void Init()
        {
            Binder.Release();
            ThrobberActive = true;
            PropertyChanged(() => ThrobberActive);
            PropertyChanged(() => Rank);
            _user = Use<IInfoLoadingService>().GetUserFromCacheOrDownload(_userId);
            Binder.BindProperty(_user, u => u.Loaded, OnUserInfoLoadedChanged);
            OnUserInfoLoadedChanged(_user.Loaded, _user.Loaded);
        }

        private void OnUserInfoLoadedChanged(bool oldVal, bool newVal)
        {
            if (!newVal)
                return;

            _avatar = Use<IInfoLoadingService>().GetAvatarFromCacheOrDownload(_user);
            Binder.BindProperty(_avatar, a => a.Loaded, OnAvatarLoadedChanged);
            OnAvatarLoadedChanged(_avatar.Loaded, _avatar.Loaded);
            PropertyChanged(() => Name);
            PropertyChanged(() => Level);
            PropertyChanged(() => Rank);
            PropertyChanged(() => SocialIconName);
        }

        private void OnAvatarLoadedChanged(bool oldVal, bool newVal)
        {
            if (!newVal)
                return;
            
            ThrobberActive = false;
            PropertyChanged(() => Avatar);
            PropertyChanged(() => ThrobberActive);
        }
        
    };
}
