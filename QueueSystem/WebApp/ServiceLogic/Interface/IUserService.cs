using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Models.Dtos;

namespace WebApp.ServiceLogic.Interface
{
    public interface IUserService
    {
        Task<AuthDto> AuthenticateAsync(string username, string password);
    }
}
