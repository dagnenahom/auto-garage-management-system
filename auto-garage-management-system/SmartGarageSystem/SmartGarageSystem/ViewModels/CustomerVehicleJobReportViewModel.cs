using SmartGarageSystem.Models;
using SmartGarageSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartGarageSystem.ViewModels
{
    public class CustomerVehicleJobReportViewModel : BaseViewModel
    {
        private readonly IReportService _reportService;
        private CustomerVehicleJobReport _report;
        private bool _isLoading;

        public CustomerVehicleJobReport Report
        {
            get => _report;
            set { _report = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ICommand RefreshCommand { get; }

        public CustomerVehicleJobReportViewModel(IReportService reportService)
        {
            _reportService = reportService;
            RefreshCommand = new RelayCommand(async _ => await LoadAsync());
            Task.Run(async () => await LoadAsync());
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                Report = await _reportService.GetCustomerVehicleJobReportAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

}
