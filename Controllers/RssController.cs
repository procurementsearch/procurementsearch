using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;

using SearchProcurement.Models;
using SearchProcurement.Helpers;

namespace SearchProcurement.Controllers
{
    public class RssController : Controller
    {
        public IActionResult Index()
        {
            // Set up the RSS model and results
            Rss r = new Rss();

            // Load up the feed text
            searchItem[] rssItems = r.latest();
            string rss = RssHelper.makeRss(Defines.RssTitle, rssItems);

            // Output the RSS
            Response.ContentType = "application/rss+xml; charset=utf-8";
            return Content(rss);
        }



        public IActionResult Search(string kw)
        {
            kw = WebUtility.UrlDecode(kw).Trim();

            // Set up the RSS model and results
            Rss r = new Rss();
            searchItem[] rssItems = r.byKeyword(kw);
            string myTitle = Defines.RssTitle + ": " + kw;

            // Load up the feed text
            string rss = RssHelper.makeRss(myTitle, rssItems);

            // Output the RSS
            Response.ContentType = "application/rss+xml; charset=utf-8";
            return Content(rss);
        }



        public IActionResult ByAgency(int? agency, int? source)
        {
            // Set up the RSS model and results
            Rss r = new Rss();

            // Make sure we handle the deprecated case
            if( agency == null )
                agency = source;

            searchItem[] rssItems = r.byAgency(agency.GetValueOrDefault());
            string myTitle = Defines.RssTitle + ": " + SearchHelper.loadAgencyName(agency.GetValueOrDefault());

            // Load up the feed text
            string rss = RssHelper.makeRss(myTitle, rssItems);

            // Output the RSS
            Response.ContentType = "application/rss+xml; charset=utf-8";
            return Content(rss);
        }



        public IActionResult ByAgency(string agencyshortname)
        {
            // Set up the RSS model and results
            Rss r = new Rss();

            int agency = RssHelper.getAgencyByShortname(agencyshortname);

            searchItem[] rssItems = r.byAgency(agency);
            string myTitle = Defines.RssTitle + ": " + SearchHelper.loadAgencyName(agency);

            // Load up the feed text
            string rss = RssHelper.makeRss(myTitle, rssItems);

            // Output the RSS
            Response.ContentType = "application/rss+xml; charset=utf-8";
            return Content(rss);
        }


    }
}
