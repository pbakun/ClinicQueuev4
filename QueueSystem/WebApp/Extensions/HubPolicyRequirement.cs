using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Extensions
{
    public class HubPolicyRequirement : AuthorizationHandler<HubPolicyRequirement, HubInvocationContext>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HubPolicyRequirement requirement, HubInvocationContext resource)
        {
            Log.Information(context.User.Identity.Name);

            if (context.User.Identity.Name != null)
                context.Succeed(requirement);
            else context.Fail();

            return Task.CompletedTask;
        }
    }
}
