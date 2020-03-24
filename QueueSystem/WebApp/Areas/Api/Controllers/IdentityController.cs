using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace WebApp.Areas.Api.Controllers
{
    [Area("Api")]
    public class IdentityController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;

        public IdentityController(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [Route("api/auth/index")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Route("/api/login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            Log.Debug(username);
            var result = await _signInManager.PasswordSignInAsync(username, password, false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return Ok();
            }
            return BadRequest();
        }
    }
}