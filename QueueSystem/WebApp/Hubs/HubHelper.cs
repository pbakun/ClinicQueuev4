using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entities.Models;
using Microsoft.AspNetCore.SignalR;
using WebApp.BackgroundServices.Tasks;

namespace WebApp.Hubs
{
    public class HubHelper : IQueueHub
    {
        private readonly IHubContext<QueueHub> _hubContext;

        public HubHelper(IHubContext<QueueHub> hubContext)
        {
            _hubContext = hubContext;
        }
       
        public async void InitGroupScreen(HubUser hubUser)
        {
            await _hubContext.Clients.Group(hubUser.GroupName).SendAsync("ReceiveDoctorFullName", hubUser.UserId, string.Empty);
            await _hubContext.Clients.Group(hubUser.GroupName).SendAsync("ReceiveQueueNo", hubUser.UserId, SettingsHandler.ApplicationSettings.MessageWhenNoDoctorActiveInQueue);
            await _hubContext.Clients.Group(hubUser.GroupName).SendAsync("ReceiveAdditionalInfo", hubUser.UserId, string.Empty);
        }
    }
}
