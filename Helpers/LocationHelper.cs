using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace SearchProcurement.Helpers
{

    public struct Location
    {
        public int LocationId;
        public string LocationName;
    }



    public class LocationHelper
    {

        /**
         * Get a list of states where people can post listings
         * @return Location[] an array of location structs
         */
        public static Location[] getAvailableStates()
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT location_name, location_id FROM location WHERE location_type = 'State' ORDER BY location_name ASC";
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
                        // First, build the list
                        List<Location> locs = new List<Location>();

                        while( r.Read() )
                        {
                            locs.Add(new Location {
                                LocationId = r.GetInt32(1),
                                LocationName = r.GetString(0)
                            });
                        }

                        // And we're done!
                        return locs.ToArray();

                    }
                }
            }
        }





        /**
         * Loads the location name for the given location ID
         * @param int locId The location ID
         * @return string The location name
         */
		public static string getNameForId(int locId)
		{
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT location_name FROM location WHERE location_id = @id";
					cmd.Parameters.AddWithValue("@id",locId);
					cmd.Prepare();
                    return Convert.ToString(cmd.ExecuteScalar());
                }
            }
		}




        /**
         * Get a list of regional subsites for the given state location ID
         * @param int locId The location ID -- should be a state-level ID
         * @return Location[] an array of location structs
         */
        public static Location[] getRegionsForState(int locId)
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT location_name, location_id FROM location AS l " +
                        "LEFT JOIN location_location_join AS lj " +
                        "ON l.location_id = lj.child_location_id " +
                        "WHERE lj.parent_location_id = @locId ORDER BY location_name ASC;";
                    cmd.Parameters.AddWithValue("@locId", locId);
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
                        // First, build the list
                        List<Location> locs = new List<Location>();

                        while( r.Read() )
                        {
                            locs.Add(new Location {
                                LocationId = r.GetInt32(1),
                                LocationName = r.GetString(0)
                            });
                        }

                        // And we're done!
                        return locs.ToArray();

                    }
                }
            }
        }


    }


}
