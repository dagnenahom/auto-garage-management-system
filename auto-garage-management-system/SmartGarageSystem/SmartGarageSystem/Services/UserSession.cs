using SmartGarageSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
    public interface IUserSession
    {
        User CurrentUser { get; }
        bool IsLoggedIn { get; }
        void SetUser(User user);
        void Clear();
        bool IsInRole(string roleName);
        bool IsInAnyRole(params string[] roleNames);
    }

    public class UserSession : IUserSession
    {
        public User CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;

        public void SetUser(User user) => CurrentUser = user;
        public void Clear() => CurrentUser = null;

        public bool IsInRole(string roleName)
        {
            return IsLoggedIn && CurrentUser.Roles != null &&
                   CurrentUser.Roles.Any(r => r.RoleName.Equals(roleName, System.StringComparison.OrdinalIgnoreCase) && r.IsActive);
        }

        public bool IsInAnyRole(params string[] roleNames)
        {
            return IsLoggedIn && CurrentUser.Roles != null &&
                   CurrentUser.Roles.Any(r => r.IsActive && roleNames.Contains(r.RoleName, System.StringComparer.OrdinalIgnoreCase));
        }
    }

}
