using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Hubs
{
    public class QueueHubContext : IQueueHubContext
    {
        private readonly IHubContext<QueueHub> _hubContext;

        public QueueHubContext(IHubContext<QueueHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendAdditionalInfo(string groupName, string userId, string message)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveAdditionalInfo", userId, message);
        }

        public async Task SendQueueNo(string groupName, string userId, string queueNoMessage)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveQueueNo", userId, queueNoMessage);
        }
    }
}
