using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.IO;
using System.ServiceModel.Syndication;

namespace SearchProcurement.Helpers
{

	public class RssHelper
	{

        /**
         * Load an array of search Items into the syndication feed generator
         * @param string title The title of the RSS feed
         * @param searchItems[] rssItems The search items
         * @return string The output of the feed generator
         */
        public static string makeRss(string title, searchItem[] rssItems)
        {

            List<SyndicationItem> items = new List<SyndicationItem>();
            foreach(var item in rssItems)
            {
                SyndicationItem myItem = new SyndicationItem(
                    item.Title,
                    item.Description,
                    new Uri(Defines.RssDetailsUrl + "/" + item.Id.ToString()));
                myItem.Id = item.Id.ToString();
                myItem.PublishDate = item.Created;
                items.Add(myItem);

                // Update the number of accesses by RSS for the item
                LogHelper.updateRssAccesses(item.Id);
            }

            // Create the syndication feed
            SyndicationFeed feed = new SyndicationFeed(title, Defines.RssDescription, new Uri(Defines.RssUrl));
            feed.Items = items;

            // And generate the text itself
            var rss = new Rss20FeedFormatter(feed);
            using (var stream = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true }))
                {
                    rss.WriteTo(writer);
                    writer.Flush();

                    // And we're done here
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
                
            }

        }

    }

}