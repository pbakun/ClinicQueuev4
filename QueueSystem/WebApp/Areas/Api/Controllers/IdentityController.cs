using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using WebApp.Areas.Identity.Pages.Account.Manage;
using WebApp.Hubs;
using WebApp.Models.Dtos;
using WebApp.ServiceLogic;
using WebApp.ServiceLogic.Interface;
using WebApp.Utility;

namespace WebApp.Areas.Api.Controllers
{
    [Area("Api")]
    [ApiController]
    [Route("api/auth")]
    [Authorize(
            Roles = StaticDetails.AdminUser,
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            )
    ]
    public class IdentityController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly CustomUserManager _userManager;
        private readonly IQueueService _queueService;
        private readonly IManageHubUser _manageHubUser;
        private readonly IQueueHub _queueHub;
        private readonly IAntiforgery _antiforgery;
        private readonly IUserService _userService;

        public IdentityController(SignInManager<IdentityUser> signInManager,
                                CustomUserManager userManager,
                                IQueueHub queueHub,
                                IManageHubUser manageHubUser,
                                IQueueService queueService,
                                IAntiforgery antiforgery,
                                IUserService userService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _queueHub = queueHub;
            _manageHubUser = manageHubUser;
            _queueService = queueService;
            _antiforgery = antiforgery;
            _userService = userService;
        }

        [HttpGet("status")]
        public async Task<IActionResult> CheckIfLogged()
        {
            var claimIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            Log.Information("Login attempt");

            Entities.Models.User user = await _userManager.FindByIdAsync(claim.Value);
            if (user == null)
                throw new UnauthorizedAccessException("No user with id {id} in database.");

            LoginResponse response = Authenticate(user.FirstName);

            return Ok(response);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody]LoginDto input)
         {
            AuthDto response = await _userService.AuthenticateAsync(input.Username, input.Password);
            if (response == null)
                return BadRequest(new { message = "Username or password incorrect" });
            return Ok(response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var claimIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            _queueService.SetQueueInactive(claim.Value);

            var hubUser = _manageHubUser.GetConnectedUserById(claim.Value);
            if (hubUser != null && hubUser.Count() == 1)
            {
                _queueHub.InitGroupScreen(hubUser.FirstOrDefault());
            }
            await _signInManager.SignOutAsync();

            return Ok();
        }

        private LoginResponse Authenticate(string username)
        {

            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            var response = new LoginResponse
            {
                Username = username,
                Token = tokens.RequestToken
            };

            return response;
        }
    }

    public class LoginResponse
    {
        public string Username { get; set; }
        public string Token { get; set; }
    }
}