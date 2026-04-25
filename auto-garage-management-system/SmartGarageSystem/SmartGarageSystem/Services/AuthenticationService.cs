using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
    public class AuthenticationService : IAuthenticationService
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

        
    }
}


