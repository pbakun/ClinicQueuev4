using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models.ViewModel
{
    public class ManageHubUserViewModel
    {
        public string GroupName { get; set; }
        public User GroupMaster { get; set; }
        public IEnumerable<HubUser> ConnectedUsers { get; set; }
        public IEnumerable<HubUser> WaitingUsers { get; set; }
    }
}
