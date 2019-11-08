using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.Models
{
    public class HubUser
    {
        [Key]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string ConnectionId { get; set; }
        public string GroupName { get; set; }

    }
}

