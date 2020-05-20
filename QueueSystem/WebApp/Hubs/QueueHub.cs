using Entities.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    [Authorize(Policy = "HubRestricted")]
    public class QueueHub : Hub
    {
        private readonly IRepositoryWrapper _repo;
        private readonly IQueueService _queueService;
        private readonly IManageHubUser _hubUser;

        public QueueHub(IRepositoryWrapper repo,
            IQueueService queueService,
            IManageHubUser hubUser)
        {
            _repo = repo;
            _queueService = queueService;
            _hubUser = hubUser;
        }
        [Authorize("Combined")]
        public async Task RegisterDoctor(string roomNo)
        {
            var claimsIdentity = (ClaimsIdentity)Context.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                await Task.FromException(new UnauthorizedAccessException());

            if (claim.Value == null)
                await Task.FromException(new UnauthorizedAccessException());

            var userId = claim.Value;

            var newUser = new HubUser {
                UserId = claim.Value,
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
            Log.Information(LogMessage.HubRegisterDoctor(newUser.ConnectionId, roomNo));
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
            Log.Information(LogMessage.HubRegisterPatient(newUser.ConnectionId, roomNo));
        }
        [AllowAnonymous]
        public async override Task OnConnectedAsync()
        {
            var connectionId = this.Context.ConnectionId;
            Log.Information(LogMessage.HubNewUser(connectionId));

            try
            {
                await base.OnConnectedAsync();
            }
            catch(Exception ex)
            {
                Log.Error(LogMessage.HubNewUserError(connectionId, ex.Message));
            }
            
        }

        [AllowAnonymous]
        public async override Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;
            var groupMember = _hubUser.GetUserByConnectionId(connectionId);

            try
            {
                if (exception != null)
                {
                    Log.Warning(LogMessage.HubOnDisconnectedException(connectionId, groupMember.GroupName, exception.Message));
                }
                else
                {
                    Log.Information(LogMessage.HubOnDisconnectingUser(connectionId, groupMember.GroupName));
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
                        Log.Information(LogMessage.HubDisconnectingRoomMaster(groupMember.ConnectionId, groupMember.GroupName, groupMember.UserId));
                        InitGroupScreen(groupMember);
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Error(ex.Message);
                exception = ex;
            }
            finally
            {
                await base.OnDisconnectedAsync(exception);
            }
        }

        [Authorize("Combined")]
        public async Task QueueNoUp(string roomNo)
        {
            var claimsIdentity = (ClaimsIdentity)Context.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var userId = claim.Value;
            var hubUser = _hubUser.GetConnectedUsers().Where(u => u.UserId == userId).FirstOrDefault();
            if (hubUser != null)
            {
                WebApp.Models.Queue outputQueue = await _queueService.QueueNoUp(userId);

                await Clients.Group(roomNo).SendAsync("ReceiveQueueNo", userId, outputQueue.QueueNoMessage);
            }
        }

        [Authorize("Combined")]
        public async Task QueueNoDown(string roomNo)
        {
            var claimsIdentity = (ClaimsIdentity)Context.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var userId = claim.Value;
            var hubUser = _hubUser.GetConnectedUsers().Where(u => u.UserId == userId).FirstOrDefault();
            if (hubUser != null)
            {
                WebApp.Models.Queue outputQueue = await _queueService.QueueNoDown(userId);

                await Clients.Group(roomNo).SendAsync("ReceiveQueueNo", userId, outputQueue.QueueNoMessage);
            }
        }

        [Authorize("Combined")]
        public async Task NewQueueNo(int queueNo, string roomNo)
        {
            var claimsIdentity = (ClaimsIdentity)Context.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var userId = claim.Value;
            var hubUser = _hubUser.GetConnectedUsers().Where(u => u.UserId == userId).FirstOrDefault();
            if (hubUser != null)
            {
                WebApp.Models.Queue outputQueue = await _queueService.NewQueueNo(userId, queueNo);

                await Clients.Group(roomNo).SendAsync("ReceiveQueueNo", userId, outputQueue.QueueNoMessage);
            }
        }

        [Authorize("Combined")]
        public async Task NewAdditionalInfo(string roomNo, string message)
        {
            var claimsIdentity = (ClaimsIdentity)Context.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var userId = claim.Value;
            var hubUser = _hubUser.GetConnectedUsers().Where(u => u.UserId == userId).FirstOrDefault();
            if (hubUser != null)
            {
                WebApp.Models.Queue outputQueue = await _queueService.NewAdditionalInfo(userId, message);

                await Clients.Group(roomNo).SendAsync("ReceiveAdditionalInfo", userId, outputQueue.AdditionalMessage);
            }
        }

        [Authorize("Combined")]
        public async void InitGroupScreen(HubUser hubUser)
        {
            await Clients.Group(hubUser.GroupName).SendAsync("ReceiveDoctorFullName", hubUser.UserId, string.Empty);
            await Clients.Group(hubUser.GroupName).SendAsync("ReceiveQueueNo", hubUser.UserId, SettingsHandler.ApplicationSettings.MessageWhenNoDoctorActiveInQueue);
            await Clients.Group(hubUser.GroupName).SendAsync("ReceiveAdditionalInfo", hubUser.UserId, string.Empty);
            Log.Information(LogMessage.HubRoomInitialized(hubUser.GroupName));
        }

    }
}
