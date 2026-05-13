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

        public ReportService(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
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
    }
}