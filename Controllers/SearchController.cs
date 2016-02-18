using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

using SteveHavelka.SimpleFTS;
using SearchProcurement.Models;

namespace SearchProcurement.Controllers
{
    public class SearchController : Controller
    {
        public IActionResult Index(string kw)
        {
            if( kw == null )
                return Redirect("/");

            // Get the search model ready
            Search s = new Search(kw);

            // Load up some of the data
            ViewBag.searchString = s.searchString;
            ViewBag.searchCount = s.searchCount;
            ViewBag.searchUrl = s.searchUrl;

            // Load the model
            return View(s);
        }

    }
}
