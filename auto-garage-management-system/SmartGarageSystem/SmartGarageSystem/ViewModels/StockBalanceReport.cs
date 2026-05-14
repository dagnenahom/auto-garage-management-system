using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.ViewModels
{
    public class StockBalanceReport
    {
        public List<StockBalanceItem> Items { get; set; } = new();
        public decimal TotalValue => Items.Sum(i => i.TotalValue);
    }

    public class StockBalanceItem
    {
        public int InventoryItemId { get; set; }
        public string PartName { get; set; }
        public string PartNumber { get; set; }
        public int QuantityInStock { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue => QuantityInStock * UnitPrice;
    }

}
