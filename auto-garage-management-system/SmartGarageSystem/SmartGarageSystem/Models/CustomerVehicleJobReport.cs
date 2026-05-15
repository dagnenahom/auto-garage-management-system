using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Models
{
    public class CustomerVehicleJobReport
    {
        public List<CustomerReportItem> Customers { get; set; } = new();
    }

    public class CustomerReportItem
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; }
        public List<VehicleReportItem> Vehicles { get; set; } = new();
    }

    public class VehicleReportItem
    {
        public int VehicleId { get; set; }
        public string LicensePlate { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public List<JobCardReportItem> JobCards { get; set; } = new();
    }

    public class JobCardReportItem
    {
        public int JobCardId { get; set; }
        public string Status { get; set; }
        public string DateCreated { get; set; }   // formatted string for display
        public string Description { get; set; }
    }

}
