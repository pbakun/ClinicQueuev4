using Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class HubUserContext : DbContext
    {
        public DbSet<HubUser> ConnectedUsers { get; set; }
        public DbSet<HubUser> WaitingUsers { get; set; }
    }
}
