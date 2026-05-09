using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Models
{
    public class InventoryItem
    {
        public int InventoryItemId { get; set; }
        public string PartName { get; set; }
        public string PartNumber { get; set; }
        public string Description { get; set; }
        public int QuantityInStock { get; set; }
        public decimal UnitPrice { get; set; }
        public int ReorderLevel { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}