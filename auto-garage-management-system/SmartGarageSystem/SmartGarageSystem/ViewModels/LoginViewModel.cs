
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SmartGarageSystem.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly Services.IAuthenticationService _authService;
        private string _username = string.Empty;
        public LoginViewModel(Services.IAuthenticationService authService)
        {
            _authService = authService;
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
        }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public string Username
        {
            get => _username;
            set { _username = value ?? string.Empty; OnPropertyChanged(); LoginCommand.RaiseCanExecuteChanged(); }
        }

        public string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }
        
        public RelayCommand LoginCommand { get; }

        
        private bool CanExecuteLogin(object parameter) =>
            !string.IsNullOrWhiteSpace(Username);

        private async void ExecuteLogin(object parameter)
        {
            var passwordBox = parameter as PasswordBox;
            string password = passwordBox?.Password ?? string.Empty;

            var result = await _authService.AuthenticateAsync(Username, password);
            if (result.IsSuccess)
            {
                MessageBox.Show("Login Success");
                
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
    }
}
