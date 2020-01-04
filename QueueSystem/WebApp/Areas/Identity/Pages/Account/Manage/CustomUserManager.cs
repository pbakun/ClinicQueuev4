using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Utility;

namespace WebApp.Areas.Identity.Pages.Account.Manage
{
    public class CustomUserManager : UserManager<IdentityUser>
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        public CustomUserManager(IUserStore<IdentityUser> store, 
                                IOptions<IdentityOptions> optionsAccessor,
                                IPasswordHasher<IdentityUser> passwordHasher,
                                IEnumerable<IUserValidator<IdentityUser>> userValidators,
                                IEnumerable<IPasswordValidator<IdentityUser>> passwordValidators,
                                ILookupNormalizer keyNormalizer,
                                IdentityErrorDescriber errors,
                                IServiceProvider services,
                                ILogger<UserManager<IdentityUser>> logger, RoleManager<IdentityRole> roleManager
                                ) : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, 
                                    keyNormalizer, errors, services, logger)
        {
                
        }

        public override async Task<bool> IsEmailConfirmedAsync(IdentityUser user)
        {
            var rolesList = await GetRolesAsync(user);
            foreach(var role in rolesList)
            {
                if(!role.Equals(StaticDetails.AdminUser) && !role.Equals(StaticDetails.DoctorUser) && !role.Equals(StaticDetails.NurseUser))
                {
                    return await base.IsEmailConfirmedAsync(user);
                }
            }
            return true;
        }
    }
}
