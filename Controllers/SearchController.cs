using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;

using SearchProcurement.Models;
using SearchProcurement.Helpers;

namespace SearchProcurement.Controllers
{
    public class SearchController : Controller
    {
        public IActionResult Index(string kw, int? agencyId)
        {
            if( kw == null && agencyId == null)
                return Redirect("/");

            // Get the search model ready
            Search s;
            if( kw == null ) {
                s = Search.loadByAgency(agencyId.GetValueOrDefault());
                ViewBag.extraTitle = "Showing all " + s.searchString + " opportunities";
            }
            else
            {
                s = new Search(kw);
                ViewBag.extraTitle = "Searching Opportunities: " + s.searchString;
                ViewBag.kwMatch = "?" + s.searchUrl;
            }

            // Load up some of the data
            ViewBag.searchString = s.searchString;
            ViewBag.searchCount = s.searchCount;
            ViewBag.searchUrl = s.searchUrl;
            ViewBag.searchUrlEncoded = s.searchUrl.Replace("+", "%2B");
            ViewBag.rssUrl = Defines.RssUrl;

            // Update the search results views
            foreach(searchItem my_s in s.searchResults)
            {
                LogHelper.updateSearchAccesses(my_s.Id);
            }

            // And log the search terms
            string kwlog = (kw == null) ? "source:" + s.searchString : "kw:" + kw;
            LogHelper.logSearchTerms(kwlog, s.searchCount, HttpContext.Features.Get<IHttpRequestFeature>().Headers["X-Real-IP"]);

            // Load the model
            return View(s);
        }

    }
}
