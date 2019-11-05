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
            if (_hubUser.ConnectedUsers.Where(u => u.GroupName == user.GroupName && !String.IsNullOrEmpty(u.UserId)).SingleOrDefault() == null)
            {
                _hubUser.ConnectedUsers.Add(userToSave);
            }
            else if (_hubUser.ConnectedUsers.Where(u => u.UserId == user.UserId && u.GroupName == user.GroupName).SingleOrDefault() != null)
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
            if (!_hubUser.ConnectedUsers.Contains(user))
            {
                await _hubUser.ConnectedUsers.AddAsync(userToSave);
            }
            else if (_hubUser.ConnectedUsers.Where(u => u.UserId == user.UserId).Single() != null)
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

        public IEnumerable<HubUser> GetConnectedUsers()
        {
            //var list =_hubUser.ConnectedUsers.ToList();
            //return _mapper.Map<IEnumerable<HubUser>>(list);
            return _hubUser.ConnectedUsers.AsNoTracking().ToList();
        }

        public HubUser GetRoomOwner(int? roomNo)
        {
            if (roomNo == null)
                return null;

            return _hubUser.ConnectedUsers.Where(u => u.GroupName == roomNo.ToString() && u.UserId != null).AsNoTracking().SingleOrDefault();
        }

        public HubUser GetUserByConnectionId(string connectionId)
        {
            if (connectionId == null)
                return null;

            var user = _hubUser.ConnectedUsers.Where(u => u.ConnectionId == connectionId).AsNoTracking().SingleOrDefault();

            if (user != null)
                return user;

            var waitingUser = _hubUser.WaitingUsers.Where(u => u.ConnectionId == connectionId).AsNoTracking().SingleOrDefault();
            return _mapper.Map<HubUser>(waitingUser);
        }

        public HubUser GetUserById(string id)
        {
            if (id == null)
                return null;

            var user = _hubUser.ConnectedUsers.Where(u => u.UserId == id).AsNoTracking().SingleOrDefault();

            if (user != null)
                return user;

            var waitingUser = _hubUser.WaitingUsers.Where(u => u.UserId == id).AsNoTracking().SingleOrDefault();
            return _mapper.Map<HubUser>(waitingUser);
        }

        public IEnumerable<HubUser> GetWaitingUsers()
        {
            var list = _hubUser.WaitingUsers.AsNoTracking().ToList();
            return _mapper.Map<IEnumerable<HubUser>>(list);
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
            var userAsConnected = _mapper.Map<ConnectedHubUser>(user);
            var userAsWaiting = _mapper.Map<WaitingHubUser>(user);
            if (_hubUser.ConnectedUsers.Contains(user))
            {
                Remove(userAsConnected);
            }
            else if (_hubUser.WaitingUsers.Contains(userAsWaiting))
            {
                Remove(userAsWaiting);
            }
        }
        private void Remove(ConnectedHubUser user)
        {
            _hubUser.Entry(user).State = EntityState.Detached;
            _hubUser.ConnectedUsers.Remove(user);
        }
        private void Remove(WaitingHubUser user)
        {
            _hubUser.WaitingUsers.Remove(user);
        }
    }

}
