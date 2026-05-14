using SmartGarageSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartGarageSystem.ViewModels
{
    public class StockBalanceReportViewModel : BaseViewModel
    {
        private readonly IReportService _reportService;
        private StockBalanceReport _report;
        private bool _isLoading;

        public StockBalanceReport Report
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

        public StockBalanceReportViewModel(IReportService reportService)
        {
            _reportService = reportService;
            RefreshCommand = new RelayCommand(async _ => await LoadReportAsync());
            Task.Run(async () => await LoadReportAsync());
        }

        private async Task LoadReportAsync()
        {
            IsLoading = true;
            try
            {
                Report = await _reportService.GetStockBalanceReportAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

}
