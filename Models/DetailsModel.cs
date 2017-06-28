using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

using SearchProcurement.Helpers;

namespace SearchProcurement.Models
{
    /* The definition of an attachment */
	public struct attachment
	{
		public string Url;
		public string Title;
		public string Filetype;
		public string Snippet;
		public bool exactMatch;
	}

	/* The details of the embedded iframe */
	public struct frameDetails
	{
		public int sourceId;
		public string contents;
		public string rawContents;
		public string redirectUrl;
	}

	/* Details for subcontracts */
	public struct subDetails
	{
		public int id;
		public string title;
	}


	public class Details
	{
		public string sourceName { get; private set; }
		public string title { get; private set; }
		public string subtitle { get; private set; }
		public int id { get; private set; }
		public int parentId { get; private set; }
		public string actionSteps { get; private set; }

		/* The attachments */
		public attachment[] attachments { get; private set; }
		public attachment[] snippets { get; private set; }

		/* Subcontracts */
		public subDetails[] subcontracts { get; private set; }


		/**
		 * The procurement listing details class constructor
		 *
		 * @param my_id  The listing ID to retreive details for
		 */
		public Details(int my_id, string kw)
		{
			id = my_id;

			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT a.agency_name, " + // 0
                        "l.title, " + // 1
                        "a.agency_action_steps, " + // 2
	                    "l.origin_id, " + // 3
                        "l.origin_url, " + // 4
                        "l.origin_opportunity_no, " + // 5
                        "l.contact, " + // 6
                        "a.is_attachment_visible, " + // 7
						"l.listing_parent_id " + // 8
                        "FROM listing AS l LEFT JOIN agency AS a ON l.agency_id = a.agency_id " +
	                    "WHERE l.listing_id = @id";
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

						// Has a parent ID?  If so...
						if( !r.IsDBNull(8) )
						{
							subtitle = DetailsHelper.loadTitle(r.GetInt32(8));
							parentId = r.GetInt32(8);
						}

						// And, get the attachments if the source wants to show them or
						// if we're showing some matching search terms
						if( r.GetInt32(7) == 1 )
							attachments = loadAttachments(my_id);
						else
							attachments = new attachment[] {};

						if( kw != null )
    						snippets = loadSnippets(my_id, kw);
						else
							snippets = new attachment[] {};


						// Last, do we have subcontracts?
						int[] subIds = DetailsHelper.findSubcontractIds(id);
						if( subIds.Length != 0 )
						{
							// Make a list of subcontract titles/ids
							List<subDetails> subs = new List<subDetails>();

							foreach( int i in subIds )
							{
								subDetails sub = new subDetails {};
								sub.id = i;
								sub.title = DetailsHelper.loadTitle(i);
								subs.Add(sub);
							}

							// And save the subcontract details to the details array
							subcontracts = subs.ToArray();

							// Sort the subcontracts alphabetically
							Array.Sort(subcontracts, delegate(subDetails a, subDetails b)
							{
								return a.title.CompareTo(b.title);
							});

						}
						else
							subcontracts = new subDetails[] {};

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
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT l.agency_id, l.contents, l.raw_contents, l.origin_url, a.feed_redirect_url_suffix " +
                        "FROM listing AS l LEFT JOIN agency AS a ON l.agency_id = a.agency_id " +
                        "WHERE listing_id = @id";
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
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT a.title, a.filetype, a.url, a.redirect_url FROM attachment AS a " +
	                    "WHERE a.listing_id = @id AND a.is_hidden = 0";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
						while(r.Read())
						{
							// And load the data into the attchment
							attachment a = new attachment{};
							a.Title = r.GetString(0);

							// Load the attachment URLs, but make sure we catch redirect URLs first
							if( !r.IsDBNull(3) )
								a.Url = r.GetString(3);
							else
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



		/**
		 * Load the attachments for a given procurement listing,
		 * and look for the given keywords, to show the snippets.
		 *
		 * @param id  The listing ID to retrieve attachments for
		 * @return The attachments for the procurement listing
		 */
		private attachment[] loadSnippets(int id, string kw)
		{
			List<attachment> adata = new List<attachment>();

			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.myConnectionString), my_sph = new MySqlConnection(Defines.myConnectionString))
			{
				// Open the DB and Sphinx connections
				my_dbh.Open();
				my_sph.Open();

				// Pull the item data out of the database
				using(MySqlCommand cmd = new MySqlCommand(), sph = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT a.attachment_id, a.title, a.text FROM attachment AS a WHERE a.listing_id = @id";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Prepare();

					// set up the Sphinx ..
					sph.Connection = my_sph;
					sph.CommandText = "SELECT SPHINX_SNIPPETS(@attachment_file, @idx, @kw, true AS load_files, true AS allow_empty);";
					sph.Parameters.Add(new MySqlParameter("@attachment_file", ""));
					sph.Parameters.AddWithValue("@idx", Defines.mySphinxIndex);
					sph.Parameters.AddWithValue("@kw", kw);

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
						while(r.Read())
						{
							// And load the data into the attchment
							attachment a = new attachment {};
							a.Title = r.GetString(1);

							// Rarely, we do have a null text field
							if( r.IsDBNull(2) )
								continue;

							// Prepare the snippet
							int attachment_id = r.GetInt32(0);

							sph.Parameters["@attachment_file"].Value = attachment_id + ".txt";
							sph.Prepare();

							// Pull out the result from the snippet .. if it's empty,
							// this attachment does not contain anything matching, and we
							// discard it
							using(MySqlDataReader ar = sph.ExecuteReader())
							{
								// Read the line item
								ar.Read();
								if( ar.IsDBNull(0) )
									continue;

								string atext = ar.GetString(0);
								if( atext == "" )
									continue;

								// And we're done, we have our snippet
								a.Snippet = atext;
								adata.Add(a);
							}

						}

            			// and we're done, we have attachments
            			return adata.ToArray();
					}
				}
			}

		}

	}
}