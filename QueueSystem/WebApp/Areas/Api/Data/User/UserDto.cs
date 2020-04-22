using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Areas.Api.Data.User
{
    public class UserDto
    {
        [DataType(DataType.Text)]
        public string FirstName { get; set; }
        [DataType(DataType.Text)]
        public string LastName { get; set; }
        [DataType(DataType.Text)]
        public string Username { get; set; }
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
    }
}
