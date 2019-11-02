using Entities;
using Entities.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Hubs
{
    public class ManageHubUser : IManageHubUser
    {
        private readonly HubUserContext _hubUser;

        public ManageHubUser(HubUserContext hubUser)
        {
            _hubUser = hubUser;
        }

        public bool AddUser(HubUser user)
        {
            if (!_hubUser.ConnectedUsers.Contains(user))
            {
                _hubUser.ConnectedUsers.Add(user);
                return true;
            }
            else if(_hubUser.ConnectedUsers.Where(u => u.Id == user.Id).Single() != null) {
                _hubUser.ConnectedUsers.Add(user);
                return true;
            }
            else
            {
                _hubUser.WaitingUsers.Add(user);
                return false;
            }
        }

        public IEnumerable<HubUser> GetConnectedUsers()
        {
            return _hubUser.ConnectedUsers.ToList();
        }

        public HubUser GetRoomOwner(int? roomNo)
        {
            if (roomNo == null)
                return null;

            return _hubUser.ConnectedUsers.Where(u => u.GroupName == roomNo.ToString() && u.Id != null).SingleOrDefault();
        }

        public HubUser GetUserByConnectionId(string connectionId)
        {
            if (connectionId == null)
                return null;

            var user = _hubUser.ConnectedUsers.Where(u => u.ConnectionId == connectionId).SingleOrDefault();

            if (user == null)
                user = _hubUser.WaitingUsers.Where(u => u.ConnectionId == connectionId).SingleOrDefault();

            return user;
        }

        public HubUser GetUserById(string id)
        {
            if (id == null)
                return null;

            var user = _hubUser.ConnectedUsers.Where(u => u.Id == id).SingleOrDefault();

            if (user == null)
                user = _hubUser.WaitingUsers.Where(u => u.Id == id).SingleOrDefault();

            return user;
        }

        public IEnumerable<HubUser> GetWaitingUsers()
        {
            return _hubUser.WaitingUsers.ToList();
        }

        public void RemoveUser(HubUser user)
        {
            _hubUser.ConnectedUsers.Remove(user);
            _hubUser.WaitingUsers.Remove(user);
        }
    }

}
