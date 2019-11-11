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
        IEnumerable<HubUser> GetUserByUserId(string userId);
        IEnumerable<HubUser> GetGroupMaster(string groupName);
        HubUser GetUserByConnectionId(string connectionId);
        HubUser GetRoomOwner(int? roomNo);
    }
}
