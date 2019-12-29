using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Entities
{
    public class HubUserContext : DbContext
    {
        public HubUserContext(DbContextOptions options) : base(options)
        {

        }
        //Enitites cannot have same type. Not supported by ef core
        public DbSet<ConnectedHubUser> ConnectedUsers { get; set; }
        public DbSet<WaitingHubUser> WaitingUsers { get; set; }
    }
}
