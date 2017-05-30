using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

using SearchProcurement.Helpers;

namespace SearchProcurement.Models
{

	public class Price
	{

		public static decimal loadPrice(string agency_type, string listing_type)
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
					cmd.CommandText = "SELECT price FROM price WHERE agency_type = @a AND listing_type = @l";
					cmd.Parameters.AddWithValue("@a", agency_type);
					cmd.Parameters.AddWithValue("@l", listing_type);
					cmd.Prepare();
                    return Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
		}
	}
}
