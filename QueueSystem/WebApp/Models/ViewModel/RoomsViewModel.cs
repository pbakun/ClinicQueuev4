﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models.ViewModel
{
    public class RoomsViewModel
    {
        public Queue Queue { get; set; }
        public string RoomNo { get; set; }
        public string UserName { get; set; }
        public int QuantityOfAssignedUsers { get; set; }
    }
}
