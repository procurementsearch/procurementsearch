using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;

using SearchProcurement.Models;
using SearchProcurement.Helpers;

namespace SearchProcurement.Controllers
{
    public class RssController : Controller
    {
        public IActionResult Index(string kw, int? agency)
        {
            kw = WebUtility.UrlDecode(kw);

            // Set up the RSS model and results
            Rss r = new Rss();
            searchItem[] rssItems;

            // Set up the title for the feed
            string myTitle;

            if( kw != null )
            {
                rssItems = r.byKeyword(kw);
                myTitle = Defines.RssTitle + ": " + kw;
            }
            else if( agency != null )
            {
                rssItems = r.byAgency(agency.GetValueOrDefault());
                myTitle = Defines.RssTitle + ": " + SearchHelper.loadAgencyName(agency.GetValueOrDefault());
            }
            else
            {
                rssItems = r.latest();
                myTitle = Defines.RssTitle;
            }

            // Load up the feed text
            string rss = RssHelper.makeRss(myTitle, rssItems);

            // Output the RSS
            Response.ContentType = "application/rss+xml; charset=utf-8";
            return Content(rss);
        }

    }
}
