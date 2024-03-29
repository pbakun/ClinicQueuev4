﻿using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repository;
using System;
using System.Collections.Generic;
using System.Text;
using WebApp;

namespace XUnitTests
{
    public class TStartup : Startup
    {
        public TStartup(IConfiguration configuration) : base(configuration)
        {

        }

        protected override void SetUpDatabase(IServiceCollection services, string connectionString)
        {
            services.AddDbContext<RepositoryContext>(options =>
            {
                options.UseInMemoryDatabase("HubTestDb");
                options.EnableSensitiveDataLogging();
            });
            services.ConfigureRepositoryWrapper();
        }

        protected override void EnsureDbCreated(string connectionString)
        {

        }

        protected override void SetUpHubUserDatabase(IServiceCollection services)
        {
            services.AddDbContext<HubUserContext>(options =>
            {
                options.UseInMemoryDatabase("HubUserTestDb");
            });
        }
    }
}
