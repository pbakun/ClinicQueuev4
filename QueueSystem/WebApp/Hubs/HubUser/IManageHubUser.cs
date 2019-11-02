using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Hubs
{
    interface IManageHubUser
    {
        bool AddUser(HubUser user);
        void RemoveUser(HubUser user);
        IEnumerable<HubUser> GetConnectedUsers();
        IEnumerable<HubUser> GetWaitingUsers();
        HubUser GetUserById(string id);
        HubUser GetUserByConnectionId(string id);
        HubUser GetRoomOwner(int? roomNo);
    }
}
