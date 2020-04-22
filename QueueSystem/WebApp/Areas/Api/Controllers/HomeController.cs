using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.BackgroundServices.Tasks;

namespace WebApp.Areas.Api.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/home")]
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var availableRooms = SettingsHandler.ApplicationSettings.AvailableRooms;
            return Ok(availableRooms);
        }
    }
}