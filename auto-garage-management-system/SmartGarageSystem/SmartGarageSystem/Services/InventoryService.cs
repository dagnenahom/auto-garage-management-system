using Microsoft.Extensions.Configuration;
using SmartGarageSystem.Models;
using SmartGarageSystem.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly string _connectionString;
        public InventoryService(IConfiguration configuration) =>
            _connectionString = configuration.GetConnectionString("SmartGarage");

        public async Task<List<InventoryItem>> GetAllItemsAsync()
        {
            var items = new List<InventoryItem>();
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = "SELECT * FROM InventoryItems ORDER BY PartName";
            using var cmd = new SqlCommand(sql, con);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                items.Add(MapItem(reader));
            return items;
        }

        public async Task<List<InventoryItem>> SearchItemsAsync(string searchText)
        {
            var items = new List<InventoryItem>();
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = @"SELECT * FROM InventoryItems
                           WHERE PartName LIKE @Search OR PartNumber LIKE @Search
                           ORDER BY PartName";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Search", $"%{searchText}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                items.Add(MapItem(reader));
            return items;
        }

        public async Task<InventoryItem> GetItemByIdAsync(int id)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = "SELECT * FROM InventoryItems WHERE InventoryItemId = @Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return MapItem(reader);
        }

        public async Task<int> CreateItemAsync(InventoryItem item)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = @"INSERT INTO InventoryItems (PartName, PartNumber, Description, QuantityInStock, UnitPrice, ReorderLevel)
                           OUTPUT INSERTED.InventoryItemId
                           VALUES (@PartName, @PartNumber, @Description, @QuantityInStock, @UnitPrice, @ReorderLevel)";
            using var cmd = new SqlCommand(sql, con);
            AddParameters(cmd, item);
            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task UpdateItemAsync(InventoryItem item)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = @"UPDATE InventoryItems SET
                            PartName = @PartName, PartNumber = @PartNumber, Description = @Description,
                            QuantityInStock = @QuantityInStock, UnitPrice = @UnitPrice, ReorderLevel = @ReorderLevel
                           WHERE InventoryItemId = @Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", item.InventoryItemId);
            AddParameters(cmd, item);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteItemAsync(int id)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = "DELETE FROM InventoryItems WHERE InventoryItemId = @Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AdjustStockAsync(int itemId, int quantityChange)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = @"UPDATE InventoryItems
                           SET QuantityInStock = QuantityInStock + @Change
                           WHERE InventoryItemId = @Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", itemId);
            cmd.Parameters.AddWithValue("@Change", quantityChange);
            await cmd.ExecuteNonQueryAsync();
        }

        private void AddParameters(SqlCommand cmd, InventoryItem item)
        {
            cmd.Parameters.AddWithValue("@PartName", item.PartName);
            cmd.Parameters.AddWithValue("@PartNumber", (object)item.PartNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object)item.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@QuantityInStock", item.QuantityInStock);
            cmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
            cmd.Parameters.AddWithValue("@ReorderLevel", item.ReorderLevel);
        }

        private InventoryItem MapItem(SqlDataReader reader)
        {
            return new InventoryItem
            {
                InventoryItemId = reader.GetInt32(reader.GetOrdinal("InventoryItemId")),
                PartName = reader.GetString(reader.GetOrdinal("PartName")),
                PartNumber = reader.IsDBNull(reader.GetOrdinal("PartNumber")) ? null : reader.GetString(reader.GetOrdinal("PartNumber")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                QuantityInStock = reader.GetInt32(reader.GetOrdinal("QuantityInStock")),
                UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                ReorderLevel = reader.GetInt32(reader.GetOrdinal("ReorderLevel")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"))
            };
        }
    }
}