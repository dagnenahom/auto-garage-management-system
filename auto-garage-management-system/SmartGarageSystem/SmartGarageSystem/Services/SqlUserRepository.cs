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
    
    public class SqlUserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public SqlUserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SmartGarage");
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // 1. Load user base info (no Role column)
            string userQuery = @"SELECT UserId, Username, PasswordHash, FullName 
                                 FROM Users 
                                 WHERE Username = @Username";
            var user = new User();

            using (var cmd = new SqlCommand(userQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                    return null;                           // user not found

                await reader.ReadAsync();
                user.UserId = reader.GetInt32(0);
                user.Username = reader.GetString(1);
                user.PasswordHash = reader.GetString(2);
                user.FullName = reader.GetString(3);
            }

            // 2. Load the user's active roles
            string rolesQuery = @"
                SELECT r.RoleId, r.RoleName, r.IsActive, r.Description
                FROM UserRoles ur
                INNER JOIN Roles r ON ur.RoleId = r.RoleId
                WHERE ur.UserId = @UserId AND r.IsActive = 1";
            using var cmdRoles = new SqlCommand(rolesQuery, connection);
            cmdRoles.Parameters.AddWithValue("@UserId", user.UserId);
            using var readerRoles = await cmdRoles.ExecuteReaderAsync();
            while (await readerRoles.ReadAsync())
            {
                user.Roles.Add(new Role
                {
                    RoleId = readerRoles.GetInt32(0),
                    RoleName = readerRoles.GetString(1),
                    IsActive = readerRoles.GetBoolean(2),
                    Description = readerRoles.IsDBNull(3) ? null : readerRoles.GetString(3)
                });
            }

            return user;
        }
    }
}
