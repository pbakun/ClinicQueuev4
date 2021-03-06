﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Models
{
    [Table("Queue")]
    public class Queue
    {
        [Key]
        public int Id { get; set; }
        public int QueueNo { get; set; }
        public bool IsBreak { get; set; }
        public string AdditionalMessage { get; set; }
        public string OwnerInitials { get; set; }
        public string RoomNo { get; set; }
        public DateTime Timestamp { get; set; }
        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        public bool IsSpecial { get; set; }
        public bool IsActive { get; set; }

    }
}
