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
			using(MySqlConnection my_dbh = new MySqlConnection())
			{
				// Open the DB connection
				my_dbh.ConnectionString = Defines.myConnectionString;
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
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

            DateTime my_date = DateTime.Today;

			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection())
			{
				// Open the DB connection
				my_dbh.ConnectionString = Defines.myConnectionString;
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "select count(*) from detailslog where year = @y and month = @m and listing_id = @id";
					cmd.Parameters.AddWithValue("@y", my_date.Year);
					cmd.Parameters.AddWithValue("@m", my_date.Month);
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Prepare();

                    // Do we need to add a detailslog row for this entry?
                    if( Convert.ToInt32(cmd.ExecuteScalar()) == 0 )
                    {
                        cmd.CommandText = "insert into detailslog (year, month, listing_id, views) values (@y, @m, @id, 1)";
                        cmd.Prepare();
                        cmd.ExecuteScalar();
                    }
                    else
                    {
                        // No, we have a row, just update
                        cmd.CommandText = "update detailslog set views = views + 1 where year = @y and month = @m and listing_id = @id";
                        cmd.Prepare();
                        cmd.ExecuteScalar();
                    }

				}

			}

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
			using(MySqlConnection my_dbh = new MySqlConnection())
			{
				// Open the DB connection
				my_dbh.ConnectionString = Defines.myConnectionString;
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
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