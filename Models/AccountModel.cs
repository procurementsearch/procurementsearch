using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;

using SearchProcurement.Helpers;

namespace SearchProcurement.Models
{
    /* The address struct */
	public struct Address
	{
		public string Address1;
		public string Address2;
		public string City;
		public string State;
		public string Country;
        public string Postal;
	}

    public enum AgencyTypes
    {
        [Display(Name="Government/Non-profit")]
        GovernmentNP,
        [Display(Name="Private Sector (General Contractor, etc.)")]
        Private
    }

	public class Account {

        int AgencyId;

        [Display(Name="Your name")]
        public string UserRealName { get; set; }
        [Display(Name="Your email address")]
        public string UserEmailAddress { get; set; }
        [Display(Name="The name of your agency")]
        public string AgencyName { get; set; }

        public AgencyTypes AgencyType { get; set; }
        [Display(Name="A few words about your agency")]
        public string AgencyAboutText { get; set; }
        [Display(Name="Your Website")]
        public string AgencyUrl { get; set; }
        public string AgencyLogo { get; set; }

        [Display(Name="Contact Person")]
        public string AgencyContactName { get; set; }
        [Display(Name="Contact Email")]
        public string AgencyContactEmail { get; set; }
        [Display(Name="Phone Number")]
        public string AgencyPhone { get; set; }
        [Display(Name="Fax Number")]
        public string AgencyFax { get; set; }
        public Address BillingAddress { get; set; }
        public Address ShippingAddress { get; set; }


        // For the HTML
        public List<SelectListItem> States { get; set; } = Library.StateListItems();
        public List<SelectListItem> Countries { get; set; } = Library.CountryListItems();




        /**
         * Do we have an account for this unique identifier?  If so, then we're
         * probably sending the user to their account page.  If not, we're
         * definitely sending them to the new account page.
         * @param uniq The unique identifier
         * @return bool Do we have this identifier in our database?
         */
        public static bool isKnownAgency(string uniq)
        {
			// Set up the database connection, there has to be a better way!
			using(MySql.Data.MySqlClient.MySqlConnection my_dbh = new MySqlConnection())
			{
				// Open the DB connection
				my_dbh.ConnectionString = Defines.myConnectionString;
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "select count(*) " +
                        "from agency_login as a " +
                        "where uniqueidentifier = @uniq";
					cmd.Parameters.AddWithValue("@uniq", uniq);
					cmd.Prepare();

					// Run the DB command
                    return Convert.ToBoolean(cmd.ExecuteScalar());
                }
            }
        }



        /**
         * Load the account by the unique identifier key.
         * @param uniq The unique identifier
         * @return bool Do we have this identifier in our database?
         */
        public void loadByAgencyIdentifier(string uniq)
        {
			// Set up the database connection, there has to be a better way!
			using(MySql.Data.MySqlClient.MySqlConnection my_dbh = new MySqlConnection())
			{
				// Open the DB connection
				my_dbh.ConnectionString = Defines.myConnectionString;
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT agency_name, " + // 0
                        "agency_type, " + // 1
                        "user_real_name, " + // 2
                        "user_email_address, " + // 3
                        "agency_contact_name, " + // 4
                        "agency_contact_email, " + // 5
                        "agency_url, " + // 6
                        "agency_logo_url, " + // 7
                        "agency_phone, " + // 8
                        "agency_fax, " + // 9
                        "agency_about_text, " + // 10
                        "billing_address_1, " + // 11
                        "billing_address_2, " + // 12
                        "billing_city, " + // 13
                        "billing_state, " + // 14
                        "billing_country, " + // 15
                        "billing_postal, " + // 16
                        "shipping_address_1, " + // 17
                        "shipping_address_2, " + // 18
                        "shipping_city, " + // 19
                        "shipping_state, " + // 20
                        "shipping_country, " + // 21
                        "shipping_postal " + // 22
                        "FROM agency AS a " +
                        "LEFT JOIN agency_login AS al ON al.agency_id = a.agency_id " +
                        "WHERE al.uniqueidentifier = @uniq";
					cmd.Parameters.AddWithValue("@uniq", uniq);

					// Run the DB command
					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
                        if( r.HasRows )
                        {
                            r.Read();
        
                            // Store the agency data
                            AgencyName = r.GetString(0);
                            AgencyType = r.GetString(1) == "government_np" ? AgencyTypes.GovernmentNP : AgencyTypes.Private;
                            UserRealName = r.GetString(2);
                            UserEmailAddress = r.GetString(3);
                            AgencyContactName = r.IsDBNull(4) ? null : r.GetString(4);
                            AgencyContactEmail = r.IsDBNull(5) ? null : r.GetString(5);
                            AgencyUrl = r.IsDBNull(6) ? null : r.GetString(6);
                            AgencyLogo = r.IsDBNull(7) ? null : r.GetString(7);
                            AgencyPhone = r.IsDBNull(8) ? null : r.GetString(8);
                            AgencyFax = r.IsDBNull(9) ? null : r.GetString(9);
                            AgencyAboutText = r.IsDBNull(10) ? null : r.GetString(10);
                            BillingAddress = new Address {
                                Address1 = r.IsDBNull(11) ? null : r.GetString(11),
                                Address2 = r.IsDBNull(12) ? null : r.GetString(12),
                                City = r.IsDBNull(13) ? null : r.GetString(13),
                                State = r.IsDBNull(14) ? null : r.GetString(14),
                                Country = r.IsDBNull(15) ? null : r.GetString(15),
                                Postal = r.IsDBNull(16) ? null : r.GetString(16)
                            };
                            ShippingAddress = new Address {
                                Address1 = r.IsDBNull(17) ? null : r.GetString(17),
                                Address2 = r.IsDBNull(18) ? null : r.GetString(18),
                                City = r.IsDBNull(19) ? null : r.GetString(19),
                                State = r.IsDBNull(20) ? null : r.GetString(20),
                                Country = r.IsDBNull(21) ? null : r.GetString(21),
                                Postal = r.IsDBNull(22) ? null : r.GetString(22)
                            };

                        }
                        else
                            throw new System.ArgumentException("Couldn't find the agency by unique ID");

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
			using(MySql.Data.MySqlClient.MySqlConnection my_dbh = new MySqlConnection())
			{
				// Open the DB connection
				my_dbh.ConnectionString = Defines.myConnectionString;
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "INSERT INTO agency " +
                        "(agency_name, agency_type, user_real_name, user_email_address, agency_contact_name, agency_contact_email, " +
                        "agency_url, agency_phone, agency_fax, agency_about_text, " +
                        "billing_address_1, billing_address_2, billing_city, billing_state, billing_country, billing_postal, " +
                        "shipping_address_1, shipping_address_2, shipping_city, shipping_state, shipping_country, shipping_postal, " +
                        "created, created_ipaddr, updated) VALUES (" +
                        "@a1, @a2, @a3, @a4, @a5, @a6, " +
                        "@a7, @a9, @a10, @a11, " +
                        "@a12, @a13, @a14, @a15, @a16, @a17, " +
                        "@a18, @a19, @a20, @a21, @a22, @a23, " +
                        "now(), @ip_addr, now())";
					cmd.Parameters.AddWithValue("@a1", AgencyName);
					cmd.Parameters.AddWithValue("@a2", AgencyType);
					cmd.Parameters.AddWithValue("@a3", UserRealName);
					cmd.Parameters.AddWithValue("@a4", UserEmailAddress);
					cmd.Parameters.AddWithValue("@a5", AgencyContactName);
					cmd.Parameters.AddWithValue("@a6", AgencyContactEmail);
					cmd.Parameters.AddWithValue("@a7", AgencyUrl);
					cmd.Parameters.AddWithValue("@a9", AgencyPhone);
					cmd.Parameters.AddWithValue("@a10", AgencyFax);
					cmd.Parameters.AddWithValue("@a11", AgencyAboutText);
					cmd.Parameters.AddWithValue("@a12", BillingAddress.Address1);
					cmd.Parameters.AddWithValue("@a13", BillingAddress.Address2);
					cmd.Parameters.AddWithValue("@a14", BillingAddress.City);
					cmd.Parameters.AddWithValue("@a15", BillingAddress.State);
					cmd.Parameters.AddWithValue("@a16", BillingAddress.Country);
					cmd.Parameters.AddWithValue("@a17", BillingAddress.Postal);
					cmd.Parameters.AddWithValue("@a18", ShippingAddress.Address1);
					cmd.Parameters.AddWithValue("@a19", ShippingAddress.Address2);
					cmd.Parameters.AddWithValue("@a20", ShippingAddress.City);
					cmd.Parameters.AddWithValue("@a21", ShippingAddress.State);
					cmd.Parameters.AddWithValue("@a22", ShippingAddress.Country);
					cmd.Parameters.AddWithValue("@a23", ShippingAddress.Postal);
					cmd.Parameters.AddWithValue("@ip_addr", ip_addr ?? "");

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't add the agency");

                }


				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT LAST_INSERT_ID()";

                    // Get the new agency ID
                    AgencyId = Convert.ToInt32(cmd.ExecuteScalar());
                }

				// Save the agency unique auth0 identifier
				using(MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "INSERT INTO agency_login " +
                        "(agency_id, uniqueidentifier) VALUES (@a1, @a2)";
					cmd.Parameters.AddWithValue("@a1", AgencyId);
					cmd.Parameters.AddWithValue("@a2", uniq);

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't add the agency login identifier!!");

                }


                // Parse out the image data, and get ready to commit it to DreamObjects
                byte[] decodedLogo;
                byte[] decodedLogoMd5;
                if( AgencyLogo.IndexOf("data:image/png;base64,") == 0 )
                {
                    // Decode the logo, and get an mp3 for the s3 file name
                    decodedLogo = Convert.FromBase64String( AgencyLogo.Replace("data:image/png;base64,", ""));

                    // And get the md5dum for the logo
                    using (IncrementalHash hasher = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
                    {
                        hasher.AppendData(decodedLogo);
                        hasher.AppendData(Encoding.ASCII.GetBytes(UserEmailAddress));
                        decodedLogoMd5 = hasher.GetHashAndReset();

                        // And upload the logo to S3
                        SearchProcurement.AWS.S3 s = new SearchProcurement.AWS.S3();
                        AgencyLogo = s.UploadBuffer(decodedLogo, Defines.s3Bucket, Defines.s3LogoPath + "/" + decodedLogoMd5 + ".png");

                    }

                }

				// And save the agency logo to the database
				using(MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "UPDATE agency SET agency_logo_url = @url WHERE agency_id = @id";
                    cmd.Parameters.AddWithValue("@url", AgencyLogo);
                    cmd.Parameters.AddWithValue("@id", AgencyId);
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }

            }

            return true;

        }




        /**
         * Check to see if this email address exists in the agency table
         * @param email The email to check
         * @return bool If it exists, return true
         */
        public static bool emailExists(string email)
        {
			// Set up the database connection, there has to be a better way!
			using(MySql.Data.MySqlClient.MySqlConnection my_dbh = new MySqlConnection())
			{
				// Open the DB connection
				my_dbh.ConnectionString = Defines.myConnectionString;
				my_dbh.Open();
	
				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "select count(*) " +
                        "from agency as a " +
                        "where user_email_address = @e";
					cmd.Parameters.AddWithValue("@e", email);
					cmd.Prepare();

					// Run the DB command
                    return Convert.ToBoolean(cmd.ExecuteScalar());

                }
            }
            
        }
    }

}