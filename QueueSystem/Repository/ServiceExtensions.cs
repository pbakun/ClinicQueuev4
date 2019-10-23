﻿using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repository
{
    public static class ServiceExtensions
    {
        public static void ConfigureSqliteContext(this IServiceCollection services)
        {
            services.AddEntityFrameworkSqlite().AddDbContext<RepositoryContext>(options =>
            {
                options.UseSqlite("Filename=AppData/AppData.db3");
            });
        }

        public static void ConfigureRepositoryWrapper(this IServiceCollection services)
        {
            services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
        }
    }
}
