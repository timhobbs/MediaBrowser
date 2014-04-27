﻿using MediaBrowser.Api;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Constants;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Implementations;
using MediaBrowser.Common.Implementations.ScheduledTasks;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.News;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Controller.Themes;
using MediaBrowser.Dlna;
using MediaBrowser.Dlna.Eventing;
using MediaBrowser.Dlna.Main;
using MediaBrowser.Dlna.PlayTo;
using MediaBrowser.Dlna.Server;
using MediaBrowser.MediaEncoding.BdInfo;
using MediaBrowser.MediaEncoding.Encoder;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Updates;
using MediaBrowser.Providers.Manager;
using MediaBrowser.Server.Implementations;
using MediaBrowser.Server.Implementations.Channels;
using MediaBrowser.Server.Implementations.Collections;
using MediaBrowser.Server.Implementations.Configuration;
using MediaBrowser.Server.Implementations.Drawing;
using MediaBrowser.Server.Implementations.Dto;
using MediaBrowser.Server.Implementations.EntryPoints;
using MediaBrowser.Server.Implementations.FileOrganization;
using MediaBrowser.Server.Implementations.HttpServer;
using MediaBrowser.Server.Implementations.IO;
using MediaBrowser.Server.Implementations.Library;
using MediaBrowser.Server.Implementations.LiveTv;
using MediaBrowser.Server.Implementations.Localization;
using MediaBrowser.Server.Implementations.MediaEncoder;
using MediaBrowser.Server.Implementations.Notifications;
using MediaBrowser.Server.Implementations.Persistence;
using MediaBrowser.Server.Implementations.ServerManager;
using MediaBrowser.Server.Implementations.Session;
using MediaBrowser.Server.Implementations.Themes;
using MediaBrowser.Server.Implementations.WebSocket;
using MediaBrowser.ServerApplication.EntryPoints;
using MediaBrowser.ServerApplication.FFMpeg;
using MediaBrowser.ServerApplication.IO;
using MediaBrowser.ServerApplication.Native;
using MediaBrowser.ServerApplication.Networking;
using MediaBrowser.WebDashboard.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Class CompositionRoot
    /// </summary>
    public class ApplicationHost : BaseApplicationHost<ServerApplicationPaths>, IServerApplicationHost
    {
        /// <summary>
        /// Gets the server configuration manager.
        /// </summary>
        /// <value>The server configuration manager.</value>
        public IServerConfigurationManager ServerConfigurationManager
        {
            get { return (IServerConfigurationManager)ConfigurationManager; }
        }

        /// <summary>
        /// Gets the name of the web application that can be used for url building.
        /// All api urls will be of the form {protocol}://{host}:{port}/{appname}/...
        /// </summary>
        /// <value>The name of the web application.</value>
        public string WebApplicationName
        {
            get { return "mediabrowser"; }
        }

        /// <summary>
        /// Gets the HTTP server URL prefix.
        /// </summary>
        /// <value>The HTTP server URL prefix.</value>
        private IEnumerable<string> HttpServerUrlPrefixes
        {
            get
            {
                var list = new List<string>
                {
                    "http://+:" + ServerConfigurationManager.Configuration.HttpServerPortNumber + "/" + WebApplicationName + "/"
                };

                return list;
            }
        }

        /// <summary>
        /// Gets the configuration manager.
        /// </summary>
        /// <returns>IConfigurationManager.</returns>
        protected override IConfigurationManager GetConfigurationManager()
        {
            return new ServerConfigurationManager(ApplicationPaths, LogManager, XmlSerializer);
        }

        /// <summary>
        /// Gets or sets the server manager.
        /// </summary>
        /// <value>The server manager.</value>
        private IServerManager ServerManager { get; set; }
        /// <summary>
        /// Gets or sets the user manager.
        /// </summary>
        /// <value>The user manager.</value>
        public IUserManager UserManager { get; set; }
        /// <summary>
        /// Gets or sets the library manager.
        /// </summary>
        /// <value>The library manager.</value>
        internal ILibraryManager LibraryManager { get; set; }
        /// <summary>
        /// Gets or sets the directory watchers.
        /// </summary>
        /// <value>The directory watchers.</value>
        private ILibraryMonitor LibraryMonitor { get; set; }
        /// <summary>
        /// Gets or sets the provider manager.
        /// </summary>
        /// <value>The provider manager.</value>
        private IProviderManager ProviderManager { get; set; }
        /// <summary>
        /// Gets or sets the HTTP server.
        /// </summary>
        /// <value>The HTTP server.</value>
        private IHttpServer HttpServer { get; set; }
        private IDtoService DtoService { get; set; }
        private IImageProcessor ImageProcessor { get; set; }
        private ISeriesOrderManager SeriesOrderManager { get; set; }

        /// <summary>
        /// Gets or sets the media encoder.
        /// </summary>
        /// <value>The media encoder.</value>
        private IMediaEncoder MediaEncoder { get; set; }

        private ISessionManager SessionManager { get; set; }

        private ILiveTvManager LiveTvManager { get; set; }

        internal ILocalizationManager LocalizationManager { get; set; }

        private IEncodingManager EncodingManager { get; set; }
        private IChannelManager ChannelManager { get; set; }

        /// <summary>
        /// Gets or sets the user data repository.
        /// </summary>
        /// <value>The user data repository.</value>
        private IUserDataManager UserDataManager { get; set; }
        private IUserRepository UserRepository { get; set; }
        internal IDisplayPreferencesRepository DisplayPreferencesRepository { get; set; }
        internal IItemRepository ItemRepository { get; set; }
        private INotificationsRepository NotificationsRepository { get; set; }
        private IFileOrganizationRepository FileOrganizationRepository { get; set; }
        private IProviderRepository ProviderRepository { get; set; }

        private INotificationManager NotificationManager { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationHost"/> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="logManager">The log manager.</param>
        public ApplicationHost(ServerApplicationPaths applicationPaths, ILogManager logManager, bool isRunningAsService)
            : base(applicationPaths, logManager)
        {
            _isRunningAsService = isRunningAsService;
        }

        private readonly bool _isRunningAsService;
        public override bool IsRunningAsService
        {
            get { return _isRunningAsService; }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Media Browser Server";
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can self restart.
        /// </summary>
        /// <value><c>true</c> if this instance can self restart; otherwise, <c>false</c>.</value>
        public override bool CanSelfRestart
        {
            get { return NativeApp.CanSelfRestart; }
        }

        public bool SupportsAutoRunAtStartup
        {
            get { return NativeApp.SupportsAutoRunAtStartup; }
        }

        /// <summary>
        /// Runs the startup tasks.
        /// </summary>
        /// <returns>Task.</returns>
        public override async Task RunStartupTasks()
        {
            await base.RunStartupTasks().ConfigureAwait(false);

            Logger.Info("Core startup complete");

            Parallel.ForEach(GetExports<IServerEntryPoint>(), entryPoint =>
            {
                try
                {
                    entryPoint.Run();
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error in {0}", ex, entryPoint.GetType().Name);
                }
            });

            LogManager.RemoveConsoleOutput();
        }

        public override Task Init(IProgress<double> progress)
        {
            DeleteDeprecatedModules();

            return base.Init(progress);
        }

        private void DeleteDeprecatedModules()
        {
            try
            {
                MigrateUserFolders();
            }
            catch (IOException)
            {
            }

            try
            {
                File.Delete(Path.Combine(ApplicationPaths.PluginsPath, "MBPhoto.dll"));
            }
            catch (IOException)
            {
                // Not there, no big deal
            }

            Task.Run(() =>
            {
                try
                {
                    Directory.Delete(Path.Combine(ApplicationPaths.DataPath, "remote-images"), true);
                }
                catch (IOException)
                {
                    // Not there, no big deal
                }

                try
                {
                    Directory.Delete(Path.Combine(ApplicationPaths.DataPath, "chapter-images"), true);
                }
                catch (IOException)
                {
                    // Not there, no big deal
                }

                try
                {
                    Directory.Delete(Path.Combine(ApplicationPaths.DataPath, "extracted-video-images"), true);
                }
                catch (IOException)
                {
                    // Not there, no big deal
                }

                try
                {
                    Directory.Delete(Path.Combine(ApplicationPaths.DataPath, "extracted-audio-images"), true);
                }
                catch (IOException)
                {
                    // Not there, no big deal
                }

                try
                {
                    Directory.Delete(Path.Combine(ApplicationPaths.DataPath, "tmdb-tv"), true);
                }
                catch (IOException)
                {
                    // Not there, no big deal
                }

                try
                {
                    Directory.Delete(Path.Combine(ApplicationPaths.DataPath, "tmdb-collections"), true);
                }
                catch (IOException)
                {
                    // Not there, no big deal
                }

                try
                {
                    Directory.Delete(Path.Combine(ApplicationPaths.DataPath, "tmdb-movies"), true);
                }
                catch (IOException)
                {
                    // Not there, no big deal
                }

                try
                {
                    Directory.Delete(Path.Combine(ApplicationPaths.DataPath, "fanart-movies"), true);
                }
                catch (IOException)
                {
                    // Not there, no big deal
                }

                try
                {
                    Directory.Delete(Path.Combine(ApplicationPaths.DataPath, "fanart-music"), true);
                }
                catch (IOException)
                {
                    // Not there, no big deal
                }

                try
                {
                    Directory.Delete(Path.Combine(ApplicationPaths.DataPath, "fanart-tv"), true);
                }
                catch (IOException)
                {
                    // Not there, no big deal
                }

                try
                {
                    Directory.Delete(Path.Combine(ApplicationPaths.DataPath, "tmdb-people"), true);
                }
                catch (IOException)
                {
                    // Not there, no big deal
                }

                try
                {
                    Directory.Delete(Path.Combine(ApplicationPaths.DataPath, "tvdb-v3"), true);
                }
                catch (IOException)
                {
                    // Not there, no big deal
                }
            });
        }

        private void MigrateUserFolders()
        {
            var rootPath = ApplicationPaths.RootFolderPath;

            var folders = new DirectoryInfo(rootPath).EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Where(i => !string.Equals(i.Name, "default", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var folder in folders)
            {
                Directory.Delete(folder.FullName, true);
            }
        }

        /// <summary>
        /// Registers resources that classes will depend on
        /// </summary>
        /// <returns>Task.</returns>
        protected override async Task RegisterResources(IProgress<double> progress)
        {
            await base.RegisterResources(progress).ConfigureAwait(false);

            RegisterSingleInstance<IHttpResultFactory>(new HttpResultFactory(LogManager, FileSystemManager, JsonSerializer));

            RegisterSingleInstance<IServerApplicationHost>(this);
            RegisterSingleInstance<IServerApplicationPaths>(ApplicationPaths);

            RegisterSingleInstance(ServerConfigurationManager);

            LocalizationManager = new LocalizationManager(ServerConfigurationManager, FileSystemManager, JsonSerializer);
            RegisterSingleInstance(LocalizationManager);

            RegisterSingleInstance<IWebSocketServer>(() => new AlchemyServer(Logger));

            RegisterSingleInstance<IBlurayExaminer>(() => new BdInfoExaminer());

            UserDataManager = new UserDataManager(LogManager);
            RegisterSingleInstance(UserDataManager);

            UserRepository = await GetUserRepository().ConfigureAwait(false);
            RegisterSingleInstance(UserRepository);

            DisplayPreferencesRepository = new SqliteDisplayPreferencesRepository(ApplicationPaths, JsonSerializer, LogManager);
            RegisterSingleInstance(DisplayPreferencesRepository);

            ItemRepository = new SqliteItemRepository(ApplicationPaths, JsonSerializer, LogManager);
            RegisterSingleInstance(ItemRepository);

            ProviderRepository = new SqliteProviderInfoRepository(ApplicationPaths, LogManager);
            RegisterSingleInstance(ProviderRepository);

            FileOrganizationRepository = await GetFileOrganizationRepository().ConfigureAwait(false);
            RegisterSingleInstance(FileOrganizationRepository);

            UserManager = new UserManager(LogManager.GetLogger("UserManager"), ServerConfigurationManager, UserRepository);
            RegisterSingleInstance(UserManager);

            LibraryManager = new LibraryManager(Logger, TaskManager, UserManager, ServerConfigurationManager, UserDataManager, () => LibraryMonitor, FileSystemManager, () => ProviderManager);
            RegisterSingleInstance(LibraryManager);

            var musicManager = new MusicManager(LibraryManager);
            RegisterSingleInstance<IMusicManager>(new MusicManager(LibraryManager));

            LibraryMonitor = new LibraryMonitor(LogManager, TaskManager, LibraryManager, ServerConfigurationManager, FileSystemManager);
            RegisterSingleInstance(LibraryMonitor);

            ProviderManager = new ProviderManager(HttpClient, ServerConfigurationManager, LibraryMonitor, LogManager, FileSystemManager);
            RegisterSingleInstance(ProviderManager);

            SeriesOrderManager = new SeriesOrderManager();
            RegisterSingleInstance(SeriesOrderManager);

            RegisterSingleInstance<ISearchEngine>(() => new SearchEngine(LogManager, LibraryManager, UserManager));

            HttpServer = ServerFactory.CreateServer(this, LogManager, "Media Browser", "mediabrowser", "dashboard/index.html");
            RegisterSingleInstance(HttpServer, false);
            progress.Report(10);

            ServerManager = new ServerManager(this, JsonSerializer, LogManager.GetLogger("ServerManager"), ServerConfigurationManager);
            RegisterSingleInstance(ServerManager);

            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p => progress.Report((.75 * p) + 15));

            await RegisterMediaEncoder(innerProgress).ConfigureAwait(false);
            progress.Report(90);

            ImageProcessor = new ImageProcessor(LogManager.GetLogger("ImageProcessor"), ServerConfigurationManager.ApplicationPaths, FileSystemManager, JsonSerializer, MediaEncoder);
            RegisterSingleInstance(ImageProcessor);

            DtoService = new DtoService(Logger, LibraryManager, UserManager, UserDataManager, ItemRepository, ImageProcessor, ServerConfigurationManager, FileSystemManager, ProviderManager);
            RegisterSingleInstance(DtoService);

            SessionManager = new SessionManager(UserDataManager, ServerConfigurationManager, Logger, UserRepository, LibraryManager, UserManager, musicManager, DtoService, ImageProcessor);
            RegisterSingleInstance(SessionManager);

            var newsService = new Server.Implementations.News.NewsService(ApplicationPaths, JsonSerializer);
            RegisterSingleInstance<INewsService>(newsService);

            var fileOrganizationService = new FileOrganizationService(TaskManager, FileOrganizationRepository, LogManager.GetLogger("FileOrganizationService"), LibraryMonitor, LibraryManager, ServerConfigurationManager, FileSystemManager, ProviderManager);
            RegisterSingleInstance<IFileOrganizationService>(fileOrganizationService);

            progress.Report(15);

            EncodingManager = new EncodingManager(ServerConfigurationManager, FileSystemManager, Logger, ItemRepository,
                MediaEncoder);
            RegisterSingleInstance(EncodingManager);

            ChannelManager = new ChannelManager(UserManager, DtoService, LibraryManager, Logger, ServerConfigurationManager, FileSystemManager);
            RegisterSingleInstance(ChannelManager);

            var appThemeManager = new AppThemeManager(ApplicationPaths, FileSystemManager, JsonSerializer, Logger);
            RegisterSingleInstance<IAppThemeManager>(appThemeManager);

            var dlnaManager = new DlnaManager(XmlSerializer, FileSystemManager, ApplicationPaths, LogManager.GetLogger("Dlna"), JsonSerializer);
            RegisterSingleInstance<IDlnaManager>(dlnaManager);

            var dlnaEventManager = new EventManager(LogManager, HttpClient);
            RegisterSingleInstance<IEventManager>(dlnaEventManager);

            var contentDirectory = new ContentDirectory(dlnaManager, UserDataManager, ImageProcessor, DtoService, LibraryManager, LogManager, ServerConfigurationManager, UserManager, dlnaEventManager);
            RegisterSingleInstance<IContentDirectory>(contentDirectory);

            var collectionManager = new CollectionManager(LibraryManager, FileSystemManager, LibraryMonitor);
            RegisterSingleInstance<ICollectionManager>(collectionManager);

            LiveTvManager = new LiveTvManager(ServerConfigurationManager, FileSystemManager, Logger, ItemRepository, ImageProcessor, UserDataManager, DtoService, UserManager, LibraryManager, TaskManager);
            RegisterSingleInstance(LiveTvManager);

            NotificationManager = new NotificationManager(LogManager, UserManager, ServerConfigurationManager);
            RegisterSingleInstance(NotificationManager);

            var displayPreferencesTask = Task.Run(async () => await ConfigureDisplayPreferencesRepositories().ConfigureAwait(false));
            var itemsTask = Task.Run(async () => await ConfigureItemRepositories().ConfigureAwait(false));
            var userdataTask = Task.Run(async () => await ConfigureUserDataRepositories().ConfigureAwait(false));

            await ConfigureNotificationsRepository().ConfigureAwait(false);
            progress.Report(92);

            await Task.WhenAll(itemsTask, displayPreferencesTask, userdataTask).ConfigureAwait(false);
            progress.Report(100);

            SetStaticProperties();

            await ((UserManager)UserManager).Initialize().ConfigureAwait(false);

            SetKernelProperties();
        }

        protected override INetworkManager CreateNetworkManager()
        {
            return new NetworkManager();
        }

        protected override IFileSystem CreateFileSystemManager()
        {
            return FileSystemFactory.CreateFileSystemManager(LogManager);
        }

        /// <summary>
        /// Registers the media encoder.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task RegisterMediaEncoder(IProgress<double> progress)
        {
            var info = await new FFMpegDownloader(Logger, ApplicationPaths, HttpClient, ZipClient, FileSystemManager).GetFFMpegInfo(progress).ConfigureAwait(false);

            MediaEncoder = new MediaEncoder(LogManager.GetLogger("MediaEncoder"), ApplicationPaths, JsonSerializer, info.Path, info.ProbePath, info.Version, FileSystemManager);
            RegisterSingleInstance(MediaEncoder);
        }

        /// <summary>
        /// Sets the kernel properties.
        /// </summary>
        private void SetKernelProperties()
        {
            LocalizedStrings.StringFiles = GetExports<LocalizedStringData>();
        }

        /// <summary>
        /// Gets the user repository.
        /// </summary>
        /// <returns>Task{IUserRepository}.</returns>
        private async Task<IUserRepository> GetUserRepository()
        {
            var repo = new SqliteUserRepository(JsonSerializer, LogManager, ApplicationPaths);

            await repo.Initialize().ConfigureAwait(false);

            return repo;
        }

        /// <summary>
        /// Gets the file organization repository.
        /// </summary>
        /// <returns>Task{IUserRepository}.</returns>
        private async Task<IFileOrganizationRepository> GetFileOrganizationRepository()
        {
            var repo = new SqliteFileOrganizationRepository(LogManager, ServerConfigurationManager.ApplicationPaths);

            await repo.Initialize().ConfigureAwait(false);

            return repo;
        }

        /// <summary>
        /// Configures the repositories.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task ConfigureNotificationsRepository()
        {
            var repo = new SqliteNotificationsRepository(LogManager, ApplicationPaths);

            await repo.Initialize().ConfigureAwait(false);

            NotificationsRepository = repo;

            RegisterSingleInstance(NotificationsRepository);
        }

        /// <summary>
        /// Configures the repositories.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task ConfigureDisplayPreferencesRepositories()
        {
            await DisplayPreferencesRepository.Initialize().ConfigureAwait(false);
        }

        /// <summary>
        /// Configures the item repositories.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task ConfigureItemRepositories()
        {
            await ItemRepository.Initialize().ConfigureAwait(false);

            await ProviderRepository.Initialize().ConfigureAwait(false);

            ((LibraryManager)LibraryManager).ItemRepository = ItemRepository;
        }

        /// <summary>
        /// Configures the user data repositories.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task ConfigureUserDataRepositories()
        {
            var repo = new SqliteUserDataRepository(ApplicationPaths, LogManager);

            await repo.Initialize().ConfigureAwait(false);

            ((UserDataManager)UserDataManager).Repository = repo;
        }

        /// <summary>
        /// Dirty hacks
        /// </summary>
        private void SetStaticProperties()
        {
            // For now there's no real way to inject these properly
            BaseItem.Logger = LogManager.GetLogger("BaseItem");
            BaseItem.ConfigurationManager = ServerConfigurationManager;
            BaseItem.LibraryManager = LibraryManager;
            BaseItem.ProviderManager = ProviderManager;
            BaseItem.LocalizationManager = LocalizationManager;
            BaseItem.ItemRepository = ItemRepository;
            User.XmlSerializer = XmlSerializer;
            User.UserManager = UserManager;
            LocalizedStrings.ApplicationPaths = ApplicationPaths;
            Folder.UserManager = UserManager;
            BaseItem.FileSystem = FileSystemManager;
            BaseItem.UserDataManager = UserDataManager;
        }

        /// <summary>
        /// Finds the parts.
        /// </summary>
        protected override void FindParts()
        {
            if (IsFirstRun)
            {
                RegisterServerWithAdministratorAccess();
            }

            base.FindParts();

            HttpServer.Init(GetExports<IRestfulService>(false));

            ServerManager.AddWebSocketListeners(GetExports<IWebSocketListener>(false));

            StartServer(true);

            LibraryManager.AddParts(GetExports<IResolverIgnoreRule>(),
                                    GetExports<IVirtualFolderCreator>(),
                                    GetExports<IItemResolver>(),
                                    GetExports<IIntroProvider>(),
                                    GetExports<IBaseItemComparer>(),
                                    GetExports<ILibraryPostScanTask>());

            ProviderManager.AddParts(GetExports<IImageProvider>(), GetExports<IMetadataService>(), GetExports<IMetadataProvider>(),
                                    GetExports<IMetadataSaver>(),
                                    GetExports<IImageSaver>(),
                                    GetExports<IExternalId>());

            SeriesOrderManager.AddParts(GetExports<ISeriesOrderProvider>());

            ImageProcessor.AddParts(GetExports<IImageEnhancer>());

            LiveTvManager.AddParts(GetExports<ILiveTvService>());

            SessionManager.AddParts(GetExports<ISessionControllerFactory>());

            ChannelManager.AddParts(GetExports<IChannel>());

            NotificationManager.AddParts(GetExports<INotificationService>(), GetExports<INotificationTypeFactory>());
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <param name="retryOnFailure">if set to <c>true</c> [retry on failure].</param>
        private void StartServer(bool retryOnFailure)
        {
            try
            {
                ServerManager.Start(HttpServerUrlPrefixes, ServerConfigurationManager.Configuration.EnableHttpLevelLogging);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error starting http server", ex);

                if (retryOnFailure)
                {
                    RegisterServerWithAdministratorAccess();

                    StartServer(false);
                }
                else
                {
                    throw;
                }
            }

            ServerManager.StartWebSocketServer();
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnConfigurationUpdated(object sender, EventArgs e)
        {
            base.OnConfigurationUpdated(sender, e);

            HttpServer.EnableHttpRequestLogging = ServerConfigurationManager.Configuration.EnableHttpLevelLogging;

            if (!HttpServer.UrlPrefixes.SequenceEqual(HttpServerUrlPrefixes, StringComparer.OrdinalIgnoreCase))
            {
                NotifyPendingRestart();
            }

            else if (!ServerManager.SupportsNativeWebSocket && ServerManager.WebSocketPortNumber != ServerConfigurationManager.Configuration.LegacyWebSocketPortNumber)
            {
                NotifyPendingRestart();
            }
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        public override async Task Restart()
        {
            if (!CanSelfRestart)
            {
                throw new InvalidOperationException("The server is unable to self-restart. Please restart manually.");
            }

            try
            {
                await SessionManager.SendServerRestartNotification(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error sending server restart notification", ex);
            }

            NativeApp.Restart();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        public override bool CanSelfUpdate
        {
            get
            {
#if DEBUG
                return false;
#endif
#pragma warning disable 162
                return NativeApp.CanSelfUpdate;
#pragma warning restore 162
            }
        }

        /// <summary>
        /// Gets the composable part assemblies.
        /// </summary>
        /// <returns>IEnumerable{Assembly}.</returns>
        protected override IEnumerable<Assembly> GetComposablePartAssemblies()
        {
            var list = GetPluginAssemblies()
                .ToList();

            // Gets all plugin assemblies by first reading all bytes of the .dll and calling Assembly.Load against that
            // This will prevent the .dll file from getting locked, and allow us to replace it when needed

            // Include composable parts in the Api assembly 
            list.Add(typeof(ApiEntryPoint).Assembly);

            // Include composable parts in the Dashboard assembly 
            list.Add(typeof(DashboardService).Assembly);

            // Include composable parts in the Model assembly 
            list.Add(typeof(SystemInfo).Assembly);

            // Include composable parts in the Common assembly 
            list.Add(typeof(IApplicationHost).Assembly);

            // Include composable parts in the Controller assembly 
            list.Add(typeof(IServerApplicationHost).Assembly);

            // Include composable parts in the Providers assembly 
            list.Add(typeof(ProviderUtils).Assembly);

            // Common implementations
            list.Add(typeof(TaskManager).Assembly);

            // Server implementations
            list.Add(typeof(ServerApplicationPaths).Assembly);

            // MediaEncoding
            list.Add(typeof(MediaEncoder).Assembly);

            // Dlna 
            list.Add(typeof(DlnaEntryPoint).Assembly);

            list.AddRange(Assemblies.GetAssembliesWithParts());

            // Include composable parts in the running assembly
            list.Add(GetType().Assembly);

            return list;
        }

        /// <summary>
        /// Gets the plugin assemblies.
        /// </summary>
        /// <returns>IEnumerable{Assembly}.</returns>
        private IEnumerable<Assembly> GetPluginAssemblies()
        {
            try
            {
                return Directory.EnumerateFiles(ApplicationPaths.PluginsPath, "*.dll", SearchOption.TopDirectoryOnly)
                    .Select(LoadAssembly)
                    .Where(a => a != null)
                    .ToList();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<Assembly>();
            }
        }

        private readonly string _systemId = Environment.MachineName.GetMD5().ToString();

        /// <summary>
        /// Gets the system status.
        /// </summary>
        /// <returns>SystemInfo.</returns>
        public virtual SystemInfo GetSystemInfo()
        {
            return new SystemInfo
            {
                HasPendingRestart = HasPendingRestart,
                Version = ApplicationVersion.ToString(),
                IsNetworkDeployed = CanSelfUpdate,
                WebSocketPortNumber = ServerManager.WebSocketPortNumber,
                SupportsNativeWebSocket = ServerManager.SupportsNativeWebSocket,
                FailedPluginAssemblies = FailedAssemblies.ToList(),
                InProgressInstallations = InstallationManager.CurrentInstallations.Select(i => i.Item1).ToList(),
                CompletedInstallations = InstallationManager.CompletedInstallations.ToList(),
                Id = _systemId,
                ProgramDataPath = ApplicationPaths.ProgramDataPath,
                LogPath = ApplicationPaths.LogDirectoryPath,
                ItemsByNamePath = ApplicationPaths.ItemsByNamePath,
                InternalMetadataPath = ApplicationPaths.InternalMetadataPath,
                CachePath = ApplicationPaths.CachePath,
                MacAddress = GetMacAddress(),
                HttpServerPortNumber = HttpServerPort,
                OperatingSystem = Environment.OSVersion.ToString(),
                CanSelfRestart = CanSelfRestart,
                CanSelfUpdate = CanSelfUpdate,
                WanAddress = GetWanAddress(),
                HasUpdateAvailable = HasUpdateAvailable,
                SupportsAutoRunAtStartup = SupportsAutoRunAtStartup,
                TranscodingTempPath = ApplicationPaths.TranscodingTempPath,
                IsRunningAsService = IsRunningAsService,
                ServerName = string.IsNullOrWhiteSpace(ServerConfigurationManager.Configuration.ServerName) ? Environment.MachineName : ServerConfigurationManager.Configuration.ServerName
            };
        }

        public int HttpServerPort
        {
            get { return ServerConfigurationManager.Configuration.HttpServerPortNumber; }
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private string GetWanAddress()
        {
            var ip = ServerConfigurationManager.Configuration.WanDdns;

            if (string.IsNullOrWhiteSpace(ip))
            {
                ip = WanAddressEntryPoint.WanAddress;
            }

            if (!string.IsNullOrEmpty(ip))
            {
                if (!ip.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !ip.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    ip = "http://" + ip;
                }

                return ip + ":" + ServerConfigurationManager.Configuration.HttpServerPortNumber.ToString(_usCulture);
            }

            return null;
        }

        /// <summary>
        /// Gets the mac address.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetMacAddress()
        {
            try
            {
                return NetworkManager.GetMacAddress();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error getting mac address", ex);
                return null;
            }
        }

        /// <summary>
        /// Shuts down.
        /// </summary>
        public override async Task Shutdown()
        {
            try
            {
                await SessionManager.SendServerShutdownNotification(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error sending server shutdown notification", ex);
            }

            NativeApp.Shutdown();
        }

        /// <summary>
        /// Registers the server with administrator access.
        /// </summary>
        private void RegisterServerWithAdministratorAccess()
        {
            Logger.Info("Requesting administrative access to authorize http server");

            try
            {
                ServerAuthorization.AuthorizeServer(
                    ServerConfigurationManager.Configuration.HttpServerPortNumber,
                    HttpServerUrlPrefixes.First(),
                    ServerConfigurationManager.Configuration.LegacyWebSocketPortNumber,
                    UdpServerEntryPoint.PortNumber,
                    ConfigurationManager.CommonApplicationPaths.TempDirectory);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error authorizing server", ex);
            }
        }

        public event EventHandler HasUpdateAvailableChanged;

        private bool _hasUpdateAvailable;
        public bool HasUpdateAvailable
        {
            get { return _hasUpdateAvailable; }
            set
            {
                var fireEvent = value && !_hasUpdateAvailable;

                _hasUpdateAvailable = value;

                if (fireEvent)
                {
                    EventHelper.FireEventIfNotNull(HasUpdateAvailableChanged, this, EventArgs.Empty, Logger);
                }
            }
        }

        /// <summary>
        /// Checks for update.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task{CheckForUpdateResult}.</returns>
        public override async Task<CheckForUpdateResult> CheckForApplicationUpdate(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var availablePackages = await InstallationManager.GetAvailablePackagesWithoutRegistrationInfo(cancellationToken).ConfigureAwait(false);

            var version = InstallationManager.GetLatestCompatibleVersion(availablePackages, Constants.MbServerPkgName, null, ApplicationVersion,
                                                           ConfigurationManager.CommonConfiguration.SystemUpdateLevel);

            HasUpdateAvailable = version != null;

            return version != null ? new CheckForUpdateResult { AvailableVersion = version.version, IsUpdateAvailable = version.version > ApplicationVersion, Package = version } :
                       new CheckForUpdateResult { AvailableVersion = ApplicationVersion, IsUpdateAvailable = false };
        }

        /// <summary>
        /// Updates the application.
        /// </summary>
        /// <param name="package">The package that contains the update</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public override async Task UpdateApplication(PackageVersionInfo package, CancellationToken cancellationToken, IProgress<double> progress)
        {
            await InstallationManager.InstallPackage(package, progress, cancellationToken).ConfigureAwait(false);

            HasUpdateAvailable = false;

            OnApplicationUpdated(package.version);
        }

        /// <summary>
        /// Configures the automatic run at startup.
        /// </summary>
        /// <param name="autorun">if set to <c>true</c> [autorun].</param>
        protected override void ConfigureAutoRunAtStartup(bool autorun)
        {
            if (SupportsAutoRunAtStartup)
            {
                Autorun.Configure(autorun);
            }
        }
    }
}
