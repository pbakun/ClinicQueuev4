﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Interfaces;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.Models.ViewModel;
using WebApp.ServiceLogic;
using WebApp.Utility;

namespace WebApp.Areas.Patient.Controllers
{
    [Area("Patient")]
    public class PatientController : Controller
    {

        private readonly IRepositoryWrapper _repo;
        private readonly IMapper _mapper;
        private readonly IQueueService _queueService;

        public PatientViewModel PatientVM { get; set; }

        public PatientController(IRepositoryWrapper repo, IMapper mapper, IQueueService queueService)
        {
            _repo = repo;
            _mapper = mapper;
            _queueService = queueService;
        }

        [HttpGet("patient/{roomNo}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Index(string roomNo)
        {
            var queue = _queueService.FindByRoomNo(roomNo);
            PatientVM = new PatientViewModel();
            
            if (queue == null)
            {
                queue = new Queue();
                queue.RoomNo = roomNo;
                PatientVM.DoctorFullName = string.Empty;
            }
            else
            {
                var user = _repo.User.FindByCondition(u => u.Id == queue.UserId).FirstOrDefault();
                PatientVM.DoctorFullName = QueueHelper.GetDoctorFullName(user);
            }
            PatientVM.QueueNoMessage = queue.QueueNoMessage;
            PatientVM.QueueAdditionalInfo = queue.AdditionalMessage;

            return View(PatientVM);

        }

        [HttpGet("api/patient/{roomNo}")]
        public IActionResult IndexApi(string roomNo)
        {
            var queue = _queueService.FindByRoomNo(roomNo);
            PatientVM = new PatientViewModel();

            if (queue == null)
            {
                queue = new Queue();
                queue.RoomNo = roomNo;
                PatientVM.DoctorFullName = string.Empty;
            }
            else
            {
                var user = _repo.User.FindByCondition(u => u.Id == queue.UserId).FirstOrDefault();
                PatientVM.DoctorFullName = QueueHelper.GetDoctorFullName(user);
            }
            PatientVM.QueueNoMessage = queue.QueueNoMessage;
            PatientVM.RoomNo = roomNo;
            PatientVM.QueueAdditionalInfo = queue.AdditionalMessage;

            return Ok(PatientVM);
        }
    }
}