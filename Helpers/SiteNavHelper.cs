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

    public struct NavLocation
    {
        public string url;
        public string name;
        public bool sub;
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





        public static string[] getAgencyLogoUrls(int locId)
        {
            // Set up the database connection, there has to be a better way!
            using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
            {
                // Open the DB connection
                my_dbh.Open();
                using(MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = my_dbh;
                    cmd.CommandText = "SELECT a.agency_logo_url " +
                        "FROM agency AS a " +
                        "LEFT JOIN agency_location_defaults AS ald ON a.agency_id = ald.agency_id " +
                        "WHERE a.agency_logo_url IS NOT NULL AND ald.location_id = @id " +
                        "ORDER BY a.agency_name ASC";
                    cmd.Parameters.AddWithValue("@id", locId);
                    cmd.Prepare();

                    // Run the DB command
                    using(MySqlDataReader r = cmd.ExecuteReader())
                    {
                        List<string> l = new List<string>();

                        while(r.Read())
                        {
                            l.Add(r.GetString(0));
                        }

                        // And we're done
                        return l.ToArray();
                    }
                }
            }
        }




        public static NavLocation[] getLocations()
        {
            List<NavLocation> l = new List<NavLocation>();

            // Set up the database connection, there has to be a better way!
            using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
            {
                // Open the DB connection
                my_dbh.Open();
                using(MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = my_dbh;
                    cmd.CommandText = "SELECT location_id, location_name, location_domain " +
                        "FROM location " +
                        "WHERE location_type = 'state' AND location_name != 'Sandbox' " +
                        "ORDER BY location_name ASC";
                    cmd.Prepare();

                    // Run the DB command
                    using(MySqlDataReader r = cmd.ExecuteReader())
                    {
                        int locId;

                        while(r.Read())
                        {
                            locId = r.GetInt32(0);
                            l.Add(new NavLocation
                            {
                                name = r.GetString(1),
                                url = r.GetString(2),
                                sub = false
                            });

                            // Now get the subregions
                            using(MySqlConnection my_dbh2 = new MySqlConnection(Defines.AppSettings.myConnectionString))
                            {
                                // Open the DB connection
                                my_dbh2.Open();
                                using(MySqlCommand cmd2 = new MySqlCommand())
                                {
                                    cmd2.Connection = my_dbh2;
                                    cmd2.CommandText = "SELECT location_name, location_domain " +
                                        "FROM location AS l " +
                                        "LEFT JOIN location_location_join AS llj " +
                                        "ON llj.child_location_id = l.location_id " +
                                        "WHERE llj.parent_location_id = @id " +
                                        "ORDER BY location_name ASC";
                                    cmd2.Parameters.AddWithValue("@id", locId);
                                    cmd2.Prepare();

                                    // Run the DB command
                                    using(MySqlDataReader r2 = cmd2.ExecuteReader())
                                    {
                                        while(r2.Read())
                                        {
                                            l.Add(new NavLocation
                                            {
                                                name = r2.GetString(0),
                                                url = r2.GetString(1),
                                                sub = true
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // And we're done
            return l.ToArray();

        }
    }
}