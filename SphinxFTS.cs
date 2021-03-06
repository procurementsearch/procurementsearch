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
		public string searchUrlEncoded { get; private set; }
		public string resultCount { get; private set; }
	
		public string searchUrlSeparator { get; set; } = "&";
	
		public bool absoluteMatch  { get; set; } = true;
		public bool buildPartial  { get; set; } = false;
		public string kwTable  { get; set; } = "kw";
		public int locationId { get; set; }

		public string sortBy;
		public string show;
		public int[] agencyLimit;

		/* for internal use */
		private string query { get; set; }
		public string lastUsedQuery { get; private set; }



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
			searchUrlEncoded = WebUtility.UrlEncode(searchUrl);
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

			using(MySqlConnection my_sph = new MySqlConnection())
			{
				// create the DB connection
			    my_sph.ConnectionString = Defines.AppSettings.myConnectionString;
			    my_sph.Open();

				/* Our database command */
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_sph;

					/* let's get a total possible count */
					// SELECT COUNT(*) FROM search_pdx WHERE query='\\"concrete formwork\\"; groupby=attr:listing_id; mode=extended;';
					cmd.CommandText =
						"SELECT SQL_NO_CACHE COUNT(*) FROM " + kwTable +
						" WHERE query='" + words + ";" +
						" groupby=attr:listing_id;" +
						" limit=1;" +
						(locationId != 0 ? " filter=location_id," + locationId + ";" : "" ) +
						(agencyLimit != null && agencyLimit.Length > 0 ? " filter=agency_id," + String.Join(",", agencyLimit) + ";" : "" ) +
						(show != null ? " filter=status," + show + ";" : "" ) +
						" mode=extended;'";
					cmd.Prepare();

					/* save this for debug capture */
					lastUsedQuery = cmd.CommandText;

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
			if( words == null || words.Length == 0 || limit == 0 )
				return null;

			using(MySqlConnection my_sph = new MySqlConnection())
			{
				// create the DB connection
			    my_sph.ConnectionString = Defines.AppSettings.myConnectionString;
			    my_sph.Open();

				/* Our database command */
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_sph;

					// GRR!!  Sphinx Search has a hard-coded limit on the size of data
					// it can return (similar to mysql_max_packet) in the source of
					// ha_sphinx.cc, so what we have to do is page through the results
					// on our own, here, up to the limit requested by the user.
					//
					// We'll use a limit of 10 results at a time, to make sure we never
					// exceed the sphinxse hard-coded limit.
					int my_limit = 5;
					int my_offset = offset;
					List<int> ids = new List<int>();

					for(;;)
					{
						/* let's get a total possible count */
						// select listing_id from search_pdx where query='\\"concrete formwork\\"; groupby=attr:listing_id; limit=20; groupsort=@weight desc; mode=extended;';
						cmd.CommandText =
							"SELECT SQL_NO_CACHE listing_id FROM " + kwTable +
							" WHERE query='" + words + ";" +
							" groupby=attr:listing_id;" +
							" limit=" + my_limit + ";" +
							" offset=" + my_offset + ";" +
							(locationId != 0 ? " filter=location_id," + locationId + ";" : "" ) +
							(agencyLimit != null && agencyLimit.Length > 0 ? " filter=agency_id," + String.Join(",", agencyLimit) + ";" : "" ) +
							(show != null ? " filter=status," + show + ";" : "" ) +
							(sortBy != "bidsduefirst" && sortBy != "bidsduelast" ? " groupsort=@weight desc;" : "" ) +
							(sortBy == "bidsduefirst" ? " groupsort=close_date_ts asc;" : "" ) +
							(sortBy == "bidsduelast" ? " groupsort=close_date_ts desc;" : "" ) +
							" mode=extended;'";
						cmd.Prepare();

						/* save this for debug capture */
						lastUsedQuery = cmd.CommandText;

						// Get the number of results, for the case where this latest paging
						// through the results returned nothing
						bool got_results = false;

						/* And read out our data */
						using(MySqlDataReader r = cmd.ExecuteReader())
						{
							while( r.Read() )
							{
								ids.Add(Convert.ToInt32(r.GetString(0)));

								// Did we hit the requested limit while looping?
								if( ids.Count == limit )
									break;

								got_results = true;

							}

						}

						// Did we hit the requested limit after looping?
						if( ids.Count == limit || got_results == false )
						{
							/* Then we're done */
							return ids.ToArray();
						}

						// Advance to the next result set
						my_offset += my_limit;

					}

				}

			}

		}

	}

}
