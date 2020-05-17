using AutoMapper;
using Entities;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Hubs
{
    public class ManageHubUser : IManageHubUser
    {
        private readonly HubUserContext _hubUser;
        private readonly IMapper _mapper;

        public ManageHubUser(HubUserContext hubUser, IMapper mapper)
        {
            _hubUser = hubUser;
            _mapper = mapper;
        }

        public void AddUser(HubUser user)
        {
            var userToSave = _mapper.Map<ConnectedHubUser>(user);
            if (_hubUser.ConnectedUsers.Where(u => u.GroupName == user.GroupName && !String.IsNullOrEmpty(u.UserId)).FirstOrDefault() == null)
            {
                _hubUser.ConnectedUsers.Add(userToSave);
            }
            else if (_hubUser.ConnectedUsers.Where(u => u.UserId == user.UserId && u.GroupName == user.GroupName).FirstOrDefault() != null)
            {
                _hubUser.ConnectedUsers.Add(userToSave);
            }
            else if(String.IsNullOrEmpty(user.UserId) && !String.IsNullOrEmpty(user.GroupName))
            {
                _hubUser.ConnectedUsers.Add(userToSave);
            }
            else
            {
                var waitingUserToSave = _mapper.Map<WaitingHubUser>(user);
                _hubUser.WaitingUsers.Add(waitingUserToSave);
            }
            _hubUser.SaveChanges();
        }

        public async Task AddUserAsync(HubUser user)
        {
            var userToSave = _mapper.Map<ConnectedHubUser>(user);
            if (_hubUser.ConnectedUsers.Where(u => u.GroupName == user.GroupName && !String.IsNullOrEmpty(u.UserId)).FirstOrDefault() == null)
            {
                await _hubUser.ConnectedUsers.AddAsync(userToSave);
            }
            else if (_hubUser.ConnectedUsers.Where(u => u.UserId == user.UserId && u.GroupName == user.GroupName).FirstOrDefault() != null)
            {
                await _hubUser.ConnectedUsers.AddAsync(userToSave);
            }
            else if (String.IsNullOrEmpty(user.UserId) && !String.IsNullOrEmpty(user.GroupName))
            {
                await _hubUser.ConnectedUsers.AddAsync(userToSave);
            }
            else
            {
                var waitingUserToSave = _mapper.Map<WaitingHubUser>(user);
                await _hubUser.WaitingUsers.AddAsync(waitingUserToSave);
            }
            await _hubUser.SaveChangesAsync();

        }

        public IEnumerable<HubUser> GetConnectedUserById(string userId)
        {
            if (String.IsNullOrEmpty(userId))
                return null;

            var user = _hubUser.ConnectedUsers.Where(u => u.UserId == userId).AsNoTracking().AsEnumerable();
            if (user.Count() > 0)
                return user;

            return null;
        }

        public IEnumerable<HubUser> GetConnectedUsers()
        {
            return _hubUser.ConnectedUsers.AsNoTracking().ToList();
        }

        public IEnumerable<HubUser> GetConnectedUsers(string groupName)
        {
            if (String.IsNullOrEmpty(groupName))
                return null;

            var user = _hubUser.ConnectedUsers.Where(u => u.GroupName == groupName).AsNoTracking().AsEnumerable();
            if (user.Count() > 0)
                return user;

            return null;
        }

        public int GetConnectedUsersCount(string groupName)
        {
            var users = GetConnectedUsers(groupName);

            if (users != null)
                return users.Count();

            return 0;
        }

        public IEnumerable<HubUser> GetGroupMaster(string groupName)
        {
            if (groupName == null)
                return null;

            var output = _hubUser.ConnectedUsers.Where(u => !String.IsNullOrEmpty(u.UserId) && u.GroupName == groupName).ToList();

            if (output.Count > 0)
                return output;

            return null;
        }

        public IEnumerable<HubUser> GetGroupUsers(string groupName)
        {
            if (String.IsNullOrEmpty(groupName))
                return null;

            var users = new List<HubUser>();
            var connectedUsers = GetConnectedUsers(groupName);
            var waitingUsers = GetWaitingUsers(groupName);
            if(connectedUsers != null && connectedUsers.Count() > 0)
            {
                foreach (var user in connectedUsers)
                    users.Add(user);
            }
            if (waitingUsers != null && waitingUsers.Count() > 0)
            {
                foreach (var user in waitingUsers)
                    users.Add(user);
            }

            if (users.Count() > 0)
                return users.AsEnumerable();

            return null;
        }

        public HubUser GetUserByConnectionId(string connectionId)
        {
            if (String.IsNullOrEmpty(connectionId))
                return null;

            var user = _hubUser.ConnectedUsers.Where(u => u.ConnectionId == connectionId).AsNoTracking().SingleOrDefault();

            if (user != null)
                return user;

            var waitingUser = _hubUser.WaitingUsers.Where(u => u.ConnectionId == connectionId).AsNoTracking().SingleOrDefault();
            return _mapper.Map<HubUser>(waitingUser);
        }

        public IEnumerable<HubUser> GetUserByUserId(string userId)
        {
            if (String.IsNullOrEmpty(userId))
                return null;

            var user = _hubUser.ConnectedUsers.Where(u => u.UserId == userId).AsNoTracking().AsEnumerable();
            if (user.Count()>0)
                return user;

            var waitingUser = _hubUser.WaitingUsers.Where(u => u.UserId == userId).AsNoTracking().AsEnumerable();
            if (waitingUser.Count() > 0)
                return waitingUser;
            else return null;
        }

        public IEnumerable<HubUser> GetWaitingUsers()
        {
            return _hubUser.WaitingUsers.AsNoTracking().ToList();
        }

        public IEnumerable<HubUser> GetWaitingUsers(string groupName)
        {
            if (String.IsNullOrEmpty(groupName))
                return null;

            var user = _hubUser.WaitingUsers.Where(u => u.GroupName == groupName).AsNoTracking().AsEnumerable();
            if (user.Count() > 0)
                return user;

            return null;
        }

        public int GetWaitingUsersCount(string groupName)
        {
            var users = GetWaitingUsers(groupName);

            if (users != null)
                return users.Count();

            return 0;
        }

        public void RemoveUser(HubUser user)
        {
            ChooseTypeToRemove(user);
            _hubUser.SaveChanges();
        }

        public async Task RemoveUserAsync(HubUser user)
        {
            ChooseTypeToRemove(user);
            await _hubUser.SaveChangesAsync();
        }

        private void ChooseTypeToRemove(HubUser user)
        {
            var userAsConnected = _hubUser.ConnectedUsers.Where(u => u.Id == user.Id).SingleOrDefault();
            if (userAsConnected != null)
            {
                Remove(userAsConnected);
                return;
            }
            var userAsWaiting = _hubUser.WaitingUsers.Where(u => u.Id == user.Id).SingleOrDefault();
            if (userAsWaiting != null)
            {
                Remove(userAsWaiting);
                return;
            }
        }
        private void Remove(ConnectedHubUser user)
        {
            _hubUser.ConnectedUsers.Remove(user);
        }
        private void Remove(WaitingHubUser user)
        {
            _hubUser.WaitingUsers.Remove(user);
        }
    }

}
