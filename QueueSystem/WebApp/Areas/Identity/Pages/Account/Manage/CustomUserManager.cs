using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.ServiceLogic;
using WebApp.Utility;

namespace WebApp.Areas.Identity.Pages.Account.Manage
{
    public class CustomUserManager : UserManager<IdentityUser>
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IRepositoryWrapper _repo;
        private readonly IQueueService _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        public CustomUserManager(IUserStore<IdentityUser> store, 
                                IOptions<IdentityOptions> optionsAccessor,
                                IPasswordHasher<IdentityUser> passwordHasher,
                                IEnumerable<IUserValidator<IdentityUser>> userValidators,
                                IEnumerable<IPasswordValidator<IdentityUser>> passwordValidators,
                                ILookupNormalizer keyNormalizer,
                                IdentityErrorDescriber errors,
                                IServiceProvider services,
                                ILogger<UserManager<IdentityUser>> logger,
                                RoleManager<IdentityRole> roleManager,
                                IRepositoryWrapper repo,
                                IQueueService queue,
                                IServiceScopeFactory serviceScopeFactory
                                ) : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, 
                                    keyNormalizer, errors, services, logger)
        {
            _repo = repo;
            _queue = queue;
            _scopeFactory = serviceScopeFactory;
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

        public async Task<IList<string>> GetRolesAsync(WebApp.Models.User user)
        {
            var identityUser = await base.FindByIdAsync(user.Id);

            return await base.GetRolesAsync(identityUser);
        }

        public async Task<bool> SetFirstNameAsync(User user, string newFirstName)
        {
            user.FirstName = newFirstName;
            var initialsUpdateSucess = _queue.UpdateOwnerInitials(user);
            if (!initialsUpdateSucess)
            {
                return await Task.FromResult<bool>(false);
            }

            using ( var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IRepositoryWrapper>();
                repo.User.Update(user);
                await _repo.SaveAsync();
            }

            return await Task.FromResult<bool>(true);
        }

        public async Task<bool> SetLastNameAsync(User user, string newLastName)
        {
            user.LastName = newLastName;
            var initialsUpdateSucess = _queue.UpdateOwnerInitials(user);
            if(!initialsUpdateSucess)
            {
                return await Task.FromResult<bool>(false);
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IRepositoryWrapper>();
                repo.User.Update(user);
                await _repo.SaveAsync();
            }

            return await Task.FromResult<bool>(true);
        }

    }
}
