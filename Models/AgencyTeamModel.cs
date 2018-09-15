using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;

using SearchProcurement.Helpers;

namespace SearchProcurement.Models
{
    // The class for the login
    public class AgencyTeam
    {
        public int AgencyTeamId { get; set; }
        public int AgencyId { get; set; }

        // The logged-in user display name and email address
        [Display(Name="Your name")]
        public string UserRealName { get; set; }
        [Display(Name="Your email address")]
        public string UserEmailAddress { get; set; }

        // The contact info for this logged-in user
        public Address Contact { get; set; }

        // is this person an admin?
        public bool isAdmin { get; set; }


        // For the HTML
        public List<SelectListItem> States { get; set; } = Library.StateListItems();
        public List<SelectListItem> Countries { get; set; } = Library.CountryListItems();


        /**
         * Constructor with no arguments, just instantiate the object
         */
        public AgencyTeam() {}

        /**
         * Instantiate and load object data from ID
         * @param int id The agency team member ID
         */
        public AgencyTeam(int id)
        {
            loadDataById(id);
        }


        /**
         * Instantiate and load object data from unique identifier
         * @param string uniq The unique identifier
         */
        public AgencyTeam(string uniq)
        {
            using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
            {
                // Open the DB connection
                my_dbh.Open();
                using(MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = my_dbh;
                    cmd.CommandText = "SELECT agency_team_id FROM agency_team " +
                        "WHERE uniqueidentifier=@uniq";
                    cmd.Parameters.AddWithValue("@uniq", uniq);

                    int id = Convert.ToInt32(cmd.ExecuteScalar());
                    if( id == 0 )
                        throw new System.ArgumentException("Could not find agency team member by unique identifier");

                    // ok!  we must have gotten one, so let's load the object
                    loadDataById(id);

                }
            }
        }




        public void loadDataById(int id)
        {
            using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
            {
                // Open the DB connection
                my_dbh.Open();
                using(MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = my_dbh;
                    cmd.CommandText = "SELECT user_real_name, " + // 0
                        "user_email_address, " +                  // 1
                        "user_contact_phone, " +                  // 2
                        "user_contact_address, " +                // 3
                        "user_contact_address_2, " +              // 4
                        "user_contact_city, " +                   // 5
                        "user_contact_state, " +                  // 6
                        "user_contact_country, " +                // 7
                        "user_contact_postal, " +                 // 8
                        "is_admin, " +                            // 9
                        "agency_id " +                            // 10
                        "FROM agency_team " +
                        "WHERE agency_team_id = @id";
                    cmd.Parameters.AddWithValue("@id", id);

                    // Run the DB command
                    using(MySqlDataReader r = cmd.ExecuteReader())
                    {
                        if( r.HasRows )
                        {
                            r.Read();
                            AgencyId = r.GetInt32(10);
                            UserRealName = r.GetString(0);
                            UserEmailAddress = r.GetString(1);
                            Contact = new Address {
                                Phone = r.IsDBNull(2) ? null : r.GetString(2),
                                Address1 = r.IsDBNull(3) ? null : r.GetString(3),
                                Address2 = r.IsDBNull(4) ? null : r.GetString(4),
                                City = r.IsDBNull(5) ? null : r.GetString(5),
                                State = r.IsDBNull(6) ? null : r.GetString(6),
                                Country = r.IsDBNull(7) ? null : r.GetString(7),
                                Postal = r.IsDBNull(8) ? null : r.GetString(8)
                            };
                            isAdmin = r.GetInt32(9) == 1 ? true : false;
                        }
                        else
                            throw new System.ArgumentException("Couldn't find the agency by unique ID");

                    }

                    // And save the agency team ID
                    AgencyTeamId = id;
                }
            }
        }





        /**
         * Do we have a pending team invitation for this email?
         * @param email The email address
         * @return bool Do we have this email as a pending invitation in our database?
         */
        public static bool isPendingTeamInvitation(string email)
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT COUNT(*) " +
                        "FROM agency_team_invitation AS A " +
                        "WHERE email_address = @email " +
                        "AND accepted IS NULL";
					cmd.Parameters.AddWithValue("@email", email);
					cmd.Prepare();

					// Run the DB command
                    return Convert.ToBoolean(cmd.ExecuteScalar());
                }
            }
        }






        /**
         * Add the account to the database
         */
        public bool add(string uniq, string ip_addr)
        {

			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "INSERT INTO agency_team " +
                        "(user_real_name, user_email_address, user_contact_phone, user_contact_address, " +
                        "user_contact_address_2, user_contact_city, user_contact_state, user_contact_country, " +
                        "user_contact_postal, agency_id, is_admin, uniqueidentifier, " +
                        "user_created, user_created_ip_addr) VALUES (" +
                        "@a1, @a2, @a3, @a4, " +
                        "@a5, @a6, @a7, @a8, " +
                        "@a9, @a10, @a11, @a12, " +
                        "now(), @ip_addr)";
					cmd.Parameters.AddWithValue("@a1", UserRealName);
					cmd.Parameters.AddWithValue("@a2", UserEmailAddress);
					cmd.Parameters.AddWithValue("@a3", Contact.Phone);
					cmd.Parameters.AddWithValue("@a4", Contact.Address1);
					cmd.Parameters.AddWithValue("@a5", Contact.Address2);
					cmd.Parameters.AddWithValue("@a6", Contact.City);
					cmd.Parameters.AddWithValue("@a7", Contact.State);
					cmd.Parameters.AddWithValue("@a8", Contact.Country);
					cmd.Parameters.AddWithValue("@a9", Contact.Postal);
					cmd.Parameters.AddWithValue("@a10", AgencyId);
					cmd.Parameters.AddWithValue("@a11", isAdmin ? 1 : 0);
					cmd.Parameters.AddWithValue("@a12", uniq);
					cmd.Parameters.AddWithValue("@ip_addr", ip_addr ?? "");

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't add the agency");

                }

				// Pull the item data out of the database
                AgencyTeamId = Library.lastInsertId(my_dbh);

            }

            return true;

        }





        /**
         * Update the account
         */
        public bool update()
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
					cmd.CommandText = "UPDATE agency_team SET " +
                        "user_real_name=@a1, " +
                        "user_email_address=@a2, " +
                        "user_contact_email=@a3, " +
                        "user_contact_address=@a4, " +
                        "user_contact_address_2=@a5, " +
                        "user_contact_city=@a6, " +
                        "user_contact_state=@a7, " +
                        "user_contact_country=@a8, " +
                        "user_contact_postal=@a9, " +
                        "agency_id=@a10, " +
                        "is_admin=@a11, " +
                        "updated=NOW() " +
                        "WHERE agency_id=@id";
					cmd.Parameters.AddWithValue("@a1", UserRealName);
					cmd.Parameters.AddWithValue("@a2", UserEmailAddress);
					cmd.Parameters.AddWithValue("@a3", Contact.Phone);
					cmd.Parameters.AddWithValue("@a4", Contact.Address1);
					cmd.Parameters.AddWithValue("@a5", Contact.Address2);
					cmd.Parameters.AddWithValue("@a6", Contact.City);
					cmd.Parameters.AddWithValue("@a7", Contact.State);
					cmd.Parameters.AddWithValue("@a8", Contact.Country);
					cmd.Parameters.AddWithValue("@a9", Contact.Postal);
					cmd.Parameters.AddWithValue("@a10", AgencyId);
					cmd.Parameters.AddWithValue("@a11", isAdmin ? 1 : 0);

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't update the agency team member");

                }

            }

            return true;

        }






        /**
         * Update the team member's assigned agency
         */
        public bool updateAssignedAgency(int id)
        {

			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "UPDATE agency_team SET " +
                        "agency_id=@agencyId " +
                        "WHERE agency_team_id=@myId";
					cmd.Parameters.AddWithValue("@agencyId", id);
					cmd.Parameters.AddWithValue("@myId", AgencyTeamId);

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't update the agency team member");

                    // and update the object
                    AgencyId = id;
                }

            }

            // And save the agency ID to the object
            AgencyId = id;
            return true;

        }





        /**
         * Accept the team member invitation and mark the invitation as accepted
         */
        public void acceptTeamInvitation()
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "UPDATE agency_team_invitation SET " +
                        "accepted=NOW() " +
                        "WHERE email_address=@email";
					cmd.Parameters.AddWithValue("@email", UserEmailAddress);

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't accept the team member invitation");

                }

            }

        }





        /**
         * Check to see if this email address exists in the agency table
         * @param email The email to check
         * @return bool If it exists, return true
         */
        public static bool emailExists(string email)
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT COUNT(*) " +
                        "FROM agency_team AS a " +
                        "WHERE user_email_address = @e";
					cmd.Parameters.AddWithValue("@e", email);
					cmd.Prepare();
                    return Convert.ToBoolean(cmd.ExecuteScalar());
                }
            }
            
        }
    }

}