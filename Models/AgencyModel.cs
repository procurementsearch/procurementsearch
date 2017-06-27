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
    /* The address struct */
	public class Address
	{
		public string Address1 { get; set; }
		public string Address2 { get; set; }
		public string City { get; set; }
		public string State { get; set; }
		public string Country { get; set; }
        public string Postal { get; set; }
	}


	public class Agency {

        public int AgencyId;

        [Display(Name="Your name")]
        public string UserRealName { get; set; }
        [Display(Name="Your email address")]
        public string UserEmailAddress { get; set; }
        [Display(Name="The name of your agency")]
        public string AgencyName { get; set; }

        public string AgencyType { get; set; }
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
         * Constructor with no arguments, just instantiate the object
         */
        public Agency() {}

        /**
         * Instantiate and load ID and data from unique string
         * @param string uniq The unique identifier
         */
        public Agency(string uniq)
        {
            if( Agency.isKnownAgency(uniq) )
            {
                this.loadIdByAgencyIdentifier(uniq);
                this.loadDataByAgencyIdentifier(uniq);
            }
        }




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
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
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
         * Load the account ID by the unique identifier key.
         * @param uniq The unique identifier
         * @return int The agency ID
         */
        public void loadIdByAgencyIdentifier(string uniq)
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
					cmd.CommandText = "SELECT a.agency_id " +
                        "FROM agency AS a " +
                        "LEFT JOIN agency_login AS al ON al.agency_id = a.agency_id " +
                        "WHERE al.uniqueidentifier = @uniq";
					cmd.Parameters.AddWithValue("@uniq", uniq);

					// Run the DB command
					AgencyId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }



        /**
         * Does this location have a payment token (unlimited, or unused
         * one-time use) to the specified state?
         * @param int locId The location ID
         * @return bool Can they post here?
         */
        public bool hasAvailablePaymentToken(int locId)
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
					cmd.CommandText = "SELECT COUNT(*) " +
                        "FROM agency_payment_token " +
                        "WHERE agency_id = @id " +
                        "AND ((location_id = @locId AND token_type = 'unlimited' AND token_expires <= CURDATE()) " +
                        "OR (location_id IS NULL AND token_type = 'single' AND token_used = 0))";
					cmd.Parameters.AddWithValue("@id", AgencyId);
					cmd.Parameters.AddWithValue("@locId", locId);

					// Run the DB command
					return Convert.ToBoolean(cmd.ExecuteScalar());
                }
            }

        }




        /**
         * How many uses of a specific payment token type does this agency
         * have for the given state?
         * @param int locId The location ID
         * @param string type The listing type, simple or umbrella listing
         * @return int How many tokens available (an unlimited token always returns true for the region)
         */
        public int getPaymentTokens(int locId, string type)
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
					cmd.CommandText = "SELECT COUNT(*) " +
                        "FROM agency_payment_token " +
                        "WHERE agency_id = @id " +
                        "AND (" +
                            "(location_id = @locId AND token_type = 'unlimited' AND token_expires <= CURDATE()) " +
                            "OR (location_id IS NULL AND token_type = 'single' AND listing_type = @listingType AND token_used = 0)" +
                        ")";
					cmd.Parameters.AddWithValue("@id", AgencyId);
					cmd.Parameters.AddWithValue("@locId", locId);
					cmd.Parameters.AddWithValue("@listingType", type);

					// Run the DB command
					return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

        }






        /**
         * Add a payment token and register it to this account
         * @param listingType The type of listing they've paid for
         * @return none
         */
        public void addPaymentToken(string listingType, decimal amount, string stripeToken)
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
					cmd.CommandText = "INSERT INTO agency_payment_token " +
                        "(agency_id, token_type, listing_type, amount_paid, stripe_token, created) " +
                        "VALUES " +
                        "(@agency_id, 'single', @type, @amt, @token, NOW())";
					cmd.Parameters.AddWithValue("@agency_id", AgencyId);
                    cmd.Parameters.AddWithValue("@type", listingType);
                    cmd.Parameters.AddWithValue("@amt", amount);
                    cmd.Parameters.AddWithValue("@token", stripeToken);
					cmd.Prepare();

					// Run the DB command, and we're done
                    cmd.ExecuteScalar();

                }
            }
        }





        /**
         * Use a payment token!
         * @param int locId The location ID
         * @param string type The listing type, simple or umbrella listing
         * @return int How many tokens available (an unlimited token always returns true for the region)
         */
        public bool usePaymentToken(int locId, string type, string ip_addr)
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT COUNT(*) FROM agency_payment_token WHERE agency_id = @id " +
                        "AND location_id = @locId AND token_type = 'unlimited' AND token_expires <= CURDATE()";
					cmd.Parameters.AddWithValue("@id", AgencyId);
					cmd.Parameters.AddWithValue("@locId", locId);

                    // If they've got an unlimited access token for this region, always preferentially apply that
                    if( Convert.ToInt32(cmd.ExecuteScalar()) > 0 )
                        return true;

                }

                // Otherwise, use their payment token
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "UPDATE agency_payment_token " +
                        "SET token_used = 1, activated = NOW(), activated_ipaddr = @ip_addr " +
                        "WHERE " +
                        "agency_id = @id AND location_id IS NULL AND token_type = 'single' AND listing_type = @listingType AND token_used = 0 " +
                        "LIMIT 1";
					cmd.Parameters.AddWithValue("@id", AgencyId);
                    cmd.Parameters.AddWithValue("@listingType", type);
                    cmd.Parameters.AddWithValue("@ip_addr", ip_addr);

                    // If we've actually updated a row, that means the payment token
                    // has been successfully applied!
                    return Convert.ToInt32(cmd.ExecuteNonQuery()) > 0 ? true : false;
                }
            }
        }




        /**
         * Load the account by the unique identifier key.
         * @param uniq The unique identifier
         * @return bool Do we have this identifier in our database?
         */
        public void loadDataByAgencyIdentifier(string uniq)
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
					cmd.CommandText = "SELECT agency_name, " + // 0
                        "agency_type, " +                      // 1
                        "user_real_name, " +                   // 2
                        "user_email_address, " +               // 3
                        "agency_contact_name, " +              // 4
                        "agency_contact_email, " +             // 5
                        "agency_url, " +                       // 6
                        "agency_logo_url, " +                  // 7
                        "agency_phone, " +                     // 8
                        "agency_fax, " +                       // 9
                        "agency_about_text, " +                // 10
                        "billing_address_1, " +                // 11
                        "billing_address_2, " +                // 12
                        "billing_city, " +                     // 13
                        "billing_state, " +                    // 14
                        "billing_country, " +                  // 15
                        "billing_postal, " +                   // 16
                        "shipping_address_1, " +               // 17
                        "shipping_address_2, " +               // 18
                        "shipping_city, " +                    // 19
                        "shipping_state, " +                   // 20
                        "shipping_country, " +                 // 21
                        "shipping_postal " +                   // 22
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
                            AgencyType = r.GetString(1);
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
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "INSERT INTO agency " +
                        "(agency_name, agency_type, user_real_name, user_email_address, agency_contact_name, agency_contact_email, " +
                        "agency_url, agency_phone, agency_fax, agency_about_text, " +
                        "billing_address_1, billing_address_2, billing_city, billing_state, billing_country, billing_postal, " +
                        "shipping_address_1, shipping_address_2, shipping_city, shipping_state, shipping_country, shipping_postal, " +
                        "created, created_ipaddr, updated) VALUES (" +
                        "@a1, @a2, @a3, @a4, @a5, @a6, " +
                        "@a7, @a8, @a9, @a10, @a11, " +
                        "@a12, @a13, @a14, @a15, @a16, @a17, " +
                        "@a18, @a19, @a20, @a21, @a22, " +
                        "now(), @ip_addr, now())";
					cmd.Parameters.AddWithValue("@a1", AgencyName);
					cmd.Parameters.AddWithValue("@a2", AgencyType);
					cmd.Parameters.AddWithValue("@a3", UserRealName);
					cmd.Parameters.AddWithValue("@a4", UserEmailAddress);
					cmd.Parameters.AddWithValue("@a5", AgencyContactName);
					cmd.Parameters.AddWithValue("@a6", AgencyContactEmail);
					cmd.Parameters.AddWithValue("@a7", AgencyUrl);
					cmd.Parameters.AddWithValue("@a8", AgencyPhone);
					cmd.Parameters.AddWithValue("@a9", AgencyFax);
					cmd.Parameters.AddWithValue("@a10", AgencyAboutText);
					cmd.Parameters.AddWithValue("@a11", BillingAddress.Address1);
					cmd.Parameters.AddWithValue("@a12", BillingAddress.Address2);
					cmd.Parameters.AddWithValue("@a13", BillingAddress.City);
					cmd.Parameters.AddWithValue("@a14", BillingAddress.State);
					cmd.Parameters.AddWithValue("@a15", BillingAddress.Country);
					cmd.Parameters.AddWithValue("@a16", BillingAddress.Postal);
					cmd.Parameters.AddWithValue("@a17", ShippingAddress.Address1);
					cmd.Parameters.AddWithValue("@a18", ShippingAddress.Address2);
					cmd.Parameters.AddWithValue("@a19", ShippingAddress.City);
					cmd.Parameters.AddWithValue("@a20", ShippingAddress.State);
					cmd.Parameters.AddWithValue("@a21", ShippingAddress.Country);
					cmd.Parameters.AddWithValue("@a22", ShippingAddress.Postal);
					cmd.Parameters.AddWithValue("@ip_addr", ip_addr ?? "");

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't add the agency");

                }


				// Pull the item data out of the database
                AgencyId = Library.lastInsertId(my_dbh);

				// Save the agency unique auth0 identifier
				using(MySqlCommand cmd = new MySqlCommand())
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

            }

            return true;

        }





        /**
         * Update the account
         */
        public bool update()
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
					cmd.CommandText = "UPDATE agency SET " +
                        "agency_name=@a1, " +
                        "user_real_name=@a2, " +
                        "user_email_address=@a3, " +
                        "agency_contact_name=@a4, " +
                        "agency_contact_email=@a5, " +
                        "agency_url=@a6, " +
                        "agency_phone=@a7, " +
                        "agency_fax=@a8, " +
                        "agency_about_text=@a9, " +
                        "billing_address_1=@a10, " +
                        "billing_address_2=@a11, " +
                        "billing_city=@a12, " +
                        "billing_state=@a13, " +
                        "billing_country=@a14, " +
                        "billing_postal=@a15, " +
                        "shipping_address_1=@a16, " +
                        "shipping_address_2=@a17, " +
                        "shipping_city=@a18, " +
                        "shipping_state=@a19, " +
                        "shipping_country=@a20, " +
                        "shipping_postal=@a21, " +
                        "updated=NOW() " +
                        "WHERE agency_id=@id";
					cmd.Parameters.AddWithValue("@a1", AgencyName);
					cmd.Parameters.AddWithValue("@a2", UserRealName);
					cmd.Parameters.AddWithValue("@a3", UserEmailAddress);
					cmd.Parameters.AddWithValue("@a4", AgencyContactName);
					cmd.Parameters.AddWithValue("@a5", AgencyContactEmail);
					cmd.Parameters.AddWithValue("@a6", AgencyUrl);
					cmd.Parameters.AddWithValue("@a7", AgencyPhone);
					cmd.Parameters.AddWithValue("@a8", AgencyFax);
					cmd.Parameters.AddWithValue("@a9", AgencyAboutText);
					cmd.Parameters.AddWithValue("@a10", BillingAddress.Address1);
					cmd.Parameters.AddWithValue("@a11", BillingAddress.Address2);
					cmd.Parameters.AddWithValue("@a12", BillingAddress.City);
					cmd.Parameters.AddWithValue("@a13", BillingAddress.State);
					cmd.Parameters.AddWithValue("@a14", BillingAddress.Country);
					cmd.Parameters.AddWithValue("@a15", BillingAddress.Postal);
					cmd.Parameters.AddWithValue("@a16", ShippingAddress.Address1);
					cmd.Parameters.AddWithValue("@a17", ShippingAddress.Address2);
					cmd.Parameters.AddWithValue("@a18", ShippingAddress.City);
					cmd.Parameters.AddWithValue("@a19", ShippingAddress.State);
					cmd.Parameters.AddWithValue("@a20", ShippingAddress.Country);
					cmd.Parameters.AddWithValue("@a21", ShippingAddress.Postal);
					cmd.Parameters.AddWithValue("@id", AgencyId);

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't update the agency");

                }

            }

            return true;

        }




        /**
         * Add a logo to an agency, uploading it to DreamObjects/S3 in the process
         * @param string The logo, base 64 encoded
         * @return bool Success?
         */
        public void saveLogo(string encodedLogo)
        {
            // Parse out the image data, and get ready to commit it to DreamObjects
            byte[] decodedLogo;
            string decodedLogoMd5;

            if( encodedLogo.IndexOf("data:image/png;base64,") == 0 )
            {
                // Decode the logo, and get an mp3 for the s3 file name
                decodedLogo = Convert.FromBase64String( encodedLogo.Replace("data:image/png;base64,", ""));

                // And get the md5dum for the logo
                using (IncrementalHash hasher = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
                {
                    hasher.AppendData(decodedLogo);
                    hasher.AppendData(Encoding.ASCII.GetBytes(UserEmailAddress));
                    decodedLogoMd5 = BitConverter.ToString(hasher.GetHashAndReset()).Replace("-", "");

                    // And upload the logo to S3
                    SearchProcurement.AWS.S3 s = new SearchProcurement.AWS.S3();
                    
                    Console.WriteLine(Defines.s3Bucket);
                    Console.WriteLine(Defines.s3LogoPath + "/" + decodedLogoMd5 + ".png");

                    AgencyLogo = s.UploadBuffer(decodedLogo, Defines.s3Bucket, Defines.s3LogoPath + "/" + decodedLogoMd5 + ".png");

                }

                // Set up the database connection, there has to be a better way!
                using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
                {
                    // Open the DB connection
                    my_dbh.Open();

                    // And save the agency logo to the database
                    using(MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = my_dbh;
                        cmd.CommandText = "UPDATE agency SET agency_logo_url = @url WHERE agency_id = @id";
                        cmd.Parameters.AddWithValue("@url", AgencyLogo);
                        cmd.Parameters.AddWithValue("@id", AgencyId);
                        cmd.Prepare();
                        cmd.ExecuteNonQuery();
                    }

                }


            }
            else
                throw new System.ArgumentException("Not base 64-encoded image!!");


        }




        /**
         * Remove the logo from an agency
         * @return none
         */
        public void removeLogo()
        {
            // Make sure we have a logo to delete...
            if( AgencyLogo != "" )
            {
                // Set up the database connection, there has to be a better way!
                using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
                {
                    // Open the DB connection
                    my_dbh.Open();

                    // And save the agency logo to the database
                    using(MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = my_dbh;
                        cmd.CommandText = "UPDATE agency SET agency_logo_url = NULL WHERE agency_id = @id";
                        cmd.Parameters.AddWithValue("@id", AgencyId);
                        cmd.Prepare();
                        cmd.ExecuteNonQuery();
                    }

                }

                // And, delete the object from s3.  First, get the object name from the logo URL
                string objectName = AgencyLogo.Split('/').Last();

                // And upload the logo to S3
                SearchProcurement.AWS.S3 s = new SearchProcurement.AWS.S3();
                s.Delete(Defines.s3Bucket, objectName);

            }

        }









        /**
         * Load up active listings
         * @return array The active listings
         */
        public Listing[] getActiveListings()
        {
            return ListingHelper.loadListings(AgencyId, new[]{ListingStatus.Published, ListingStatus.Open, ListingStatus.Draft});
        }


        /**
         * Load up inactive listings
         * @return array The active listings
         */
        public Listing[] getInactiveListings()
        {
            return ListingHelper.loadListings(AgencyId, new[]{ListingStatus.Closed, ListingStatus.Canceled, ListingStatus.Disabled});
        }





        /**
         * Check to see if this email address exists in the agency table
         * @param email The email to check
         * @return bool If it exists, return true
         */
        public static bool emailExists(string email)
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
					cmd.CommandText = "SELECT COUNT(*) " +
                        "FROM agency AS a " +
                        "WHERE user_email_address = @e";
					cmd.Parameters.AddWithValue("@e", email);
					cmd.Prepare();

					// Run the DB command
                    return Convert.ToBoolean(cmd.ExecuteScalar());

                }
            }
            
        }
    }

}