using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;

using SearchProcurement.Helpers;

namespace SearchProcurement.Models
{


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

        // The listing status
        public string Status;

        // The listing type - simple or umbrella?
        public string Type;

        // The bid documents
        public Attachment[] BidDocuments { get; set; }

        // Does the listing have sublistings?  If so, stick 'em here.
        public Listing[] Sublistings { get; set; }

        // Alternately, does the listing have a parent ID?
        public int? ParentId;

        // The listing geographical regions
        public int PrimaryLocationId { get; set; }
        public int[] SecondaryLocationIds { get; set; }


        /**
         * Return the agency ID for a listing
         * @return int The agency ID
         */
        public int getAgencyId()
        {
            // Failsafe
            if( ListingId == 0 )
                throw new System.Exception("No listing ID means no whiskey!");

			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT source_id FROM listing " +
                        "WHERE listing_id = @id";
					cmd.Parameters.AddWithValue("@id", ListingId);

					// Run the DB command
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }




        /**
         * Add a listing to the database
         * @param ListingStatus status The status of the listing--are we adding it now, or saving it for later?
         * @param string ip_addr The remote IP adding this listing
         * @return none
         */
        public void add(string status, string type, string ip_addr)
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
					cmd.CommandText = "INSERT INTO listing " +
                        "(source_id, open_date, close_date, title, description, contents, " +
                        "contact, action_steps, status, listing_type, created, created_ip_addr) VALUES (" +
                        "@l1, @l2, @l3, @l4, @l5, " +
                        "@l6, @l7, @l8, @l9, @l10, now(), @ip_addr)";
					cmd.Parameters.AddWithValue("@l1", AgencyId);
					cmd.Parameters.AddWithValue("@l2", OpenDate == DateTime.MinValue ? null : OpenDate.ToString("yyyy-MM-dd hh:mm:ss"));
					cmd.Parameters.AddWithValue("@l3", CloseDate == DateTime.MinValue ? null : CloseDate.ToString("yyyy-MM-dd hh:mm:ss"));
					cmd.Parameters.AddWithValue("@l4", Title);
					cmd.Parameters.AddWithValue("@l5", Description);
					cmd.Parameters.AddWithValue("@l6", Title + "\n" + Description);
					cmd.Parameters.AddWithValue("@l7", Contact);
					cmd.Parameters.AddWithValue("@l8", ActionSteps);
					cmd.Parameters.AddWithValue("@l9", status);
					cmd.Parameters.AddWithValue("@l10", type);
					cmd.Parameters.AddWithValue("@ip_addr", ip_addr ?? "");

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't add the listing");

                    // And grab the listing ID
                    ListingId = Library.lastInsertId(my_dbh);

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
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
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



        /**
         * Remove a listing from a location
         * @param int locId The location ID
         * @return none
         */
        public void removeLocationById(int locId)
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
					cmd.CommandText = "DELETE FROM location_listing_join " +
                        "WHERE location_id = @l1 AND listing_id = @l2";
					cmd.Parameters.AddWithValue("@l1", locId);
					cmd.Parameters.AddWithValue("@l2", ListingId);

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't remove the listing from the location");

                }
            }
        }





        /**
         * Add an attachment to a listing
         * @param Attachment attach The attachment to add to the listing
         * @return none
         */
        public void addAttachment(Attachment a)
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
					cmd.CommandText = "INSERT INTO attachment " +
                        "(listing_id, title, filetype, url, redirect_url, deletion_identifier, is_staged) " +
                        "VALUES " +
                        "(@l1, @l2, @l3, @l4, @l5, @l6, 1)";
					cmd.Parameters.AddWithValue("@l1", ListingId);
					cmd.Parameters.AddWithValue("@l2", a.DocumentName);
					cmd.Parameters.AddWithValue("@l3", MimeTypes.MimeTypeMap.GetMimeType(Path.GetExtension(a.FileName)));
					cmd.Parameters.AddWithValue("@l4", a.Url);
					cmd.Parameters.AddWithValue("@l5", a.RedirectUrl);
					cmd.Parameters.AddWithValue("@l6", a.FileName);

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't add the attachment to the listing");

                }
            }
        }






        /**
         * Update the listing
         * @param string updateMode The update mode, revision..addendum..?
         * @return none
         */
        public bool update(string updateMode)
        {
            // Is this an addendum?  If it is, we need to capture the differences
            // across the five watched fields:  close_date, title, description,
            // contents, contact
            if( updateMode == ListingUpdateMode.Addendum )
            {
                // OK!
                Listing old = new Listing();
                old.loadById(ListingId);

                if( old.CloseDate != CloseDate )
                    ListingHelper.logAddendum(ListingId, "close_date", old.CloseDate.ToString(), CloseDate.ToString());
                if( old.Title != Title )
                    ListingHelper.logAddendum(ListingId, "title", old.Title, Title);
                if( old.Description != Description )
                    ListingHelper.logAddendum(ListingId, "description", old.Description, Description);
                if( old.Contact != Contact )
                    ListingHelper.logAddendum(ListingId, "contact", old.Contact, Contact);

            }


			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "UPDATE listing SET " +
                        "open_date=@l1, " +
                        "close_date=@l2, " +
                        "title=@l3, " +
                        "description=@l4, " +
                        "contents=@l5, " +
                        "contact=@l6, " +
                        "action_steps=@l7, " +
                        "status=@l8 " +
                        "WHERE listing_id=@id";
					cmd.Parameters.AddWithValue("@l1", OpenDate == DateTime.MinValue ? null : OpenDate.ToString("yyyy-MM-dd hh:mm:ss"));
					cmd.Parameters.AddWithValue("@l2", CloseDate == DateTime.MinValue ? null : CloseDate.ToString("yyyy-MM-dd hh:mm:ss"));
					cmd.Parameters.AddWithValue("@l3", Title);
					cmd.Parameters.AddWithValue("@l4", Description);
					cmd.Parameters.AddWithValue("@l5", Title + "\n" + Description);
					cmd.Parameters.AddWithValue("@l6", Contact);
					cmd.Parameters.AddWithValue("@l7", ActionSteps);
					cmd.Parameters.AddWithValue("@l8", Status);
                    cmd.Parameters.AddWithValue("@id", ListingId);

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't update the listing");
                }

                // And, if there are subcontracts, do those too...
                using(MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = my_dbh;
                    cmd.CommandText = "UPDATE listing SET " +
                        "open_date=@l1, " +
                        "close_date=@l2, " +
                        "status=@l3 " +
                        "WHERE listing_parent_id=@id";
					cmd.Parameters.AddWithValue("@l1", OpenDate == DateTime.MinValue ? null : OpenDate.ToString("yyyy-MM-dd hh:mm:ss"));
					cmd.Parameters.AddWithValue("@l2", CloseDate == DateTime.MinValue ? null : CloseDate.ToString("yyyy-MM-dd hh:mm:ss"));
                    cmd.Parameters.AddWithValue("@l3", Status);
                    cmd.Parameters.AddWithValue("@id", ListingId);
                    cmd.ExecuteNonQuery();
                }

            }

            return true;

        }



        /**
         * Set the parent ID for the listing
         * @param int parentId The ID of the parent listing to use
         * @return none
         */
        public void setParent(int myParentId)
        {
            // Load the parent ID to sync up open/close date, status, etc.
            Listing p = new Listing();
            p.loadById(myParentId);

			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "UPDATE listing SET " +
                        "listing_parent_id=@parentId, " +
                        "open_date=@l1, " +
                        "close_date=@l2, " +
                        "status=@l3 " +
                        "WHERE listing_id=@id";
					cmd.Parameters.AddWithValue("@l1", OpenDate == DateTime.MinValue ? null : OpenDate.ToString("yyyy-MM-dd hh:mm:ss"));
					cmd.Parameters.AddWithValue("@l2", CloseDate == DateTime.MinValue ? null : CloseDate.ToString("yyyy-MM-dd hh:mm:ss"));
					cmd.Parameters.AddWithValue("@l3", Status);
                    cmd.Parameters.AddWithValue("@parentId", myParentId);
                    cmd.Parameters.AddWithValue("@id", ListingId);

					// Run the DB command
                    if( cmd.ExecuteNonQuery() == 0 )
                        throw new System.ArgumentException("Couldn't update the listing!");

                }

            }


            // Now, sync up the location information
            if( PrimaryLocationId != 0 )
                removeLocationById(PrimaryLocationId);

            if( SecondaryLocationIds != null )
                foreach( var locId in SecondaryLocationIds )
                    removeLocationById(locId);

            // And add the locations from the parent
            addLocationById(p.PrimaryLocationId);

            foreach( var locId in p.SecondaryLocationIds )
                addLocationById(locId);

            // And that's it, we're done

        }









        /**
         * Load a single listing by listing ID
         * @param int id The listing ID
         * @return none
         */
        public void loadById(int id)
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
					cmd.CommandText = "SELECT source_id, " +
                        "title, " +
                        "description, " +
                        "open_date, " +
                        "close_date, " +
                        "contact, " +
                        "action_steps, " +
                        "status, " +
                        "listing_type, " +
                        "listing_parent_id " +
                        "FROM listing WHERE listing_id = @id";
					cmd.Parameters.AddWithValue("@id", id);

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
                        // Do we have any rows here?
                        if( r.HasRows )
                        {
                            // Yes!  Ok!  Let's go!
                            r.Read();
                            AgencyId = r.GetInt32(0);
                            Title = r.GetString(1);
                            Description = r.IsDBNull(2) ? "" : r.GetString(2);
                            OpenDate = r.IsDBNull(3) ? new DateTime() : r.GetDateTime(3);
                            CloseDate = r.IsDBNull(4) ? new DateTime() : r.GetDateTime(4);
                            Contact = r.IsDBNull(5) ? "" : r.GetString(5);
                            ActionSteps = r.IsDBNull(6) ? "" : r.GetString(6);
                            Status = r.GetString(7);
                            Type = r.GetString(8);

                            if( r.IsDBNull(9) )
                                ParentId = null;
                            else
                                ParentId = r.GetInt32(9);
                        }
                        else
                            throw new System.ArgumentException("Couldn't find the requested listing!");

                    }

                }

                // And next, set the listing ID
                ListingId = id;

				// Pull the listing locations out of the database
                loadLocations();

                // And pull out the attachments
                loadAttachments();

                // Go through the attachments and assign a GUID to each of them
                for (int i = 0; i < BidDocuments.Length; i++)
                    BidDocuments[i].Guid = Guid.NewGuid().ToString();

                // Load sublistings, if there are sublistings
                loadSublistings();

            }
        }






        /**
         * Load the location IDs
         * @return none
         */
        public void loadLocations()
        {
            // Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

				// Pull the listing locations out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
                    // Now pull out the locations for this entry .. first, pull out just the state.
                    // Everyone will have a state.
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT l.location_id FROM location_listing_join AS lj " +
                        "LEFT JOIN location AS l ON lj.location_id = l.location_id " +
                        "WHERE lj.listing_id = @id AND l.location_type = 'State'";
					cmd.Parameters.AddWithValue("@id", ListingId);

                    // And execute the query
                    PrimaryLocationId = Convert.ToInt32(cmd.ExecuteScalar());

                    // A failsafe
                    if( PrimaryLocationId == 0 )
                        throw new System.ArgumentException("Couldn't find a primary location for the requested listing!");
                }

				using(MySqlCommand cmd = new MySqlCommand())
				{
                    // Now pull out the locations for this entry .. first, pull out just the state.
                    // Everyone will have a state.
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT l.location_id FROM location_listing_join AS lj " +
                        "LEFT JOIN location AS l ON lj.location_id = l.location_id " +
                        "WHERE lj.listing_id = @id AND l.location_type <> 'State'";
					cmd.Parameters.AddWithValue("@id", ListingId);

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
                            SecondaryLocationIds = ids.ToArray();
                        }
                        else
                            // Initialize an empty list
                            SecondaryLocationIds = new int[] {};
                    }
                }
            }
        }






        /**
         * Load the attachments
         * @return none
         */
        public void loadAttachments()
        {
            // Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

				// Pull the listing locations out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
                    // Now pull out the locations for this entry .. first, pull out just the state.
                    // Everyone will have a state.
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT attachment_id, title, url, redirect_url, is_staged "+
                        "FROM attachment WHERE listing_id = @id";
					cmd.Parameters.AddWithValue("@id", ListingId);

                    // And execute the query
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
                        // Do we have any rows here?  Then we have additional locations (by design,
                        // at least right now, only one additional location)
                        if( r.HasRows )
                        {
                            List <Attachment>atts = new List<Attachment>();

                            while( r.Read() )
                            {
                                atts.Add(new Attachment
                                {
                                    AttachmentId = r.GetInt32(0),
                                    DocumentName = r.GetString(1),
                                    Url = r.IsDBNull(2) ? "" : r.GetString(2),
                                    RedirectUrl = r.IsDBNull(3) ? "" : r.GetString(3),
                                    IsStaged = Convert.ToBoolean(r.GetInt32(4))
                                });
                            }

                            // And assign the IDs to the location ID array
                            BidDocuments = atts.ToArray();
                        }
                        else
                            // Initialize an empty list
                            BidDocuments = new Attachment[] {};
                    }
                }
            }
        }





        /**
         * Load sublistings
         * @return none
         */
        public void loadSublistings()
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
					cmd.CommandText = "SELECT listing_id, title FROM listing " +
                        "WHERE listing_parent_id = @id ORDER BY title";
					cmd.Parameters.AddWithValue("@id", ListingId);

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
                        List<Listing> listings = new List<Listing>();

						while(r.Read())
						{
                            Listing l = new Listing
                            {
                                ListingId = r.GetInt32(0),
                                Title = r.GetString(1)
                            };
                            listings.Add(l);
                        }

                        // And we're done
                        Sublistings = listings.ToArray();

                    }
                }
            }
        }







        /**
         * Remove a listing and all its attachments
         * @return none
         */
        public void removeListing()
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

                using(MySqlTransaction tr = my_dbh.BeginTransaction())
                {
                    // Delete the attachment(s)
                    using(MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = my_dbh;
                        cmd.Transaction = tr;
                        cmd.CommandText = "DELETE FROM attachment WHERE listing_id = @id";
                        cmd.Parameters.AddWithValue("@id", ListingId);
                        cmd.ExecuteNonQuery();
                    }

                    // And the location(s)
                    using(MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = my_dbh;
                        cmd.Transaction = tr;
                        cmd.CommandText = "DELETE FROM location_listing_join WHERE listing_id = @id";
                        cmd.Parameters.AddWithValue("@id", ListingId);
                        cmd.ExecuteNonQuery();
                    }

                    // And the listing
                    using(MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = my_dbh;
                        cmd.Transaction = tr;
                        cmd.CommandText = "DELETE FROM listing WHERE listing_id = @id";
                        cmd.Parameters.AddWithValue("@id", ListingId);
                        cmd.ExecuteNonQuery();
                    }

                    // Commit the transaction
                    tr.Commit();
                }
            }
        }



    }

}