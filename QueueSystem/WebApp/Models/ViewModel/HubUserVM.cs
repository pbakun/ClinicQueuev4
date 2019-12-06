using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models.ViewModel
{
    public class HubUserVM
    {
        public Entities.Models.User User { get; set; }
        public Entities.Models.HubUser HubUser { get; set; }
    }
}
