using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Repository.Interfaces;
using WebApp.Areas.Identity.Pages.Account.Manage;
using WebApp.Models;
using WebApp.Models.ViewModel;
using WebApp.Utility;

namespace WebApp.Areas.Admin.Controllers
{
    [Authorize(Roles = StaticDetails.AdminUser)]
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly IRepositoryWrapper _repo;
        private readonly IMapper _mapper;
        private readonly CustomUserManager _userManager;

        public List<UserViewModel> UserVM { get; set; }

        public UserController(IRepositoryWrapper repo, IMapper mapper, CustomUserManager userManager)
        {
            _repo = repo;
            _mapper = mapper;
            _userManager = userManager;

            UserVM = new List<UserViewModel>();
        }

        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim.Value == null)
                return BadRequest();

            var users = _repo.User.FindAll().ToList();
            var usersInModel = _mapper.Map<List<User>>(users);

            foreach (var user in usersInModel)
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                UserVM.Add(new UserViewModel()
                {
                    User = user,
                    Roles = userRoles.ToList()
                });
            }

            return View(UserVM);
        }

        public async Task<IActionResult> Lock(string id)
        {
            if (id == null)
                return NotFound();

            var claimIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if(id == claim.Value)
                return RedirectToAction(nameof(Index));

            var user = _repo.User.FindByCondition(u => u.Id == id).FirstOrDefault();

            if (user == null)
                return NotFound();

            user.LockoutEnd = DateTime.Now.AddYears(1000);

            _repo.User.Update(user);
            await _repo.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UnLock(string id)
        {
            if (id == null)
                return NotFound();

            var user = _repo.User.FindByCondition(u => u.Id == id).FirstOrDefault();

            if (user == null)
                return NotFound();

            user.LockoutEnd = DateTime.Now;

            _repo.User.Update(user);
            await _repo.SaveAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}