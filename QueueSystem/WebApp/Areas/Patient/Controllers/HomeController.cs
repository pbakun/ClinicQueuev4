using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Entities.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Repository.Interfaces;
using Serilog;
using WebApp.BackgroundServices.Tasks;
using WebApp.Models;

namespace WebApp.Areas.Patient.Controllers
{
    [Area("Patient")]
    public class HomeController : Controller
    {

        private readonly IRepositoryWrapper _repo;

        public HomeController(IRepositoryWrapper repo)
        {
            _repo = repo;
        }

        public IActionResult Index()
        {
            throw new Exception("Test exception");
            var availableRooms = SettingsHandler.ApplicationSettings.AvailableRooms;

            return View(availableRooms);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
