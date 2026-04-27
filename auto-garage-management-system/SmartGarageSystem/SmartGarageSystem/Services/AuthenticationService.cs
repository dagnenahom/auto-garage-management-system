using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SmartGarageSystem.Models;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
    /* this was used first for testing purpose
     * public class AuthenticationService : IAuthenticationService
    {
        public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
        {
            await Task.Delay(300);

            if (username == "admin" && password == "1234")
            {
                return new AuthenticationResult { IsSuccess = true };
            }
            else
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid username or password."
                };
            }
        }

        
    }*/

    public class AuthenticationService : IAuthenticationService
    {
        private readonly string _connectionString;

        public AuthenticationService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SmartGarage");
        }

        public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
        {
            var result = new AuthenticationResult();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                result.ErrorMessage = "Username and password are required.";
                return result;
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1. Get user base info
                string userQuery = "SELECT UserId, Username, PasswordHash, FullName FROM Users WHERE Username = @Username";
                var user = new User();

                using (var cmd = new SqlCommand(userQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (!reader.HasRows)
                    {
                        result.ErrorMessage = "Invalid username or password.";
                        return result;
                    }
                    await reader.ReadAsync();
                    user.UserId = reader.GetInt32(0);
                    user.Username = reader.GetString(1);
                    user.PasswordHash = reader.GetString(2);
                    user.FullName = reader.GetString(3);
                }

                // 2. Verify password
                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    result.ErrorMessage = "Invalid username or password.";
                    return result;
                }

                // 3. Load user roles (only active ones)
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

                result.IsSuccess = true;
                result.AuthenticatedUser = user;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "Login error: " + ex.Message;
            }

            return result;
        }
    }
}


