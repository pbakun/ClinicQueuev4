using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WebApp.Areas.Identity.Pages.Account.Manage;
using WebApp.Models;
using WebApp.Models.Dtos;
using WebApp.ServiceLogic.Interface;

namespace WebApp.ServiceLogic
{
    public class UserService : IUserService
    {
        private readonly IRepositoryWrapper _repo;
        private readonly CustomUserManager _userManager;
        private readonly AuthSettings _authSettings;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserService(IRepositoryWrapper repo,
                           CustomUserManager userManager,
                           IOptions<AuthSettings> authSettings,
                           RoleManager<IdentityRole> roleManager
            )
        {
            _repo = repo;
            _userManager = userManager;
            _authSettings = authSettings.Value;
            _roleManager = roleManager;
        }

        #region authentication

        public async Task<AuthDto> AuthenticateAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
                return null;

            var result = await _userManager.CheckPasswordAsync(user as IdentityUser, password);

            if (!result)
                throw new UnauthorizedAccessException();

            return new AuthDto
            {
                FirstName = user.FirstName,
                Token = await GetToken(user as IdentityUser)
            };
        }

        private async Task<string> GetToken(IdentityUser user)
        {

            var userRoles = await _userManager.GetRolesAsync(user);
            var claim = new Claim(ClaimTypes.NameIdentifier, user.Id);
            var claims = new List<Claim>() { claim };
            foreach(var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_authSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        #endregion

        public Entities.Models.User GetUserById(string id)
        {
            return _repo.User.FindByCondition(u => u.Id == id).SingleOrDefault();
        }
    }
}
