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
            public string PageTitle => "Dashboard";
            public string Message => "The section is under construction.";
            public string SubMessage => "Please check back later for exciting new features!";
        }
    }