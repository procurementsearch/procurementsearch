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
         * Does this location have a payment token (unlimited, or unused
         * one-time use) to the specified state?
         * @param int locId The location ID
         * @return bool Can they post here?
         */
        public bool hasAvailablePaymentToken(int locId)
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
                        "FROM agency_payment_token " +
                        "WHERE agency_id = @id " +
                        "AND ((location_id = @locId AND token_type = 'unlimited' AND CURDATE() <= token_expires) " +
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
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
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
                            "(location_id = @locId AND token_type = 'unlimited' AND CURDATE() <= token_expires) " +
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
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
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
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
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

    }

}