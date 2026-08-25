using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Project_Nook.Infrastructure;
using Project_Nook.Services;
using Project_Nook.ViewModels;
using Project_Nook.UI;
using Project_Nook.Widgets;
using Project_Nook.Widgets.Descriptors;
using System;
using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;

namespace Project_Nook
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public IServiceProvider Services { get; }

        public App()
        {
            Services = ConfigureServices();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddDebug();
            });

            // Services
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<IStorageService, StorageService>();
            services.AddSingleton<IShelfService, ShelfService>();
            
            // System Events & Triggers
            services.AddSingleton<Project_Nook.Services.SystemEvents.AudioEventService>();
            services.AddSingleton<Project_Nook.Services.SystemEvents.MediaEventService>();
            services.AddSingleton<Project_Nook.Services.SystemEvents.PowerEventService>();
            services.AddSingleton<Project_Nook.Services.SystemEvents.ForegroundWindowService>();
            services.AddSingleton<TriggerEngine>();
            
            // Widgets: one descriptor per widget, discovered by the registry through IEnumerable injection.
            // Adding a widget is a descriptor here plus its view model below.
            services.AddSingleton<IWidgetDescriptor, MusicWidgetDescriptor>();
            services.AddSingleton<IWidgetDescriptor, CalendarWidgetDescriptor>();
            services.AddSingleton<IWidgetDescriptor, VolumeWidgetDescriptor>();
            services.AddSingleton<IWidgetDescriptor, TimerWidgetDescriptor>();
            services.AddSingleton<IWidgetDescriptor, SystemStatsWidgetDescriptor>();
            services.AddSingleton<IWidgetDescriptor, NotesWidgetDescriptor>();
            services.AddSingleton<IWidgetDescriptor, ClipboardWidgetDescriptor>();
            services.AddSingleton<IWidgetDescriptor, ScreenshotWidgetDescriptor>();
            services.AddSingleton<IWidgetDescriptor, QuickLaunchWidgetDescriptor>();
            services.AddSingleton<WidgetRegistry>();

            services.AddSingleton<SystemStatsService>();
            services.AddSingleton<IWidgetDataService, WidgetDataService>();
            services.AddSingleton<Project_Nook.Services.SystemEvents.ClipboardEventService>();
            services.AddSingleton<ScreenCaptureService>();

            // Screenshot needs the island to hide itself before the shutter; lazy for the same
            // construction-order reason as the trigger engine.
            services.AddSingleton<Func<IslandViewModel>>(sp => () => sp.GetRequiredService<IslandViewModel>());

            // The timer widget notifies through the trigger engine, which in turn depends on the island
            // view model. Resolving it lazily keeps that from becoming a construction-order problem.
            services.AddSingleton<Func<TriggerEngine>>(sp => () => sp.GetRequiredService<TriggerEngine>());

            services.AddSingleton<ControlCenterViewModel>();
            services.AddSingleton<IslandViewModel>();
            services.AddSingleton<ShelfViewModel>();
            services.AddSingleton<WidgetDashboardViewModel>();
            services.AddSingleton<MusicWidgetViewModel>();
            services.AddSingleton<CalendarWidgetViewModel>();
            services.AddSingleton<VolumeWidgetViewModel>();
            services.AddSingleton<TimerWidgetViewModel>();
            services.AddSingleton<SystemStatsWidgetViewModel>();
            services.AddSingleton<NotesWidgetViewModel>();
            services.AddSingleton<ClipboardWidgetViewModel>();
            services.AddSingleton<ScreenshotWidgetViewModel>();
            services.AddSingleton<QuickLaunchWidgetViewModel>();

            // Windows
            services.AddSingleton<MainWindow>();
            services.AddSingleton<IslandWindow>();

            return services.BuildServiceProvider();
        }

        private TaskbarIcon? _notifyIcon;
        private Mutex? _mutex;
        private bool _ownsMutex;
        private readonly CancellationTokenSource _shutdown = new();
        private const string MutexName = "WinNookSingleInstanceMutex";
        private const string PipeName = "WinNookSingleInstancePipe";

        protected override void OnStartup(StartupEventArgs e)
        {
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(exeDir))
            {
                Environment.CurrentDirectory = exeDir;
            }

            _mutex = new Mutex(true, MutexName, out bool createdNew);
            _ownsMutex = createdNew;

            if (!createdNew)
            {
                try
                {
                    using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                    client.Connect(1000);
                    using var writer = new StreamWriter(client);
                    writer.WriteLine("ShowSettings");
                    writer.Flush();
                }
                catch { }

                Current.Shutdown();
                return;
            }

            Task.Run(() => ListenForSecondInstance(_shutdown.Token));

            InstallCrashHandlers();

            base.OnStartup(e);

            // Initialize the Island
            var islandWindow = Services.GetRequiredService<IslandWindow>();
            islandWindow.Show();

            // Start the Trigger Engine
            Services.GetRequiredService<TriggerEngine>();

            StartBackgroundWidgets();

            CreateTrayIcon();
        }

        /// <summary>
        /// Most widgets are built lazily, the first time the dashboard is shown. That is wrong for a widget
        /// whose whole job is to record things in the background: a clipboard history that only started
        /// listening once you happened to expand the notch would miss everything copied before then.
        /// So enabled background widgets are constructed at startup, and again whenever one is switched on.
        /// </summary>
        private void StartBackgroundWidgets()
        {
            var registry = Services.GetRequiredService<Widgets.WidgetRegistry>();

            void Ensure()
            {
                if (registry.IsEnabled(Widgets.WidgetIds.Clipboard))
                    Services.GetRequiredService<ClipboardWidgetViewModel>();
            }

            Ensure();
            registry.LayoutChanged += (s, e) => Ensure();
        }

        private async Task ListenForSecondInstance(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                    await server.WaitForConnectionAsync(token);

                    using var reader = new StreamReader(server);
                    var msg = await reader.ReadLineAsync(token);

                    if (msg == "ShowSettings")
                    {
                        Dispatcher.Invoke(() => ShowControlCenter());
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch
                {
                    // Something is wrong with the pipe. Without this pause the loop would fail and retry
                    // as fast as the CPU allows, which is worse than being briefly unresponsive.
                    try { await Task.Delay(TimeSpan.FromSeconds(1), token); }
                    catch (OperationCanceledException) { return; }
                }
            }
        }

        /// <summary>
        /// A tray app that dies to an unhandled exception just vanishes, with no window to show a message.
        /// Record what happened and, for UI-thread faults, keep running rather than taking the whole app down.
        /// </summary>
        private void InstallCrashHandlers()
        {
            DispatcherUnhandledException += (s, args) =>
            {
                LogCrash("Dispatcher", args.Exception);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
                LogCrash("AppDomain", args.ExceptionObject as Exception);

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                LogCrash("Task", args.Exception);
                args.SetObserved();
            };
        }

        private static void LogCrash(string source, Exception? ex)
        {
            if (ex == null) return;

            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinNook", "logs");
                Directory.CreateDirectory(dir);

                var path = Path.Combine(dir, $"error-{DateTime.Now:yyyy-MM-dd}.log");
                File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss}] {source}: {ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch { }
        }

        private void CreateTrayIcon()
        {
            _notifyIcon = new TaskbarIcon
            {
                ToolTipText = "WinNook"
            };

            // The .ico carries 16 through 256 packed, so Windows picks the right one for the tray's
            // current DPI instead of downscaling a single large PNG into mush.
            try
            {
                var iconStream = GetResourceStream(new Uri("pack://application:,,,/Assets/winnook.ico"))?.Stream;
                if (iconStream != null)
                {
                    using (iconStream)
                    {
                        _notifyIcon.Icon = new System.Drawing.Icon(iconStream, System.Windows.Forms.SystemInformation.SmallIconSize);
                    }
                }
            }
            catch { }

            var menu = new ContextMenu();
            
            var settingsItem = new MenuItem { Header = "Control Center" };
            settingsItem.Click += (s, e) => ShowControlCenter();
            
            var quitItem = new MenuItem { Header = "Quit WinNook" };
            quitItem.Click += (s, e) => Shutdown();

            menu.Items.Add(settingsItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(quitItem);

            _notifyIcon.ContextMenu = menu;
            _notifyIcon.TrayMouseDoubleClick += (s, e) => ShowControlCenter();
        }

        /// <summary>Tab indexes in the Control Center's sidebar, matching the ListBox order.</summary>
        public const int TabAppearance = 0;
        public const int TabEffect = 1;
        public const int TabActivity = 2;
        public const int TabApps = 3;
        public const int TabOptions = 4;
        public const int TabDeveloper = 5;

        public void ShowControlCenter(int tabIndex)
        {
            Services.GetRequiredService<ViewModels.ControlCenterViewModel>().SelectedTabIndex = tabIndex;
            ShowControlCenter();
        }

        public void ShowControlCenter()
        {
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _shutdown.Cancel();
            _notifyIcon?.Dispose();

            // Disposes every singleton that holds an OS resource: the WinEvent foreground hook, the
            // clipboard format listener, the WASAPI endpoint, and the settings writer's pending flush.
            // Without this the hooks outlive the process teardown and settings changed just before
            // quitting are never written.
            (Services as IDisposable)?.Dispose();

            if (_mutex != null)
            {
                if (_ownsMutex)
                {
                    try { _mutex.ReleaseMutex(); } catch { }
                }
                _mutex.Dispose();
            }

            _shutdown.Dispose();
            base.OnExit(e);
        }
    }
}
