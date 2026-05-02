using SmartGarageSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByUsernameAsync(string username);
        Task<List<User>> GetAllUsersAsync();          // including roles
        Task<int> CreateUserAsync(User user, List<int> roleIds);
        Task UpdateUserAsync(User user, List<int> roleIds);
        Task DeleteUserAsync(int userId);
        Task<bool> UsernameExistsAsync(string username);
        Task<List<Role>> GetAllActiveRolesAsync();
        Task<List<Role>> GetRolesForUserAsync(int userId);
    }
}
