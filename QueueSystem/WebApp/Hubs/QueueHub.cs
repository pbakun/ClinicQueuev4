using Entities.Models;
using Microsoft.AspNetCore.SignalR;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.BackgroundServices.Tasks;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.ServiceLogic;
using WebApp.Utility;

namespace WebApp.Hubs
{
    public class QueueHub : Hub
    {
        public static List<HubUser> _connectedUsers = new List<HubUser>();
        public static List<HubUser> _waitingUsers = new List<HubUser>();

        private readonly IRepositoryWrapper _repo;
        private readonly IQueueService _queueService;
        private readonly IManageHubUser _hubUser;

        //hubContext for sending messages to clients from long-running processes (like Timer)
        private readonly IHubContext<QueueHub> _hubContext;

        private DoctorDisconnectedTimer _timer;

        public QueueHub(IRepositoryWrapper repo,
            IQueueService queueService,
            IHubContext<QueueHub> hubContext,
            IManageHubUser hubUser)
        {
            _repo = repo;
            _queueService = queueService;
            _hubContext = hubContext;
            _hubUser = hubUser;
        }
        public async Task RegisterDoctor(string userId, int roomNo)
        {
            var newUser = new HubUser {
                UserId = userId,
                ConnectionId = Context.ConnectionId,
                GroupName = roomNo.ToString()
            };

            var userInControl = _hubUser.GetGroupMaster(newUser.GroupName);
            if(userInControl == null || userInControl.FirstOrDefault().UserId == newUser.UserId)
            {
                await Groups.AddToGroupAsync(newUser.ConnectionId, newUser.GroupName);

                var user = _repo.User.FindByCondition(u => u.Id == userId).FirstOrDefault();

                string doctorFullName = QueueHelper.GetDoctorFullName(user);

                await Clients.Group(roomNo.ToString()).SendAsync("ReceiveDoctorFullName", userId, doctorFullName);

                var queue = _queueService.FindByUserId(userId);
                _queueService.SetQueueActive(queue);

                await Clients.Group(roomNo.ToString()).SendAsync("ReceiveQueueNo", userId, queue.QueueNoMessage);
                await Clients.Group(roomNo.ToString()).SendAsync("ReceiveAdditionalInfo", userId, queue.AdditionalMessage);
            }
            else
            {
                await Clients.Caller.SendAsync("NotifyQueueOccupied", StaticDetails.QueueOccupiedMessage);
            }
            await _hubUser.AddUserAsync(newUser);
        }

        public async Task RegisterPatientView(int roomNo)
        {   
            var newUser = new HubUser
            {
                ConnectionId = Context.ConnectionId,
                GroupName = roomNo.ToString()
            };

            await Groups.AddToGroupAsync(newUser.ConnectionId, newUser.GroupName);

            await _hubUser.AddUserAsync(newUser);
        }

        public async override Task OnDisconnectedAsync(Exception exception)
        {
            string connectionString = Context.ConnectionId;
            var groupMember = _hubUser.GetUserByConnectionId(connectionString);
            
            int memberRoomNo = Convert.ToInt32(groupMember.GroupName);

            //if group member changed roomNo reload patient view
            if (groupMember.UserId != null && !_queueService.CheckRoomSubordination(groupMember.UserId, memberRoomNo))
            {
                _queueService.SetQueueInactive(groupMember.UserId);
                await Clients.Group(groupMember.GroupName).SendAsync("Refresh", groupMember.GroupName);
            }
            else if (groupMember.UserId != null)
            {
                //if Doctor disconnected start timer and send necessery info to Patient View after
                _timer = new DoctorDisconnectedTimer(groupMember, SettingsHandler.ApplicationSettings.PatientViewNotificationAfterDoctorDisconnectedDelay);
                _timer.TimerFinished += Timer_TimerFinished;
            }

            await _hubUser.RemoveUserAsync(groupMember);
            await Groups.RemoveFromGroupAsync(connectionString, groupMember.GroupName);

            await base.OnDisconnectedAsync(exception);
        }

        public async void Timer_TimerFinished(object sender, EventArgs e)
        {
            var groupMember = sender as HubUser;
            if (_connectedUsers.Where(i => i.UserId == groupMember.UserId).FirstOrDefault() == null)
            {
                _queueService.SetQueueInactive(groupMember.UserId);
                await _hubContext.Clients.Group(groupMember.GroupName).SendAsync("Refresh", groupMember.GroupName);
            }
            _timer.Dispose();
        }

        public async Task NewQueueNo(string userId, int queueNo, int roomNo)
        {
            var hubUser = _hubUser.GetConnectedUsers().Where(u => u.UserId == userId).FirstOrDefault();
            if (hubUser != null)
            {
                WebApp.Models.Queue outputQueue = await _queueService.NewQueueNo(userId, queueNo);

                await Clients.Group(roomNo.ToString()).SendAsync("ReceiveQueueNo", userId, outputQueue.QueueNoMessage);
            }
        }

        public async Task NewAdditionalInfo(string userId, int roomNo, string message)
        {
            var hubUser = _hubUser.GetConnectedUsers().Where(u => u.UserId == userId).FirstOrDefault();
            if (hubUser != null)
            {
                WebApp.Models.Queue outputQueue = await _queueService.NewAdditionalInfo(userId, message);

                await Clients.Group(roomNo.ToString()).SendAsync("ReceiveAdditionalInfo", userId, outputQueue.AdditionalMessage);
            }
        }

    }
}
