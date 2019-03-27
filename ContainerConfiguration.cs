using System;
using Assets.Scripts.Common;
using Assets.Scripts.GameData.Models;
using Assets.Scripts.Processing;
using Assets.Scripts.Gui.CrossPromo;
using Assets.Scripts.Gui.Dialog;
using Assets.Scripts.Infrastracture.Services;
using Assets.Scripts.Rewards;
using UnityAds;
using DLResources.Loading;
using DLResources.Loading.CDN;
using DLResources.Loading.Local;
using Dragonlands.Core.Config;
using Dragonlands.SocialManagement;
using Facebook.Unity;
using GameInput;
using Guilds.Models;
using GuildChat;
using Guilds.SharedLogic;
using Infrastracture.Services;
using LibraryUpgrades.SharedLogic;
using Network.WebRequestBridge;using Network.WebRequestBridge.Unity;
using Parsing;
using Scripts.Common.Helpers;
using SocialQuantum.DragonLands.StarterPack.SharedLogic;
using ViewCreation;

namespace Assets.Scripts.Infrastracture
{
    /// <summary>
    /// Основная конфигурация контейнера
    /// </summary>
    public static class ContainerConfiguration
    {
        public static void Configure(IServiceContainer container)
        {
            RegisterType<ILocale,Locale>(container);
            RegisterType<IEventDispatcher, EventDispatcher>(container);
            RegisterType<IIMGuiDrawingService, IMGuiDrawingService>(container);
            RegisterType<IInputDetection, MouseInputDetection>(container);

            RegisterType<IObjectTracker, ObjectsTrackerMobile>(container);
            RegisterType<IHintManager, HintManager>(container);
            RegisterType<IInfoLoadingService, InfoLoadingService>(container);
            

            
            RegisterType<INameGenerator, NameGenerator>(container);
            //RegisterType<ISharedLogicLoader, SharedLogicLoader>(container);
            RegisterType<ISharedLogicLoader, HaxeSharedLogicLoader>(container);
            RegisterType<INavigationService, NavigationServiceHolder>(container);
            RegisterType<IQuestFactory, QuestFactory>(container);
            RegisterType<INewsHelper, NewsHelper>(container);
            RegisterType<IStartBattleProxy, StartBattleProxy>(container);
            RegisterType<IObjectTransferService, ObjectTransferService>(container);
            RegisterType<IViewModelFactory, ViewModelFactory>(container);
            RegisterType<IViewFactory, ViewFactory>(container);
            RegisterType<IResourceGUIHelper, ResourceGUIHelper>(container);
            RegisterType<IGameDataLoader, GameDataLoader>(container);
            RegisterType<IMarketHelper, MarketHelper>(container);
            RegisterType<IBuyWealth, BuyWealth>(container);
            RegisterType<IServiceExecutor, ServiceExecutor>(container);
            RegisterType<IServiceCenter, ServiceCenter>(container);
            RegisterType<IServiceFactory, ServiceFactory>(container);
            RegisterType<IFxSoundManager, FxSoundManager>(container);
            RegisterType<IWorldBoundsModel, WorldBoundsModel>(container);
            RegisterType<IActivePackOpenService, ActivePackOpenService>(container);
            RegisterType<IGameStateService, GameStateService>(container);
            RegisterType<IEggHelper, EggHelper>(container);
            RegisterType<IMapFactory, MapFactory>(container);
            RegisterType<IQuestFactory, QuestFactory>(container);
            RegisterType<ISwitchRoomController, SwitchRoomController>(container);
            RegisterType<IQuestHintPool, QuestHintPool>(container);
            RegisterType<IQuestHintAction, QuestHintAction>(container);
            RegisterType<IMapView, MapView>(container);
            RegisterType<IResourceStorage, ResourceStorage>(container);
            RegisterType<IConfig, ConfigFacade>(container);
            RegisterType<IGcAuthinicateionService, GcAuthinicationService>(container);
            RegisterType<IMoveDragonService, MoveDragonService>(container);
            RegisterType<DevelopmentLogger, DevelopmentLogger>(container);
            RegisterType<Instantiator, Instantiator>(container);
            RegisterType<ICacheProvider, CacheProvider>(container);
            RegisterType<IDefGenerator, DefGenerator>(container);
            RegisterType<IMarketInitializer, MarketInitializer>(container);
            RegisterType<IMarketNewLabelHandler, MarketNewLabelHandler>(container);
            RegisterType<GiftsForReviewHelper, GiftsForReviewHelper>(container);
            RegisterType<IMemoryProfiler, MemoryProfiler>(container);
            RegisterType<IDeviceBridgeService, DeviceBridgeService>(container);
            RegisterType<IInboxManager, InboxManagerStub>(container);
            RegisterType<ISocialManager, SocialManager>(container);
            RegisterType<ITimeProvider, TimeProvider>(container);
            RegisterType<IGameCamera, GameCamera>(container);
            RegisterType<ICameraSphereMover, CameraSphereMover>(container);
            RegisterType<ICameraInputManager, CameraInputManager>(container);
            RegisterType<IViewInput, ViewInput>(container);
            RegisterType<IInputManager, InputManager>(container);
            RegisterType<ICameraMoverSwitcher, CameraMoverSwitcher>(container);
            RegisterType<IInputAdapter, NGUIInputAdapter>(container);
            RegisterType<IZipService, ZipService>(container);
            RegisterType<IHeadTrackingService, HeadTrackingServiceStub>(container);
            RegisterType<ILogSenderService, LogSenderService>(container);

            RegisterType<IMusicManager, MusicManager>(container);
            RegisterType<IFXManager, FxManager>(container);
            RegisterType<IFaceBookHelper, FacebookHelper>(container);
            RegisterType<IScenarioService, ScenarioService>(container);
            RegisterType<IScenesController, ScenesController>(container);
            RegisterType<ISceneUpdater, SceneUpdater>(container);
            RegisterType<INativeDialogManager, NativeDialogManager>(container);
            RegisterType<IDragonFiltrationHelper, DragonFiltrationHelper>(container);
            RegisterType<IPlayGameServices, PlayGameServicesEmpty>(container);
            RegisterType<IGuiMap, GuiMediatorsMap>(container);
            RegisterType<ICursorManager, DummyCursorManager>(container);
            RegisterType<INotificationManager, NotificationManagerEmpty>(container);
            RegisterType<ITutorialService, TutorialHelper>(container);
            RegisterType<IResourcesUsageUtility, ResourceUsageUtilityEmpty>(container);

            RegisterType<IHardwareInfo, HardwareInfoEmpty>(container);

            RegisterType<ISocialNetworkAuthenticateManager, SocialNetworkAuthenticateManager>(container);
            RegisterType<IServerMessageResolver, ServerMessageResolver>(container);

            RegisterType<ISocialReportManager, SocialReportManagerUnity>(container);
            RegisterType<ISocialUser, LocalUserUnity>(container);
            RegisterType<ISessionReceiver, SessionReceiver>(container);

            RegisterType<IContextEventDispatcher, ContextEventDispatcher>(container);
            RegisterType<ISharedController,SharedController>(container);
            RegisterType<IDataCenter, DataCenter>(container);
            RegisterType<IModelParser, ModelParser>(container);
            RegisterType<IObjectModelFactory,ObjectModelFactory>(container);

            //ToDo: расширить container, чтобы проходил по всем интерфейсам сервиса 
            RegisterType<ITimersService, TimersService>(container);
            RegisterType<IModelTimersService, TimersService>(container);
            RegisterType<IActiveEventsHolder, ActiveEventsHolder>(container);

            RegisterType<IGameObjectsMediator,GameObjectsMediator>(container);

            RegisterType<ICrossPromo, CrossPromoService>(container);
            RegisterType<EffectManager, EffectManager>(container);
            RegisterType<IMapObjectDragger, MapObjectDragger>(container);
            RegisterType<CoroutineDispatcher, CoroutineDispatcher>(container);
            RegisterType<IMapRectDrawer, DummyMapRectDrawer>(container);
            //RegisterType<ICrossPromo, TestCrossPromoService>(container);
            RegisterType<IMarketController, MarketController>(container);
            RegisterType<IRequestFactory, UnityRequestFactory>(container);
            RegisterType<IPlayerNameProvider, PlayerNameProvider>(container);
            RegisterType<IRewardsHelper, RewardsHelper>(container);

            RegisterType<IToolManager,ToolManager>(container);

            RegisterType<IInfoDialogService, InfoDialogService>(container);
			RegisterType<ISeenItems, SeenItemsService>(container);

            RegisterType<ResourceLoader, ResourceLoader>(container);
            RegisterType<ITimeLimitedProcessingService,TimeLimitedProcessingService>(container);
            RegisterType<IRateUsShowSerivce, RateUsShowSerivce>(container);
            RegisterType<ITaskScheduler,TaskScheduler>(container);
            RegisterType < ITickController,TickController>(container);
            RegisterType<IScreenMapServices, ScreenMapServices>(container);
            RegisterType<IUnityAdsService, UnityAdsServiceEmpty>(container);
            RegisterType<IModelBindingService, ModelBindingService>(container);
            RegisterType<IGuildDebugService, GuildDebugService>(container);
            RegisterType<IFabricDebugService, FabricDebug>(container);

            RegisterType<IBundleNameProvider, CdnBundleNameProvider>(container);
            RegisterType<IGuiDropDownManager, GuiDropDownManager>(container);
            RegisterType<IGuildChatService, GuildChatService>(container);
            RegisterType<IThreadingService, ThreadingService>(container);
            RegisterResourceManager(container);

            RegisterType<IMemoryDebugInfo, SimpleLineChart>(container);
            RegisterType<IUserGroupDictionary, UserGroupDictionary>(container);
            RegisterType<ISharedLogicQuery, SharedLogicQuery>(container);
            RegisterType<IGuildService, GuildService>(container);
            RegisterType<IGuildSharedLogicCommands, GuildSharedLogicCommands>(container);
            RegisterType<IChatCounterService, ChatCounterService>(container);
            RegisterType<IGuildSharedLogicQueries, GuildSharedLogicQueries>(container);
            RegisterType<IFakeUserContainer, FakeUsersContainer>(container);
            RegisterType<IRemoteNotificationService, EditorNotificationService>(container);
            RegisterType<IGuiManager, GuiManager>(container);
            RegisterType<IFacebookWrapper, FacebookWrapper>(container);

            RegisterType<ILibraryDebugService, LibraryDebugService>(container);
            RegisterType<ILibrarySharedLogicQueries, LibrarySharedLogicQueries>(container);
            RegisterType<ILibrarySharedLogicCommands, LibrarySharedLogicCommands>(container);

            RegisterType<IEventManagerSharedLogicQueries, EventManagerSharedLogicQueries>(container);
            RegisterType<IEventManagerSharedLogicCommands, EventManagerSharedLogicCommands>(container);

#if UNITY_IOS
            IosSpecific(container);
#endif
#if UNITY_ANDROID
            AndroidSpecific(container);
            if (container.Use<IConfig>().BuildTypeId == BuildType.Amazon)
            {
                AmazonSpecific(container);
            }
#endif

#if UNITY_WSA
            WpSpecific(container);
#endif


#if UNITY_EDITOR
            EditorSpecific(container);
#endif

        }

        private static void IosSpecific(IServiceContainer container)
        {
            RegisterType<IInputDetection, TouchInputDetection>(container);
            RegisterType<IFacebookReceiver, FacebookReceiver>(container);
            RegisterType<IFacebookProvider, FacebookProviderAndroidIOS>(container);
            RegisterType<ICameraPlaneMover, MobileCameraPlaneMover>(container);
            RegisterType<IGameCenterReceiver, GameCenterReceiver>(container);
            RegisterType<ILoggerEx, LoggerEx>(container);
            RegisterType<IAdReward, AdRewardMobile>(container);
            RegisterType<IInAppPurchase, InAppPurchaseIOS>(container);
            RegisterType<INotificationManager, NotificationManagerIos>(container);
            RegisterType<IHardwareInfo, HardwareInfoIos>(container);
            RegisterType<IResourcesUsageUtility ,IosResourceUsageUtilty>(container);
            RegisterType<ISocialReportManager, SocialReportManagerIOS>(container);
            RegisterType<ISocialUser, LocalUserIos>(container);
            RegisterType<IUnityAdsService, UnityAdsService>(container);
        }

        private static void AndroidSpecific(IServiceContainer container)
        {
            RegisterType<IInputDetection, TouchInputDetection>(container);
            RegisterType<IFacebookReceiver, FacebookReceiver>(container);
            RegisterType<IFacebookProvider, FacebookProviderAndroidIOS>(container);
            RegisterType<ICameraPlaneMover, MobileCameraPlaneMover>(container);
            RegisterType<IGameCenterReceiver, GameCenterReceiver>(container);
            RegisterType<ILoggerEx, LoggerEx>(container);
            RegisterType<IAdReward, AdRewardMobile>(container);
            RegisterType<IInAppPurchase, InAppPurchaseAndroid>(container);
            RegisterType<IPlayGameServices, GpgService>(container);
            RegisterType<INotificationManager, NotificationManagerAndroid>(container);
            RegisterType<IResourcesUsageUtility, AndroidResourcesUsageUtility>(container);
            RegisterType<IHardwareInfo, HardwareInfoAndroid>(container);

            RegisterType<ISocialReportManager, SocialReportManagerAndroid>(container);
            RegisterType<ISocialUser, LocalUserAndroid>(container);
            RegisterType<IUnityAdsService, UnityAdsService>(container);

            RegisterType<IRemoteNotificationService, AndroidBasedNotificationService>(container);
        }

        private static void AmazonSpecific(IServiceContainer container)
        {
            // TODO: Win10 :/
#if UNITY_ANDROID
                   RegisterType<IInputDetection, TouchInputDetection>(container);
                   RegisterType<IInAppPurchase, InAppPurchaseAmazon>(container);
                   RegisterType<IPlayGameServices, PlayGameServicesEmpty>(container);

                   RegisterType<ISocialReportManager, SocialReportManagerAmazon>(container);
                   RegisterType<ISocialUser, LocalUserAmazon>(container);

                   RegisterType<ICrossPromo, CrossPromoServiceEmpty>(container);
                   RegisterType<IHeadTrackingService, HeadTrackingService>(container);
#endif
        }

        private static void WpSpecific(IServiceContainer container)
        {
#if UNITY_WSA
            RegisterType<IInputDetection, UniversalInputDetection>(container);
            RegisterType<IFacebookReceiver, FacebookReceiver>(container);
            RegisterType<IFacebookProvider, FacebookProviderWp>(container);
            RegisterType<ICameraPlaneMover, WebCameraPlaneMover>(container);
            RegisterType<ICameraSphereMover, CameraSphereMover>(container);
            //RegisterType<IMapObjectDragger, WebMapObjectDragger>(container);
            RegisterType<ILoggerEx, LoggerEx>(container);
            RegisterType<IAdReward, AdRewardEmpty>(container);
            //RegisterType<ISharedLogicLoader, HaxeSharedLogicLoader>(container);
            //RegisterType<IGameCenterReceiver, GameCenterReceiver>(container);
            //RegisterType<IInAppPurchase, InAppPurchaseAndroid>(container);
            RegisterType<IPlayGameServices, PlayGameServicesEmpty>(container);
            RegisterType<INotificationManager, NotificationManagerWp>(container);
            RegisterType<IResourcesUsageUtility, UwpResourceUsageUtility>(container);

            RegisterType<ISocialUser, LocalUserWP>(container);

            RegisterType<ISocialReportManager, SocialReportManagerWp>(container);

            RegisterType<IHardwareInfo, HardwareInfoWp>(container);
            RegisterType<IInAppPurchase, InAppPurchaseWp>(container);
            // No unity ads 4 WP
            RegisterType<IUnityAdsService, UnityAdsServiceEmpty>(container);
            RegisterType<IRemoteNotificationService, WpNotificationService>(container);
#endif
        }

        private static void EditorSpecific(IServiceContainer container)
        {
            RegisterType<IInputDetection, MouseInputDetection>(container);
            RegisterType<IPlayGameServices, PlayGameServicesEmpty>(container);
            RegisterType<IFacebookReceiver, FacebookReceiver>(container);
            RegisterType<IFacebookProvider, FacebookProviderAndroidIOS>(container);
            RegisterType<ICameraPlaneMover, CameraPlaneMover>(container);
            RegisterType<IAdReward, AdRewardMobile>(container);
            RegisterType<IInAppPurchase, InAppPurchaseUnity>(container);
            RegisterType<IResourcesUsageUtility, ResourceUsageUtilityEmpty>(container);
            RegisterType<IHardwareInfo, HardwareInfoEditor>(container);
            RegisterType<ISocialUser, LocalUserUnity>(container);
            RegisterType<IUnityAdsService, TestUnityAdsService>(container);
            RegisterType<ILogSenderService, LogSenderServiceStub>(container);
            RegisterType<ISocialReportManager, SocialReportManagerUnity>(container);
            RegisterType<IRemoteNotificationService, EditorNotificationService>(container);
        }

        private static void RegisterResourceManager(IServiceContainer container)
        {
            var resLoader = container.Use<IConfig>().ResLoader;
            switch (resLoader)
            {
                case ResourceLoaderType.CDN:
                    RegisterType<ResourceManagerBase, CDNResourceManager>(container);
                    RegisterType<IResourceDispatcher, CDNResourceDispatcher>(container);
                    break;
                case ResourceLoaderType.LOCAL:
                    RegisterType<ResourceManagerBase, LocalResourceManager>(container);
                    RegisterType <IResourceDispatcher, LocalResourceDispatcher>(container);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void RegisterType<TInterface, TImplementation>(IServiceContainer container)
            where TInterface: IServiceBase
            where TImplementation : TInterface,IServiceBase
        {
            container.RegisterType<TInterface, TImplementation>();
        }
    }
}