using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models.ViewModel
{
    public class UserViewModel
    {
        public User User { get; set; }
        public List<string> Roles { get; set; }
    }
}
