using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace SearchProcurement.Helpers
{
	public struct searchItem
	{
		public int Id;
		public string Title;
		public string Description;
		public DateTimeOffset Created;
	}


	public class SearchHelper
	{

		// Load up an item
		public static searchItem loadItem(int id)
		{
			searchItem item;
			item.Id = id;

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
					cmd.CommandText = "select title, description, created from listing where listing_id = @id";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
						r.Read();

						// Store the item data
						item.Title = r.GetString(0);
						if( !r.IsDBNull(1) ) {
							item.Description = Library.stripHtml(r.GetString(1));
							item.Description = item.Description.Substring(0, Math.Min(100, item.Description.Length));
						} else {
							item.Description = "";
						}
						item.Created = r.GetDateTime(2);
					}
				}
			}

			// And we're done!
			return item;
		}



		// Filter search items according to an array of source IDs
		public static int[] filter(int[] search_ids, int[] source_ids)
		{
			// failsafe
			if( search_ids.Length == 0 )
				return new int[0];

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
					cmd.CommandText = "select listing_id from listing where listing_id in (" +
						string.Join(", ", search_ids) + ") and source_id in (" +
						string.Join(", ", source_ids) + ")";
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
						// Only these IDs should show up in search and RSS results..
						List<int> filtered_ids = new List<int>();

						while(r.Read())
						{
							filtered_ids.Add(r.GetInt32(0));							
						}

						// And we're done!
						return filtered_ids.ToArray();

					}
				}
			}
		}


	}
}