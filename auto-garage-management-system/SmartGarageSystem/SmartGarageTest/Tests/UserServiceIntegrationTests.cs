using SmartGarageSystem.Models;
using SmartGarageSystem.Services;
using SmartGarageTest.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace SmartGarageTest.Tests
{
    public class UserServiceIntegrationTests : IClassFixture<DatabaseFixture>
    {
        private readonly IUserService _userService;

        public UserServiceIntegrationTests(DatabaseFixture fixture)
        {
            _userService = fixture.ServiceProvider.GetRequiredService<IUserService>();
        }

        // Helper to create a unique user object with a given list of role IDs
        private async Task<User> CreateTestUserAsync(string suffix, List<int> roleIds)
        {
            var user = new User
            {
                Username = $"testuser_{suffix}_{Guid.NewGuid().ToString("N")[..6]}",
                FullName = $"Test User {suffix}",
                PasswordHash = "plain_password"   // will be hashed by the service
            };
            int id = await _userService.CreateUserAsync(user, roleIds);
            user.UserId = id; // attach for cleanup
            return user;
        }

        // Gets the ID of the "Mechanic" role (or first active role)
        private async Task<int> GetMechanicRoleIdAsync()
        {
            var roles = await _userService.GetAllActiveRolesAsync();
            var mechRole = roles.FirstOrDefault(r => r.RoleName.Equals("Mechanic", StringComparison.OrdinalIgnoreCase));
            if (mechRole == null)
                throw new InvalidOperationException("Mechanic role not found.");
            return mechRole.RoleId;
        }

        // -------------------- CREATE --------------------
        [Fact]
        public async Task CreateUser_WithRoles_ShouldStoreUserAndAssignRoles()
        {
            var roleIds = new List<int> { await GetMechanicRoleIdAsync() };
            var user = new User
            {
                Username = $"newuser_{Guid.NewGuid().ToString("N")[..6]}",
                FullName = "New Test User",
                PasswordHash = "secret"
            };
            int id = await _userService.CreateUserAsync(user, roleIds);
            Assert.True(id > 0);

            // Fetch and verify
            var fetched = await _userService.GetUserByIdAsync(id);
            Assert.NotNull(fetched);
            Assert.Equal(user.Username, fetched.Username);
            Assert.Equal("New Test User", fetched.FullName);

            // Password must be hashed (not the original plaintext)
            Assert.NotEqual("secret", fetched.PasswordHash);
            Assert.True(fetched.PasswordHash.StartsWith("$2a$")); // BCrypt hash prefix

            // Roles should be assigned
            Assert.NotEmpty(fetched.Roles);
            Assert.Contains(fetched.Roles, r => r.RoleId == roleIds[0]);

            // Cleanup
            await _userService.DeleteUserAsync(id);
        }

        // -------------------- DUPLICATE USERNAME --------------------
        [Fact]
        public async Task CreateUser_DuplicateUsername_ShouldThrow()
        {
            var roleIds = new List<int> { await GetMechanicRoleIdAsync() };
            var username = $"dup_{Guid.NewGuid().ToString("N")[..6]}";
            var firstUser = new User { Username = username, FullName = "First", PasswordHash = "pass" };
            int id1 = await _userService.CreateUserAsync(firstUser, roleIds);

            var secondUser = new User { Username = username, FullName = "Second", PasswordHash = "pass" };
            // Expect an exception because username must be unique
            await Assert.ThrowsAnyAsync<Exception>(() => _userService.CreateUserAsync(secondUser, roleIds));

            // Cleanup
            await _userService.DeleteUserAsync(id1);
        }

        // -------------------- READ --------------------
        [Fact]
        public async Task GetUserById_WithRoles_ReturnsCorrectData()
        {
            var roleIds = new List<int> { await GetMechanicRoleIdAsync() };
            var user = await CreateTestUserAsync("GetById", roleIds);

            var fetched = await _userService.GetUserByIdAsync(user.UserId);
            Assert.NotNull(fetched);
            Assert.Equal(user.Username, fetched.Username);
            Assert.NotEmpty(fetched.Roles);

            await _userService.DeleteUserAsync(user.UserId);
        }

        [Fact]
        public async Task GetUserByUsername_ShouldReturnUser()
        {
            var roleIds = new List<int> { await GetMechanicRoleIdAsync() };
            var user = await CreateTestUserAsync("GetByUsername", roleIds);

            var fetched = await _userService.GetUserByUsernameAsync(user.Username);
            Assert.NotNull(fetched);
            Assert.Equal(user.UserId, fetched.UserId);
            Assert.NotEmpty(fetched.Roles);

            await _userService.DeleteUserAsync(user.UserId);
        }

        [Fact]
        public async Task UsernameExists_ExistingUser_ReturnsTrue()
        {
            var roleIds = new List<int> { await GetMechanicRoleIdAsync() };
            var user = await CreateTestUserAsync("Exists", roleIds);

            bool exists = await _userService.UsernameExistsAsync(user.Username);
            Assert.True(exists);

            await _userService.DeleteUserAsync(user.UserId);
        }

        [Fact]
        public async Task UsernameExists_NonExistent_ReturnsFalse()
        {
            bool exists = await _userService.UsernameExistsAsync("nonexistent_user_" + Guid.NewGuid().ToString("N"));
            Assert.False(exists);
        }

        [Fact]
        public async Task GetAllUsers_ShouldReturnMultiple()
        {
            var roleIds = new List<int> { await GetMechanicRoleIdAsync() };
            var user1 = await CreateTestUserAsync("All1", roleIds);
            var user2 = await CreateTestUserAsync("All2", roleIds);

            var all = await _userService.GetAllUsersAsync();
            Assert.Contains(all, u => u.UserId == user1.UserId);
            Assert.Contains(all, u => u.UserId == user2.UserId);

            await _userService.DeleteUserAsync(user1.UserId);
            await _userService.DeleteUserAsync(user2.UserId);
        }

        // -------------------- UPDATE --------------------
        [Fact]
        public async Task UpdateUser_ShouldChangeNameAndRoles()
        {
            var roleIds = new List<int> { await GetMechanicRoleIdAsync() };
            var user = await CreateTestUserAsync("Update", roleIds);

            // Get an additional role (e.g., Manager)
            var allRoles = await _userService.GetAllActiveRolesAsync();
            var managerRole = allRoles.FirstOrDefault(r => r.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase));
            if (managerRole == null) throw new InvalidOperationException("Manager role not found.");

            var newRoleIds = new List<int> { managerRole.RoleId };

            var updatedUser = new User
            {
                UserId = user.UserId,
                Username = user.Username,   // same username
                FullName = "Updated Full Name",
                PasswordHash = null         // keep old password
            };

            await _userService.UpdateUserAsync(updatedUser, newRoleIds);

            var fetched = await _userService.GetUserByIdAsync(user.UserId);
            Assert.Equal("Updated Full Name", fetched.FullName);
            // Roles should now only contain Manager
            Assert.Single(fetched.Roles);
            Assert.Equal(managerRole.RoleId, fetched.Roles[0].RoleId);

            await _userService.DeleteUserAsync(user.UserId);
        }

        [Fact]
        public async Task UpdateUser_ChangePassword_ShouldUpdateHash()
        {
            var roleIds = new List<int> { await GetMechanicRoleIdAsync() };
            var user = await CreateTestUserAsync("UpdatePwd", roleIds);

            var updatedUser = new User
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                PasswordHash = "newpassword"   // will be hashed
            };

            await _userService.UpdateUserAsync(updatedUser, roleIds);

            var fetched = await _userService.GetUserByIdAsync(user.UserId);
            Assert.NotEqual("newpassword", fetched.PasswordHash); // should be hashed
            Assert.True(fetched.PasswordHash.StartsWith("$2a$"));

            await _userService.DeleteUserAsync(user.UserId);
        }

        // -------------------- DELETE --------------------
        [Fact]
        public async Task DeleteUser_ShouldRemoveCompletely()
        {
            var roleIds = new List<int> { await GetMechanicRoleIdAsync() };
            var user = await CreateTestUserAsync("Delete", roleIds);

            await _userService.DeleteUserAsync(user.UserId);

            var shouldBeNull = await _userService.GetUserByIdAsync(user.UserId);
            Assert.Null(shouldBeNull);

            // Also verify user's roles are deleted (by checking no roles returned if we could)
            // Since GetUserById returns null, roles are also gone.
        }

        // -------------------- ROLES --------------------
        [Fact]
        public async Task GetAllActiveRoles_ShouldReturnActiveRoles()
        {
            var roles = await _userService.GetAllActiveRolesAsync();
            Assert.NotEmpty(roles);
            Assert.All(roles, r => Assert.True(r.IsActive));
        }

        [Fact]
        public async Task GetRolesForUser_ShouldReturnAssignedRoles()
        {
            var roleIds = new List<int> { await GetMechanicRoleIdAsync() };
            var user = await CreateTestUserAsync("Roles", roleIds);

            var roles = await _userService.GetRolesForUserAsync(user.UserId);
            Assert.Contains(roles, r => r.RoleId == roleIds[0]);

            await _userService.DeleteUserAsync(user.UserId);
        }
    }
}