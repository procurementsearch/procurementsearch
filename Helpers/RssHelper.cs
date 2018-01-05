using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.IO;
using MySql.Data.MySqlClient;

using WilderMinds.RssSyndication;

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
            // Create the syndication feed
            var my_feed = new Feed()
            {
                Title = title,
                Description = Defines.RssDescription,
                Link = new Uri(Defines.RssUrl)
            };

            foreach(var item in rssItems)
            {
                var my_item = new Item()
                {
                    Title = Library.trimNonAscii(item.Title),
                    Body = Library.trimNonAscii(item.Description),
                    Link = new Uri(Defines.RssDetailsUrl + "/" + item.Id.ToString()),
                    Permalink = Defines.RssDetailsUrl + "/" + item.Id.ToString(),
                    PublishDate = item.Created.DateTime
                };
                my_feed.Items.Add(my_item);

                // Update the number of accesses by RSS for the item
                LogHelper.updateRssAccesses(item.Id);
            }

            // And generate the text itself
            return my_feed.Serialize();

        }



        /**
         * Look up the id of an agency by its short name, for RSS feeds
         * @param string shortname The agency short name
         * @return int The agency ID
         */
        public static int getAgencyByShortname(string shortname)
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT agency_id FROM agency WHERE agencyshortname = @name";
					cmd.Parameters.AddWithValue("@name",shortname);
					cmd.Prepare();
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

    }

}