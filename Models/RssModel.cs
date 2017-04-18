using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

using SteveHavelka.SphinxFTS;
using SearchProcurement.Helpers;

namespace SearchProcurement.Models
{
	public class Rss
	{
        /**
         * Return the latest procurement listings
         * @return An array of search items
         */
		public searchItem[] latest()
		{
			// Set up the database connection, there has to be a better way!
			using(MySql.Data.MySqlClient.MySqlConnection my_dbh = new MySqlConnection())
			{
				// Open the DB connection
				my_dbh.ConnectionString = Defines.myConnectionString;
				my_dbh.Open();
	
				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "select listing_id from (select * from listing where status = 'open' order by created desc limit " + Defines.RssLimit + ") tmp order by created asc";
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
						// The listing IDs
						List<int> ids = new List<int>();
						while(r.Read())
						{
							ids.Add(r.GetInt32(0));
						}

						// And filter by source and build the result array
						List<searchItem> rssItems = new List<searchItem>();
						foreach(int id in SearchHelper.filter(ids.ToArray(), Defines.mySources))
						{
							rssItems.Add(SearchHelper.loadItem(id));
						}

						// And we're done!
						return rssItems.ToArray();

					}
				}
			}
		}


        /**
         * Return the latest procurement listings for a source
         * @return An array of search items
         */
		public searchItem[] bySource(int source)
		{
			// Set up the database connection, there has to be a better way!
			using(MySql.Data.MySqlClient.MySqlConnection my_dbh = new MySqlConnection())
			{
				// Open the DB connection
				my_dbh.ConnectionString = Defines.myConnectionString;
				my_dbh.Open();
	
				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "select listing_id from (select * from listing where status = 'open' and source_id = @id order by created desc limit 40) tmp order by created asc";
					cmd.Parameters.AddWithValue("@id", source);
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{

						// Create the list of rss items
						List<searchItem> rssItems = new List<searchItem>();
						while(r.Read())
						{
							rssItems.Add(SearchHelper.loadItem(r.GetInt32(0)));
						}

						// And we're done!
						return rssItems.ToArray();

					}
				}
			}
		}



        /**
         * Return the latest procurement listings for keywords
         * @param string kw The keywords
         * @return An array of search items
         */
		public searchItem[] byKeyword(string kw)
		{
			// Instantiate the search object
			SphinxFTS s = new SphinxFTS();
			s.kwTable = Defines.mySphinxTable;
			s.setWords(kw);

			// Run the search
			int[] searchIds = s.search();

			// Load the data from the search IDs
			List<searchItem> items = new List<searchItem>();
			foreach(int id in searchIds)
			{
				// filter the item based on site-specific limits
				searchItem i = SearchHelper.loadItem(id);

				// Yep, it's in there!
				items.Add(i);
			}

			// save the search results
			return items.ToArray();
		}

	}
}