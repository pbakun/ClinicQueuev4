using AutoMapper;
using Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repository;
using Repository.Initialization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using WebApp.BackgroundServices.Tasks;
using WebApp.Extensions;
using WebApp.Helpers;
using WebApp.Hubs;
using WebApp.Mappings;
using WebApp.Models;
using WebApp.Models.Settings;
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

            //add db context
            var dbSection = Configuration.GetSection("ConnectionStrings");
            var connectionString = dbSection.Get<ConnectionStrings>();
            SetUpDatabase(services, connectionString.DefaultConnection);

            services.AddLocalization();

            services.ConfigureIdentity();

            var authSection = Configuration.GetSection("AuthSettings");
            services.Configure<AuthSettings>(authSection);
            var authSettings = authSection.Get<AuthSettings>();

            services.ConfigureAuthMethods(authSettings.Secret);

            services.ConfigureAuthorization();

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

            services.AddSwagger();

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

            var swaggerOptions = new Utility.SwaggerOptions();
            Configuration.GetSection(nameof(Utility.SwaggerOptions)).Bind(swaggerOptions);
            app.UseCustomSwagger(swaggerOptions.Description);

            //create DB on startup
            var dbSection = Configuration.GetSection("ConnectionStrings");
            var connectionString = dbSection.Get<ConnectionStrings>();
            EnsureDbCreated(connectionString.DefaultConnection);

            dbInitializer.Initialize();
            SettingsHandler.Settings.ReadSettings();
            app.UseHttpsRedirection();
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
            services.AddDbContext<HubUserContext>(options => options.UseInMemoryDatabase("HubUsers"));
        }
    }
}
