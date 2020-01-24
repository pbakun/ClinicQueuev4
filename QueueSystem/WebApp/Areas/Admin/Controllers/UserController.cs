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
using WebApp.ServiceLogic;
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
        private readonly IQueueService _queueService;

        [BindProperty]
        public List<UserViewModel> UserVM { get; set; }

        [BindProperty]
        public UserViewModel EditUserVM { get; set; }

        public UserController(IRepositoryWrapper repo, 
                            IMapper mapper, 
                            CustomUserManager userManager, 
                            IQueueService queueService)
        {
            _repo = repo;
            _mapper = mapper;
            _userManager = userManager;
            _queueService = queueService;

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

            if (id == claim.Value)
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

        [HttpGet]
        public async Task<IActionResult> Details(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);
            EditUserVM = new UserViewModel()
            {
                User = user,
                Roles = roles.ToList()
            };

            return PartialView("_ManageUserDetails", EditUserVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                bool changes = false;
                if (!user.FirstName.Equals(EditUserVM.User.FirstName))
                {
                    user.FirstName = EditUserVM.User.FirstName;
                    changes = true;
                }

                if (!user.LastName.Equals(EditUserVM.User.LastName))
                {
                    user.LastName = EditUserVM.User.LastName;
                    changes = true;
                }
                if (changes)
                {
                    _queueService.UpdateOwnerInitials(user);
                }
                if (!user.RoomNo.Equals(EditUserVM.User.RoomNo))
                {
                    user.RoomNo = EditUserVM.User.RoomNo;
                    await _userManager.SetRoomNoAsync(user.Id, user.RoomNo);
                    await _queueService.ChangeUserRoomNo(user.Id, user.RoomNo);
                }
                _repo.User.Update(user);
                await _repo.SaveAsync();
                return LocalRedirect("/Admin/User");
            }
            return StatusCode(500);

        }

        [HttpGet]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetAllRoles(string userId)
        {
            var user = _repo.User.FindByCondition(u => u.Id == userId).SingleOrDefault();
            var userRoles = await _userManager.GetRolesAsync(_mapper.Map<User>(user));
            var model = new ModifyUserRoles()
            {
                UserId = userId,
                Roles = (userRoles.Count() > 0) ? userRoles.ToArray() : new string[0],
                AvailableRoles = _userManager.GetRoles().ToList()
            };

            //return Ok(model);
            return PartialView("_AvailableRoles", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRole([FromBody]ModifyUserRoles addRole)
        {
            var newRoles = await _userManager.AddToRolesAsync(addRole.UserId, addRole.Roles);

            return Ok(newRoles);
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole([FromBody]ModifyUserRoles deleteRole)
        {
            var newRoles = await _userManager.RemoveFromRolesAsync(deleteRole.UserId, deleteRole.Roles);
            
            return Ok(newRoles);
        }
    }
}