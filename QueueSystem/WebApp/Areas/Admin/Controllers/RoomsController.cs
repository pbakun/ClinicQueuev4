﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Repository.Interfaces;
using WebApp.BackgroundServices.Tasks;
using WebApp.Hubs;
using WebApp.Models;
using WebApp.Models.ViewModel;
using WebApp.ServiceLogic;
using WebApp.Utility;

namespace WebApp.Areas.Admin.Controllers
{
    [Authorize(Roles = StaticDetails.AdminUser)]
    [Area("Admin")]
    public class RoomsController : Controller
    {
        private readonly IQueueService _queueService;
        private readonly IRepositoryWrapper _repo;
        private readonly ApplicationSettings _appSettings = SettingsHandler.ApplicationSettings;
        private readonly IManageHubUser _manageHubUser;
        private readonly IMapper _mapper;
        private readonly IHubContext<QueueHub> _hubContext;
        [BindProperty]
        public List<RoomsViewModel> RoomsVM { get; set; }

        [BindProperty]
        public ManageHubUserViewModel ManageHubUserVM { get; set; }

        public RoomsController(IQueueService queueService, 
            IRepositoryWrapper repo, 
            IManageHubUser manageHubUser, 
            IMapper mapper, 
            IHubContext<QueueHub> hubContext)
        {
            _queueService = queueService;
            _repo = repo;
            _manageHubUser = manageHubUser;
            _mapper = mapper;
            _hubContext = hubContext;
            RoomsVM = new List<RoomsViewModel>();
        }

        public async Task<IActionResult> Index()
        {
            var queues = _queueService.FindAll();

            var availableRooms = SettingsHandler.ApplicationSettings.AvailableRooms;

            RoomsViewModel roomVMElement = new RoomsViewModel();
            foreach (var room in availableRooms)
            {
                int usersQuantity = queues.Where(q => q.RoomNo.Equals(room)).ToList().Count();
                var queue = queues.Where(q => q.RoomNo.Equals(room)).OrderByDescending(t => t.Timestamp).FirstOrDefault();
                if (queue != null)
                {
                    var user = _repo.User.FindByCondition(u => u.Id == queue.UserId).FirstOrDefault();

                    roomVMElement.Queue = queue;
                    roomVMElement.RoomNo = room;
                    roomVMElement.UserName = user.UserName;
                    roomVMElement.QuantityOfAssignedUsers = usersQuantity;
                }
                else
                {
                    roomVMElement.RoomNo = room;
                }
                RoomsVM.Add(roomVMElement);
                roomVMElement = new RoomsViewModel();
            }

            return View(RoomsVM);
        }

        public async Task<IActionResult> Create()
        {
            return PartialView("_CreateNewRoom");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] string roomNo)
        {
            if (!_appSettings.AvailableRooms.Contains(roomNo))
            {
                if (roomNo.Length > 0)
                {
                    _appSettings.AvailableRooms.Add(roomNo);
                    SettingsHandler.Settings.WriteAllSettings(_appSettings);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string roomNo)
        {
            var queues = _queueService.FindAll();

            queues = queues.Where(q => q.RoomNo.Equals(roomNo)).OrderByDescending(q => q.Timestamp).ToList();
            if (queues != null)
            {
                foreach (var queue in queues)
                {
                    queue.Timestamp = queue.Timestamp.ToLocalTime();
                    var roomVMElement = new RoomsViewModel()
                    {
                        Queue = queue,
                        RoomNo = roomNo,
                        UserName = _repo.User.FindByCondition(u => u.Id == queue.UserId).Select(u => u.UserName).FirstOrDefault()
                    };
                    RoomsVM.Add(roomVMElement);
                }
            }
            return View(RoomsVM);
        }

        public async Task<IActionResult> Delete(string roomNo)
        {
            _appSettings.AvailableRooms.Remove(roomNo);
            SettingsHandler.Settings.WriteAllSettings(_appSettings);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ManageHubUser(string roomNo)
        {
            var userMaster = new Entities.Models.User();
            var groupMaster = _manageHubUser.GetGroupMaster(roomNo);
            if (groupMaster != null)
            {
                userMaster = _repo.User.FindByCondition(u => u.Id == groupMaster.First().UserId).FirstOrDefault();
            }

            var connectedUsers = new List<HubUserVM>();
            GenerateHubUserVM(connectedUsers, _manageHubUser.GetConnectedUsers(roomNo));

            var waitingUsers = new List<HubUserVM>();
            GenerateHubUserVM(waitingUsers, _manageHubUser.GetWaitingUsers(roomNo));

            ManageHubUserVM = new ManageHubUserViewModel()
            {
                GroupName = roomNo,
                ConnectedUsers = connectedUsers,
                WaitingUsers = waitingUsers,
                GroupMaster = _mapper.Map<WebApp.Models.User>(userMaster)
            };

            return View(ManageHubUserVM);
        }

        private void GenerateHubUserVM(List<HubUserVM> hubUsersToVM, IEnumerable<HubUser> hubUsers)
        {
            if (hubUsers != null)
            {
                foreach (var hubUser in hubUsers)
                {
                    if (hubUser.UserId != null)
                    {
                        hubUsersToVM.Add(new HubUserVM()
                        {
                            HubUser = hubUser,
                            User = _repo.User.FindByCondition(u => u.Id == hubUser.UserId).FirstOrDefault()
                        });
                    }
                    else
                    {
                        hubUsersToVM.Add(new HubUserVM()
                        {
                            HubUser = hubUser
                        });
                    }
                }
            }
        }
    }
}