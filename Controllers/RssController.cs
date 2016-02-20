using System;
using System.Collections.Generic;
using System.Xml;
using System.Net;
using System.Text;
using System.ServiceModel.Syndication;
using Microsoft.AspNet.Mvc;

using SearchProcurement.Models;
using SearchProcurement.Helpers;

namespace SearchProcurement.Controllers
{
    public class RssController : Controller
    {
        public IActionResult Index(string kw)
        {
            kw = WebUtility.UrlDecode(kw);

            // Set up the RSS model and results
            Rss r = new Rss();
            searchItem[] rssItems;

            // Set up the title for the feed
            string myTitle;

            if( kw == null )
            {
                rssItems = r.latest();
                myTitle = Defines.RssTitle;
            }
            else
            {
                rssItems = r.byKeyword(kw);
                myTitle = Defines.RssTitle + ": " + kw;
            }

            // Load up the feed text
            List<SyndicationItem> items = new List<SyndicationItem>();
            foreach(var item in rssItems)
            {
                SyndicationItem myItem = new SyndicationItem(
                    item.Title,
                    item.Description,
                    new Uri(Defines.RssDetailsUrl + "/" + item.Id.ToString()),
                    item.Id.ToString(),
                    item.Created);
                items.Add(myItem);

                // Update the number of accesses by RSS for the item
                AccessesHelper.updateRss(item.Id);
            }

            // Create the syndication feed
            SyndicationFeed feed = new SyndicationFeed(myTitle, Defines.RssDescription, new Uri(Defines.RssUrl));
            feed.Items = items;

            // And generate the text itself
            var rss = new Rss20FeedFormatter(feed);
            var rss_output = new StringBuilder();
            using (var writer = XmlWriter.Create(rss_output, new XmlWriterSettings { Indent = true }))
            {
                rss.WriteTo(writer);
                writer.Flush();
            }

            // Output the RSS
            Response.ContentType = "application/rss+xml";
            return Content(rss_output.ToString());
        }

    }
}
