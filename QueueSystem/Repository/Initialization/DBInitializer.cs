using Entities;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Repository.Initialization
{
    public class DBInitializer : IDBInitializer
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly RepositoryContext _repo;
        private readonly IConfiguration _configuration;

        public const string AdminUser = "Admin";
        public const string DoctorUser = "Doctor";
        public const string NurseUser = "Nurse";
        public const string PatientUser = "Patient";

        public DBInitializer(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, RepositoryContext repo, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _repo = repo;
            _configuration = configuration;
        }

        public async void Initialize()
        {
            //Create roles
            if (!_roleManager.RoleExistsAsync(AdminUser).GetAwaiter().GetResult())
                 _roleManager.CreateAsync(new IdentityRole(AdminUser)).GetAwaiter().GetResult();
            if (! _roleManager.RoleExistsAsync(DoctorUser).GetAwaiter().GetResult())
                 _roleManager.CreateAsync(new IdentityRole(DoctorUser)).GetAwaiter().GetResult();
            if (! _roleManager.RoleExistsAsync(NurseUser).GetAwaiter().GetResult())
                 _roleManager.CreateAsync(new IdentityRole(NurseUser)).GetAwaiter().GetResult();
            if (! _roleManager.RoleExistsAsync(PatientUser).GetAwaiter().GetResult())
                 _roleManager.CreateAsync(new IdentityRole(PatientUser)).GetAwaiter().GetResult();

            var initialUser = _configuration.GetSection("InitialUser").Get<InitialUser>();

            //Create admin user
            var result = _userManager.CreateAsync(new User
            {
                UserName= initialUser.Username,
                Email=initialUser.Email,
                EmailConfirmed= true,
                FirstName=initialUser.FirstName,
                LastName=initialUser.LastName,
                RoomNo=initialUser.RoomNo
            }, initialUser.Password).GetAwaiter().GetResult();

            if(result.Succeeded)
                Log.Information("Initial user from configuration created");

            IdentityUser user = await _repo.User.FirstOrDefaultAsync(u => u.UserName == initialUser.Username);

            await _userManager.AddToRoleAsync(user, AdminUser);
        }
    }
}
