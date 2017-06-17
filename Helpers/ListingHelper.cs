using System;
using System.Collections.Generic;
using System.Xml.Linq;
using MySql.Data.MySqlClient;

namespace SearchProcurement.Helpers
{

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
			using(MySqlConnection my_dbh = new MySqlConnection())
			{
				// Open the DB connection
				my_dbh.ConnectionString = Defines.myConnectionString;
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

	}
}