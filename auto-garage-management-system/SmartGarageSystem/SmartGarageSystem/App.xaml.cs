using Microsoft.Extensions.DependencyInjection;
using SmartGarageSystem.Services;
using SmartGarageSystem.ViewModels;
using SmartGarageSystem.Views;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Navigation;

namespace SmartGarageSystem
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);   // important: call base first

            // 1. Build the service provider
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // 2. Create and show the login window
            var loginWindow = new LoginWindow();
            loginWindow.Show();   // constructor now finds a non-null App.ServiceProvider
        }


        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddTransient<LoginViewModel>();
        }

    }
}
