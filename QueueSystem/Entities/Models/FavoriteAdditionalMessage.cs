using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Models
{
    public class FavoriteAdditionalMessage
    {
        [PrimaryKey]
        public string Id { get; set; }
        public string Message { get; set; }
        [ForeignKey("AspNetUsers")]
        public string userId { get; set; }
    }
}
