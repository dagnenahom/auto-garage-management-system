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
    public class VehicleService : IVehicleService
    {
        private readonly string _connectionString;

        public VehicleService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SmartGarage");
        }

        public async Task<List<Vehicle>> GetAllVehiclesAsync()
        {
            var vehicles = new List<Vehicle>();
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = @"SELECT v.VehicleId, v.CustomerId, v.LicensePlate, v.Make, v.Model, v.Year, v.Color, v.VIN, v.CreatedDate,
                                  c.FullName AS CustomerName
                           FROM Vehicles v
                           INNER JOIN Customers c ON v.CustomerId = c.CustomerId
                           ORDER BY v.LicensePlate";
            using var cmd = new SqlCommand(sql, con);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                vehicles.Add(MapVehicle(reader));
            return vehicles;
        }

        public async Task<List<Vehicle>> SearchVehiclesAsync(string searchText)
        {
            var vehicles = new List<Vehicle>();
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = @"SELECT v.VehicleId, v.CustomerId, v.LicensePlate, v.Make, v.Model, v.Year, v.Color, v.VIN, v.CreatedDate,
                                  c.FullName AS CustomerName
                           FROM Vehicles v
                           INNER JOIN Customers c ON v.CustomerId = c.CustomerId
                           WHERE v.LicensePlate LIKE @Search OR v.Make LIKE @Search OR v.Model LIKE @Search OR c.FullName LIKE @Search
                           ORDER BY v.LicensePlate";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Search", $"%{searchText}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                vehicles.Add(MapVehicle(reader));
            return vehicles;
        }

        public async Task<Vehicle> GetVehicleByIdAsync(int id)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = @"SELECT v.VehicleId, v.CustomerId, v.LicensePlate, v.Make, v.Model, v.Year, v.Color, v.VIN, v.CreatedDate,
                                  c.FullName AS CustomerName
                           FROM Vehicles v
                           INNER JOIN Customers c ON v.CustomerId = c.CustomerId
                           WHERE v.VehicleId = @Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return MapVehicle(reader);
        }

        public async Task<int> CreateVehicleAsync(Vehicle vehicle)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = @"INSERT INTO Vehicles (CustomerId, LicensePlate, Make, Model, Year, Color, VIN)
                           OUTPUT INSERTED.VehicleId
                           VALUES (@CustomerId, @LicensePlate, @Make, @Model, @Year, @Color, @VIN)";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@CustomerId", vehicle.CustomerId);
            cmd.Parameters.AddWithValue("@LicensePlate", vehicle.LicensePlate);
            cmd.Parameters.AddWithValue("@Make", vehicle.Make);
            cmd.Parameters.AddWithValue("@Model", vehicle.Model);
            cmd.Parameters.AddWithValue("@Year", (object)vehicle.Year ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Color", (object)vehicle.Color ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@VIN", (object)vehicle.VIN ?? DBNull.Value);
            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task UpdateVehicleAsync(Vehicle vehicle)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = @"UPDATE Vehicles SET 
                            CustomerId = @CustomerId,
                            LicensePlate = @LicensePlate,
                            Make = @Make,
                            Model = @Model,
                            Year = @Year,
                            Color = @Color,
                            VIN = @VIN
                           WHERE VehicleId = @Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", vehicle.VehicleId);
            cmd.Parameters.AddWithValue("@CustomerId", vehicle.CustomerId);
            cmd.Parameters.AddWithValue("@LicensePlate", vehicle.LicensePlate);
            cmd.Parameters.AddWithValue("@Make", vehicle.Make);
            cmd.Parameters.AddWithValue("@Model", vehicle.Model);
            cmd.Parameters.AddWithValue("@Year", (object)vehicle.Year ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Color", (object)vehicle.Color ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@VIN", (object)vehicle.VIN ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteVehicleAsync(int id)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = "DELETE FROM Vehicles WHERE VehicleId = @Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<Customer>> GetCustomersForDropdownAsync()
        {
            var customers = new List<Customer>();
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = "SELECT CustomerId, FullName FROM Customers ORDER BY FullName";
            using var cmd = new SqlCommand(sql, con);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                customers.Add(new Customer
                {
                    CustomerId = reader.GetInt32(0),
                    FullName = reader.GetString(1)
                });
            }
            return customers;
        }

        private Vehicle MapVehicle(SqlDataReader reader)
        {
            return new Vehicle
            {
                VehicleId = reader.GetInt32(reader.GetOrdinal("VehicleId")),
                CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                LicensePlate = reader.GetString(reader.GetOrdinal("LicensePlate")),
                Make = reader.GetString(reader.GetOrdinal("Make")),
                Model = reader.GetString(reader.GetOrdinal("Model")),
                Year = reader.IsDBNull(reader.GetOrdinal("Year")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("Year")),
                Color = reader.IsDBNull(reader.GetOrdinal("Color")) ? null : reader.GetString(reader.GetOrdinal("Color")),
                VIN = reader.IsDBNull(reader.GetOrdinal("VIN")) ? null : reader.GetString(reader.GetOrdinal("VIN")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                CustomerName = reader.GetString(reader.GetOrdinal("CustomerName"))
            };
        }
    }

}
