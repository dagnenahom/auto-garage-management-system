using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Models
{
    public class StockReport
    {
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public List<InventoryItem> AllItems { get; set; } = new();
        public List<InventoryItem> LowStockItems { get; set; } = new();
        public decimal TotalInventoryValue { get; set; }
        public int TotalItemsCount => AllItems.Count;
        public int LowStockCount => LowStockItems.Count;
    }
}
