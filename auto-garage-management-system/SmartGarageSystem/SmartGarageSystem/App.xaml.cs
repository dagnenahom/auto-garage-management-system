using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartGarageSystem.Services;
using SmartGarageSystem.ViewModels;
using SmartGarageSystem.Views;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Extensions.Configuration.Json;

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
        
            // Configuration (read connection string from appsettings.json)
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            services.AddSingleton<IConfiguration>(configuration);

            // Services
            services.AddSingleton<IUserSession, UserSession>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<ICustomerService, CustomerService>();
            services.AddSingleton<IVehicleService, VehicleService>();
            services.AddSingleton<IInventoryService, InventoryService>();
            services.AddSingleton<IJobCardService, JobCardService>();

            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<INavigationService, Services.NavigationService>();

            services.AddSingleton<IUserRepository, SqlUserRepository>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();

            // ViewModels
            services.AddTransient<ShellViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<LoginViewModel>();

            services.AddTransient<UserManagementViewModel>();
            services.AddTransient<CustomerManagementViewModel>();
            services.AddTransient<VehiclesViewModel>();
            services.AddTransient<InventoryViewModel>();
            services.AddTransient<JobCardViewModel>();
        }
    }
}
