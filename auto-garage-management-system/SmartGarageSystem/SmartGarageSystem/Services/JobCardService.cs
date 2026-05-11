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
    public class JobCardService : IJobCardService
    {
        private readonly string _conn;
        public JobCardService(IConfiguration config) => _conn = config.GetConnectionString("SmartGarage");

        public async Task<List<JobCard>> GetAllJobCardsAsync()
        {
            var cards = new List<JobCard>();
            using var con = new SqlConnection(_conn);
            await con.OpenAsync();
            string sql = @"SELECT j.*, v.LicensePlate, c.FullName AS CustomerName, u.FullName AS AssignedUserName
                        FROM JobCards j
                        INNER JOIN Vehicles v ON j.VehicleId = v.VehicleId
                        INNER JOIN Customers c ON v.CustomerId = c.CustomerId
                        INNER JOIN Users u ON j.AssignedUserId = u.UserId
                        ORDER BY j.DateCreated DESC";
            using var cmd = new SqlCommand(sql, con);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                cards.Add(MapJobCard(reader));
            return cards;
        }

        public async Task<JobCard> GetJobCardByIdAsync(int id)
        {
            using var con = new SqlConnection(_conn);
            await con.OpenAsync();
            string sql = @"SELECT j.*, v.LicensePlate, c.FullName AS CustomerName, u.FullName AS AssignedUserName
                        FROM JobCards j
                        INNER JOIN Vehicles v ON j.VehicleId = v.VehicleId
                        INNER JOIN Customers c ON v.CustomerId = c.CustomerId
                        INNER JOIN Users u ON j.AssignedUserId = u.UserId
                        WHERE j.JobCardId = @Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            var card = MapJobCard(reader);
            reader.Close();

            // Load items for this card
            string itemsSql = @"SELECT i.JobCardItemId, i.JobCardId, i.InventoryItemId, i.Quantity, i.UnitPrice, inv.PartName
                            FROM JobCardItems i
                            INNER JOIN InventoryItems inv ON i.InventoryItemId = inv.InventoryItemId
                            WHERE i.JobCardId = @JobCardId";
            using var cmdItems = new SqlCommand(itemsSql, con);
            cmdItems.Parameters.AddWithValue("@JobCardId", id);
            using var readerItems = await cmdItems.ExecuteReaderAsync();
            while (await readerItems.ReadAsync())
            {
                card.Items.Add(new JobCardItem
                {
                    JobCardItemId = readerItems.GetInt32(0),
                    JobCardId = readerItems.GetInt32(1),
                    InventoryItemId = readerItems.GetInt32(2),
                    Quantity = readerItems.GetInt32(3),
                    UnitPrice = readerItems.GetDecimal(4),
                    PartName = readerItems.GetString(5)
                });
            }
            return card;
        }

        public async Task<int> CreateJobCardAsync(JobCard jobCard)
        {
            using var con = new SqlConnection(_conn);
            await con.OpenAsync();
            string sql = @"INSERT INTO JobCards (VehicleId, AssignedUserId, Status, Description)
                        OUTPUT INSERTED.JobCardId
                        VALUES (@VehicleId, @AssignedUserId, @Status, @Description)";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@VehicleId", jobCard.VehicleId);
            cmd.Parameters.AddWithValue("@AssignedUserId", jobCard.AssignedUserId);
            cmd.Parameters.AddWithValue("@Status", jobCard.Status);
            cmd.Parameters.AddWithValue("@Description", (object)jobCard.Description ?? DBNull.Value);
            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task UpdateJobCardAsync(JobCard jobCard)
        {
            using var con = new SqlConnection(_conn);
            await con.OpenAsync();
            string sql = @"UPDATE JobCards SET
                        VehicleId = @VehicleId, AssignedUserId = @AssignedUserId,
                        Status = @Status, Description = @Description,
                        DateCompleted = @DateCompleted
                        WHERE JobCardId = @Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", jobCard.JobCardId);
            cmd.Parameters.AddWithValue("@VehicleId", jobCard.VehicleId);
            cmd.Parameters.AddWithValue("@AssignedUserId", jobCard.AssignedUserId);
            cmd.Parameters.AddWithValue("@Status", jobCard.Status);
            cmd.Parameters.AddWithValue("@Description", (object)jobCard.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DateCompleted", (object)jobCard.DateCompleted ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteJobCardAsync(int id)
        {
            using var con = new SqlConnection(_conn);
            await con.OpenAsync();
            // Items will be cascade-deleted if you have FK constraint, but we must return stock first
            // For simplicity, we manually delete items and return stock.
            var itemsSql = "SELECT InventoryItemId, Quantity FROM JobCardItems WHERE JobCardId = @JobCardId";
            using var cmd = new SqlCommand(itemsSql, con);
            cmd.Parameters.AddWithValue("@JobCardId", id);
            var stockDict = new Dictionary<int, int>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                stockDict[reader.GetInt32(0)] = reader.GetInt32(1);
            reader.Close();

            foreach (var (invId, qty) in stockDict)
                await AdjustStock(con, invId, qty); // return stock

            string delItems = "DELETE FROM JobCardItems WHERE JobCardId = @Id";
            using var cmdDel = new SqlCommand(delItems, con);
            cmdDel.Parameters.AddWithValue("@Id", id);
            await cmdDel.ExecuteNonQueryAsync();

            string delCard = "DELETE FROM JobCards WHERE JobCardId = @Id";
            using var cmdDelCard = new SqlCommand(delCard, con);
            cmdDelCard.Parameters.AddWithValue("@Id", id);
            await cmdDelCard.ExecuteNonQueryAsync();
        }

        public async Task AddItemToJobCardAsync(int jobCardId, JobCardItem item)
        {
            using var con = new SqlConnection(_conn);
            await con.OpenAsync();
            using var tx = con.BeginTransaction();
            try
            {
                // Insert item
                string sql = @"INSERT INTO JobCardItems (JobCardId, InventoryItemId, Quantity, UnitPrice)
                            VALUES (@JobCardId, @InvId, @Qty, @Price)";
                using var cmd = new SqlCommand(sql, con, tx);
                cmd.Parameters.AddWithValue("@JobCardId", jobCardId);
                cmd.Parameters.AddWithValue("@InvId", item.InventoryItemId);
                cmd.Parameters.AddWithValue("@Qty", item.Quantity);
                cmd.Parameters.AddWithValue("@Price", item.UnitPrice);
                await cmd.ExecuteNonQueryAsync();

                // Deduct stock
                await AdjustStock(con, item.InventoryItemId, -item.Quantity, tx);
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task RemoveItemFromJobCardAsync(int jobCardItemId)
        {
            using var con = new SqlConnection(_conn);
            await con.OpenAsync();
            using var tx = con.BeginTransaction();
            try
            {
                // Get item details before deleting
                string getSql = "SELECT InventoryItemId, Quantity FROM JobCardItems WHERE JobCardItemId = @Id";
                using var cmd = new SqlCommand(getSql, con, tx);
                cmd.Parameters.AddWithValue("@Id", jobCardItemId);
                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return;
                int invId = reader.GetInt32(0);
                int qty = reader.GetInt32(1);
                reader.Close();

                // Delete the item
                string delSql = "DELETE FROM JobCardItems WHERE JobCardItemId = @Id";
                using var cmdDel = new SqlCommand(delSql, con, tx);
                cmdDel.Parameters.AddWithValue("@Id", jobCardItemId);
                await cmdDel.ExecuteNonQueryAsync();

                // Return stock
                await AdjustStock(con, invId, qty, tx);
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task AdjustStock(SqlConnection con, int itemId, int change, SqlTransaction tx = null)
        {
            string sql = "UPDATE InventoryItems SET QuantityInStock = QuantityInStock + @Change WHERE InventoryItemId = @Id";
            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.AddWithValue("@Id", itemId);
            cmd.Parameters.AddWithValue("@Change", change);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<User>> GetMechanicsAsync()
        {
            var users = new List<User>();
            using var con = new SqlConnection(_conn);
            await con.OpenAsync();
            string sql = @"SELECT u.UserId, u.FullName FROM Users u
                        INNER JOIN UserRoles ur ON u.UserId = ur.UserId
                        INNER JOIN Roles r ON ur.RoleId = r.RoleId
                        WHERE r.RoleName IN ('Admin','Manager','Mechanic') AND r.IsActive = 1
                        ORDER BY u.FullName";
            using var cmd = new SqlCommand(sql, con);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                users.Add(new User { UserId = reader.GetInt32(0), FullName = reader.GetString(1) });
            return users;
        }

        public async Task<List<Vehicle>> GetVehiclesAsync()
        {
            var vehicles = new List<Vehicle>();
            using var con = new SqlConnection(_conn);
            await con.OpenAsync();
            string sql = @"SELECT VehicleId, LicensePlate, Make, Model, c.CustomerId, c.FullName AS CustomerName
                        FROM Vehicles v
                        INNER JOIN Customers c ON v.CustomerId = c.CustomerId
                        ORDER BY LicensePlate";
            using var cmd = new SqlCommand(sql, con);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                vehicles.Add(new Vehicle
                {
                    VehicleId = reader.GetInt32(0),
                    LicensePlate = reader.GetString(1),
                    Make = reader.GetString(2),
                    Model = reader.GetString(3),
                    CustomerId = reader.GetInt32(4),
                    CustomerName = reader.GetString(5)
                });
            return vehicles;
        }

        public async Task<List<InventoryItem>> GetInventoryItemsAsync()
        {
            var items = new List<InventoryItem>();
            using var con = new SqlConnection(_conn);
            await con.OpenAsync();
            string sql = "SELECT InventoryItemId, PartName, UnitPrice, QuantityInStock FROM InventoryItems WHERE QuantityInStock > 0 ORDER BY PartName";
            using var cmd = new SqlCommand(sql, con);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new InventoryItem
                {
                    InventoryItemId = reader.GetInt32(0),
                    PartName = reader.GetString(1),
                    UnitPrice = reader.GetDecimal(2),
                    QuantityInStock = reader.GetInt32(3)
                });
            }
            return items;
        }

        private JobCard MapJobCard(SqlDataReader reader)
        {
            return new JobCard
            {
                JobCardId = reader.GetInt32(reader.GetOrdinal("JobCardId")),
                VehicleId = reader.GetInt32(reader.GetOrdinal("VehicleId")),
                AssignedUserId = reader.GetInt32(reader.GetOrdinal("AssignedUserId")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                DateCreated = reader.GetDateTime(reader.GetOrdinal("DateCreated")),
                DateCompleted = reader.IsDBNull(reader.GetOrdinal("DateCompleted")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateCompleted")),
                VehiclePlate = reader.GetString(reader.GetOrdinal("LicensePlate")),
                CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                AssignedUserName = reader.GetString(reader.GetOrdinal("AssignedUserName"))
            };
        }
    }
}
