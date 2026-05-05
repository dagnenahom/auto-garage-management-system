using SmartGarageSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
    public interface IVehicleService
    {
        Task<List<Vehicle>> GetAllVehiclesAsync();
        Task<List<Vehicle>> SearchVehiclesAsync(string searchText);
        Task<Vehicle> GetVehicleByIdAsync(int id);
        Task<int> CreateVehicleAsync(Vehicle vehicle);
        Task UpdateVehicleAsync(Vehicle vehicle);
        Task DeleteVehicleAsync(int id);
        Task<List<Customer>> GetCustomersForDropdownAsync();   // to pick customer
    }

}
