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
        public int SourceId;
        public string SourceName;
		public DateTimeOffset Created;
		public int ParentId;
	}


	public class SearchHelper
	{

		// Load up an item
		public static searchItem loadItem(int id)
		{
			searchItem item;
			item.Id = id;

			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
	
				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT l.title, l.description, l.created, l.agency_id, a.agency_name, l.listing_parent_id " +
                        "FROM listing AS l " +
                        "LEFT JOIN agency AS a ON a.agency_id = l.agency_id " +
                        "where listing_id = @id";
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
							item.Description = Library.makeExcerpt(item.Description, 100);
						} else {
							item.Description = "";
						}
						item.Created = r.GetDateTime(2);
                        item.SourceId = r.GetInt32(3);
                        item.SourceName = r.GetString(4);

						// And do a little extra for subcontracts
						if( !r.IsDBNull(5) )
						{
							item.ParentId = r.GetInt32(5);
							item.Title = DetailsHelper.loadTitle(item.ParentId) + ": " + item.Title;
						}
						else
							item.ParentId = 0;

					}
				}
			}

			// And we're done!
			return item;
		}



        /**
         * Load the IDs for every open procurement opportunity for the
         * given source
         * @param sourceId The source ID
         * @return int[] sources The listing IDs
         */
        public static int[] findBySourceId(int id)
		{
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "select listing_id from listing where agency_id = @id and status='open'";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
			            List<int> ids = new List<int>();

						while(r.Read())
						{
                            ids.Add(r.GetInt32(0));
                        }

                        // And we're done
                        return ids.ToArray();

                    }

                }

            }

        }




        /**
         * Load the source name for the agency ID
         * @param int id The agency ID
         * @return int[] sources The listing IDs
         */
        public static string loadAgencyName(int id)
		{
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "select agency_name from agency where agency_id = @id";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
                        // Return the source name or that it is not a known source 
						r.Read();
						return r.IsDBNull(0) ? "unknown" : r.GetString(0);
                    }

                }

            }

        }


		// Filter search items according to an array of source IDs
		public static int[] filter(int[] search_ids, int[] agency_ids)
		{
			// failsafe
			if( search_ids.Length == 0 )
				return new int[0];

			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "select listing_id from listing where listing_id in (" +
						string.Join(", ", search_ids) + ") and agency_id in (" +
						string.Join(", ", agency_ids) + ")";
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