using Entities.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;

namespace Entities
{
    public class RepositoryContext : IdentityDbContext
    {
        public RepositoryContext(DbContextOptions<RepositoryContext> options) : base(options)
        {

        }

        public DbSet<Models.Queue> Queue { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<FavoriteAdditionalMessage> FavoriteAdditionalMessage { get; set; }
    }
}
