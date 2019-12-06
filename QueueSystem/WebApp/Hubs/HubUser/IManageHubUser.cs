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
        IEnumerable<HubUser> GetConnectedUsers(string groupName);
        IEnumerable<HubUser> GetWaitingUsers(string groupName);
        IEnumerable<HubUser> GetUserByUserId(string userId);
        IEnumerable<HubUser> GetGroupMaster(string groupName);
        HubUser GetUserByConnectionId(string connectionId);
        IEnumerable<HubUser> GetConnectedUserById(string userId);
        IEnumerable<HubUser> GetGroupUsers(string groupName);
        int GetConnectedUsersCount(string groupName);
        int GetWaitingUsersCount(string groupName);
    }
}
