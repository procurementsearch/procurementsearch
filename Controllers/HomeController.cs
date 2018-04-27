using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using SearchProcurement.Helpers;

namespace SearchProcurement.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // The home page has a default title prefix
            ViewBag.agencyUrls = SiteNavHelper.getAgencyLogoUrls();
            ViewBag.extraTitle = "Your one-stop shop for discovering government contracting opportunities";
            return View();
        }

        /**
         * Handle user logged-in state, redirecting as necessary
         */
        public IActionResult MyAccount()
        {
            if (User.Identity.IsAuthenticated)
                return Redirect("/Account");
            else
                return View();
        }

        /**
         * Show the coming-soon page
         */
        public IActionResult ComingSoon()
        {
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
