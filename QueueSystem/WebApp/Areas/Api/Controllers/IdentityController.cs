using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using WebApp.Hubs;
using WebApp.ServiceLogic;

namespace WebApp.Areas.Api.Controllers
{
    [Area("Api")]
    [AllowAnonymous]
    [ApiController]
    [Route("api/auth")]
    public class IdentityController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IQueueService _queueService;
        private readonly IManageHubUser _manageHubUser;
        private readonly IQueueHub _queueHub;

        public IdentityController(SignInManager<IdentityUser> signInManager,
                                IQueueHub queueHub,
                                IManageHubUser manageHubUser,
                                IQueueService queueService)
        {
            _signInManager = signInManager;
            _queueHub = queueHub;
            _manageHubUser = manageHubUser;
            _queueService = queueService;
        }

        //[Route("api/auth/index")]
        public IActionResult Index()
        {
            return NotFound();
        }

        [HttpPost("login")]
        //[Route("/api/auth/login")]
        public async Task<IActionResult> Login([FromBody]LoginModel input)
         {
            var result = await _signInManager.PasswordSignInAsync(input.Username, input.Password, false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return Ok();
            }
            return BadRequest();
        }

        [HttpPost("logout")]
        //[Route("/api/auth/logout")]
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
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}