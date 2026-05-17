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
    public class JobCardServiceIntegrationTests : IClassFixture<DatabaseFixture>
    {
        private readonly IJobCardService _jobCardService;
        private readonly ICustomerService _customerService;
        private readonly IVehicleService _vehicleService;
        private readonly IInventoryService _inventoryService;
        private readonly IUserService _userService;

        public JobCardServiceIntegrationTests(DatabaseFixture fixture)
        {
            _jobCardService = fixture.ServiceProvider.GetRequiredService<IJobCardService>();
            _customerService = fixture.ServiceProvider.GetRequiredService<ICustomerService>();
            _vehicleService = fixture.ServiceProvider.GetRequiredService<IVehicleService>();
            _inventoryService = fixture.ServiceProvider.GetRequiredService<IInventoryService>();
            _userService = fixture.ServiceProvider.GetRequiredService<IUserService>();
        }

        // Helper: creates all prerequisite data and returns their IDs
        private async Task<(int customerId, int vehicleId, int mechanicUserId, int inventoryItemId)> CreatePrerequisitesAsync()
        {
            // Customer
            var customer = new Customer { FullName = $"JobCust {Guid.NewGuid().ToString("N")[..6]}", Phone = "000", Email = "jc@test.com" };
            int custId = await _customerService.CreateCustomerAsync(customer);

            // Vehicle
            var vehicle = new Vehicle { CustomerId = custId, LicensePlate = $"JC-{Guid.NewGuid().ToString("N")[..5]}", Make = "JobMake", Model = "JobModel", Year = 2020 };
            int vehId = await _vehicleService.CreateVehicleAsync(vehicle);

            // Mechanic user (with role 'Mechanic')
            // First, ensure the 'Mechanic' role exists and is active; we assume it does (from your DB setup).
            // Create a user
            var user = new User
            {
                Username = $"mech_{Guid.NewGuid().ToString("N")[..6]}",
                FullName = "Test Mechanic",
                PasswordHash = "test123" // will be hashed by service
            };
            // Get the Mechanic role ID
            var allRoles = await _userService.GetAllActiveRolesAsync();
            var mechRole = allRoles.FirstOrDefault(r => r.RoleName.Equals("Mechanic", StringComparison.OrdinalIgnoreCase));
            if (mechRole == null)
                throw new InvalidOperationException("Mechanic role not found in database. Ensure the role is inserted.");
            int userId = await _userService.CreateUserAsync(user, new List<int> { mechRole.RoleId });

            // Inventory item with sufficient stock
            var invItem = new InventoryItem
            {
                PartName = $"TestPart {Guid.NewGuid().ToString("N")[..6]}",
                PartNumber = "TP-001",
                QuantityInStock = 50,
                UnitPrice = 10.00m,
                ReorderLevel = 5
            };
            int invId = await _inventoryService.CreateItemAsync(invItem);

            return (custId, vehId, userId, invId);
        }

        // Helper: cleanup everything created
        private async Task CleanupAsync(int customerId, int vehicleId, int userId, int inventoryItemId, int? jobCardId = null)
        {
            if (jobCardId.HasValue)
            {
                // Deleting job card will also delete items and return stock via the service's delete method
                try { await _jobCardService.DeleteJobCardAsync(jobCardId.Value); } catch { /* ignore */ }
            }
            await _vehicleService.DeleteVehicleAsync(vehicleId);
            await _customerService.DeleteCustomerAsync(customerId);
            await _userService.DeleteUserAsync(userId);
            await _inventoryService.DeleteItemAsync(inventoryItemId);
        }

        // -------------------- CREATE JOB CARD --------------------
        [Fact]
        public async Task CreateJobCard_ShouldAddOpenJobCard()
        {
            var (custId, vehId, mechId, invId) = await CreatePrerequisitesAsync();

            var jobCard = new JobCard
            {
                VehicleId = vehId,
                AssignedUserId = mechId,
                Status = "Open",
                Description = "Test job"
            };
            int jobId = await _jobCardService.CreateJobCardAsync(jobCard);
            Assert.True(jobId > 0);

            var fetched = await _jobCardService.GetJobCardByIdAsync(jobId);
            Assert.NotNull(fetched);
            Assert.Equal("Open", fetched.Status);
            Assert.Equal(vehId, fetched.VehicleId);
            Assert.False(string.IsNullOrEmpty(fetched.VehiclePlate)); // join worked
            Assert.False(string.IsNullOrEmpty(fetched.CustomerName));

            await CleanupAsync(custId, vehId, mechId, invId, jobId);
        }

        // -------------------- READ --------------------
        [Fact]
        public async Task GetJobCardById_WithItems_ReturnsFullDetail()
        {
            var (custId, vehId, mechId, invId) = await CreatePrerequisitesAsync();

            var jobCard = new JobCard { VehicleId = vehId, AssignedUserId = mechId, Status = "InProgress" };
            int jobId = await _jobCardService.CreateJobCardAsync(jobCard);

            // Add an item to the job card
            var item = new JobCardItem { InventoryItemId = invId, Quantity = 2, UnitPrice = 10.00m };
            await _jobCardService.AddItemToJobCardAsync(jobId, item);

            var fetched = await _jobCardService.GetJobCardByIdAsync(jobId);
            Assert.NotNull(fetched);
            Assert.NotEmpty(fetched.Items);
            Assert.Equal(2, fetched.Items[0].Quantity);
            Assert.Equal(20.00m, fetched.Items[0].LineTotal);

            // Stock should have decreased by 2
            var invAfter = await _inventoryService.GetItemByIdAsync(invId);
            Assert.Equal(48, invAfter.QuantityInStock);

            await CleanupAsync(custId, vehId, mechId, invId, jobId);
        }


        [Fact]
        public async Task GetAllJobCards_ShouldReturnAllWithDetails()
        {
            var (custId, vehId, mechId, invId) = await CreatePrerequisitesAsync();

            var job1 = new JobCard { VehicleId = vehId, AssignedUserId = mechId, Status = "Open" };
            var job2 = new JobCard { VehicleId = vehId, AssignedUserId = mechId, Status = "Completed" };
            int id1 = await _jobCardService.CreateJobCardAsync(job1);
            int id2 = await _jobCardService.CreateJobCardAsync(job2);

            var all = await _jobCardService.GetAllJobCardsAsync();
            Assert.Contains(all, j => j.JobCardId == id1);
            Assert.Contains(all, j => j.JobCardId == id2);

            // Delete both job cards first (order doesn't matter)
            await _jobCardService.DeleteJobCardAsync(id1);
            await _jobCardService.DeleteJobCardAsync(id2);

            // Now safe to delete the vehicle and other dependencies
            await _vehicleService.DeleteVehicleAsync(vehId);
            await _customerService.DeleteCustomerAsync(custId);
            await _userService.DeleteUserAsync(mechId);
            await _inventoryService.DeleteItemAsync(invId);
        }

        // -------------------- UPDATE JOB CARD --------------------
        [Fact]
        public async Task UpdateJobCard_ShouldChangeStatusAndFields()
        {
            var (custId, vehId, mechId, invId) = await CreatePrerequisitesAsync();

            var jobCard = new JobCard { VehicleId = vehId, AssignedUserId = mechId, Status = "Open" };
            int jobId = await _jobCardService.CreateJobCardAsync(jobCard);

            var updated = new JobCard
            {
                JobCardId = jobId,
                VehicleId = vehId,
                AssignedUserId = mechId,
                Status = "Completed",
                Description = "Done",
                DateCompleted = DateTime.UtcNow
            };
            await _jobCardService.UpdateJobCardAsync(updated);

            var fetched = await _jobCardService.GetJobCardByIdAsync(jobId);
            Assert.Equal("Completed", fetched.Status);
            Assert.Equal("Done", fetched.Description);
            Assert.NotNull(fetched.DateCompleted);

            await CleanupAsync(custId, vehId, mechId, invId, jobId);
        }

        // -------------------- DELETE JOB CARD (should return stock) --------------------
        [Fact]
        public async Task DeleteJobCard_WithItems_ShouldReturnStockAndRemoveAll()
        {
            var (custId, vehId, mechId, invId) = await CreatePrerequisitesAsync();

            var jobCard = new JobCard { VehicleId = vehId, AssignedUserId = mechId, Status = "Open" };
            int jobId = await _jobCardService.CreateJobCardAsync(jobCard);

            var item = new JobCardItem { InventoryItemId = invId, Quantity = 5, UnitPrice = 10.00m };
            await _jobCardService.AddItemToJobCardAsync(jobId, item);

            // Stock after adding: 50 - 5 = 45
            var invAfterAdd = await _inventoryService.GetItemByIdAsync(invId);
            Assert.Equal(45, invAfterAdd.QuantityInStock);

            // Delete job card
            await _jobCardService.DeleteJobCardAsync(jobId);

            // Stock should return to original 50
            var invAfterDelete = await _inventoryService.GetItemByIdAsync(invId);
            Assert.Equal(50, invAfterDelete.QuantityInStock);

            // Job card should be gone
            var jc = await _jobCardService.GetJobCardByIdAsync(jobId);
            Assert.Null(jc);

            // No need to delete job card again
            await CleanupAsync(custId, vehId, mechId, invId); // no jobId
        }

        // -------------------- ADD / REMOVE ITEMS --------------------
        [Fact]
        public async Task AddItemToJobCard_ShouldDecreaseStock()
        {
            var (custId, vehId, mechId, invId) = await CreatePrerequisitesAsync();
            var jobCard = new JobCard { VehicleId = vehId, AssignedUserId = mechId, Status = "Open" };
            int jobId = await _jobCardService.CreateJobCardAsync(jobCard);

            var item = new JobCardItem { InventoryItemId = invId, Quantity = 3, UnitPrice = 10.00m };
            await _jobCardService.AddItemToJobCardAsync(jobId, item);

            var fetched = await _jobCardService.GetJobCardByIdAsync(jobId);
            Assert.Single(fetched.Items);
            Assert.Equal(3, fetched.Items[0].Quantity);

            var inv = await _inventoryService.GetItemByIdAsync(invId);
            Assert.Equal(47, inv.QuantityInStock); // 50 - 3

            await CleanupAsync(custId, vehId, mechId, invId, jobId);
        }

        [Fact]
        public async Task RemoveItemFromJobCard_ShouldIncreaseStock()
        {
            var (custId, vehId, mechId, invId) = await CreatePrerequisitesAsync();
            var jobCard = new JobCard { VehicleId = vehId, AssignedUserId = mechId, Status = "Open" };
            int jobId = await _jobCardService.CreateJobCardAsync(jobCard);

            // Add an item
            var item = new JobCardItem { InventoryItemId = invId, Quantity = 5, UnitPrice = 10.00m };
            await _jobCardService.AddItemToJobCardAsync(jobId, item);

            // Verify stock decreased
            var invAfterAdd = await _inventoryService.GetItemByIdAsync(invId);
            Assert.Equal(45, invAfterAdd.QuantityInStock);

            // Get the item ID
            var fullJob = await _jobCardService.GetJobCardByIdAsync(jobId);
            int itemId = fullJob.Items[0].JobCardItemId;

            // Remove it
            await _jobCardService.RemoveItemFromJobCardAsync(itemId);

            // Stock back to 50
            var invAfterRemove = await _inventoryService.GetItemByIdAsync(invId);
            Assert.Equal(50, invAfterRemove.QuantityInStock);

            // Job card should have no items
            var finalJob = await _jobCardService.GetJobCardByIdAsync(jobId);
            Assert.Empty(finalJob.Items);

            await CleanupAsync(custId, vehId, mechId, invId, jobId);
        }

        // -------------------- EDGE CASES --------------------
       [Fact]
        public async Task AddItem_WhenNotEnoughStock_ShouldThrow()
        {
            var (custId, vehId, mechId, invId) = await CreatePrerequisitesAsync();
            var jobCard = new JobCard { VehicleId = vehId, AssignedUserId = mechId, Status = "Open" };
            int jobId = await _jobCardService.CreateJobCardAsync(jobCard);

            var item = new JobCardItem { InventoryItemId = invId, Quantity = 100, UnitPrice = 10.00m };

            // Now expects InvalidOperationException
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _jobCardService.AddItemToJobCardAsync(jobId, item));

            // Optional: verify the message
            Assert.Contains("Insufficient stock", ex.Message);

            await CleanupAsync(custId, vehId, mechId, invId, jobId);
        }

        [Fact]
        public async Task GetMechanics_ShouldReturnUsersWithMechanicRole()
        {
            // At least one mechanic should exist from previous tests or seed data
            var mechanics = await _jobCardService.GetMechanicsAsync();
            Assert.NotEmpty(mechanics);
            Assert.All(mechanics, m => Assert.True(m.FullName.Length > 0));
        }

        [Fact]
        public async Task GetVehicles_ShouldReturnVehiclesWithCustomerName()
        {
            var (custId, vehId, mechId, invId) = await CreatePrerequisitesAsync();
            var vehicles = await _jobCardService.GetVehiclesAsync();
            Assert.Contains(vehicles, v => v.VehicleId == vehId && !string.IsNullOrEmpty(v.CustomerName));
            await CleanupAsync(custId, vehId, mechId, invId);
        }
    }
}
