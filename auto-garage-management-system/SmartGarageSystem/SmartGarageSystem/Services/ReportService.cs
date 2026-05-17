using SmartGarageSystem.Models;
using SmartGarageSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
    public class ReportService : IReportService
    {
        private readonly IInventoryService _inventoryService;
        private readonly ICustomerService _customerService;
        private readonly IVehicleService _vehicleService;
        private readonly IJobCardService _jobCardService;

        public ReportService(IInventoryService inventoryService,
            ICustomerService customerService,
            IVehicleService vehicleService,
            IJobCardService jobCardService)
        {
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));
            _jobCardService = jobCardService ?? throw new ArgumentNullException(nameof(jobCardService));
        }

        public async Task<StockReport> GetStockReportAsync()
        {
            // Fetch all items from the inventory service (which is already abstracted)
            var allItems = await _inventoryService.GetAllItemsAsync();

            // Build the report
            var report = new StockReport
            {
                AllItems = allItems,
                LowStockItems = allItems.Where(i => i.QuantityInStock <= i.ReorderLevel).ToList(),
                TotalInventoryValue = allItems.Sum(i => i.QuantityInStock * i.UnitPrice)
            };

            return report;
        }
        public async Task<StockBalanceReport> GetStockBalanceReportAsync()
        {
            var items = await _inventoryService.GetAllItemsAsync();
            return new StockBalanceReport
            {
                Items = items.Select(i => new StockBalanceItem
                {
                    InventoryItemId = i.InventoryItemId,
                    PartName = i.PartName,
                    PartNumber = i.PartNumber,
                    QuantityInStock = i.QuantityInStock,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };
        }
        public async Task<CustomerVehicleJobReport> GetCustomerVehicleJobReportAsync()
        {
            // Fetch all data (could be parallelized)
            var customers = await _customerService.GetAllCustomersAsync();
            var vehicles = await _vehicleService.GetAllVehiclesAsync();   // includes LicensePlate, Make, Model, CustomerId
            var jobCards = await _jobCardService.GetAllJobCardsAsync();   // includes VehicleId, Status, DateCreated, Description

            // Build hierarchy
            var report = new CustomerVehicleJobReport();
            foreach (var customer in customers)
            {
                var custItem = new CustomerReportItem
                {
                    CustomerId = customer.CustomerId,
                    FullName = customer.FullName
                };

                var customerVehicles = vehicles.Where(v => v.CustomerId == customer.CustomerId).ToList();
                foreach (var vehicle in customerVehicles)
                {
                    var vehItem = new VehicleReportItem
                    {
                        VehicleId = vehicle.VehicleId,
                        LicensePlate = vehicle.LicensePlate,
                        Make = vehicle.Make,
                        Model = vehicle.Model
                    };

                    var vehicleJobCards = jobCards.Where(j => j.VehicleId == vehicle.VehicleId).ToList();
                    foreach (var jc in vehicleJobCards)
                    {
                        vehItem.JobCards.Add(new JobCardReportItem
                        {
                            JobCardId = jc.JobCardId,
                            Status = jc.Status,
                            DateCreated = jc.DateCreated.ToString("yyyy-MM-dd"),
                            Description = jc.Description
                        });
                    }
                    custItem.Vehicles.Add(vehItem);
                }
                report.Customers.Add(custItem);
            }
            return report;
        }
    }
}