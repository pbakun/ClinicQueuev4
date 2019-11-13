using AutoMapper;
using Entities;
using Entities.Models;
using Microsoft.AspNetCore.SignalR;
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

        public IEnumerable<HubUser> GetGroupMaster(string groupName)
        {
            if (groupName == null)
                return null;

            var output = _hubUser.ConnectedUsers.Where(u => u.UserId != null && u.GroupName == groupName).ToList();

            if (output.Count > 0)
                return output;

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
