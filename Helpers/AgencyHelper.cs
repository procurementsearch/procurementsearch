using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SearchProcurement.Controllers;

using MySql.Data.MySqlClient;


namespace SearchProcurement.Helpers
{

    public static class AgencyTypes
    {
        public const string GovernmentNP = "governmentnp";
        public const string Private = "private";
        public const string TribalGovt = "tribalgov";
    }

    public static class PaymentTokenType
    {
        public const string Single = "single";
        public const string Umbrella = "umbrella";
    }



    public class AgencyHelper
    {
        /**
         * Load the account ID by the unique identifier key.
         * @param uniq The unique identifier
         * @return int The agency ID
         */
        public static int getIdForAgencyIdentifier(string uniq)
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT a.agency_id " +
                        "FROM agency AS a " +
                        "LEFT JOIN agency_team AS al ON al.agency_id = a.agency_id " +
                        "WHERE al.uniqueidentifier = @uniq";
					cmd.Parameters.AddWithValue("@uniq", uniq);
					return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }



        /**
         * Do we have an account for this unique identifier?  If so, then we're
         * probably sending the user to their account page.  If not, we're
         * definitely sending them to the new account page.
         * @param uniq The unique identifier
         * @return bool Do we have this identifier in our database?
         */
        public static bool isKnownLogin(string uniq)
        {
            // in case they pass in a null string, which shouldn't happen but can
            if( uniq == null )
                return false;

			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "select count(*) " +
                        "from agency_team as a " +
                        "where uniqueidentifier = @uniq";
					cmd.Parameters.AddWithValue("@uniq", uniq);
					cmd.Prepare();

					// Run the DB command
                    return Convert.ToBoolean(cmd.ExecuteScalar());
                }
            }
        }




        /**
         * Do we have a pending team invitation for this email?
         * @param email The email address
         * @return bool Do we have this email as a pending invitation in our database?
         */
        public static string getTeamInvitationAgencyName(string email)
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT agency_name FROM agency AS a " +
                        "LEFT JOIN agency_team_invitation AS at " +
                        "ON at.agency_id = a.agency_id " +
                        "WHERE at.email_address = @email";
					cmd.Parameters.AddWithValue("@email", email);
					cmd.Prepare();

					// Run the DB command
                    return Convert.ToString(cmd.ExecuteScalar());
                }
            }
        }
    }


}
