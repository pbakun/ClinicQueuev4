using Entities;
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

        public static void ConfigureSqliteContext(this IServiceCollection services, string connectionString)
        {
            services.AddEntityFrameworkSqlite().AddDbContext<RepositoryContext>(options =>
            {
                options.UseSqlite(connectionString);
            });
        }

        public static bool EnsureDbCreated(string connectionString)
        {
            DbContextOptions<RepositoryContext> options = new DbContextOptionsBuilder<RepositoryContext>()
                .UseSqlite(connectionString).Options;
            using(var db = new RepositoryContext(options))
            {
                return db.Database.EnsureCreated();
            }
        }

        public static void ConfigureRepositoryWrapper(this IServiceCollection services)
        {
            services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
        }
    }
}
