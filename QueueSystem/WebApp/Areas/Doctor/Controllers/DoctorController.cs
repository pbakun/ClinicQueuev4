using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Repository.Interfaces;
using Serilog;
using WebApp.Areas.Identity.Pages.Account.Manage;
using WebApp.Hubs;
using WebApp.Models;
using WebApp.Models.ViewModel;
using WebApp.ServiceLogic;
using WebApp.Utility;

namespace WebApp.Areas.Doctor.Controllers
{
    [Authorize(Roles = StaticDetails.AdminUser + "," + StaticDetails.DoctorUser)]
    [Area("Doctor")]
    public class DoctorController : Controller
    {

        private IRepositoryWrapper _repo;
        private readonly IMapper _mapper;
        private readonly IQueueService _queueService;
        private readonly IQueueHubContext _queueHubContext;
        private readonly CustomUserManager _userManager;

        [BindProperty]
        public DoctorViewModel DoctorVM { get; set; }

        public DoctorController(IRepositoryWrapper repo,
                                IMapper mapper,
                                IQueueService queueService,
                                IQueueHubContext queueHubContext,
                                CustomUserManager userManager)
        {
            _repo = repo;
            _mapper = mapper;
            _queueService = queueService;
            _queueHubContext = queueHubContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var user = _repo.User.FindByCondition(u => u.Id == claim.Value).FirstOrDefault();
            if (user == null)
                return NotFound();

            var queue = _queueService.FindByUserId(user.Id);

            DoctorVM = new DoctorViewModel()
            {
                Queue = queue
            };

            return View(DoctorVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Next()
        {
            
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)this.User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

                var queue = _repo.Queue.FindByCondition(u => u.UserId == claim.Value).FirstOrDefault();
                queue.QueueNo++;
                _repo.Queue.Update(queue);
                await _repo.SaveAsync();

                var outputQueue = _mapper.Map<Queue>(queue);

                return View("Index", outputQueue);
            }
            
            return NotFound();
            
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Doctor/Doctor/NewRoomNo")]
        public async Task<IActionResult> NewRoomNo(DoctorViewModel VM)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var roomNo = VM.Queue.RoomNo;
            var user = _repo.User.FindByCondition(u => u.Id == claim.Value).FirstOrDefault();
            
            var queue = await _queueService.ChangeUserRoomNo(user.Id, roomNo);
            await _userManager.SetRoomNoAsync(user.Id, roomNo);
            DoctorVM.Queue = queue;

            return View("Index", DoctorVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFavoriteMessage([FromBody]string message)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if(message != null && message.Length > 0)
            {
                var favMsg = new Entities.Models.FavoriteAdditionalMessage
                {
                    Message = message,
                    UserId = claim.Value
                };

                await _repo.FavoriteAdditionalMessage.AddAsync(favMsg);
                await _repo.SaveAsync();

                return Ok();
            }
            return BadRequest();
        }

        [HttpGet]
        public IActionResult PickFavMessage(string userId)
        {
            var favMessages = _repo.FavoriteAdditionalMessage.FindByCondition(u => u.UserId == userId).ToList();
            
            return PartialView("_ShowFavMessage", favMessages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PickFavMessagePost([FromBody]string messageId)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claims.Value == null)
                return NotFound();

            var roomNo = _queueService.GetRoomNoByUserId(claims.Value);

            var message = _repo.FavoriteAdditionalMessage.FindByCondition(m => m.Id == messageId).FirstOrDefault();
            if(message != null)
            {
                try
                {
                    await _queueService.NewAdditionalInfo(claims.Value, message.Message);
                    await _queueHubContext.SendAdditionalInfo(roomNo, claims.Value, message.Message);
                }
                catch(Exception ex)
                {
                    Log.Error(ex.Message);
                }
                return Ok(message.Message);
            }
            return BadRequest();
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFavMessage([FromBody]string messageId)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claims.Value == null)
                return StatusCode(500);

            var message = _repo.FavoriteAdditionalMessage.FindByCondition(m => m.Id == messageId).FirstOrDefault();
            if(message != null)
            {
                _repo.FavoriteAdditionalMessage.Delete(message);
                await _repo.SaveAsync();
                return Ok();
            }
            return BadRequest();
        }
    }
}