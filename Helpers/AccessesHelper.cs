using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace SearchProcurement.Helpers
{
	public class AccessesHelper
	{

		/**
		 * Update the number of times the listing has appeared in search results
		 *
		 * @param id  The listing ID
		 */
		public static void updateForSearch(int id)
		{
			incrementCounter(id, "search");
		}

		/**
		 * Update the number of times the listing has been viewed in detail
		 *
		 * @param id  The listing ID
		 */
		public static void updateForDetails(int id)
		{
			incrementCounter(id, "viewed");
		}

		/**
		 * Update the number of times the listing has appeared in the RSS feed
		 *
		 * @param id  The listing ID
		 */
		public static void updateForRss(int id)
		{
			incrementCounter(id, "rss");
		}


		/**
		 * The routine that actually updates the database for the access count helpers
		 *
		 * @param id  The listing ID in question
		 * @param which  The access count field to update
		 */
		private static void incrementCounter(int id, string which)
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
					// Which one are we incrementing?
					which = "accesses_" + which;

					cmd.Connection = my_dbh;
					cmd.CommandText = "update listing set " + which + " = " + which + " + 1 where listing_id = @id";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Prepare();
					cmd.ExecuteScalar();
				}

			}

		}

	}

}