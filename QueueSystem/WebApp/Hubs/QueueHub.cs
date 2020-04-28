using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Repository.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApp.BackgroundServices.Tasks;
using WebApp.Helpers;
using WebApp.ServiceLogic;
using WebApp.Utility;

namespace WebApp.Hubs
{
    [Authorize(Policy = "Combined")]
    public class QueueHub : Hub
    {
        private readonly IRepositoryWrapper _repo;
        private readonly IQueueService _queueService;
        private readonly IManageHubUser _hubUser;

        private string logPrefix = StaticDetails.logPrefixHub;

        public QueueHub(IRepositoryWrapper repo,
            IQueueService queueService,
            IManageHubUser hubUser)
        {
            _repo = repo;
            _queueService = queueService;
            _hubUser = hubUser;
        }
        public async Task RegisterDoctor(string userId, string roomNo)
        {
            var newUser = new HubUser {
                UserId = userId,
                ConnectionId = Context.ConnectionId,
                GroupName = roomNo
            };

            var userInControl = _hubUser.GetGroupMaster(newUser.GroupName);
            if(userInControl == null || userInControl.FirstOrDefault().UserId == newUser.UserId)
            {
                await Groups.AddToGroupAsync(newUser.ConnectionId, newUser.GroupName);

                var user = _repo.User.FindByCondition(u => u.Id == userId).FirstOrDefault();

                string doctorFullName = QueueHelper.GetDoctorFullName(user);
                await Clients.Group(roomNo).SendAsync("ReceiveDoctorFullName", userId, doctorFullName);

                var queue = _queueService.FindByUserId(userId);
                _queueService.SetQueueActive(queue);

                await Clients.Group(roomNo).SendAsync("ReceiveQueueNo", userId, queue.QueueNoMessage);
                await Clients.Group(roomNo).SendAsync("ReceiveAdditionalInfo", userId, queue.AdditionalMessage);
            }
            else
            {
                await Clients.Caller.SendAsync("NotifyQueueOccupied", StaticDetails.QueueOccupiedMessage);
            }
            await _hubUser.AddUserAsync(newUser);

            Log.Information(String.Concat(logPrefix, "Client [ ",
                                newUser.ConnectionId, " ] registered as Doctor for room: [ ", roomNo, " ]"));
        }

        [AllowAnonymous]
        public async Task RegisterPatientView(string roomNo)
        {   
            var newUser = new HubUser
            {
                ConnectionId = Context.ConnectionId,
                GroupName = roomNo
            };

            await Groups.AddToGroupAsync(newUser.ConnectionId, newUser.GroupName);

            await _hubUser.AddUserAsync(newUser);

            Log.Information(String.Concat(logPrefix, "Client [ ", newUser.ConnectionId, " ] registered as Patient for room: [ ",
                                        roomNo, " ]"));
        }
        [AllowAnonymous]
        public async override Task OnConnectedAsync()
        {
            var connectionId = this.Context.ConnectionId;
            Log.Information(String.Concat(logPrefix, "New hub client [ ", connectionId, " ]"));
            var hubUsers = _hubUser.GetConnectedUsersCount("12");
            Log.Debug($"Hub Users Count: {hubUsers}");

            try
            {
                await base.OnConnectedAsync();
            }
            catch(Exception ex)
            {
                Log.Error(String.Concat("Connecting user [ ", connectionId, " ] :", ex.Message));
            }
            
        }

        [AllowAnonymous]
        public async override Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;
            var groupMember = _hubUser.GetUserByConnectionId(connectionId);

            if (exception != null)
            {
                Log.Warning(String.Concat(logPrefix, "Disconnecting user: [ ", connectionId, " ] for room: [ ",
                                        groupMember.GroupName, " ]: \n", exception.Message));
            }
            else
            {
                Log.Information(String.Concat(logPrefix, "Disconnecting user: [ ", connectionId, " ] from room: [ ",
                                            groupMember.GroupName, " ]"));
            }

            await _hubUser.RemoveUserAsync(groupMember);
            await Groups.RemoveFromGroupAsync(connectionId, groupMember.GroupName);

            //if group member changed roomNo exit patient view
            if (groupMember.UserId != null && !_queueService.CheckRoomSubordination(groupMember.UserId, groupMember.GroupName))
            {
                _queueService.SetQueueInactive(groupMember.UserId);
                InitGroupScreen(groupMember);
            }
            else if (groupMember.UserId != null)
            {
                await Task.Delay(TimeSpan.FromMinutes(SettingsHandler.ApplicationSettings.PatientViewNotificationAfterDoctorDisconnectedDelay));
                if (_hubUser.GetConnectedUserById(groupMember.UserId) == null)
                {
                    _queueService.SetQueueInactive(groupMember.UserId);
                    Log.Information(String.Concat(logPrefix, "Room: [ ", groupMember.GroupName, " ] master disconnected, connectionId: [ ",
                                                groupMember.ConnectionId, " ] userId: [ ", groupMember.UserId, " ]"));
                    InitGroupScreen(groupMember);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task UserDisconnect()
        {
            var connectionId = this.Context.ConnectionId;
            var user = _hubUser.GetUserByConnectionId(connectionId);

            await _hubUser.RemoveUserAsync(user);
            await Groups.RemoveFromGroupAsync(connectionId, user.GroupName);

            if (!_queueService.CheckRoomSubordination(user.UserId, user.GroupName))
            {
                _queueService.SetQueueInactive(user.UserId);
                InitGroupScreen(user);
                Log.Information(String.Concat(logPrefix, "Room: [ ", user.GroupName, " ] master disconnected, connectionId: [ ",
                                                user.ConnectionId, " ] userId: [ ", user.UserId, " ]"));
            }
            await base.OnDisconnectedAsync(null);
        }

        public async Task QueueNoUp(string userId, string roomNo)
        {
            var hubUser = _hubUser.GetConnectedUsers().Where(u => u.UserId == userId).FirstOrDefault();
            if (hubUser != null)
            {
                WebApp.Models.Queue outputQueue = await _queueService.QueueNoUp(userId);

                await Clients.Group(roomNo).SendAsync("ReceiveQueueNo", userId, outputQueue.QueueNoMessage);
            }
        }

        public async Task QueueNoDown(string userId, string roomNo)
        {
            var hubUser = _hubUser.GetConnectedUsers().Where(u => u.UserId == userId).FirstOrDefault();
            if (hubUser != null)
            {
                WebApp.Models.Queue outputQueue = await _queueService.QueueNoDown(userId);

                await Clients.Group(roomNo).SendAsync("ReceiveQueueNo", userId, outputQueue.QueueNoMessage);
            }
        }

        public async Task NewQueueNo(string userId, int queueNo, string roomNo)
        {
            var hubUser = _hubUser.GetConnectedUsers().Where(u => u.UserId == userId).FirstOrDefault();
            if (hubUser != null)
            {
                WebApp.Models.Queue outputQueue = await _queueService.NewQueueNo(userId, queueNo);

                await Clients.Group(roomNo).SendAsync("ReceiveQueueNo", userId, outputQueue.QueueNoMessage);
            }
        }

        public async Task NewAdditionalInfo(string userId, string roomNo, string message)
        {
            var hubUser = _hubUser.GetConnectedUsers().Where(u => u.UserId == userId).FirstOrDefault();
            if (hubUser != null)
            {
                WebApp.Models.Queue outputQueue = await _queueService.NewAdditionalInfo(userId, message);

                await Clients.Group(roomNo).SendAsync("ReceiveAdditionalInfo", userId, outputQueue.AdditionalMessage);
            }
        }

        public async void InitGroupScreen(HubUser hubUser)
        {
            await Clients.Group(hubUser.GroupName).SendAsync("ReceiveDoctorFullName", hubUser.UserId, string.Empty);
            await Clients.Group(hubUser.GroupName).SendAsync("ReceiveQueueNo", hubUser.UserId, SettingsHandler.ApplicationSettings.MessageWhenNoDoctorActiveInQueue);
            await Clients.Group(hubUser.GroupName).SendAsync("ReceiveAdditionalInfo", hubUser.UserId, string.Empty);
            Log.Information(String.Concat(logPrefix, "Room: [ ", hubUser.GroupName, " ] view initialized with empty data"));
        }

    }
}
