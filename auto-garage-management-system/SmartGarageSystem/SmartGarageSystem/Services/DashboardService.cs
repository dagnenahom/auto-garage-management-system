using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IVehicleService _vehicleService;
        private readonly ICustomerService _customerService;
        private readonly IJobCardService _jobCardService;
        private readonly IInventoryService _inventoryService;

        public DashboardService(
            IVehicleService vehicleService,
            ICustomerService customerService,
            IJobCardService jobCardService,
            IInventoryService inventoryService)
        {
            _vehicleService = vehicleService;
            _customerService = customerService;
            _jobCardService = jobCardService;
            _inventoryService = inventoryService;
        }
        public async Task<DashboardData> GetDashboardDataAsync()
        {
            var vehicles = await _vehicleService.GetAllVehiclesAsync();
            var customers = await _customerService.GetAllCustomersAsync();
            var jobCards = await _jobCardService.GetAllJobCardsAsync();
            var inventoryItems = await _inventoryService.GetAllItemsAsync();

            var data = new DashboardData
            {
                TotalVehicles = vehicles.Count,
                TotalCustomers = customers.Count,
                OpenJobCards = jobCards.Count(j => j.Status == "Open" || j.Status == "InProgress"),
                LowStockItems = inventoryItems.Count(i => i.QuantityInStock <= i.ReorderLevel),

                VehiclesByMake = vehicles
                    .GroupBy(v => v.Make)
                    .ToDictionary(g => g.Key, g => g.Count()),

                JobCardsByStatus = jobCards
                    .GroupBy(j => j.Status)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            // Build recent activities
            var activities = new List<string>();

            // Recent job cards (last 5, sorted by date descending)
            var recentJobs = jobCards
                .OrderByDescending(j => j.DateCreated)
                .Take(5);
            foreach (var job in recentJobs)
            {
                activities.Add($"Job card #{job.JobCardId} for {job.VehiclePlate} – {job.Status}");
            }

            // Recent customers (last 5, sorted by creation date? We don't have CreatedDate on Customer model...)
            // Since Customer lacks a CreatedDate, we could skip or just show the count.
            // Let's simply note total customers added (we can't sort). We'll omit individual entries.
            activities.Add($"{customers.Count} customers are registered in the system.");

            // Low stock items
            if (data.LowStockItems > 0)
            {
                activities.Add($"⚠️ {data.LowStockItems} inventory items are low on stock.");
            }

            data.RecentActivities = activities;
            return data;
        }
    }

}
