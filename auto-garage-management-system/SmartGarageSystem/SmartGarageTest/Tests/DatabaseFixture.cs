using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartGarageSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageTest.Tests
{
    public class DatabaseFixture : IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public DatabaseFixture()
        {
            var services = new ServiceCollection();

            // Build configuration from appsettings.json in the output folder
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: false, reloadOnChange: true)
                .Build();
            services.AddSingleton<IConfiguration>(configuration);

            // Register the real services (same as in your App.xaml.cs)
            services.AddSingleton<ICustomerService, CustomerService>();
            services.AddSingleton<IInventoryService, InventoryService>();
            services.AddSingleton<IVehicleService, VehicleService>();
            services.AddSingleton<IJobCardService, JobCardService>();
            // Password hasher
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            // Repository
            services.AddSingleton<IUserRepository, SqlUserRepository>();
            // User service
            services.AddSingleton<IUserService, UserService>();
            //Reports
            services.AddSingleton<IReportService, ReportService>();

            ServiceProvider = services.BuildServiceProvider();
        }

        public void Dispose()
        {
            // Cleanup if necessary
        }
    }
}