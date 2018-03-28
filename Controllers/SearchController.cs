using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

using SearchProcurement.Models;
using SearchProcurement.Helpers;

namespace SearchProcurement.Controllers
{
    public class SearchController : Controller
    {
        public IActionResult Index(string kw, string agencies, string sortBy, string show)
        {
            if( kw == null )
                return Redirect("/");

            // Get the search model ready
            Search s = new Search(kw);
            RouteValueDictionary urlparams = new RouteValueDictionary();
            urlparams.Add("kw", kw);

            // Do we have a list of agency limits?
            if( agencies != null )
            {
                try
                {
                    int[] myAgencies = agencies.Split(',').Select(Int32.Parse).ToArray();
                    s.setAgencies(myAgencies);
                    urlparams.Add("agencies", agencies);
                }
                catch(FormatException e) {};  // if they gave us a bad string, we really don't care
            }

            // do we have a sortBy string?
            if( sortBy == SortBy.Relevance || sortBy == SortBy.BidsDueFirst || sortBy == SortBy.BidsDueLast )
            {
                s.setSortBy(sortBy);
                urlparams.Add("sortBy", sortBy);
            }

            // do we have a show open/closed string?
            if( show == Show.Open || show == Show.Closed )
            {
                s.setShow(show);
                urlparams.Add("show", show);
            }

            // And finally, run the search
            s.run();

            ViewBag.urlparams = urlparams;
            ViewBag.extraTitle = "Searching Opportunities: " + s.searchString;
            ViewBag.kwMatch = "?" + s.searchUrl;

            // Load up some of the data
            ViewBag.searchString = s.searchString;
            ViewBag.searchCount = s.searchCount;
            ViewBag.searchUrl = s.searchUrl;
            ViewBag.searchUrlEncoded = s.searchUrlEncoded;
            ViewBag.rssUrl = Defines.RssUrl;

            // Update the search results views
            foreach(searchItem my_s in s.searchResults)
            {
                LogHelper.updateSearchAccesses(my_s.Id);
            }

            // And log the search terms
            string kwlog = "kw:" + kw;
            LogHelper.logSearchTerms(kwlog, s.searchCount, HttpContext.Features.Get<IHttpRequestFeature>().Headers["X-Real-IP"]);

            // Load the model
            return View(s);
        }


        public IActionResult Agency(int agency)
        {
            if( agency == 0 )
                return Redirect("/");

            // Get the search model ready
            Search s = Search.loadByAgency(agency);
            ViewBag.extraTitle = "Showing all " + s.searchString + " opportunities";
            ViewBag.agencyHeader = SearchHelper.loadAgencyHeader(agency);

            // Load up some of the data
            ViewBag.searchString = s.searchString;
            ViewBag.searchCount = s.searchCount;
            ViewBag.searchUrl = s.searchUrl;
            ViewBag.searchUrlEncoded = s.searchUrlEncoded;
            ViewBag.rssUrl = Defines.RssUrl;

            // Update the search results views
            foreach(searchItem my_s in s.searchResults)
            {
                LogHelper.updateSearchAccesses(my_s.Id);
            }

            // And log the search terms
            string kwlog = "source:" + s.searchString;
            LogHelper.logSearchTerms(kwlog, s.searchCount, HttpContext.Features.Get<IHttpRequestFeature>().Headers["X-Real-IP"]);

            // Load the model
            return View(s);
        }

    }
}
