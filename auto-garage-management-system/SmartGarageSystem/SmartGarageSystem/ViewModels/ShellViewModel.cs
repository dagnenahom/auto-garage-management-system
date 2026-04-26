using SmartGarageSystem.Services;
using SmartGarageSystem.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace SmartGarageSystem.ViewModels
{
    public class ShellViewModel : BaseViewModel
    {
        private readonly INavigationService _navigation;
        public BaseViewModel CurrentView => _navigation.CurrentView;
        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }

        public ShellViewModel(INavigationService navigation)
        {
            _navigation = navigation;
            _navigation.CurrentViewChanged += () => OnPropertyChanged(nameof(CurrentView));

            NavigateCommand = new RelayCommand(param => {
                string viewName = param as string;
                switch (viewName)
                {
                    case "Dashboard":
                        _navigation.NavigateTo<DashboardViewModel>();
                        break;
                }
            });

            LogoutCommand = new RelayCommand(_ => {
                // Close Shell, show Login again
                Application.Current.MainWindow.Close();
                new LoginWindow().Show();
            });

            // Default page: Dashboard
            _navigation.NavigateTo<DashboardViewModel>();
        }
    }
}


