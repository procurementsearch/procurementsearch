using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace SearchProcurement.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // The home page has a default title prefix
            ViewBag.extraTitle = "Your one-stop shop for discovering government contracting opportunities in Oregon";
            return View();
        }

        public IActionResult Contact()
        {
            
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
