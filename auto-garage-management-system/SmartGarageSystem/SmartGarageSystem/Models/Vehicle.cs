using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Models
{
    public class Vehicle
    {
        public int VehicleId { get; set; }
        public int CustomerId { get; set; }
        public string LicensePlate { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int? Year { get; set; }
        public string Color { get; set; }
        public string VIN { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation property (optional, filled by service)
        public string CustomerName { get; set; }
    }

}
