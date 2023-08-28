using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.Models
{
    public class Room
    {
        [Required]
        [Key]
        public string RoomNo { get; set; }
        public bool IsOccupied { get; set; }

    }
}
