using System;
using System.Net;
using Microsoft.AspNet.Mvc;

using SearchProcurement.Models;
using SearchProcurement.Helpers;

namespace SearchProcurement.Controllers
{
    public class RssController : Controller
    {
        public IActionResult Index(string kw, int? source)
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
            else if( source != null )
            {
                rssItems = r.bySource(source.GetValueOrDefault());
                myTitle = Defines.RssTitle + ": " + SearchHelper.loadSourceName(source.GetValueOrDefault());
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
