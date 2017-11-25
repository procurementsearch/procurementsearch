using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;


namespace SearchProcurement
{
    public class LocationSettingsMiddleware
    {

        private readonly RequestDelegate _next;

        public LocationSettingsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            string myHostname = context.Request.Host.Host;

            // Load up the site-specific data structure
            loadByDomainName(myHostname);

            // And that's it-- we're done!

            // Call the next delegate/middleware in the pipeline
            return this._next(context);
        }



        private void loadByDomainName(string hostname)
        {
			// Set up the database connection, there has to be a better way!
			using(MySqlConnection my_dbh = new MySqlConnection(Defines.AppSettings.myConnectionString))
			{
				// Open the DB connection
				my_dbh.Open();
				using(MySqlCommand cmd = new MySqlCommand())
				{
					cmd.Connection = my_dbh;
					cmd.CommandText = "SELECT logo_url, " + // 0
                        "logo_url_small, " + // 1
                        "home_title, " + // 2
                        "home_button_text, " + // 3
                        "home_greeting_text, " + // 4
                        "css_url, " + // 5
                        "rss_title, " + // 6
                        "rss_description, " + // 7
                        "rss_url, " + // 8
                        "rss_detailsurl, " + // 9
                        "rss_filter_by_location, " + // 10
                        "site_title, " + // 11
                        "google_analytics_guid, " + // 12
                        "footer_text, " + // 13
                        "social_twitter, " + // 14
                        "social_facebook, " + // 15
                        "location_id, " + // 16
                        "css_mini_url, " + // 17
                        "css_micro_url " + // 18
                        "FROM location WHERE location_domain = @id OR location_altdomain = @id";
					cmd.Parameters.AddWithValue("@id",hostname);
					cmd.Prepare();

                    // And pull out the data
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
                        if( r.HasRows )
                        {
                            r.Read();

                            // Store the agency data
                            Defines.LocationSettings.logoUrl = r.GetString(0);
                            Defines.LocationSettings.logoUrlSmall = r.GetString(1);
                            Defines.LocationSettings.homeTitle = r.GetString(2);
                            Defines.LocationSettings.homeButtonText = r.GetString(3);
                            Defines.LocationSettings.homeGreetingText = r.GetString(4);
                            Defines.LocationSettings.cssUrl = r.GetString(5);
                            Defines.LocationSettings.rssTitle = r.GetString(6);
                            Defines.LocationSettings.rssDescription = r.GetString(7);
                            Defines.LocationSettings.rssUrl = r.GetString(8);
                            Defines.LocationSettings.rssDetailsUrl = r.GetString(9);
                            Defines.LocationSettings.rssFilterByLocation = r.GetInt32(10);
                            Defines.LocationSettings.siteTitle = r.GetString(11);
                            Defines.LocationSettings.gaGuid = r.IsDBNull(12) ? "" : r.GetString(12);
                            Defines.LocationSettings.footerText = r.GetString(13);
                            Defines.LocationSettings.socialTwitter = r.IsDBNull(14) ? "" : r.GetString(14);
                            Defines.LocationSettings.socialFacebook = r.IsDBNull(15) ? "" : r.GetString(15);
                            Defines.LocationSettings.myLocationId = r.GetInt32(16);
                            Defines.LocationSettings.cssMiniUrl = r.GetString(17);
                            Defines.LocationSettings.cssMicroUrl = r.GetString(18);
                        }
                        else
                            throw new System.ArgumentException("Couldn't find a site for this hostname!!");

                    }
                }
            }
        }

    }

}
