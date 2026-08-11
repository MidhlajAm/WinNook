using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Project_Nook.Infrastructure;
using Project_Nook.Services;
using Project_Nook.ViewModels;
using System;
using System.Windows;

namespace Project_Nook
{
    public partial class App : Application
    {
        private Window? _window;
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
            
            services.AddSingleton<ControlCenterViewModel>();
            services.AddSingleton<IslandViewModel>();
            services.AddSingleton<ShelfViewModel>();

            // Windows
            services.AddTransient<MainWindow>();
            services.AddTransient<Project_Nook.UI.IslandWindow>();

            return services.BuildServiceProvider();
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            _window = Services.GetRequiredService<MainWindow>();
            _window.Show();

            _islandWindow = Services.GetRequiredService<Project_Nook.UI.IslandWindow>();
            _islandWindow.Show();
        }
    }
}
