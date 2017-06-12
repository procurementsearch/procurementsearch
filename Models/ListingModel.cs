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

    public enum ListingStatus
    {
        AddNow,
        SaveForLater
    }


	public class Listing
    {

        public int ListingId;
        public int AgencyId;

        // The location IDs for where this listing will appear
        public int[] LocationIds;

        [Display(Name="Listing Title")]
        public string Title { get; set; }
        [Display(Name="Listing Description")]
        public string Description { get; set; }
        [Display(Name="Open Date - when the listing should go live")]
        public DateTime OpenDate { get; set; }
        [Display(Name="Bids Due - when the listing closes")]
        public DateTime CloseDate { get; set; }
        [Display(Name="Contact Information")]
        public string Contact { get; set; }
        [Display(Name="Listing Action Steps")]
        public string ActionSteps { get; set; }

        // The bid documents
        public Attachment[] BidDocuments { get; set; }




        /**
         * Add a listing to the database
         * @param ListingStatus status The status of the listing--are we adding it now, or saving it for later?
         * @param string ip_addr The remote IP adding this listing
         * @return none
         */
        public void add(ListingStatus status, string ip_addr)
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
					cmd.CommandText = "INSERT INTO listing " +
                        "(listing_type, source_id, open_date, close_date, title, description, " +
                        "contents, contact, action_steps, status, created, created_ip_addr) VALUES (" +
                        "'rfp', @l1, @l2, @l3, @l4, @l5, " +
                        "@l6, @l7, @l8, @l9, now(), @ip_addr)";
					cmd.Parameters.AddWithValue("@l1", AgencyId);
					cmd.Parameters.AddWithValue("@l2", OpenDate);
					cmd.Parameters.AddWithValue("@l3", CloseDate);
					cmd.Parameters.AddWithValue("@l4", Title);
					cmd.Parameters.AddWithValue("@l5", Description);
					cmd.Parameters.AddWithValue("@l6", Title + "\n" + Description);
					cmd.Parameters.AddWithValue("@l7", Contact);
					cmd.Parameters.AddWithValue("@l8", ActionSteps);
					cmd.Parameters.AddWithValue("@l9", status == ListingStatus.AddNow ? "waiting" : "inprogress");
					cmd.Parameters.AddWithValue("@ip_addr", ip_addr ?? "");

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't add the listing");

                }

            }

        }




        /**
         * Assign a listing to a location
         * @param int locId The location ID
         * @return none
         */
        public void addLocationById(int locId)
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
					cmd.CommandText = "REPLACE INTO location_listing_join " +
                        "(location_id, listing_id) VALUES (@l1, @l2)";
					cmd.Parameters.AddWithValue("@l1", locId);
					cmd.Parameters.AddWithValue("@l2", ListingId);

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't assign the listing to the location");

                }

            }

        }


    }




}