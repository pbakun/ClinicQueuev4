using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApp.Hubs;

namespace WebApp.Extensions
{
    public class HubRequirement : AuthorizationHandler<HubRequirement>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HubRequirement requirement)
        {

            var user = context.User.HasClaim(c => c.Type == ClaimTypes.NameIdentifier);

            if(user)
                context.Succeed(requirement);

            context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}

