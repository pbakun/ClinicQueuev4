using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Serilog;
using WebApp.Areas.Api.Data.Identity;
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
        private readonly IEmailSender _emailSender;

        public IdentityController(SignInManager<IdentityUser> signInManager,
                                CustomUserManager userManager,
                                IQueueHub queueHub,
                                IManageHubUser manageHubUser,
                                IQueueService queueService,
                                IAntiforgery antiforgery,
                                IUserService userService,
                                IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _queueHub = queueHub;
            _manageHubUser = manageHubUser;
            _queueService = queueService;
            _antiforgery = antiforgery;
            _userService = userService;
            _emailSender = emailSender;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody]LoginDtin input)
        {
            try
            {
                AuthDto response = await _userService.AuthenticateAsync(input.Username, input.Password);

                if (response == null)
                    return BadRequest(new { message = "Username or password incorrect" });
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var claimIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            _queueService.SetQueueInactive(claim.Value);

            var hubUser = _manageHubUser.GetConnectedUserById(claim.Value);
            if (hubUser != null && hubUser.Count() == 1)
            {
                _queueHub.InitGroupScreen(hubUser.FirstOrDefault());
            }

            return Ok();
        }

        [HttpPost("forgotpassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody]ForgotPasswordDtin input)
        {

            var user = await _userManager.FindByEmailAsync(input.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Don't reveal that the user does not exist or is not confirmed
                return Ok();
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);

            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { code },
                protocol: JwtBearerDefaults.AuthenticationScheme);

            string callbackUrl2 = QueryHelpers.AddQueryString("http://localhost:5000/Identity/Account/ResetPassword", "code", code);

            await _emailSender.SendEmailAsync(
                input.Email,
                "Reset Hasła",
                $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl2)}'>clicking here</a>.");

            return Ok();
        }
    }

}