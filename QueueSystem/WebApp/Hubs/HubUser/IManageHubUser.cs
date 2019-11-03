using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Hubs
{
    public interface IManageHubUser
    {
        Task AddUserAsync(HubUser user);
        void AddUser(HubUser user);
        Task RemoveUserAsync(HubUser user);
        void RemoveUser(HubUser user);
        IEnumerable<HubUser> GetConnectedUsers();
        IEnumerable<HubUser> GetWaitingUsers();
        HubUser GetUserById(string id);
        HubUser GetUserByConnectionId(string id);
        HubUser GetRoomOwner(int? roomNo);
    }
}
