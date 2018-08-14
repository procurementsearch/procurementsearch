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

	public partial class Agency {

        /**
         * Add a logo to an agency, uploading it to DreamObjects/S3 in the process
         * @param string logoName The filename of the logo
         * @param string encodedLogo The logo, base 64 encoded
         * @return bool Success?
         */
        public void saveLogo(string logoName, string encodedLogo)
        {
            // Parse out the image data, and get ready to commit it to DreamObjects
            if( encodedLogo.IndexOf("data:image/png;base64,") == 0 )
            {
                // Decode the logo, and get an mp3 for the s3 file name
                byte[] decodedLogo = Convert.FromBase64String( encodedLogo.Replace("data:image/png;base64,", ""));
                string decodedLogoMd5;

                // And get the md5dum for the logo
                using (IncrementalHash hasher = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
                {
                    hasher.AppendData(decodedLogo);
                    hasher.AppendData(Encoding.ASCII.GetBytes(MyLogin.UserEmailAddress));
                    decodedLogoMd5 = BitConverter.ToString(hasher.GetHashAndReset()).Replace("-", "");
                }

                // Save the logo to the local logo repository
                string myLogo = Library.tidyString(logoName) + "-" + decodedLogoMd5 + ".png";

                System.IO.File.WriteAllBytes(Defines.AppSettings.UploadStoragePath + Defines.UploadLogoPath + "/" + myLogo, decodedLogo);

                // Set up the database connection, there has to be a better way!
                using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
                {
                    // Open the DB connection
                    my_dbh.Open();
                    using(MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = my_dbh;
                        cmd.CommandText = "UPDATE agency SET agency_logo_url = @url WHERE agency_id = @id";
                        cmd.Parameters.AddWithValue("@url", Defines.AppSettings.UploadStorageUrl + Defines.UploadLogoPath + "/" + myLogo);
                        cmd.Parameters.AddWithValue("@id", AgencyId);
                        cmd.Prepare();
                        cmd.ExecuteNonQuery();
                    }
                }

                // And save the logo back to the object
                AgencyLogo = Defines.AppSettings.UploadStorageUrl + Defines.UploadLogoPath + "/" + myLogo;

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
                using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
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

                // And, delete the object from disk
                string logoName = AgencyLogo.Split('/').Last();
                System.IO.File.Delete(Defines.AppSettings.UploadStoragePath + Defines.UploadLogoPath + "/" + logoName);

            }

        }



        /**
         * Load just the logo for the agency
         * @return none
         */
        public void loadLogo()
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT agency_logo_url FROM agency WHERE agency_id = @id";
					cmd.Parameters.AddWithValue("@id", AgencyId);
					AgencyLogo = Convert.ToString(cmd.ExecuteScalar());
                }
            }
        }

    }

}