using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Project_Nook.Infrastructure;
using Project_Nook.Services;
using Project_Nook.ViewModels;
using Project_Nook.UI;
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
        private Window? _islandWindow;
        
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
            
            services.AddSingleton<ControlCenterViewModel>();
            services.AddSingleton<IslandViewModel>();
            services.AddSingleton<ShelfViewModel>();
            services.AddSingleton<MusicWidgetViewModel>();

            // Windows
            services.AddSingleton<MainWindow>();
            services.AddSingleton<IslandWindow>();

            return services.BuildServiceProvider();
        }

        private TaskbarIcon _notifyIcon;
        private Mutex _mutex;
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

            Task.Run(ListenForSecondInstance);

            base.OnStartup(e);

            // Initialize the Island
            var islandWindow = Services.GetRequiredService<IslandWindow>();
            islandWindow.Show();

            // Start the Trigger Engine
            Services.GetRequiredService<TriggerEngine>();

            CreateTrayIcon();
        }

        private async Task ListenForSecondInstance()
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                    await server.WaitForConnectionAsync();
                    
                    using var reader = new StreamReader(server);
                    var msg = await reader.ReadLineAsync();
                    
                    if (msg == "ShowSettings")
                    {
                        Dispatcher.Invoke(() => ShowControlCenter());
                    }
                }
                catch { }
            }
        }

        private void CreateTrayIcon()
        {
            _notifyIcon = new TaskbarIcon
            {
                ToolTipText = "WinNook"
            };

            // Safely set the icon using the existing asset
            try
            {
                _notifyIcon.IconSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/StoreLogo.png"));
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
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
