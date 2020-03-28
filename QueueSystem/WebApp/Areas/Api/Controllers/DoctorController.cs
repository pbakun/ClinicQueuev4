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
using WebApp.BackgroundServices.Tasks;
using WebApp.Hubs;
using WebApp.Models;
using WebApp.Models.ViewModel;
using WebApp.ServiceLogic;
using WebApp.Utility;
namespace WebApp.Areas.Api.Controllers
{
    [Area("Api")]
    [Route("api/doctor")]
    [Authorize(Roles = StaticDetails.AdminUser + "," + StaticDetails.DoctorUser)]
    public class DoctorController : Controller
    {

        private IRepositoryWrapper _repo;
        private readonly IMapper _mapper;
        private readonly IQueueService _queueService;
        private readonly IQueueHubContext _queueHubContext;
        private readonly CustomUserManager _userManager;

        [BindProperty]
        public DoctorResponse DoctorVM { get; set; }

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

        [HttpGet]
        public IActionResult Get()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var user = _repo.User.FindByCondition(u => u.Id == claim.Value).SingleOrDefault();
            if (user == null)
                return NotFound();

            var queue = _queueService.FindByUserId(user.Id);

            DoctorVM = new DoctorResponse()
            {
                UserId = queue.UserId,
                QueueNoMessage = queue.QueueNoMessage,
                AdditionalInfo = queue.AdditionalMessage,
                RoomNo = queue.RoomNo
            };

            return Ok(DoctorVM);
        }


        [HttpPut("newRoomNo")]
        public async Task<IActionResult> NewRoomNo([FromBody]NewRoomNoInput input)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var user = _repo.User.FindByCondition(u => u.Id == claim.Value).SingleOrDefault();

            var queue = await _queueService.ChangeUserRoomNo(user.Id, input.NewRoomNo);
            await _userManager.SetRoomNoAsync(user.Id, input.NewRoomNo);

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFavoriteMessage([FromBody]string message)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (message != null && message.Length > 0)
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

        [HttpGet("/pickfavmessage")]
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
            if (message != null)
            {
                try
                {
                    await _queueService.NewAdditionalInfo(claims.Value, message.Message);
                    await _queueHubContext.SendAdditionalInfo(roomNo, claims.Value, message.Message);
                }
                catch (Exception ex)
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
            if (message != null)
            {
                _repo.FavoriteAdditionalMessage.Delete(message);
                await _repo.SaveAsync();
                return Ok();
            }
            return BadRequest();
        }
    }

    public class DoctorResponse
    {
        public string UserId { get; set; }
        public string QueueNoMessage { get; set; }
        public string AdditionalInfo { get; set; }
        public string RoomNo { get; set; }
        public List<string> AvailableRoomNo { get; set; }
        public DoctorResponse()
        {
            AvailableRoomNo = SettingsHandler.ApplicationSettings.AvailableRooms;
        }
    }

    public class NewRoomNoInput
    {
        public string NewRoomNo { get; set; }
    }
}