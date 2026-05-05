
using SmartGarageSystem.Services;
using SmartGarageSystem.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartGarageSystem.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private readonly IUserSession _userSession;
        private string _username;
        private string _errorMessage;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); LoginCommand.RaiseCanExecuteChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }

        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public RelayCommand LoginCommand { get; }

        public LoginViewModel(IAuthenticationService authService, IUserSession userSession)
        {
            _authService = authService;
            _userSession = userSession;
            LoginCommand = new RelayCommand(async param => await ExecuteLogin(param), CanExecuteLogin);
        }

        private bool CanExecuteLogin(object parameter) =>
            !string.IsNullOrWhiteSpace(Username);

        private async Task ExecuteLogin(object parameter)
        {
            var passwordBox = parameter as PasswordBox;
            if (passwordBox == null) return;

            string password = passwordBox.Password;
            AuthenticationResult result = await _authService.AuthenticateAsync(Username, password);

            if (result.IsSuccess)
            {
                // Store the logged‑in user globally
                _userSession.SetUser(result.AuthenticatedUser);


                // Close login window
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is LoginWindow)
                    {
                        window.Close();
                        break;
                    }
                }

                // Open Shell window
                var shell = new ShellWindow();
                Application.Current.MainWindow = shell;
                shell.Show();
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
    }
}
