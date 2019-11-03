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

        public void AddUser(HubUser user)
        {
            if (_hubUser.ConnectedUsers.Where(u => u.GroupName == user.GroupName && !String.IsNullOrEmpty(u.UserId)).SingleOrDefault() == null)
            {
                _hubUser.ConnectedUsers.Add(user);
            }
            else if(_hubUser.ConnectedUsers.Where(u => u.UserId == user.UserId && u.GroupName == user.GroupName).SingleOrDefault() != null)
            {
                _hubUser.ConnectedUsers.Add(user);
            }
            else
            {
                _hubUser.WaitingUsers.Add(user);
            }
            _hubUser.SaveChanges();
        }

        public async Task AddUserAsync(HubUser user)
        {
            if (!_hubUser.ConnectedUsers.Contains(user))
            {
                await _hubUser.ConnectedUsers.AddAsync(user);
            }
            else if(_hubUser.ConnectedUsers.Where(u => u.UserId == user.UserId).Single() != null) {
                await _hubUser.ConnectedUsers.AddAsync(user);
            }
            else
            {
                await _hubUser.WaitingUsers.AddAsync(user);
            }
            await _hubUser.SaveChangesAsync();
            
        }

        public IEnumerable<HubUser> GetConnectedUsers()
        {
            return _hubUser.ConnectedUsers.ToList();
        }

        public HubUser GetRoomOwner(int? roomNo)
        {
            if (roomNo == null)
                return null;

            return _hubUser.ConnectedUsers.Where(u => u.GroupName == roomNo.ToString() && u.UserId != null).SingleOrDefault();
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

            var user = _hubUser.ConnectedUsers.Where(u => u.UserId == id).SingleOrDefault();

            if (user == null)
                user = _hubUser.WaitingUsers.Where(u => u.UserId == id).SingleOrDefault();

            return user;
        }

        public IEnumerable<HubUser> GetWaitingUsers()
        {
            return _hubUser.WaitingUsers.ToList();
        }

        public void RemoveUser(HubUser user)
        {
            Remove(user);
            _hubUser.SaveChanges();
        }

        public async Task RemoveUserAsync(HubUser user)
        {
            Remove(user);
            await _hubUser.SaveChangesAsync();
        }

        private void Remove(HubUser user)
        {
            _hubUser.ConnectedUsers.Remove(user);
            _hubUser.WaitingUsers.Remove(user);
            _hubUser.SaveChanges();
        }

    }

}
