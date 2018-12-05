using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using SearchProcurement.Helpers;
using SearchProcurement.Models;

namespace SearchProcurement.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // The home page has a default title prefix
            ViewBag.agencyUrls = SiteNavHelper.getAgencyLogoUrls(Defines.defaultSiteId);
            ViewBag.extraTitle = "Your one-stop shop for discovering government contracting opportunities";
            return View();
        }

        /**
         * Handle user logged-in state, redirecting as necessary
         */
        public IActionResult MyAccount()
        {
            if (User.Identity.IsAuthenticated)
                return Redirect("/Agency");
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

        /**
         * Show the privacy policy page
         */
        public IActionResult PrivacyPolicy()
        {
            return View();
        }

        public IActionResult Contact()
        {
            
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
