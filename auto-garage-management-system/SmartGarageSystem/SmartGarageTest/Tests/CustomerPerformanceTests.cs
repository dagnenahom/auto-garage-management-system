using SmartGarageSystem.Models;
using SmartGarageSystem.Services;
using SmartGarageTest.Tests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;


namespace SmartGarageTest.Tests
{
    public class CustomerPerformanceTests : IClassFixture<DatabaseFixture>
    {
        private readonly ICustomerService _customerService;
        private readonly ITestOutputHelper _output;

        public CustomerPerformanceTests(DatabaseFixture fixture, ITestOutputHelper output)
        {
            _customerService = fixture.ServiceProvider.GetRequiredService<ICustomerService>();
            _output = output;
        }

        // Helper to generate unique customer names
        private Customer CreateTestCustomer(string suffix) => new()
        {
            FullName = $"PerfTest_{suffix}_{Guid.NewGuid().ToString("N").Substring(0, 6)}",
            Phone = "000-0000",
            Email = $"perf_{suffix}@test.com"
        };

        [Fact]
        public async Task CreateCustomer_Latency_WithinThreshold()
        {
            const int iterations = 10;
            var timings = new List<long>();

            for (int i = 0; i < iterations; i++)
            {
                var customer = CreateTestCustomer($"Create_{i}");
                var sw = Stopwatch.StartNew();
                int id = await _customerService.CreateCustomerAsync(customer);
                sw.Stop();
                timings.Add(sw.ElapsedMilliseconds);
                _output.WriteLine($"Create iteration {i}: {sw.ElapsedMilliseconds} ms");

                // Clean up immediately to not clutter the DB
                await _customerService.DeleteCustomerAsync(id);
            }

            double avg = timings.Average();
            _output.WriteLine($"Average Create latency: {avg:F2} ms");
            Assert.True(avg < 500, $"Create latency too high: {avg} ms");
        }

        [Fact]
        public async Task SearchCustomer_Latency_WithinThreshold()
        {
            // First, create a known customer to search for
            var uniqueName = $"SearchTarget_{Guid.NewGuid():N}";
            var customer = new Customer { FullName = uniqueName, Phone = "111", Email = "search@test.com" };
            int id = await _customerService.CreateCustomerAsync(customer);

            const int iterations = 10;
            var timings = new List<long>();

            for (int i = 0; i < iterations; i++)
            {
                var sw = Stopwatch.StartNew();
                var results = await _customerService.SearchCustomersAsync(uniqueName);
                sw.Stop();
                timings.Add(sw.ElapsedMilliseconds);
                _output.WriteLine($"Search iteration {i}: {sw.ElapsedMilliseconds} ms");
            }

            await _customerService.DeleteCustomerAsync(id);

            double avg = timings.Average();
            _output.WriteLine($"Average Search latency: {avg:F2} ms");
            Assert.True(avg < 200, $"Search latency too high: {avg} ms");
        }

        [Fact]
        public async Task GetAllCustomers_Latency_WithinThreshold()
        {
            // Ensure there are some records (at least 50)
            var ids = new List<int>();
            for (int i = 0; i < 50; i++)
            {
                var c = CreateTestCustomer($"Bulk_{i}");
                ids.Add(await _customerService.CreateCustomerAsync(c));
            }

            const int iterations = 10;
            var timings = new List<long>();

            for (int i = 0; i < iterations; i++)
            {
                var sw = Stopwatch.StartNew();
                var all = await _customerService.GetAllCustomersAsync();
                sw.Stop();
                timings.Add(sw.ElapsedMilliseconds);
                _output.WriteLine($"GetAll iteration {i}: {sw.ElapsedMilliseconds} ms");
            }

            // Clean up bulk data
            foreach (var id in ids)
                await _customerService.DeleteCustomerAsync(id);

            double avg = timings.Average();
            _output.WriteLine($"Average GetAll latency: {avg:F2} ms");
            Assert.True(avg < 300, $"GetAll latency too high: {avg} ms");
        }
    }
}