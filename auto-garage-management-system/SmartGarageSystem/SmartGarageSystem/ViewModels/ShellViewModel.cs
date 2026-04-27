using SmartGarageSystem.Views;
using SmartGarageSystem.Services;
using SmartGarageSystem.ViewModels;
using System.Windows.Input;
using System.Windows;

namespace SmartGarageSystem.ViewModels
{
    public class ShellViewModel : BaseViewModel
    {
        private readonly INavigationService _navigation;
        private readonly IUserSession _userSession;

        // ---- Current View (for ContentControl binding) ----
        public BaseViewModel CurrentView => _navigation.CurrentView;

        // ---- User display info ----
        public string FullName => _userSession.CurrentUser?.FullName ?? "Unknown";
        public string Username => _userSession.CurrentUser?.Username ?? "";

        // ---- Role‑based visibility properties (dynamic!) ----
        // You can freely add new roles (like "InventoryManager") in the DB and add checks here.
        public bool CanAccessDashboard => true;                // everyone
        public bool CanAccessVehicles => _userSession.IsInAnyRole("Admin", "Manager", "Mechanic");
        public bool CanAccessCustomers => _userSession.IsInAnyRole("Admin", "Manager");
        public bool CanAccessServices => _userSession.IsInAnyRole("Admin", "Mechanic", "ServiceManager");
        public bool CanAccessUsers => _userSession.IsInRole("Admin");   // only Admins manage users
        public bool CanAccessReports => _userSession.IsInAnyRole("Admin", "Manager");

        // ---- Navigation Commands (generic, used with CommandParameter) ----
        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }

        public ShellViewModel(INavigationService navigation, IUserSession userSession)
        {
            _navigation = navigation;
            _userSession = userSession;

            // React to navigation changes
            _navigation.CurrentViewChanged += () => OnPropertyChanged(nameof(CurrentView));

            // ---- NavigateCommand ----
            NavigateCommand = new RelayCommand(param =>
            {
                if (param is string viewName)
                {
                    switch (viewName)
                    {
                        case "Dashboard":
                            _navigation.NavigateTo<DashboardViewModel>();
                            break;
                            // Add more cases as needed
                    }
                }
            });

            // ---- LogoutCommand ----
            LogoutCommand = new RelayCommand(_ =>
            {
                // Clear the session
                _userSession.Clear();

                // Close the Shell
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is ShellWindow)
                    {
                        window.Close();
                        break;
                    }
                }

                // Show Login window again
                var loginWindow = new LoginWindow();
                loginWindow.Show();
            });

            // Default page: Dashboard (always accessible)
            _navigation.NavigateTo<DashboardViewModel>();
        }
    }
}