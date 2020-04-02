using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DocumentFormat.OpenXml.Bibliography;
using Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Repository;
using Repository.Initialization;
using Swashbuckle.AspNetCore.Swagger;
using WebApp.Areas.Identity.Pages.Account.Manage;
using WebApp.BackgroundServices.Tasks;
using WebApp.Extensions;
using WebApp.Helpers;
using WebApp.Hubs;
using WebApp.Mappings;
using WebApp.Models;
using WebApp.ServiceLogic;
using WebApp.ServiceLogic.Interface;

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
            SetUpDatabase(services);

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

            //Sets 401 as a response when user unauthorized
            //services.ConfigureApplicationCookie(options => {
            //    options.Cookie.SameSite = SameSiteMode.None;
            //    options.Cookie.HttpOnly = false;
            //    options.Events.OnRedirectToLogin = context =>
            //    {
            //        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            //        return Task.CompletedTask;
            //    };
            //});
            var authSettings = authSection.Get<AuthSettings>();
            var key = Encoding.ASCII.GetBytes(authSettings.Secret);
            services.AddAuthentication()
                .AddCookie(cfg => cfg.SlidingExpiration = true)
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
                              IDBInitializer dbInitializer,
                              IAntiforgery antiforgery
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
            Configuration.GetSection(nameof(SwaggerOptions)).Bind(swaggerOptions);

            //app.UseSwagger(options =>
            //{
            //    options.RouteTemplate = swaggerOptions.JsonRoute;
            //});
            app.UseSwagger();

            app.UseSwaggerUI(options =>
            {
                //options.SwaggerEndpoint(swaggerOptions.UiEndpoint, swaggerOptions.Description);
                options.SwaggerEndpoint("/swagger/v1/swagger.json", swaggerOptions.Description);
            });
            //create DB on startup
            EnsureDbCreated();

            dbInitializer.Initialize();
            SettingsHandler.Settings.ReadSettings();
            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseCors(builder =>
            {
                builder
                    .WithOrigins(new string[] { "http://localhost:3000", "http://clinic-queue.herokuapp.com/" })
                    //.AllowAnyOrigin()
                    //.SetIsOriginAllowed(_ => true)
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

        protected virtual void SetUpDatabase(IServiceCollection services)
        {
            services.ConfigureSqliteContext();
            services.ConfigureRepositoryWrapper();
        }

        protected virtual void EnsureDbCreated()
        {
            ServiceExtensions.EnsureDbCreated();
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
