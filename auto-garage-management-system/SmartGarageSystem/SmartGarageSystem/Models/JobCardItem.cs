using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Models
{
    public class JobCardItem
    {
        public int JobCardItemId { get; set; }
        public int JobCardId { get; set; }
        public int InventoryItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public string PartName { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;
    }

}
