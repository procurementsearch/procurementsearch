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
         * Turn a string of departments into the array of strings
         * @param string departments The input we get from the textarea
         * @return nothing
         */
        public void setDepartmentsFromString(string my_departments)
        {
            my_departments = my_departments.Trim();
            Departments = my_departments.Split('\n').Select(p => p.Trim()).ToArray();
        }


        /**
         * Turn the array of strings into the single string for departments
         * @return string A string of departments
         */
        public string getStringFromDepartments()
        {
            if( Departments.Length > 0 )
                return String.Join("\n", Departments);
            else
                return "";
        }






        /**
         * Save the departments for an agency
         * @return nothing
         */
        public void saveDepartments()
        {
            // Set up the database connection, there has to be a better way!
            using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
            {
                my_dbh.Open();
                using(MySqlCommand cmd = new MySqlCommand())
                {
                    // cmd.Connection = my_dbh;
                    // cmd.CommandText = "UPDATE agency SET agency_logo_url = @url WHERE agency_id = @id";
                    // cmd.Parameters.AddWithValue("@url", Defines.AppSettings.UploadStorageUrl + Defines.UploadLogoPath + "/" + myLogo);
                    // cmd.Parameters.AddWithValue("@id", AgencyId);
                    // cmd.Prepare();
                    // cmd.ExecuteNonQuery();
                }
            }
        }




        /**
         * Load departments from the database
         * @return none
         */
        public void loadDepartments()
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

    }

}