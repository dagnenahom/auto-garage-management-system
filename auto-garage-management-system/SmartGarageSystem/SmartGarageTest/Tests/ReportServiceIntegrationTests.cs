using SmartGarageSystem.Models;
using SmartGarageSystem.Services;
using SmartGarageTest.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace SmartGarageTest.Tests
{
    public class ReportServiceIntegrationTests : IClassFixture<DatabaseFixture>
    {
        private readonly IReportService _reportService;
        private readonly ICustomerService _customerService;
        private readonly IVehicleService _vehicleService;
        private readonly IJobCardService _jobCardService;
        private readonly IInventoryService _inventoryService;
        private readonly IUserService _userService;   // for creating mechanic users

        public ReportServiceIntegrationTests(DatabaseFixture fixture)
        {
            _reportService = fixture.ServiceProvider.GetRequiredService<IReportService>();
            _customerService = fixture.ServiceProvider.GetRequiredService<ICustomerService>();
            _vehicleService = fixture.ServiceProvider.GetRequiredService<IVehicleService>();
            _jobCardService = fixture.ServiceProvider.GetRequiredService<IJobCardService>();
            _inventoryService = fixture.ServiceProvider.GetRequiredService<IInventoryService>();
            _userService = fixture.ServiceProvider.GetRequiredService<IUserService>();
        }

        // -------------------- Helpers --------------------
        private async Task<int> CreateCustomer(string nameSuffix) =>
            await _customerService.CreateCustomerAsync(new Customer
            {
                FullName = $"ReportCust {nameSuffix} {Guid.NewGuid().ToString("N")[..6]}",
                Phone = "000",
                Email = "rpt@test.com"
            });

        private async Task<int> CreateVehicle(int customerId, string plateSuffix = "") =>
            await _vehicleService.CreateVehicleAsync(new Vehicle
            {
                CustomerId = customerId,
                LicensePlate = $"RPT-{plateSuffix}-{Guid.NewGuid().ToString("N")[..5]}",
                Make = "ReportMake",
                Model = "ReportModel",
                Year = 2022
            });

        private async Task<int> CreateMechanicUser()
        {
            var roles = await _userService.GetAllActiveRolesAsync();
            var mechRole = roles.FirstOrDefault(r => r.RoleName.Equals("Mechanic", StringComparison.OrdinalIgnoreCase))
                           ?? throw new InvalidOperationException("Mechanic role not found");
            var user = new User
            {
                Username = $"rptmech_{Guid.NewGuid().ToString("N")[..6]}",
                FullName = "Report Mechanic",
                PasswordHash = "test"
            };
            return await _userService.CreateUserAsync(user, new List<int> { mechRole.RoleId });
        }

        private async Task<int> CreateInventoryItem(string nameSuffix, int quantity, decimal price, int reorderLevel)
        {
            var item = new InventoryItem
            {
                PartName = $"RptItem {nameSuffix} {Guid.NewGuid().ToString("N")[..6]}",
                QuantityInStock = quantity,
                UnitPrice = price,
                ReorderLevel = reorderLevel
            };
            return await _inventoryService.CreateItemAsync(item);
        }

        // -------------------- STOCK REPORT --------------------
        [Fact]
        public async Task GetStockReport_EmptyInventory_ReturnsEmptyReport()
        {
            var report = await _reportService.GetStockReportAsync();
            // can't guarantee the database is empty, but we can check structure.
            Assert.NotNull(report);
            Assert.Equal(report.AllItems.Count, report.TotalItemsCount);
            Assert.Equal(report.AllItems.Sum(i => i.QuantityInStock * i.UnitPrice), report.TotalInventoryValue);
        }

        [Fact]
        public async Task GetStockReport_WithMixedStock_CalculatesCorrectly()
        {
            // Create items with known stock levels
            int id1 = await CreateInventoryItem("LowStock", 2, 10m, 5);   // low (2 <= 5)
            int id2 = await CreateInventoryItem("Normal", 20, 5m, 5);     // not low
            int id3 = await CreateInventoryItem("Zero", 0, 8m, 2);        // low (0 <= 2)

            // Fetch all items from DB to compute expected total value
            var allItems = await _inventoryService.GetAllItemsAsync();
            decimal expectedTotalValue = allItems.Sum(i => i.QuantityInStock * i.UnitPrice);

            var report = await _reportService.GetStockReportAsync();

            // Verify low stock items
            var lowStockIds = report.LowStockItems.Select(i => i.InventoryItemId).ToList();
            Assert.Contains(id1, lowStockIds);
            Assert.Contains(id3, lowStockIds);
            Assert.DoesNotContain(id2, lowStockIds);

            // Total value must match sum of all items (including pre-existing ones)
            Assert.Equal(expectedTotalValue, report.TotalInventoryValue);

            // Cleanup
            await _inventoryService.DeleteItemAsync(id1);
            await _inventoryService.DeleteItemAsync(id2);
            await _inventoryService.DeleteItemAsync(id3);
        }
        // -------------------- STOCK BALANCE REPORT --------------------
        [Fact]
        public async Task GetStockBalanceReport_ShouldListAllItemsWithCorrectTotalValue()
        {
            int id1 = await CreateInventoryItem("Bal1", 5, 20m, 2);
            int id2 = await CreateInventoryItem("Bal2", 3, 15m, 1);

            // Fetch all items from DB to get the full list and expected total
            var allItems = await _inventoryService.GetAllItemsAsync();
            decimal expectedTotal = allItems.Sum(i => i.QuantityInStock * i.UnitPrice);

            var report = await _reportService.GetStockBalanceReportAsync();
            Assert.NotNull(report);

            // Our test items must be present
            var item1 = report.Items.FirstOrDefault(i => i.InventoryItemId == id1);
            var item2 = report.Items.FirstOrDefault(i => i.InventoryItemId == id2);
            Assert.NotNull(item1);
            Assert.NotNull(item2);
            Assert.Equal(5, item1.QuantityInStock);
            Assert.Equal(20m, item1.UnitPrice);
            Assert.Equal(100m, item1.TotalValue);   // 5 * 20
            Assert.Equal(45m, item2.TotalValue);    // 3 * 15

            // Total value must equal sum of all items (not just ours)
            Assert.Equal(expectedTotal, report.TotalValue);

            // Optional: internal consistency – total of items list should match TotalValue
            Assert.Equal(report.TotalValue, report.Items.Sum(i => i.TotalValue));

            await _inventoryService.DeleteItemAsync(id1);
            await _inventoryService.DeleteItemAsync(id2);
        }
        // -------------------- CUSTOMER VEHICLE JOB REPORT --------------------
        [Fact]
        public async Task GetCustomerVehicleJobReport_ShouldBuildHierarchy()
        {
            // Setup
            int custId = await CreateCustomer("JobRpt");
            int vehId = await CreateVehicle(custId);
            int mechId = await CreateMechanicUser();

            // Create job cards
            var job1 = new JobCard { VehicleId = vehId, AssignedUserId = mechId, Status = "Open", Description = "Fix brakes" };
            var job2 = new JobCard { VehicleId = vehId, AssignedUserId = mechId, Status = "InProgress", Description = "Oil change" };
            int jobId1 = await _jobCardService.CreateJobCardAsync(job1);
            int jobId2 = await _jobCardService.CreateJobCardAsync(job2);

            var report = await _reportService.GetCustomerVehicleJobReportAsync();

            // Find the customer we created
            var custReport = report.Customers.FirstOrDefault(c => c.CustomerId == custId);
            Assert.NotNull(custReport);
            Assert.Equal(1, custReport.Vehicles.Count);

            var vehReport = custReport.Vehicles[0];
            Assert.Equal(vehId, vehReport.VehicleId);
            Assert.Equal(2, vehReport.JobCards.Count);

            var statuses = vehReport.JobCards.Select(j => j.Status).ToList();
            Assert.Contains("Open", statuses);
            Assert.Contains("InProgress", statuses);

            // Cleanup order matters (job cards first, then vehicle, then customer, then mechanic)
            await _jobCardService.DeleteJobCardAsync(jobId1);
            await _jobCardService.DeleteJobCardAsync(jobId2);
            await _vehicleService.DeleteVehicleAsync(vehId);
            await _customerService.DeleteCustomerAsync(custId);
            await _userService.DeleteUserAsync(mechId);
        }

        [Fact]
        public async Task GetCustomerVehicleJobReport_CustomerWithNoVehicles_ShouldAppearWithEmptyVehicles()
        {
            int custId = await CreateCustomer("NoVehicle");
            var report = await _reportService.GetCustomerVehicleJobReportAsync();
            var cust = report.Customers.FirstOrDefault(c => c.CustomerId == custId);
            Assert.NotNull(cust);
            Assert.Empty(cust.Vehicles);

            await _customerService.DeleteCustomerAsync(custId);
        }
    }
}