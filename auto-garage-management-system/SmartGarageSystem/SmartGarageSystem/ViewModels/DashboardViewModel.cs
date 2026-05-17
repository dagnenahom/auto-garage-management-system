using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
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
        private readonly IDashboardService _dashboardService;

        // Summary numbers
        private int _totalVehicles;
        public int TotalVehicles { get => _totalVehicles; set { _totalVehicles = value; OnPropertyChanged(); } }

        private int _totalCustomers;
        public int TotalCustomers { get => _totalCustomers; set { _totalCustomers = value; OnPropertyChanged(); } }

        private int _openJobCards;
        public int OpenJobCards { get => _openJobCards; set { _openJobCards = value; OnPropertyChanged(); } }

        private int _lowStockItems;
        public int LowStockItems { get => _lowStockItems; set { _lowStockItems = value; OnPropertyChanged(); } }

        // Chart series (LiveCharts2)
        public ISeries[] VehiclesByMakeSeries { get; set; } = Array.Empty<ISeries>();
        public ISeries[] JobCardsByStatusSeries { get; set; } = Array.Empty<ISeries>();

        // X‑axis labels for the bar chart (vehicle makes)
        public string[] VehicleMakeLabels { get; set; } = Array.Empty<string>();

        // Recent activities (optional, keep as before)
        public ObservableCollection<string> RecentActivities { get; } = new();

        public ICommand RefreshCommand { get; }

        public DashboardViewModel(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
            RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());

            Task.Run(async () => await LoadDataAsync());
        }


        public async Task LoadDataAsync()
        {

            var data = await _dashboardService.GetDashboardDataAsync();

            TotalVehicles = data.TotalVehicles;
            TotalCustomers = data.TotalCustomers;
            OpenJobCards = data.OpenJobCards;
            LowStockItems = data.LowStockItems;

            // Recent activities
            RecentActivities.Clear();
            foreach (var activity in data.RecentActivities)
                RecentActivities.Add(activity);

            // Bar chart: Vehicles by Make
            VehicleMakeLabels = data.VehiclesByMake.Keys.ToArray();
            var vehicleValues = data.VehiclesByMake.Values.Select(v => (double)v).ToArray();

            VehiclesByMakeSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = vehicleValues,
                    Fill = new SolidColorPaint(SKColors.DodgerBlue),
                    Stroke = null
                }
            };

            // Pie chart: Job Cards by Status
            var statusLabels = data.JobCardsByStatus.Keys.ToArray();
            var statusValues = data.JobCardsByStatus.Values.Select(v => (double)v).ToArray();

            JobCardsByStatusSeries = statusLabels.Select((label, index) =>
                new PieSeries<double>
                {
                    Values = new double[] { statusValues[index] },
                    Name = label,
                    Fill = GetStatusColor(label)
                }).ToArray<ISeries>();

            OnPropertyChanged(nameof(VehiclesByMakeSeries));
            OnPropertyChanged(nameof(JobCardsByStatusSeries));
            OnPropertyChanged(nameof(VehicleMakeLabels));
        }

        private SolidColorPaint GetStatusColor(string status)
        {
            return status switch
            {
                "Open" => new SolidColorPaint(SKColors.Orange),
                "InProgress" => new SolidColorPaint(SKColors.DodgerBlue),
                "Completed" => new SolidColorPaint(SKColors.Green),
                "Closed" => new SolidColorPaint(SKColors.Gray),
                _ => new SolidColorPaint(SKColors.Black)
            };
        }
    }
}