using SmartGarageSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
    public interface IJobCardService
    {
        Task<List<JobCard>> GetAllJobCardsAsync();
        Task<JobCard> GetJobCardByIdAsync(int id);
        Task<int> CreateJobCardAsync(JobCard jobCard);
        Task UpdateJobCardAsync(JobCard jobCard);
        Task DeleteJobCardAsync(int id);
        Task AddItemToJobCardAsync(int jobCardId, JobCardItem item); // deducts stock
        Task RemoveItemFromJobCardAsync(int jobCardItemId); // returns stock
        Task<List<User>> GetMechanicsAsync();
        Task<List<Vehicle>> GetVehiclesAsync();
        Task<List<InventoryItem>> GetInventoryItemsAsync();
    }

}
