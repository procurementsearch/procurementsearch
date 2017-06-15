using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace SearchProcurement.Helpers
{

    public class AttachmentHelper
    {

        /**
         * Get the deletion identifier for the requested attachment
         * @param int attId The attachment ID
         * @return string The deletion identifier
         */
		public static string getDeletionIdentifier(int attId)
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
					cmd.CommandText = "SELECT deletion_identifier FROM attachment WHERE attachment_id = @id";
					cmd.Parameters.AddWithValue("@id",attId);
					cmd.Prepare();
                    return Convert.ToString(cmd.ExecuteScalar());
                }
            }
		}



        /**
         * Delete an attachment, by attachment ID
         * @param int attId The attachment ID
         * @return none
         */
		public static void deleteById(int attId)
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
					cmd.CommandText = "DELETE FROM attachment WHERE attachment_id = @id";
					cmd.Parameters.AddWithValue("@id",attId);
					cmd.Prepare();
                    cmd.ExecuteScalar();
                }
            }
		}



        /**
         * Update the redirect URL for an attachment
         * @param int attId The attachment ID
         * @param string url The new redirect URL
         * @return none
         */
		public static void updateRedirectUrl(int attId, string url)
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
					cmd.CommandText = "UPDATE attachment SET redirect_url = @url WHERE attachment_id = @id";
					cmd.Parameters.AddWithValue("@id", attId);
					cmd.Parameters.AddWithValue("@url", url);
					cmd.Prepare();
                    cmd.ExecuteScalar();
                }
            }
		}


    }


}
