using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebApp.Hubs;
using WebApp.ServiceLogic;

namespace WebApp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;
        private readonly IQueueService _queueService;
        private readonly IManageHubUser _manageHubUser;
        private readonly IQueueHub _queueHub;

        public LogoutModel(SignInManager<IdentityUser> signInManager, 
            ILogger<LogoutModel> logger, 
            IQueueService queueService,
            IManageHubUser manageHubUser,
            IQueueHub queueHub)
        {
            _signInManager = signInManager;
            _logger = logger;
            _queueService = queueService;
            _manageHubUser = manageHubUser;
            _queueHub = queueHub;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            var claimIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            _queueService.SetQueueInactive(claim.Value);

            var hubUser = _manageHubUser.GetConnectedUserById(claim.Value).FirstOrDefault();
            if(hubUser != null)
            {
                _queueHub.InitGroupScreen(hubUser);
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return Page();
            }
        }
    }
}