using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Hubs
{
    public interface IQueueHub
    {
        async void InitGroupScreen(HubUser hubUser);

    }
}
