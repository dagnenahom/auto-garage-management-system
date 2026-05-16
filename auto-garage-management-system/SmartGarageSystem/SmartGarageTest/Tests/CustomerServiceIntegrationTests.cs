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
    public class CustomerServiceIntegrationTests : IClassFixture<DatabaseFixture>
    {
        private readonly ICustomerService _customerService;

        public CustomerServiceIntegrationTests(DatabaseFixture fixture)
        {
            _customerService = fixture.ServiceProvider.GetRequiredService<ICustomerService>();
        }

        // Helper – generates a unique customer for each test
        private Customer CreateTestCustomer(string suffix = "")
        {
            return new Customer
            {
                // Fixed: Guid.ToString("N") then take first 6 chars
                FullName = $"Test Customer {suffix} {Guid.NewGuid().ToString("N")[..6]}",
                Phone = "555-0100",
                Email = $"test{suffix}@example.com",
                Address = "123 Test St"
            };
        }

        // -------------------- CREATE --------------------
        [Fact]
        public async Task CreateCustomer_ShouldAddCustomerToDatabase()
        {
            var newCustomer = CreateTestCustomer("Create");
            int id = await _customerService.CreateCustomerAsync(newCustomer);
            Assert.True(id > 0);

            var fetched = await _customerService.GetCustomerByIdAsync(id);
            Assert.NotNull(fetched);
            Assert.Equal(newCustomer.FullName, fetched.FullName);
            Assert.Equal(newCustomer.Email, fetched.Email);

            // Clean up
            await _customerService.DeleteCustomerAsync(id);
        }

        // -------------------- READ --------------------
        [Fact]
        public async Task GetCustomerById_ExistingId_ReturnsCustomer()
        {
            var customer = CreateTestCustomer("GetById");
            int id = await _customerService.CreateCustomerAsync(customer);

            var result = await _customerService.GetCustomerByIdAsync(id);
            Assert.NotNull(result);
            Assert.Equal(customer.FullName, result.FullName);

            await _customerService.DeleteCustomerAsync(id);
        }

        [Fact]
        public async Task GetCustomerById_NonExistentId_ReturnsNull()
        {
            var result = await _customerService.GetCustomerByIdAsync(int.MaxValue);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllCustomers_ShouldReturnAll()
        {
            var cust1 = CreateTestCustomer("GetAll1");
            var cust2 = CreateTestCustomer("GetAll2");
            int id1 = await _customerService.CreateCustomerAsync(cust1);
            int id2 = await _customerService.CreateCustomerAsync(cust2);

            var all = await _customerService.GetAllCustomersAsync();
            Assert.Contains(all, c => c.CustomerId == id1);
            Assert.Contains(all, c => c.CustomerId == id2);

            await _customerService.DeleteCustomerAsync(id1);
            await _customerService.DeleteCustomerAsync(id2);
        }

        [Fact]
        public async Task SearchCustomers_ShouldFilterByName()
        {
            var uniqueName = $"SearchTest_{Guid.NewGuid().ToString("N")[..6]}";
            var customer = CreateTestCustomer();
            customer.FullName = uniqueName;
            int id = await _customerService.CreateCustomerAsync(customer);

            var results = await _customerService.SearchCustomersAsync(uniqueName);
            Assert.Contains(results, r => r.CustomerId == id);
            Assert.All(results, r => Assert.Contains(uniqueName, r.FullName, StringComparison.OrdinalIgnoreCase));

            await _customerService.DeleteCustomerAsync(id);
        }

        // -------------------- UPDATE --------------------
        [Fact]
        public async Task UpdateCustomer_ShouldModifyExisting()
        {
            var original = CreateTestCustomer("Update");
            int id = await _customerService.CreateCustomerAsync(original);

            var toUpdate = new Customer
            {
                CustomerId = id,
                FullName = "Updated Name",
                Phone = "999-9999",
                Email = "updated@example.com",
                Address = "New Address"
            };

            await _customerService.UpdateCustomerAsync(toUpdate);

            var updated = await _customerService.GetCustomerByIdAsync(id);
            Assert.Equal("Updated Name", updated.FullName);
            Assert.Equal("999-9999", updated.Phone);
            Assert.Equal("updated@example.com", updated.Email);
            Assert.Equal("New Address", updated.Address);

            await _customerService.DeleteCustomerAsync(id);
        }

        // -------------------- DELETE --------------------
        [Fact]
        public async Task DeleteCustomer_ShouldRemoveFromDatabase()
        {
            var customer = CreateTestCustomer("Delete");
            int id = await _customerService.CreateCustomerAsync(customer);

            await _customerService.DeleteCustomerAsync(id);

            var shouldBeNull = await _customerService.GetCustomerByIdAsync(id);
            Assert.Null(shouldBeNull);
        }

        [Fact]
        public async Task DeleteCustomer_NonExistentId_ShouldNotThrow()
        {
            await _customerService.DeleteCustomerAsync(int.MaxValue);
            Assert.True(true); // No exception thrown
        }

        // -------------------- EDGE CASES --------------------
        [Fact]
        public async Task CreateCustomer_WithNullOptionals_ShouldStoreNulls()
        {
            var customer = new Customer
            {
                FullName = "Minimal Customer",
                Phone = null,
                Email = null,
                Address = null
            };
            int id = await _customerService.CreateCustomerAsync(customer);
            var fetched = await _customerService.GetCustomerByIdAsync(id);
            Assert.Null(fetched.Phone);
            Assert.Null(fetched.Email);
            Assert.Null(fetched.Address);

            await _customerService.DeleteCustomerAsync(id);
        }
    }

}
