using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace SearchProcurement.Helpers
{

	public class DetailsHelper
	{

		// Load up an item
		public static string loadTitle(int id)
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
					cmd.CommandText = "select l.title " +
                        "from listing as l " +
                        "where listing_id = @id";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Prepare();

					// Run the DB command
                    return Convert.ToString(cmd.ExecuteScalar());

                }
            }

        }



		// Load up an item
		public static int[] findSubcontractIds(int id)
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
					cmd.CommandText = "select l.listing_id " +
                        "from listing as l " +
                        "where listing_parent_id = @id";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Prepare();

                    // The subcontract IDs
                    List<int> ids = new List<int>();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
						r.Read();

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


   }

}
