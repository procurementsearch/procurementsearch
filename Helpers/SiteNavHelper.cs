using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace SearchProcurement.Helpers
{
    public struct NavAgency
    {
        public int agencyId;
        public string name;
    }

    public class SiteNavHelper
    {
        public static NavAgency[] getAgencies(int locId)
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT a.agency_id, a.agency_name " +
                        "FROM agency AS a " +
                        "LEFT JOIN agency_location_defaults AS ald ON a.agency_id = ald.agency_id " +
                        "WHERE ald.location_id = @id " +
                        "ORDER BY a.agency_name ASC";
					cmd.Parameters.AddWithValue("@id", locId);
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
			            List<NavAgency> l = new List<NavAgency>();

						while(r.Read())
						{
                            l.Add(new NavAgency
                            {
                                agencyId = r.GetInt32(0),
                                name = r.GetString(1)
                            });
                        }

                        // And we're done
                        return l.ToArray();
                    }
                }
            }
        }
    }
}