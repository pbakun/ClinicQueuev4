using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models.ViewModel
{
    public class ManageHubUserViewModel
    {
        public string GroupName { get; set; }
        public int ConnectedUsersQuantity { get; set; }
        public int WaitingUsersQuantity { get; set; }
        public User GroupMaster { get; set; }
        
    }
}
