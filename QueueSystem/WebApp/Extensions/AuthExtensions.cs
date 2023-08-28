using Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApp.Areas.Identity.Pages.Account.Manage;
using WebApp.Utility;

namespace WebApp.Extensions
{
    public static class AuthExtensions
    {
        public static void ConfigureIdentity(this IServiceCollection services)
        {
            services.AddIdentity<IdentityUser, IdentityRole>(config =>
            {
                config.Password.RequireNonAlphanumeric = false;
                config.Password.RequireDigit = false;
                config.Password.RequiredLength = 0;
                config.Password.RequiredUniqueChars = 0;
                config.Password.RequireUppercase = false;
                config.Password.RequireLowercase = false;
            })
               .AddRoleManager<RoleManager<IdentityRole>>()
               .AddUserManager<UserManager<IdentityUser>>()
               .AddUserManager<CustomUserManager>()
               .AddDefaultTokenProviders()
               .AddDefaultUI()
               .AddEntityFrameworkStores<RepositoryContext>(); //would be best to add this in ServiceExtensions class in Repository library
        }

        public static void ConfigureAuthMethods(this IServiceCollection services, string secretKey)
        {
            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(1);
                options.LoginPath = "/Identity/Account/Login";
                options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
                options.SlidingExpiration = true;
                options.ForwardDefault = CookieAuthenticationDefaults.AuthenticationScheme;
            });

            var key = Encoding.ASCII.GetBytes(secretKey);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, cfg => {
                    cfg.SlidingExpiration = true;
                    cfg.LoginPath = "/Identity/Account/Login";
                })
                .AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };

                    x.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            // If the request is for our hub...
                            var path = context.HttpContext.Request.Path.ToString();
                            if (!string.IsNullOrEmpty(accessToken) && string.Equals(path, "/queuehub", StringComparison.OrdinalIgnoreCase))
                            {
                                // Read the token out of the query string
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
        }

        public static void ConfigureAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Combined", new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser()
                     .RequireRole(StaticDetails.AdminUser, StaticDetails.DoctorUser)
                     .Build());

                options.AddPolicy("HubRestricted", policy =>
                {
                    policy.AddRequirements(new HubRequirement());
                    policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme);
                });

            });

            services.AddSingleton<IAuthorizationHandler, HubRequirement>();
        }
    }
}
