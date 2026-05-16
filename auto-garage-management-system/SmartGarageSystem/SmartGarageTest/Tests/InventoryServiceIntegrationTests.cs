using Microsoft.Extensions.DependencyInjection;
using SmartGarageSystem.Models;
using SmartGarageSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageTest.Tests
{
    public class InventoryServiceIntegrationTests : IClassFixture<DatabaseFixture>
    {
        private readonly IInventoryService _inventoryService;

        public InventoryServiceIntegrationTests(DatabaseFixture fixture)
        {
            _inventoryService = fixture.ServiceProvider.GetRequiredService<IInventoryService>();
        }

        // Helper: creates a unique InventoryItem for each test (avoids conflicts)
        private InventoryItem CreateTestItem(string suffix = "")
        {
            return new InventoryItem
            {
                PartName = $"Test Part {suffix} {Guid.NewGuid().ToString("N")[..6]}",
                PartNumber = $"PN-{Guid.NewGuid().ToString("N")[..4]}",
                Description = $"Description for {suffix}",
                QuantityInStock = 10,
                UnitPrice = 15.99m,
                ReorderLevel = 5
            };
        }

        // -------------------- CREATE --------------------
        [Fact]
        public async Task CreateItem_ShouldAddItemToDatabase()
        {
            var item = CreateTestItem("Create");
            int id = await _inventoryService.CreateItemAsync(item);
            Assert.True(id > 0);

            var fetched = await _inventoryService.GetItemByIdAsync(id);
            Assert.NotNull(fetched);
            Assert.Equal(item.PartName, fetched.PartName);
            Assert.Equal(item.QuantityInStock, fetched.QuantityInStock);

            // Cleanup
            await _inventoryService.DeleteItemAsync(id);
        }

        // -------------------- READ --------------------
        [Fact]
        public async Task GetItemById_ExistingId_ReturnsItem()
        {
            var item = CreateTestItem("GetById");
            int id = await _inventoryService.CreateItemAsync(item);

            var result = await _inventoryService.GetItemByIdAsync(id);
            Assert.NotNull(result);
            Assert.Equal(item.PartName, result.PartName);

            await _inventoryService.DeleteItemAsync(id);
        }

        [Fact]
        public async Task GetItemById_NonExistentId_ReturnsNull()
        {
            var result = await _inventoryService.GetItemByIdAsync(int.MaxValue);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllItems_ShouldReturnAll()
        {
            var item1 = CreateTestItem("GetAll1");
            var item2 = CreateTestItem("GetAll2");
            int id1 = await _inventoryService.CreateItemAsync(item1);
            int id2 = await _inventoryService.CreateItemAsync(item2);

            var all = await _inventoryService.GetAllItemsAsync();
            Assert.Contains(all, i => i.InventoryItemId == id1);
            Assert.Contains(all, i => i.InventoryItemId == id2);

            await _inventoryService.DeleteItemAsync(id1);
            await _inventoryService.DeleteItemAsync(id2);
        }

        [Fact]
        public async Task SearchItems_ShouldFilterByPartNameOrNumber()
        {
            var uniqueName = $"SearchTest_{Guid.NewGuid().ToString("N")[..6]}";
            var item = CreateTestItem();
            item.PartName = uniqueName;
            int id = await _inventoryService.CreateItemAsync(item);

            var results = await _inventoryService.SearchItemsAsync(uniqueName);
            Assert.Contains(results, r => r.InventoryItemId == id);
            Assert.All(results, r => Assert.True(r.PartName.Contains(uniqueName) || (r.PartNumber?.Contains(uniqueName) ?? false)));

            await _inventoryService.DeleteItemAsync(id);
        }

        // -------------------- UPDATE --------------------
        [Fact]
        public async Task UpdateItem_ShouldModifyExisting()
        {
            var original = CreateTestItem("Update");
            int id = await _inventoryService.CreateItemAsync(original);

            var updated = new InventoryItem
            {
                InventoryItemId = id,
                PartName = "Updated Part",
                PartNumber = "UPD-123",
                Description = "Updated desc",
                QuantityInStock = 25,
                UnitPrice = 22.50m,
                ReorderLevel = 8
            };

            await _inventoryService.UpdateItemAsync(updated);

            var fetched = await _inventoryService.GetItemByIdAsync(id);
            Assert.Equal("Updated Part", fetched.PartName);
            Assert.Equal(25, fetched.QuantityInStock);
            Assert.Equal(22.50m, fetched.UnitPrice);
            Assert.Equal(8, fetched.ReorderLevel);

            await _inventoryService.DeleteItemAsync(id);
        }

        // -------------------- DELETE --------------------
        [Fact]
        public async Task DeleteItem_ShouldRemoveFromDatabase()
        {
            var item = CreateTestItem("Delete");
            int id = await _inventoryService.CreateItemAsync(item);

            await _inventoryService.DeleteItemAsync(id);

            var shouldBeNull = await _inventoryService.GetItemByIdAsync(id);
            Assert.Null(shouldBeNull);
        }

        [Fact]
        public async Task DeleteItem_NonExistentId_ShouldNotThrow()
        {
            await _inventoryService.DeleteItemAsync(int.MaxValue);
            Assert.True(true);
        }

        // -------------------- STOCK ADJUSTMENT --------------------
        [Fact]
        public async Task AdjustStock_AddsQuantity_ShouldIncreaseStock()
        {
            var item = CreateTestItem("AdjustAdd");
            item.QuantityInStock = 10;
            int id = await _inventoryService.CreateItemAsync(item);

            await _inventoryService.AdjustStockAsync(id, 5); // +5

            var updated = await _inventoryService.GetItemByIdAsync(id);
            Assert.Equal(15, updated.QuantityInStock);

            await _inventoryService.DeleteItemAsync(id);
        }

        [Fact]
        public async Task AdjustStock_RemovesQuantity_ShouldDecreaseStock()
        {
            var item = CreateTestItem("AdjustRemove");
            item.QuantityInStock = 20;
            int id = await _inventoryService.CreateItemAsync(item);

            await _inventoryService.AdjustStockAsync(id, -8); // -8

            var updated = await _inventoryService.GetItemByIdAsync(id);
            Assert.Equal(12, updated.QuantityInStock);

            await _inventoryService.DeleteItemAsync(id);
        }

        // -------------------- EDGE CASES --------------------
        [Fact]
        public async Task CreateItem_WithNullOptionals_ShouldStoreNulls()
        {
            var item = new InventoryItem
            {
                PartName = "Minimal Item",
                PartNumber = null,
                Description = null,
                QuantityInStock = 0,
                UnitPrice = 0,
                ReorderLevel = 0
            };
            int id = await _inventoryService.CreateItemAsync(item);
            var fetched = await _inventoryService.GetItemByIdAsync(id);
            Assert.Null(fetched.PartNumber);
            Assert.Null(fetched.Description);

            await _inventoryService.DeleteItemAsync(id);
        }
    }

}
