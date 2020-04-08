using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using WebApp.Areas.Api.Data.User;
using WebApp.Areas.Identity.Pages.Account.Manage;
using WebApp.ServiceLogic.Interface;
using WebApp.Utility;

namespace WebApp.Areas.Api.Controllers
{
    [ApiController]
    [Authorize(
            Roles = StaticDetails.AdminUser + "," + StaticDetails.DoctorUser,
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            )
    ]
    [Route("api/user")]
    public class UserController : Controller
    {
        private readonly CustomUserManager _userManager;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public UserController(CustomUserManager userManager, IUserService userService, IMapper mapper)
        {
            _userManager = userManager;
            _userService = userService;
            _mapper = mapper;
        }

        [HttpGet("userdetails")]
        public UserDto UserDetails()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var user = _userService.GetUserById(claims.Value);

            return _mapper.Map<UserDto>(user);
        }

        [HttpPut("userdetails")]
        public async Task<IActionResult> UserDetailsPost([FromBody]UserDto input)
        {
            var user = await _userManager.GetUserAsync(User) as Entities.Models.User;

            var email = await _userManager.GetEmailAsync(user);
            if (input.Email != email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, input.Email);
                if (!setEmailResult.Succeeded)
                {
                    var userId = await _userManager.GetUserIdAsync(user);
                    throw new InvalidOperationException($"Unexpected error occurred setting email for user with ID '{userId}'.");
                }
            }

            if (input.FirstName != null && input.FirstName != user.FirstName)
            {
                var setFirstNameSuccedded = await _userManager.SetFirstNameAsync(user, input.FirstName);
                if (!setFirstNameSuccedded)
                {
                    var userId = await _userManager.GetUserIdAsync(user);
                    throw new InvalidOperationException($"Unexpected error occurred setting first name for user with ID '{userId}'.");
                }
            }

            if (input.LastName != null && input.LastName != user.LastName)
            {
                var setLastNameSuccedded = await _userManager.SetLastNameAsync(user, input.LastName);
                if (!setLastNameSuccedded)
                {
                    var userId = await _userManager.GetUserIdAsync(user);
                    throw new InvalidOperationException($"Unexpected error occurred setting last name for user with ID '{userId}'.");
                }
            }

            return Ok();
        }

        [HttpPut("changepassword")]
        public async Task<IActionResult> ChangePassword([FromBody]ChangePasswordDtin input)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Log.Error($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
                return BadRequest();
            }

            if (input.NewPassword != input.ConfirmPassword)
                return BadRequest("Password doesn't match");

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, input.OldPassword, input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    Log.Error($"Change Password Error: '{error.Description}'.");
                }
                return BadRequest(changePasswordResult.Errors.First().Description);
            }

            return Ok();
        }
    }
}