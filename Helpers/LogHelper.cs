using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace SearchProcurement.Helpers
{
	public class LogHelper
	{
        /**
         * Log the search terms
         *
         * @param kw  The search terms the user submitted
         */
        public static void logSearchTerms(string kw, int count, string ip_addr)
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
					cmd.CommandText = "insert into searchlog (search, results, created, ip_addr) values (@kw, @count, now(), @ip_addr)";
					cmd.Parameters.AddWithValue("@kw", kw);
					cmd.Parameters.AddWithValue("@count", count);
					cmd.Parameters.AddWithValue("@ip_addr", ip_addr ?? ""); // sometimes this comes through as null
					cmd.Prepare();
					cmd.ExecuteScalar();
				}

			}

        }


		/**
		 * Update the number of times the listing has appeared in search results
		 *
		 * @param id  The listing ID
		 */
		public static void updateSearchAccesses(int id)
		{
			incrementCounter(id, "search");
		}

		/**
		 * Update the number of times the listing has been viewed in detail
		 *
		 * @param id  The listing ID
		 */
		public static void updateDetailsAccesses(int id)
		{
			incrementCounter(id, "viewed");
		}

		/**
		 * Update the number of times the listing has appeared in the RSS feed
		 *
		 * @param id  The listing ID
		 */
		public static void updateRssAccesses(int id)
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