using AutoMapper;
using Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Repository;
using Repository.Initialization;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using WebApp.Areas.Identity.Pages.Account.Manage;
using WebApp.BackgroundServices.Tasks;
using WebApp.Extensions;
using WebApp.Helpers;
using WebApp.Hubs;
using WebApp.Mappings;
using WebApp.Models;
using WebApp.Models.Settings;
using WebApp.ServiceLogic;
using WebApp.ServiceLogic.Interface;
using WebApp.Utility;

namespace WebApp
{
    [System.Runtime.InteropServices.Guid("82768D70-44A2-4B79-A16C-015FC69924F1")]
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new List<CultureInfo>
                {
                    new CultureInfo("en-US"),
                    new CultureInfo("pl-PL")
                };
                options.DefaultRequestCulture = new RequestCulture("pl-PL");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));

            var authSection = Configuration.GetSection("AuthSettings");
            services.Configure<AuthSettings>(authSection);

            //add db context
            var dbSection = Configuration.GetSection("ConnectionStrings");
            var connectionString = dbSection.Get<ConnectionStrings>();
            SetUpDatabase(services, connectionString.DefaultConnection);

            services.AddLocalization();

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
                .AddDefaultUI(UIFramework.Bootstrap4)
                .AddEntityFrameworkStores<RepositoryContext>(); //would be best to add this in ServiceExtensions class in Repository library

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

            var authSettings = authSection.Get<AuthSettings>();
            var key = Encoding.ASCII.GetBytes(authSettings.Secret);
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Combined", new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser()
                     .RequireRole(StaticDetails.AdminUser, StaticDetails.DoctorUser)
                     .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme)
                     .Build());
            });

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
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                // Read the token out of the query string
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddScoped<IUserService, UserService>();

            services.AddScoped<IDBInitializer, DBInitializer>();
            services.AddAutoMapper(typeof(MappingProfile), typeof(HubUserMappingProfile));
            //all queues somehow needs to be set to inactive on app startup
            services.AddScoped<IQueueService, QueueService>();

            services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, StartupSetUp>();
            services.AddSingleton<IEmailSender, EmailSender>();

            services.AddSingleton<SettingsHandler>();

            SetUpHubUserDatabase(services);

            services.AddScoped<IManageHubUser, ManageHubUser>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc("v1", new Swashbuckle.AspNetCore.Swagger.Info
                {
                    Title = "Queue System API",
                    Version = "v1"
                });

                var security = new Dictionary<string, IEnumerable<string>>
                {
                    {"Bearer", new string[] { }},
                };


                x.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    Description = "JWT Token Auth",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });

                x.AddSecurityRequirement(security);
            });

            services.AddSignalR(options =>
            {
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });

            services.AddScoped<IQueueHubContext, QueueHubContext>();

            services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, ResetQueue>();
            services.AddScoped<IQueueHub, HubHelper>();

            services.AddCors();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
                              IHostingEnvironment env,
                              IDBInitializer dbInitializer
                              )
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseMiddleware<LoggingExceptionHandler>();
                //The default HSTS value is 30 days.You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            var swaggerOptions = new WebApp.Utility.SwaggerOptions();
            Configuration.GetSection(nameof(WebApp.Utility.SwaggerOptions)).Bind(swaggerOptions);

            app.UseSwagger();

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", swaggerOptions.Description);
            });
            //create DB on startup
            var dbSection = Configuration.GetSection("ConnectionStrings");
            var connectionString = dbSection.Get<ConnectionStrings>();
            EnsureDbCreated(connectionString.DefaultConnection);

            dbInitializer.Initialize();
            SettingsHandler.Settings.ReadSettings();
            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            var corsSettings = new CorsSettings();
            Configuration.GetSection(nameof(CorsSettings)).Bind(corsSettings);

            app.UseCors(builder =>
            {
                builder
                    .WithOrigins(corsSettings.AllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });

            app.UseAuthentication();
            app.UseSignalR(routes =>
            {
                routes.MapHub<QueueHub>("/queueHub");
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "areas",
                    template: "{area=Patient}/{controller=Home}/{action=Index}/{id?}");
            });
        }

        protected virtual void SetUpDatabase(IServiceCollection services, string connectionString)
        {
            services.ConfigureSqliteContext(connectionString);
            services.ConfigureRepositoryWrapper();
        }

        protected virtual void EnsureDbCreated(string connectionString)
        {
            var result = ServiceExtensions.EnsureDbCreated(connectionString);
            if (result)
                Log.Information("Database has been created");
        }

        protected virtual void SetUpHubUserDatabase(IServiceCollection services)
        {
            services.AddDbContext<HubUserContext>(options =>
            {
                options.UseInMemoryDatabase("HubUsers");
            });
        }
    }
}
