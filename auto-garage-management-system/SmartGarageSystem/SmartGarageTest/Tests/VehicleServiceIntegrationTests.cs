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
    public class VehicleServiceIntegrationTests : IClassFixture<DatabaseFixture>
    {
        private readonly IVehicleService _vehicleService;
        private readonly ICustomerService _customerService;

        public VehicleServiceIntegrationTests(DatabaseFixture fixture)
        {
            _vehicleService = fixture.ServiceProvider.GetRequiredService<IVehicleService>();
            _customerService = fixture.ServiceProvider.GetRequiredService<ICustomerService>();
        }

        // Helper: creates a unique customer to satisfy FK, returns the customer ID
        private async Task<int> CreateTestCustomerAsync()
        {
            var customer = new Customer
            {
                FullName = $"Test Cust {Guid.NewGuid().ToString("N")[..6]}",
                Phone = "000",
                Email = "test@test.com"
            };
            return await _customerService.CreateCustomerAsync(customer);
        }

        // Helper: creates a Vehicle object with unique data
        private Vehicle CreateTestVehicle(int customerId, string suffix = "")
        {
            return new Vehicle
            {
                CustomerId = customerId,
                LicensePlate = $"TST-{Guid.NewGuid().ToString("N")[..5]}",
                Make = "TestMake",
                Model = $"Model {suffix}",
                Year = 2023,
                Color = "Red",
                VIN = $"VIN-{Guid.NewGuid().ToString("N")[..8]}"
            };
        }

        // -------------------- CREATE --------------------
        [Fact]
        public async Task CreateVehicle_ShouldAddVehicleToDatabase()
        {
            int custId = await CreateTestCustomerAsync();
            var vehicle = CreateTestVehicle(custId, "Create");

            int id = await _vehicleService.CreateVehicleAsync(vehicle);
            Assert.True(id > 0);

            var fetched = await _vehicleService.GetVehicleByIdAsync(id);
            Assert.NotNull(fetched);
            Assert.Equal(vehicle.LicensePlate, fetched.LicensePlate);
            Assert.Equal(vehicle.Make, fetched.Make);
            Assert.Equal(custId, fetched.CustomerId);

            // Cleanup
            await _vehicleService.DeleteVehicleAsync(id);
            await _customerService.DeleteCustomerAsync(custId);
        }

        // -------------------- READ --------------------
        [Fact]
        public async Task GetVehicleById_ExistingId_ReturnsVehicleWithCustomerName()
        {
            int custId = await CreateTestCustomerAsync();
            var vehicle = CreateTestVehicle(custId);
            int id = await _vehicleService.CreateVehicleAsync(vehicle);

            var result = await _vehicleService.GetVehicleByIdAsync(id);
            Assert.NotNull(result);
            Assert.Equal(vehicle.LicensePlate, result.LicensePlate);
            Assert.False(string.IsNullOrEmpty(result.CustomerName)); // JOIN worked

            await _vehicleService.DeleteVehicleAsync(id);
            await _customerService.DeleteCustomerAsync(custId);
        }

        [Fact]
        public async Task GetVehicleById_NonExistentId_ReturnsNull()
        {
            var result = await _vehicleService.GetVehicleByIdAsync(int.MaxValue);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllVehicles_ShouldReturnAll()
        {
            int custId = await CreateTestCustomerAsync();
            var v1 = CreateTestVehicle(custId, "GetAll1");
            var v2 = CreateTestVehicle(custId, "GetAll2");
            int id1 = await _vehicleService.CreateVehicleAsync(v1);
            int id2 = await _vehicleService.CreateVehicleAsync(v2);

            var all = await _vehicleService.GetAllVehiclesAsync();
            Assert.Contains(all, v => v.VehicleId == id1);
            Assert.Contains(all, v => v.VehicleId == id2);

            await _vehicleService.DeleteVehicleAsync(id1);
            await _vehicleService.DeleteVehicleAsync(id2);
            await _customerService.DeleteCustomerAsync(custId);
        }

        [Fact]
        public async Task SearchVehicles_ShouldFilterByLicensePlateOrMakeOrCustomerName()
        {
            int custId = await CreateTestCustomerAsync();
            var uniquePlate = $"PLATE-{Guid.NewGuid().ToString("N")[..4]}";
            var vehicle = CreateTestVehicle(custId);
            vehicle.LicensePlate = uniquePlate;
            int id = await _vehicleService.CreateVehicleAsync(vehicle);

            var results = await _vehicleService.SearchVehiclesAsync(uniquePlate);
            Assert.Contains(results, r => r.VehicleId == id);

            await _vehicleService.DeleteVehicleAsync(id);
            await _customerService.DeleteCustomerAsync(custId);
        }

        // -------------------- UPDATE --------------------
        [Fact]
        public async Task UpdateVehicle_ShouldModifyExisting()
        {
            int custId = await CreateTestCustomerAsync();
            var original = CreateTestVehicle(custId, "Update");
            int id = await _vehicleService.CreateVehicleAsync(original);

            var updated = new Vehicle
            {
                VehicleId = id,
                CustomerId = custId,
                LicensePlate = "UPD-999",
                Make = "UpdatedMake",
                Model = "UpdatedModel",
                Year = 2024,
                Color = "Blue",
                VIN = "UPDATEDVIN"
            };

            await _vehicleService.UpdateVehicleAsync(updated);

            var fetched = await _vehicleService.GetVehicleByIdAsync(id);
            Assert.Equal("UPD-999", fetched.LicensePlate);
            Assert.Equal("UpdatedMake", fetched.Make);
            Assert.Equal(2024, fetched.Year);

            await _vehicleService.DeleteVehicleAsync(id);
            await _customerService.DeleteCustomerAsync(custId);
        }

        // -------------------- DELETE --------------------
        [Fact]
        public async Task DeleteVehicle_ShouldRemoveFromDatabase()
        {
            int custId = await CreateTestCustomerAsync();
            var vehicle = CreateTestVehicle(custId, "Delete");
            int id = await _vehicleService.CreateVehicleAsync(vehicle);

            await _vehicleService.DeleteVehicleAsync(id);

            var shouldBeNull = await _vehicleService.GetVehicleByIdAsync(id);
            Assert.Null(shouldBeNull);

            await _customerService.DeleteCustomerAsync(custId);
        }

        [Fact]
        public async Task DeleteVehicle_NonExistentId_ShouldNotThrow()
        {
            await _vehicleService.DeleteVehicleAsync(int.MaxValue);
            Assert.True(true);
        }

        // -------------------- EDGE CASES --------------------
        [Fact]
        public async Task CreateVehicle_WithNullOptionals_ShouldStoreNulls()
        {
            int custId = await CreateTestCustomerAsync();
            var vehicle = new Vehicle
            {
                CustomerId = custId,
                LicensePlate = "NULLABLE",
                Make = "Make",
                Model = "Model",
                Year = null,
                Color = null,
                VIN = null
            };
            int id = await _vehicleService.CreateVehicleAsync(vehicle);
            var fetched = await _vehicleService.GetVehicleByIdAsync(id);
            Assert.Null(fetched.Year);
            Assert.Null(fetched.Color);
            Assert.Null(fetched.VIN);

            await _vehicleService.DeleteVehicleAsync(id);
            await _customerService.DeleteCustomerAsync(custId);
        }
    }

}
