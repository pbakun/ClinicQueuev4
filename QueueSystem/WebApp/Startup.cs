using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Entities;
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
using Repository;
using Repository.Initialization;
using WebApp.Areas.Identity.Pages.Account.Manage;
using WebApp.BackgroundServices.Tasks;
using WebApp.Extensions;
using WebApp.Helpers;
using WebApp.Hubs;
using WebApp.Mappings;
using WebApp.Models;
using WebApp.ServiceLogic;

namespace WebApp
{
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

            services.AddSignalR(options =>
            {
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });

            services.AddScoped<IQueueHubContext, QueueHubContext>();

            services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, ResetQueue>();
            services.AddScoped<IQueueHub, HubHelper>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IDBInitializer dbInitializer)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.ConfigureExceptionHandler();
                app.UseExceptionHandler("/Home/Error");
                //The default HSTS value is 30 days.You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }


            var options = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(options.Value);

            //create DB on startup
            EnsureDbCreated();

            dbInitializer.Initialize();
            SettingsHandler.Settings.ReadSettings();
            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
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
