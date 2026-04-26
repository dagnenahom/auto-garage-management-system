using SmartGarageSystem.Services;
using SmartGarageSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartGarageSystem.ViewModels
{

    public class DashboardViewModel : BaseViewModel
    {
        private readonly INavigationService _navigation;

        // Example summary properties – you can later bind them to live data from services
        public int TotalVehicles { get; private set; } = 42;
        public int TotalCustomers { get; private set; } = 120;
        public int ActiveServices { get; private set; } = 5;
        public int CompletedToday { get; private set; } = 8;

        // Quick navigation commands (optional)
        public ICommand NavigateToVehiclesCommand { get; }
        public ICommand NavigateToCustomersCommand { get; }
        public ICommand NavigateToServicesCommand { get; }

        // You could also have a list of recent activities
        public ObservableCollection<string> RecentActivities { get; } = new ObservableCollection<string>
        {
            "Oil change completed for Toyota Camry (ABC-123)",
            "New customer added: John Doe",
            "Brake pad replacement scheduled for Honda Civic",
            "Invoice #1023 generated for Tesla Model 3"
        };

        // Constructor
        public DashboardViewModel(INavigationService navigation)
        {
            _navigation = navigation;

            // Wire up quick navigation commands
            
        }

        // If you later fetch data asynchronously, add a method like:
        // public async Task LoadDataAsync() { ... }
    }
}