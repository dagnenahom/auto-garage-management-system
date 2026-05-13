using SmartGarageSystem.Models;
using SmartGarageSystem.Services;
using SmartGarageSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace SmartGarageSystem.ViewModels
{
    public class StockReportViewModel : BaseViewModel
    {
        private readonly IReportService _reportService;
        private StockReport _report;
        private bool _isLoading;

        public StockReport Report
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

        public StockReportViewModel(IReportService reportService)
        {
            _reportService = reportService;
            RefreshCommand = new RelayCommand(async _ => await LoadReportAsync());
            Task.Run(async () => await LoadReportAsync());
        }

        public async Task LoadReportAsync()
        {
            IsLoading = true;
            try
            {
                Report = await _reportService.GetStockReportAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}