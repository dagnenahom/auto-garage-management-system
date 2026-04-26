using SmartGarageSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
    public interface INavigationService
    {
        BaseViewModel CurrentView { get; set; }
        event Action CurrentViewChanged;
        void NavigateTo<TViewModel>() where TViewModel : BaseViewModel;
    }
}