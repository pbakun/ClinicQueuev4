using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models
{
    public class ModifyUserRoles
    {
        public string UserId { get; set; }
        public string[] Roles { get; set; }
        public List<string> AvailableRoles { get; set; }
    }
}
