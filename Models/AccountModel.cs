using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
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
         * Add the account to the database
         */
        public bool add()
        {
            Console.WriteLine("\n\n");
            Console.WriteLine("User name = " + UserRealName);
            Console.WriteLine("User email = " + UserEmailAddress);
            Console.WriteLine("Agency Type = " + AgencyType);

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
                    decodedLogoMd5 = hasher.GetHashAndReset();
                }

                Console.WriteLine("Agency Logo md5 = " + Convert.ToBase64String(decodedLogoMd5));

            }

            Console.WriteLine("\n\n");

            return true;

			// Set up the database connection, there has to be a better way!
/*			using(MySql.Data.MySqlClient.MySqlConnection my_dbh = new MySqlConnection())
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
*/
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