using SmartGarageSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{

    public interface ICustomerService
    {
        Task<List<Customer>> GetAllCustomersAsync();
        Task<List<Customer>> SearchCustomersAsync(string searchText);
        Task<Customer> GetCustomerByIdAsync(int id);
        Task<int> CreateCustomerAsync(Customer customer);
        Task UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(int id);
    }

}
