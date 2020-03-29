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

        [HttpPost("AddFavoriteMessage")]
        public async Task<IActionResult> AddFavoriteMessage([FromBody]FavoriteMessageModel favMessage)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (favMessage.Message != null && favMessage.Message.Length > 0)
            {
                var favMsg = new Entities.Models.FavoriteAdditionalMessage
                {
                    Message = favMessage.Message,
                    UserId = claim.Value
                };

                await _repo.FavoriteAdditionalMessage.AddAsync(favMsg);
                await _repo.SaveAsync();

                return Ok();
            }
            return BadRequest();
        }

        [HttpGet("pickfavmessage")]
        [Route("api/doctor/pickvafmessage")]
        public IActionResult PickFavMessage()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var favMessages = _repo.FavoriteAdditionalMessage.FindByCondition(u => u.UserId == claim.Value).ToList();

            if (favMessages == null)
                return NotFound();

            return Ok(favMessages);
        }

        [HttpPost("PickFavMessage")]
        public async Task<IActionResult> PickFavMessagePost([FromBody]FavoriteMessageModel favMessage)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claims.Value == null)
                return NotFound();

            var roomNo = _queueService.GetRoomNoByUserId(claims.Value);

            var message = _repo.FavoriteAdditionalMessage.FindByCondition(m => m.Id == favMessage.Id).FirstOrDefault();
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

        [HttpDelete("DeleteFavMessage")]
        public async Task<IActionResult> DeleteFavMessage([FromBody]FavoriteMessageModel favMessage)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claims.Value == null)
                return StatusCode(500);

            var favMessages = _repo.FavoriteAdditionalMessage.FindByCondition(u => u.UserId == claims.Value && u.Id != favMessage.Id).ToList();
            var message = _repo.FavoriteAdditionalMessage.FindByCondition(m => m.Id == favMessage.Id).SingleOrDefault();

            if (message != null)
            {
                _repo.FavoriteAdditionalMessage.Delete(message);
                await _repo.SaveAsync();
                return Ok(favMessages);
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

    public class FavoriteMessageModel
    {
        public string Id { get; set; }
        public string Message { get; set; }
    }
}