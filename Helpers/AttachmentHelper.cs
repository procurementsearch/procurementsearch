using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;

using SearchProcurement.AWS;
using SearchProcurement.Models;

namespace SearchProcurement.Helpers
{
	/* The definitive attachment class definition */
    public class Attachment
    {
        public int AttachmentId;
        public string DocumentName;
        public string FileName;
        public string Url;
        public string RedirectUrl;
        public string Guid;
        public bool IsStaged;
        public bool ToDelete;
    }





    public class AttachmentHelper
    {

        /**
         * Get the deletion identifier for the requested attachment
         * @param int attId The attachment ID
         * @return string The deletion identifier
         */
		public static string getDeletionIdentifier(int attId)
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
					cmd.CommandText = "SELECT deletion_identifier FROM attachment WHERE attachment_id = @id";
					cmd.Parameters.AddWithValue("@id",attId);
					cmd.Prepare();
                    return Convert.ToString(cmd.ExecuteScalar());
                }
            }
		}



        /**
         * Delete an attachment, by attachment ID
         * @param int attId The attachment ID
         * @return none
         */
		public static void deleteById(int attId)
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
					cmd.CommandText = "DELETE FROM attachment WHERE attachment_id = @id";
					cmd.Parameters.AddWithValue("@id",attId);
					cmd.Prepare();
                    cmd.ExecuteScalar();
                }
            }
		}




		/**
		 * Remove a file attachment
		 * @param bool isStaged Is this on our disk or S3?
		 * @param string deletionIdentifier The file path or S3 object name
		 * @return none
		 */
		public static void removeAttachmentFile(bool isStaged, string deletionIdentifier)
		{
			if( isStaged )
				// Has an ID, is staged, it's still on disk, but we don't have its name handy
				System.IO.File.Delete(Defines.UploadStoragePath + "/" + deletionIdentifier);
			else
			{
				// Has an ID, not staged, it's been moved to S3
				S3 s3 = new S3();
				s3.Delete(Defines.s3Bucket, Defines.s3AttachmentPath + "/" + deletionIdentifier);
			}

		}





        /**
         * Update the redirect URL for an attachment
         * @param int id The attachment ID
         * @param string url The new redirect URL
         * @return none
         */
		public static void updateRedirectUrl(int id, string url)
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
					cmd.CommandText = "UPDATE attachment SET redirect_url = @url WHERE attachment_id = @id";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Parameters.AddWithValue("@url", url);
					cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
            }
		}




        /**
         * Get a list of attachment titles, for generating addendum diffs
         * @param int listId The listing ID
         * @return string[] The titles
         */
		public static string[] getAttachmentTitles(int listId)
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
					cmd.CommandText = "SELECT title FROM attachment WHERE listing_id = @id";
					cmd.Parameters.AddWithValue("@id",listId);
					cmd.Prepare();

					// Run the DB command
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
                        // Do we have any rows here?
                        if( r.HasRows )
                        {
							List <string>titles = new List<string>();

                            // Yes!  Ok!  Let's go!
                            while( r.Read() )
								titles.Add(r.GetString(0));

							return titles.ToArray();
						}
						else
							// No rows, return an empty string array
							return new String[] {};
					}
                }
            }
		}




		/**
		 * Process the uploaded attachments for a listing
		 * @param Attachment[] files The array of attachments to proces
		 * @param HttpContext h The HTTP Context that has the form with our redirect URLs in it
		 * @param Listing l The listing to attach these files to (or remove from)
		 * @return none
		 */
		public static void processFiles(Attachment[] files, HttpContext h, Listing l)
		{
			// Process all the files
			foreach (Attachment att in files)
			{
				string redirectUrl = h.Request.Form["redir-" + att.Guid];

				// If it hasn't got an ID, we want to save it
				if( att.AttachmentId == 0 )
				{
					// Add the attachment
					Attachment myAtt = att;
					myAtt.RedirectUrl = redirectUrl;
					l.addAttachment(myAtt);
				}
				else
				{
					// Or...possibly...delete it
					if( att.ToDelete )
					{
						// Remove the file attachment
						AttachmentHelper.removeAttachmentFile(
							att.IsStaged,
							AttachmentHelper.getDeletionIdentifier(att.AttachmentId)
						);
						AttachmentHelper.deleteById(att.AttachmentId);
					}
					else
						// Otherwise, just update its redirect URL
						AttachmentHelper.updateRedirectUrl(att.AttachmentId, redirectUrl);
				}
			}

		}




    }


}
