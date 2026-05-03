using Microsoft.Extensions.Configuration;
using SmartGarageSystem.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
     public class CustomerService : ICustomerService
    {
        private readonly string _connectionString;

        public CustomerService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SmartGarage");
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var customers = new List<Customer>();
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = "SELECT CustomerId, FullName, Phone, Email, Address, CreatedDate FROM Customers ORDER BY FullName";
            using var cmd = new SqlCommand(sql, con);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                customers.Add(MapCustomer(reader));
            }
            return customers;
        }

        public async Task<List<Customer>> SearchCustomersAsync(string searchText)
        {
            var customers = new List<Customer>();
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = @"SELECT CustomerId, FullName, Phone, Email, Address, CreatedDate 
                           FROM Customers 
                           WHERE FullName LIKE @Search OR Phone LIKE @Search OR Email LIKE @Search
                           ORDER BY FullName";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Search", $"%{searchText}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                customers.Add(MapCustomer(reader));
            }
            return customers;
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = "SELECT CustomerId, FullName, Phone, Email, Address, CreatedDate FROM Customers WHERE CustomerId = @Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return MapCustomer(reader);
        }

        public async Task<int> CreateCustomerAsync(Customer customer)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = @"INSERT INTO Customers (FullName, Phone, Email, Address)
                           OUTPUT INSERTED.CustomerId
                           VALUES (@FullName, @Phone, @Email, @Address)";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@FullName", customer.FullName);
            cmd.Parameters.AddWithValue("@Phone", (object)customer.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", (object)customer.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", (object)customer.Address ?? DBNull.Value);
            
            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = @"UPDATE Customers SET 
                            FullName = @FullName,
                            Phone = @Phone,
                            Email = @Email,
                            Address = @Address
                           WHERE CustomerId = @Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", customer.CustomerId);
            cmd.Parameters.AddWithValue("@FullName", customer.FullName);
            cmd.Parameters.AddWithValue("@Phone", (object)customer.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", (object)customer.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", (object)customer.Address ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteCustomerAsync(int id)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = "DELETE FROM Customers WHERE CustomerId = @Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        private Customer MapCustomer(SqlDataReader reader)
        {

            return new Customer
            {
                CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),               
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"))
            };

        }
    }

}
