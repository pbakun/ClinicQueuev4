using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Areas.Api.Data.Identity
{
    public class LoginDto
    {
        public string Username { get; set; }
        public string Token { get; set; }
    }
}
