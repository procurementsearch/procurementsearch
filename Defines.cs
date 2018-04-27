namespace SearchProcurement
{

	public sealed class Defines
	{
		/* The keyword table I'm using */
		public const string mySphinxTable = "search_all";
		public const string mySphinxIndex = "kw_all";

		/* The dreamobjects access and secret keys */
		public const string s3LogoPath = "agency_logos";
		public const string s3AttachmentPath = "bid_documents";


		/* The on-server storage path(s) */
		public const string UploadLogoPath = "/logos";
		public const string UploadDocumentPath = "/documents";

		/* The RSS data for this specific site */
		public const string RssTitle = "Procurement Opportunities";
		public const string RssDescription = "This is a searchable, RSSable database of government contracting opportunities, including cities, counties, regional governments, state-level agencies, and private companies.";
		public const string RssUrl = "http://ProcurementSearch.com";
		public const string RssDetailsUrl = "http://ProcurementSearch.com/details";
		public const string RssLimit = "80";
		public static readonly int[] RssLocations = { 1, 2, 3, 4, 5 };  // Oregon (Portland, Eugene), Washington (Seattle)

		/* The default site, if we've been requested one that hasn't been set up or doesn't exist */
		public const string defaultSiteName = "ProcurementSearch.com";
		public const int defaultSiteId = 17;


		/* Key names for session data */
		public static class SessionKeys
		{
			public const string LocationId = "locId";
			public const string Files = "myFiles";
			public const string ListingType = "listingType";
		}


		/* Settings loaded from AppSettings */
		public static class AppSettings
		{
			/* MariaDB configuration */
			public static string myConnectionString;

			/* Upload path configuration */
			public static string UploadStorageUrl;
			public static string UploadStoragePath;

			/* S3 configuration settings */
			public static string s3ServiceUrl;
			public static string s3AccessKey;
			public static string s3SecretKey;
			public static string s3Bucket;

			/* And the Stripe public key */
			public static string StripeKey;

		}



		/* Settings loaded from the database at runtime */
		public static class LocationSettings
		{
			public static int myLocationId;
			public static string siteTitle;

			/* Asset URLs */
			public static string logoUrl;
			public static string logoUrlSmall;
			public static string cssUrl;
			public static string cssMiniUrl;
			public static string cssMicroUrl;
			public static string gaGuid;

			/* RSS text */
			public static string rssTitle;
			public static string rssDescription;
			public static string rssUrl;
			public static string rssDetailsUrl;
			public static int rssFilterByLocation;

			/* Home page text */
			public static string homeTitle;
			public static string homeButtonText;
			public static string homeGreetingText;

			/* Footer elements */
			public static string footerText;
			public static string socialTwitter;
			public static string socialFacebook;
		}


	}

}
