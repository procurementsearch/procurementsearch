using System;
using System.Collections.Generic;
using System.Xml.Linq;
using MySql.Data.MySqlClient;

using SearchProcurement.Models;

namespace SearchProcurement.Helpers
{
	// And some type classes...
    public static class ListingTypes
    {
        public const string Simple = "simple";
        public const string Umbrella = "umbrella";
    }


    public static class ListingStatus
    {
        public const string Draft = "draft";
        public const string Published = "published";
        public const string Open = "open";
        public const string Disabled = "disabled";
        public const string Canceled = "canceled";
        public const string Closed = "closed";
    }

    public static class ListingUpdateMode
    {
        public const string Addendum = "addendum";
        public const string Revision = "revision";
    }




	public class ListingHelper
	{

		/**
		 * Save the difference of two items in the addendum table
		 * Here's an example:
		 * <?xml version="1.0" encoding="UTF-8"?>
         * <diff><field>close_date</field><old><![CDATA[2016-04-04 16:00:00]]></old><new><![CDATA[2016-04-18 16:00:00]]></new></diff>
		 *
		 * @param string field The name of the field we're diffing
		 * @param string oldData The old data
		 * @param string newData The new data
		 * @return none
		 */
		public static void logAddendum(int id, string field, string oldData, string newData)
		{
			// Generate the XML
			XDocument xml = new XDocument(
				new XDeclaration("1.0", "utf-8", null),
				new XElement("diff",
					new XElement("field", field),
					new XElement("old", oldData),
					new XElement("new", newData)
				)
			);

			// And make it into a string
			string xmldiff = xml.Declaration + "\n" + xml.ToString();

			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
	
				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "INSERT INTO addenda (listing_id, diff, updated) " +
						 "VALUES " +
						 "(@id, @xml, NOW())";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Parameters.AddWithValue("@xml", xmldiff);
					cmd.Prepare();

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't add the addendum");

				}
			}
		}




        /**
         * Get the listing status
         * @param int listId The listing ID
         * @return string Listing status
         */
		public static string getStatus(int listId)
		{
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT status FROM listing WHERE listing_id = @id";
					cmd.Parameters.AddWithValue("@id",listId);
					cmd.Prepare();
                    return Convert.ToString(cmd.ExecuteScalar());
                }
            }
		}




        /**
         * Load up active listings
         * @param int AgencyId The ID of the agency to load listings for
         * @param string status The listing status to load
         * @return array The active listings
         */
        public static Listing[] loadListings(int agencyId, string[] statuses)
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;

                    // Build the string for statuses
                    string[] statusesIn = new string[statuses.Length];
                    for (int i = 0; i < statuses.Length; i++)
                    {
                        string myStatus = "@status" + (i+1);
                        statusesIn[i] = myStatus;
                        cmd.Parameters.AddWithValue(myStatus, statuses[i]);
                    }

                    // And build the SQL for selecting statuses
					cmd.CommandText = "SELECT listing_id, title, status, listing_type, close_date FROM listing " +
                        "WHERE agency_id = @agencyId AND status in (" + String.Join(",", statusesIn) + ") " +
						"AND listing_parent_id IS NULL ORDER BY title";
					cmd.Parameters.AddWithValue("@agencyId", agencyId);

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
                        List<Listing> listings = new List<Listing>();

						while(r.Read())
						{
							// Instantiate a listing, just for this use
                            Listing l = new Listing
                            {
                                ListingId = r.GetInt32(0),
                                Title = r.GetString(1),
                                Status = r.GetString(2),
                                Type = r.GetString(3)
                            };

							// Argh!  Close date can be null
							if( !r.IsDBNull(4) )
								l.CloseDate = r.GetDateTime(4);

                            listings.Add(l);
                        }

                        // And we're done
                        return listings.ToArray();

                    }
                }
            }
        }


	}
}