using Microsoft.Extensions.DependencyInjection;
using SmartGarageSystem.Services;
using SmartGarageSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartGarageSystem.ViewModels;

namespace SmartGarageSystem.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private BaseViewModel _currentView;

        public BaseViewModel CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                CurrentViewChanged?.Invoke();
            }
        }

        public event Action CurrentViewChanged;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void NavigateTo<TViewModel>() where TViewModel : BaseViewModel
        {
            CurrentView = _serviceProvider.GetRequiredService<TViewModel>();
        }
    }
}