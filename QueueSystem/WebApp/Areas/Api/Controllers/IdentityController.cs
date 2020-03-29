using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using WebApp.Areas.Identity.Pages.Account.Manage;
using WebApp.Hubs;
using WebApp.ServiceLogic;
using WebApp.Utility;

namespace WebApp.Areas.Api.Controllers
{
    [Area("Api")]
    [ApiController]
    [Route("api/auth")]
    public class IdentityController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly CustomUserManager _userManager;
        private readonly IQueueService _queueService;
        private readonly IManageHubUser _manageHubUser;
        private readonly IQueueHub _queueHub;
        private readonly IAntiforgery _antiforgery;

        public IdentityController(SignInManager<IdentityUser> signInManager,
                                CustomUserManager userManager,
                                IQueueHub queueHub,
                                IManageHubUser manageHubUser,
                                IQueueService queueService,
                                IAntiforgery antiforgery)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _queueHub = queueHub;
            _manageHubUser = manageHubUser;
            _queueService = queueService;
            _antiforgery = antiforgery;
        }

        //[Route("api/auth/index")]
        public IActionResult Index()
        {
            return NotFound();
        }

        [HttpGet("status")]
        [Authorize(Roles = StaticDetails.AdminUser + "," + StaticDetails.DoctorUser)]
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
        public async Task<IActionResult> Login([FromBody]LoginModel input)
         {
            var result = await _signInManager.PasswordSignInAsync(input.Username, input.Password, false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                Entities.Models.User user = await _userManager.FindByNameAsync(input.Username);
                if (user == null)
                    throw new UnauthorizedAccessException("No user with id {id} in database.");

                LoginResponse response = Authenticate(user.FirstName);
                
                return Ok(response);
            }
            return BadRequest();
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

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public string Username { get; set; }
        public string Token { get; set; }
    }
}