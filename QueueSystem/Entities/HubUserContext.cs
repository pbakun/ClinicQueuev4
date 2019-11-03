using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Entities
{
    public class HubUserContext : DbContext
    {
        public HubUserContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<HubUser> ConnectedUsers { get; set; }
        public DbSet<HubUser> WaitingUsers { get; set; }
    }
}
