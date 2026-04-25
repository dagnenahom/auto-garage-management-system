using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
    public interface IAuthenticationService
    {
        public Task<AuthenticationResult> AuthenticateAsync(string username, string password)
        {
            throw new NotImplementedException();
        }

    }

}
