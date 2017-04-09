using System;
using System.Data;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

using SearchProcurement;

namespace SteveHavelka.SphinxFTS
{

    public class SphinxFTS
	{
		public int limit { get; set; } = 100;
		public int offset  { get; set; } = 0;

		public string words { get; private set; }

		public string searchString { get; private set; }
		public string searchUrl { get; private set; }
		public string resultCount { get; private set; }
	
		public string searchUrlSeparator { get; set; } = "&";
	
		public bool absoluteMatch  { get; set; } = true;
		public bool buildPartial  { get; set; } = false;
		public string kwTable  { get; set; } = "kw";
	
		/* for internal use */
		private string query { get; set; }



		/* Prepares the words based on a search string */
		public string setWords(string stg)
		{
            /*
             * Here's how we're cleaning up the input string for sending to sphinx:
             * - Remove everything that is not ", ', -, alnum, or whitespace
             * - Always escape '
             * - Unbalanced quotes?  Escape them all
             * - Balanced quotes?  Don't escape "
             * - Collapse and trim whitespace
             */
			stg = Regex.Replace(stg, @"[^""'\-\w ]+", " ").Trim();

			/* failsafe */
			if( stg.Length == 0 )
				return null;

            /* Preserve the cleaned string before escaping */
            searchString = stg;

            // Escape the punctuation characters now
            stg = stg.Replace("'", @"\'");
            if( (stg.Split('"').Length % 2) == 0 )
            {
                stg = stg.Replace(@"""", @"\\\\""");
            }

			/* ok, we've caught the empty string, so now we must have at least some words */
			words = stg;

			/* set the search URL */
			searchUrl = "kw=" + WebUtility.UrlEncode(words).Replace(" ", "+") + searchUrlSeparator;
			return words;
		}


		/*
		 * Return a count of search results for the given search query
		 */
		public int count()
		{
			/* failsafe */
			if( words == null || words.Length == 0 )
				return 0;

			using(MySql.Data.MySqlClient.MySqlConnection my_sph = new MySqlConnection())
			{
				// create the DB connection
			    my_sph.ConnectionString = Defines.myConnectionString;
			    my_sph.Open();

				/* Our database command */
				using(MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand())
				{
					cmd.Connection = my_sph;

					/* let's get a total possible count */
                    // SELECT COUNT(*) FROM search_pdx WHERE query='\\"concrete formwork\\"; groupby=attr:listing_id; mode=extended;';
					cmd.CommandText =
						"SELECT COUNT(*) FROM " + kwTable +
                        " WHERE query='" + words + "; groupby=attr:listing_id; mode=extended;'";
					cmd.Prepare();

					/* And we're done */
					return Convert.ToInt32(cmd.ExecuteScalar());

				}

			}

		}



		/*
		 * Return the search results for the given query
		 */
		public int[] search()
		{
			/* failsafe */
			if( words == null || words.Length == 0 )
				return null;

			using(MySql.Data.MySqlClient.MySqlConnection my_sph = new MySqlConnection())
			{
				// create the DB connection
			    my_sph.ConnectionString = Defines.myConnectionString;
			    my_sph.Open();

				/* Our database command */
				using(MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand())
				{
					cmd.Connection = my_sph;

					/* let's get a total possible count */
                    // select listing_id from search_pdx where query='\\"concrete formwork\\"; groupby=attr:listing_id; limit=20; groupsort=@weight desc; mode=extended;';
					cmd.CommandText =
						"SELECT listing_id FROM " + kwTable +
                        " WHERE query='" + words + "; groupby=attr:listing_id; limit=" + limit + "; offset=" + offset +
                        "; groupsort=@weight desc; mode=extended;'";
					cmd.Prepare();

					/* And read out our data */
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
						List<int> ids = new List<int>();
						while( r.Read() )
						{
							ids.Add(Convert.ToInt32(r.GetString(0)));
						}

						/* And we're done */
						return ids.ToArray();

					}

				}

			}

		}

	}

}
