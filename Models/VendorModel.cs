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
    public struct VendorCertification
    {
        public int Id;
        public string Name;
    }


	public class Vendor
    {

        public int VendorId;

        [Display(Name="Your business name")]
        public string BusinessName { get; set; }
        [Display(Name="Your name")]
        public string VendorName { get; set; }
        [Display(Name="Your email address")]
        public string VendorEmailAddress { get; set; }
        [Display(Name="The state where your business is registered")]
        public string VendorState { get; set; }

        public int[] VendorCertifications { get; set; }


        // For the HTML
        public List<SelectListItem> States { get; set; } = Library.StateListItems();
        public List<SelectListItem> Countries { get; set; } = Library.CountryListItems();





        /**
         * Constructor with no arguments, just instantiate the object
         */
        public Vendor() {}

        /**
         * Instantiate and load ID and data from unique string
         * @param string uniq The unique identifier
         */
        public Vendor(string uniq)
        {
            if( Vendor.isKnownVendor(uniq) )
            {
                this.loadIdByVendorIdentifier(uniq);
                this.loadDataByVendorIdentifier(uniq);
            }
        }




        /**
         * Do we have an account for this unique identifier?  If so, then we're
         * probably sending the user to their account page.  If not, we're
         * definitely sending them to the new account page.
         * @param uniq The unique identifier
         * @return bool Do we have this identifier in our database?
         */
        public static bool isKnownVendor(string uniq)
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
					cmd.CommandText = "select count(*) " +
                        "from vendor_login as a " +
                        "where uniqueidentifier = @uniq";
					cmd.Parameters.AddWithValue("@uniq", uniq);
					cmd.Prepare();

					// Run the DB command
                    return Convert.ToBoolean(cmd.ExecuteScalar());
                }
            }
        }





        /**
         * Load the account ID by the unique identifier key.
         * @param uniq The unique identifier
         * @return int The vendor ID
         */
        public void loadIdByVendorIdentifier(string uniq)
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
					cmd.CommandText = "SELECT v.vendor_id " +
                        "FROM vendor AS v " +
                        "LEFT JOIN vendor_login AS vl ON vl.vendor_id = v.vendor_id " +
                        "WHERE vl.uniqueidentifier = @uniq";
					cmd.Parameters.AddWithValue("@uniq", uniq);

					// Run the DB command
					VendorId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }








        /**
         * Load the account by the unique identifier key.
         * @param uniq The unique identifier
         * @return bool Do we have this identifier in our database?
         */
        public void loadDataByVendorIdentifier(string uniq)
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
					cmd.CommandText = "SELECT business_name, " + // 0
                        "vendor_name, " +                        // 1
                        "vendor_email, " +                       // 2
                        "vendor_state " +                        // 3
                        "FROM vendor AS v " +
                        "LEFT JOIN vendor_login AS vl ON vl.vendor_id = v.vendor_id " +
                        "WHERE vl.uniqueidentifier = @uniq";
					cmd.Parameters.AddWithValue("@uniq", uniq);

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
                        if( r.HasRows )
                        {
                            r.Read();
        
                            // Store the agency data
                            BusinessName = r.GetString(0);
                            VendorName = r.GetString(1);
                            VendorEmailAddress = r.GetString(2);
                            VendorState = r.GetString(3);

                        }
                        else
                            throw new System.ArgumentException("Couldn't find the vendor by unique ID");

                    }
                }

                // And load up the certifications this business might have registered
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT vendor_certification_id FROM vendor_certification_join " +
                        "WHERE vendor_id = @id";
					cmd.Parameters.AddWithValue("@id", VendorId);

                    // And execute the query
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
                        // Do we have any rows here?  Then we have additional locations (by design,
                        // at least right now, only one additional location)
                        if( r.HasRows )
                        {
                            List <int>ids = new List<int>();

                            while( r.Read() )
                                ids.Add(r.GetInt32(0));

                            // And assign the IDs to the location ID array
                            VendorCertifications = ids.ToArray();
                        }
                        else
                            // Initialize an empty list
                            VendorCertifications = new int[] {};
                    }
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

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "INSERT INTO vendor " +
                        "(business_name, vendor_name, vendor_email, vendor_state, " +
                        "created, created_ipaddr, updated) VALUES (" +
                        "@v1, @v2, @v3, @v4, " +
                        "now(), @ip_addr, now())";
					cmd.Parameters.AddWithValue("@v1", BusinessName);
					cmd.Parameters.AddWithValue("@v2", VendorName);
					cmd.Parameters.AddWithValue("@v3", VendorEmailAddress);
					cmd.Parameters.AddWithValue("@v4", VendorState);
					cmd.Parameters.AddWithValue("@ip_addr", ip_addr ?? "");

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't add the vendor");

                }


				// Pull the item data out of the database
                VendorId = Library.lastInsertId(my_dbh);


                // Save the certifications, if there are any
                saveVendorCertifications();

				// Save the agency unique auth0 identifier
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "INSERT INTO vendor_login " +
                        "(vendor_id, uniqueidentifier) VALUES (@v1, @v2)";
					cmd.Parameters.AddWithValue("@v1", VendorId);
					cmd.Parameters.AddWithValue("@v2", uniq);

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't add the vendor login identifier!!");

                }

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
					cmd.CommandText = "UPDATE vendor SET " +
                        "business_name=@v1, " +
                        "vendor_name=@v2, " +
                        "vendor_email=@v3, " +
                        "vendor_state=@v4, " +
                        "updated=NOW() " +
                        "WHERE vendor_id=@id";
					cmd.Parameters.AddWithValue("@v1", BusinessName);
					cmd.Parameters.AddWithValue("@v2", VendorName);
					cmd.Parameters.AddWithValue("@v3", VendorEmailAddress);
					cmd.Parameters.AddWithValue("@v4", VendorState);
					cmd.Parameters.AddWithValue("@id", VendorId);

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't update the agency");
                }

                // Save the certifications
                saveVendorCertifications();

            }

            return true;

        }



        /**
         * Save the vendor certifications for this vendor record
         */
        private void saveVendorCertifications()
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

                // Save the certifications, if there are any -- first removing the old cert list
                // for this vendor
                using(MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = my_dbh;
                    cmd.CommandText = "DELETE FROM vendor_certification_join WHERE vendor_id = @v1";
                    cmd.Parameters.AddWithValue("@v1", VendorId);
                    cmd.ExecuteNonQuery();
                }

                if( VendorCertifications != null && VendorCertifications.Length > 0 )
                {
                    foreach(var certId in VendorCertifications)
                    {
                        using(MySqlCommand cmd = new MySqlCommand())
                        {
                            cmd.Connection = my_dbh;
                            cmd.CommandText = "INSERT INTO vendor_certification_join " +
                                "(vendor_id, vendor_certification_id) VALUES (@v1, @v2)";
                            cmd.Parameters.AddWithValue("@v1", VendorId);
                            cmd.Parameters.AddWithValue("@v2", certId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }



        /**
         * Return a data array of certifications for the given state
         * @param state The state to pull down certs for
         * @return string[] The named certification types
         */
        public static VendorCertification[] getPossibleCertifications(string state)
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
					cmd.CommandText = "SELECT vendor_certification_id, certification_name " +
                        "FROM vendor_certification " +
                        "WHERE certification_state = @s " +
                        "ORDER BY certification_name";
					cmd.Parameters.AddWithValue("@s", state);
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
			            List<VendorCertification> certs = new List<VendorCertification>();

						while(r.Read())
						{
                            certs.Add(new VendorCertification {
                                Id = r.GetInt32(0),
                                Name = r.GetString(1)
                            });
                        }

                        // And we're done
                        return certs.ToArray();
                    }
                }
            }
        }





        /**
         * Check to see if this email address exists in the vendor table
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
	
				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT COUNT(*) " +
                        "FROM vendor AS a " +
                        "WHERE vendor_email = @e";
					cmd.Parameters.AddWithValue("@e", email);
					cmd.Prepare();

					// Run the DB command
                    return Convert.ToBoolean(cmd.ExecuteScalar());

                }
            }
        }
    }

}