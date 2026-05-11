using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Models
{
    public class JobCard
    {
        public int JobCardId { get; set; }
        public int VehicleId { get; set; }
        public int AssignedUserId { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateCompleted { get; set; }

        // Navigation
        public string VehiclePlate { get; set; }
        public string CustomerName { get; set; }
        public string AssignedUserName { get; set; }
        public List<JobCardItem> Items { get; set; } = new();
    }

}
