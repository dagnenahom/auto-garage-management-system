using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
    
    public interface IDashboardService
    {
        Task<DashboardData> GetDashboardDataAsync();
    }

    public class DashboardData
    {
        // Summary cards
        public int TotalVehicles { get; set; }
        public int TotalCustomers { get; set; }
        public int OpenJobCards { get; set; }
        public int LowStockItems { get; set; }

        // Bar chart: vehicles per make
        public Dictionary<string, int> VehiclesByMake { get; set; } = new();

        // Pie chart: job card status distribution
        public Dictionary<string, int> JobCardsByStatus { get; set; } = new();
        // Optional: recent activities (e.g., "Vehicle ABC123 added", "Job Card #456 closed")
        public List<string> RecentActivities { get; set; } = new();
    }

}
