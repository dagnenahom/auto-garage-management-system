using SmartGarageSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
    public interface IInventoryService
    {
        Task<List<InventoryItem>> GetAllItemsAsync();
        Task<List<InventoryItem>> SearchItemsAsync(string searchText);
        Task<InventoryItem> GetItemByIdAsync(int id);
        Task<int> CreateItemAsync(InventoryItem item);
        Task UpdateItemAsync(InventoryItem item);
        Task DeleteItemAsync(int id);
        Task AdjustStockAsync(int itemId, int quantityChange); // positive for restock, negative for usage
    }
}