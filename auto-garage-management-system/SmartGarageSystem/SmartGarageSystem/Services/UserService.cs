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
    public class UserService : IUserService
    {
        private readonly string _connectionString;

        public UserService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SmartGarage");
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Username", username);
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        public async Task<List<Role>> GetAllActiveRolesAsync()
        {
            var roles = new List<Role>();
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = "SELECT RoleId, RoleName, IsActive, Description FROM Roles WHERE IsActive = 1 ORDER BY RoleName";
            using var cmd = new SqlCommand(sql, con);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                roles.Add(new Role
                {
                    RoleId = reader.GetInt32(0),
                    RoleName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
            return roles;
        }

        public async Task<List<Role>> GetRolesForUserAsync(int userId)
        {
            var roles = new List<Role>();
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = @"SELECT r.RoleId, r.RoleName, r.IsActive, r.Description 
                           FROM UserRoles ur
                           INNER JOIN Roles r ON ur.RoleId = r.RoleId
                           WHERE ur.UserId = @UserId AND r.IsActive = 1";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@UserId", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                roles.Add(new Role
                {
                    RoleId = reader.GetInt32(0),
                    RoleName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
            return roles;
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = "SELECT UserId, Username, PasswordHash, FullName FROM Users WHERE UserId = @UserId";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@UserId", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows) return null;
            await reader.ReadAsync();
            var user = new User
            {
                UserId = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                FullName = reader.GetString(3)
            };
            reader.Close();
            user.Roles = await GetRolesForUserAsync(userId);
            return user;
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = "SELECT UserId, Username, PasswordHash, FullName FROM Users WHERE Username = @Username";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Username", username);
            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows) return null;
            await reader.ReadAsync();
            var user = new User
            {
                UserId = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                FullName = reader.GetString(3)
            };
            reader.Close();
            user.Roles = await GetRolesForUserAsync(user.UserId);
            return user;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            string sql = "SELECT UserId, Username, PasswordHash, FullName FROM Users ORDER BY Username";
            using var cmd = new SqlCommand(sql, con);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    PasswordHash = reader.GetString(2),
                    FullName = reader.GetString(3)
                });
            }
            reader.Close();
            // Load roles for each user (can be optimised with a bulk query)
            foreach (var user in users)
                user.Roles = await GetRolesForUserAsync(user.UserId);
            return users;
        }

        public async Task<int> CreateUserAsync(User user, List<int> roleIds)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            using var trans = con.BeginTransaction();
            try
            {
                // Insert user
                string insertUser = @"INSERT INTO Users (Username, PasswordHash, FullName) 
                                      OUTPUT INSERTED.UserId 
                                      VALUES (@Username, @PasswordHash, @FullName)";
                using var cmdUser = new SqlCommand(insertUser, con, trans);
                cmdUser.Parameters.AddWithValue("@Username", user.Username);
                // Hash the password (BCrypt)
                cmdUser.Parameters.AddWithValue("@PasswordHash",
                    BCrypt.Net.BCrypt.HashPassword(user.PasswordHash));
                cmdUser.Parameters.AddWithValue("@FullName", user.FullName ?? "");
                int userId = (int)await cmdUser.ExecuteScalarAsync();

                // Assign roles
                if (roleIds != null && roleIds.Count > 0)
                {
                    string insertRole = "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
                    foreach (var roleId in roleIds)
                    {
                        using var cmdRole = new SqlCommand(insertRole, con, trans);
                        cmdRole.Parameters.AddWithValue("@UserId", userId);
                        cmdRole.Parameters.AddWithValue("@RoleId", roleId);
                        await cmdRole.ExecuteNonQueryAsync();
                    }
                }

                await trans.CommitAsync();
                return userId;
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateUserAsync(User user, List<int> roleIds)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            using var trans = con.BeginTransaction();
            try
            {
                // Update base info (if password is empty, keep old)
                string updateUser;
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    updateUser = @"UPDATE Users SET Username = @Username, PasswordHash = @PasswordHash, FullName = @FullName 
                                   WHERE UserId = @UserId";
                }
                else
                {
                    updateUser = @"UPDATE Users SET Username = @Username, FullName = @FullName 
                                   WHERE UserId = @UserId";
                }
                using var cmdUser = new SqlCommand(updateUser, con, trans);
                cmdUser.Parameters.AddWithValue("@UserId", user.UserId);
                cmdUser.Parameters.AddWithValue("@Username", user.Username);
                if (!string.IsNullOrEmpty(user.PasswordHash))
                    cmdUser.Parameters.AddWithValue("@PasswordHash",
                        BCrypt.Net.BCrypt.HashPassword(user.PasswordHash));
                cmdUser.Parameters.AddWithValue("@FullName", user.FullName ?? "");
                await cmdUser.ExecuteNonQueryAsync();

                // Replace roles: delete all existing, then insert new ones
                string deleteRoles = "DELETE FROM UserRoles WHERE UserId = @UserId";
                using var cmdDelete = new SqlCommand(deleteRoles, con, trans);
                cmdDelete.Parameters.AddWithValue("@UserId", user.UserId);
                await cmdDelete.ExecuteNonQueryAsync();

                string insertRole = "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
                foreach (var roleId in roleIds)
                {
                    using var cmdRole = new SqlCommand(insertRole, con, trans);
                    cmdRole.Parameters.AddWithValue("@UserId", user.UserId);
                    cmdRole.Parameters.AddWithValue("@RoleId", roleId);
                    await cmdRole.ExecuteNonQueryAsync();
                }

                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteUserAsync(int userId)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            // CASCADE will remove UserRoles automatically
            string sql = "DELETE FROM Users WHERE UserId = @UserId";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await cmd.ExecuteNonQueryAsync();
        }
    }

}
