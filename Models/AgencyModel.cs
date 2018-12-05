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
	public partial class Agency {

        public int AgencyId;
        public AgencyTeam MyLogin;

        // The agency information
        [Display(Name="Your Agency Name")]
        public string AgencyName { get; set; }
        public string AgencyType { get; set; }
        [Display(Name="Your Agency Website")]
        public string AgencyUrl { get; set; }

        // Agency logo
        public string AgencyLogo { get; set; }

        // The text blocks the agency uses throughout
        [Display(Name="This text appears on the search results page for your organization.")]
        public string AgencyOverviewText { get; set; }

        [Display(Name="This text appears on every one of your solicitation pages, under the \"About this Agency\" section.")]
        public string AgencyAboutText { get; set; }

        [Display(Name="These are the solicitation action steps that are shown by default on your solicitations.  If you enter specific instructions for a specific solicitation, we don't show this for that solicitation.")]
        public string AgencyDefaultActionSteps { get; set; }

        // Contact information for the organization
        [Display(Name="Contact Person")]
        public string AgencyContactName { get; set; }
        [Display(Name="Contact Email")]
        public string AgencyContactEmail { get; set; }
        [Display(Name="Phone Number")]
        public string AgencyPhone { get; set; }
        [Display(Name="Fax Number")]
        public string AgencyFax { get; set; }

        // Departments and Categories available to this agency
        public string[] Departments { get; set; }
        public string[] Categories { get; set; }


        // Agency addresses
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
            if( AgencyHelper.isKnownLogin(uniq) )
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
                        cmd.CommandText = "SELECT a.agency_name, " + // 0
                            "a.agency_type, " +                      // 1
                            "a.agency_contact_name, " +              // 2
                            "a.agency_contact_email, " +             // 3
                            "a.agency_url, " +                       // 4
                            "a.agency_logo_url, " +                  // 5
                            "a.agency_phone, " +                     // 6
                            "a.agency_fax, " +                       // 7
                            "a.agency_overview_text, " +             // 8
                            "a.agency_about_text, " +                // 9
                            "a.agency_default_action_steps, " +      // 10
                            "a.billing_address_1, " +                // 11
                            "a.billing_address_2, " +                // 12
                            "a.billing_city, " +                     // 13
                            "a.billing_state, " +                    // 14
                            "a.billing_country, " +                  // 15
                            "a.billing_postal, " +                   // 16
                            "a.shipping_address_1, " +               // 17
                            "a.shipping_address_2, " +               // 18
                            "a.shipping_city, " +                    // 19
                            "a.shipping_state, " +                   // 20
                            "a.shipping_country, " +                 // 21
                            "a.shipping_postal, " +                  // 22
                            "a.agency_id " +                         // 23
                            "FROM agency AS a " +
                            "LEFT JOIN agency_team AS al ON al.agency_id = a.agency_id " +
                            "WHERE al.uniqueidentifier = @uniq";
                        cmd.Parameters.AddWithValue("@uniq", uniq);

                        // Run the DB command
                        using(MySqlDataReader r = cmd.ExecuteReader())
                        {
                            if( r.HasRows )
                            {
                                r.Read();

                                // Store the agency data
                                MyLogin = new AgencyTeam(uniq);
                                AgencyId = r.GetInt32(23);
                                AgencyName = r.GetString(0);
                                AgencyType = r.GetString(1);
                                AgencyContactName = r.IsDBNull(2) ? null : r.GetString(2);
                                AgencyContactEmail = r.IsDBNull(3) ? null : r.GetString(3);
                                AgencyUrl = r.IsDBNull(4) ? null : r.GetString(4);
                                AgencyLogo = r.IsDBNull(5) ? null : r.GetString(5);
                                AgencyPhone = r.IsDBNull(6) ? null : r.GetString(6);
                                AgencyFax = r.IsDBNull(7) ? null : r.GetString(7);
                                AgencyOverviewText = r.IsDBNull(8) ? null : r.GetString(8);
                                AgencyAboutText = r.IsDBNull(9) ? null : r.GetString(9);
                                AgencyDefaultActionSteps = r.IsDBNull(10) ? null : r.GetString(10);
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
        }






        /**
         * Add the account to the database
         */
        public bool add(string ip_addr)
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
					cmd.CommandText = "INSERT INTO agency " +
                        "(agency_name, agency_type, agency_contact_name, agency_contact_email, " +
                        "agency_url, agency_phone, agency_fax, " +
                        "agency_overview_text, agency_about_text, agency_default_action_steps, " +
                        "billing_address_1, billing_address_2, billing_city, billing_state, billing_country, billing_postal, " +
                        "shipping_address_1, shipping_address_2, shipping_city, shipping_state, shipping_country, shipping_postal, " +
                        "created, created_ipaddr, updated) VALUES (" +
                        "@a1, @a2, @a3, @a4, " +
                        "@a5, @a6, @a7, " +
                        "@a8, @a9, @a10, " +
                        "@a11, @a12, @a13, @a14, @a15, @a16, " +
                        "@a17, @a18, @a19, @a20, @a21, @a22, " +
                        "now(), @ip_addr, now())";
					cmd.Parameters.AddWithValue("@a1", AgencyName);
					cmd.Parameters.AddWithValue("@a2", AgencyType);
					cmd.Parameters.AddWithValue("@a3", AgencyContactName);
					cmd.Parameters.AddWithValue("@a4", AgencyContactEmail);
					cmd.Parameters.AddWithValue("@a5", AgencyUrl);
					cmd.Parameters.AddWithValue("@a6", AgencyPhone);
					cmd.Parameters.AddWithValue("@a7", AgencyFax);
					cmd.Parameters.AddWithValue("@a8", AgencyOverviewText);
					cmd.Parameters.AddWithValue("@a9", AgencyAboutText);
					cmd.Parameters.AddWithValue("@a10", AgencyDefaultActionSteps);
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
					cmd.CommandText = "UPDATE agency SET " +
                        "agency_name=@a1, " +
                        "agency_contact_name=@a2, " +
                        "agency_contact_email=@a3, " +
                        "agency_url=@a4, " +
                        "agency_phone=@a5, " +
                        "agency_fax=@a6, " +
                        "agency_overview_text=@a7, " +
                        "agency_about_text=@a8, " +
                        "agency_default_action_steps=@a9, " +
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
					cmd.Parameters.AddWithValue("@a2", AgencyContactName);
					cmd.Parameters.AddWithValue("@a3", AgencyContactEmail);
					cmd.Parameters.AddWithValue("@a4", AgencyUrl);
					cmd.Parameters.AddWithValue("@a5", AgencyPhone);
					cmd.Parameters.AddWithValue("@a6", AgencyFax);
					cmd.Parameters.AddWithValue("@a7", AgencyOverviewText);
					cmd.Parameters.AddWithValue("@a8", AgencyAboutText);
					cmd.Parameters.AddWithValue("@a9", AgencyDefaultActionSteps);
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
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
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