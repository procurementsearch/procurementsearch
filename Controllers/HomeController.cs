using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SearchProcurement.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            Console.WriteLine("User is " + User.Identity);
            // The home page has a default title prefix
            ViewBag.extraTitle = "Your one-stop shop for discovering government contracting opportunities";
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
