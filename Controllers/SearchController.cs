using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Http.Features;

using SteveHavelka.SimpleFTS;
using SearchProcurement.Models;
using SearchProcurement.Helpers;

namespace SearchProcurement.Controllers
{
    public class SearchController : Controller
    {
        public IActionResult Index(string kw, int? source)
        {
            if( kw == null && source == null)
                return Redirect("/");

            // Get the search model ready
            Search s;
            if( kw == null )
                s = Search.loadBySource(source.GetValueOrDefault());
            else
                s = new Search(kw);

            // Load up some of the data
            ViewBag.searchString = s.searchString;
            ViewBag.searchCount = s.searchCount;
            ViewBag.searchUrl = s.searchUrl;

            // Update the search results views
            foreach(searchItem my_s in s.searchResults)
            {
                LogHelper.updateSearchAccesses(my_s.Id);
            }

            // And log the search terms
            LogHelper.logSearchTerms(kw, s.searchCount, HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress.ToString());

            // Load the model
            return View(s);
        }

    }
}
