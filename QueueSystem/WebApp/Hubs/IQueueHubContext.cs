using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Hubs
{
    public interface IQueueHubContext
    {
        Task SendAdditionalInfo(string groupName, string userId, string message);
        Task SendQueueNo(string groupName, string userId, string queueMessage);
    }
}
