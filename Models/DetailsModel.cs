using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

using SteveHavelka.SimpleFTS;

namespace SearchProcurement.Models
{
    /* The definition of an attachment */
	public struct attachment
	{
		public string Url;
		public string Title;
		public string Filetype;
	}

	/* The details of the embedded iframe */
	public struct frameDetails
	{
		public int sourceId;
		public string contents;
		public string rawContents;
		public string redirectUrl;
	}


	public class Details
	{
		public string sourceName { get; private set; }
		public string title { get; private set; }
		public int id { get; private set; }
		public string actionSteps { get; private set; }

		/* The attachments */
		public attachment[] attachments { get; private set; }


		/**
		 * The procurement listing details class constructor
		 *
		 * @param my_id  The listing ID to retreive details for
		 */
		public Details(int my_id)
		{
			id = my_id;

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
					cmd.CommandText = "select s.source_name, " + // 0
                        "l.title, " + // 1
                        "s.action_steps_text, " + // 2
	                    "l.origin_id, " + // 3
                        "l.origin_url, " + // 4
                        "l.origin_opportunity_no, " + // 5
                        "l.contact, " + // 6
                        "s.show_attachments " + // 7
                        "from listing as l left join source as s on l.source_id = s.source_id " +
	                    "where l.listing_id = @id";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
						r.Read();
	
						// Store the item data
						sourceName = r.GetString(0);
						title = r.GetString(1);
						if(r.IsDBNull(2))
						{
							actionSteps = "";
						}
						else
						{
							actionSteps = r.GetString(2).
								Replace("%TITLE%", title).
								Replace("%ORIGIN_ID%", r.GetString(3)).
								Replace("%ORIGIN_OPPORTUNITY_NO%", r.IsDBNull(5) ? "" : r.GetString(5)).
								Replace("%ORIGIN_URL%", r.IsDBNull(4) ? "" : r.GetString(4)).
								Replace("%CONTACT%", r.IsDBNull(6) ? "" : r.GetString(6));
						}
	
						// And, get the attachments if the source wants to show them!
                        if( r.GetInt32(7) == 1 )
    						attachments = loadAttachments(my_id);
                        else
                            attachments = new attachment[] {};

					}

				}

			}

		}


		/**
		 * Load the embedded frame data for the procurement object
		 *
		 * @return the frameDetails structure
		 */
		public frameDetails loadFrameData()
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
					cmd.CommandText = "select l.source_id, l.contents, l.raw_contents, l.origin_url, s.redirect_url_suffix " +
                        "from listing as l left join source as s on l.source_id = s.source_id " +
                        "where listing_id = @id";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
						r.Read();

						// And load the data into the frame
						frameDetails f;
						f.sourceId = r.GetInt32(0);
						f.contents = r.GetString(1);
						f.rawContents = r.IsDBNull(2) ? "" : r.GetString(2);
						f.redirectUrl = r.IsDBNull(3) ? "" : r.GetString(3);

                        // And, if we have a url suffix for the origin URL (e.g. for analytics/UTM tagging)
                        if( !r.IsDBNull(4) )
                            f.redirectUrl += r.GetString(4);

						return f;
					}
				}
			}
		}


		/**
		 * Load the attachments for a given procurement listing
		 *
		 * @param id  The listing ID to retrieve attachments for
		 * @return The attachments for the procurement listing
		 */
		private attachment[] loadAttachments(int id)
		{
			List<attachment> adata = new List<attachment>();

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
					cmd.CommandText = "select a.title, a.filetype, a.url from attachment as a " +
	                    "where a.listing_id = @id and a.is_hidden = 0";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
						while(r.Read())
						{
							// And load the data into the attchment
							attachment a;
							a.Title = r.GetString(0);
							a.Url = r.IsDBNull(2) ? "" : r.GetString(2);
							a.Filetype = r.GetString(1);
							adata.Add(a);
						}

            			// and we're done, we have attachments
            			return adata.ToArray();
					}
				}
			}

		}

	}
}