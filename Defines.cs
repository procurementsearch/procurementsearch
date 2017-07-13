namespace SearchProcurement
{

	public sealed class Defines
	{
		/* My locations */
		public static readonly int[] myLocations = { 1, 2, 3, 4, 5 };  // Oregon (Portland, Eugene), Washington (Seattle)

		/* The keyword table I'm using */
		public const string mySphinxTable = "search_all";
		public const string mySphinxIndex = "kw_all";

		/* The sources */
		public static class SourceLookup
		{
			public const int PDC = 1;
			public const int Metro = 2;
			public const int CityOfPortland = 3;
			public const int TriMet = 4;
			public const int CityOfHillsboro = 5;
			public const int MultnomahCounty = 6;
			public const int WashingtonCounty = 7;
			public const int PortlandStateUniversity = 8;
			public const int PortlandStateUniversityConstructionContracts = 9;
			public const int CityOfBeaverton = 10;
			public const int OregonDepartmentOfCorrections = 11;
			public const int ChemeketaCommunityCollege = 12;
			public const int PortOfPortland = 13;
			public const int OregonDepartmentOfTransportation = 14;
			public const int OregonDepartmentOfTransportationEBIDS = 15;
			public const int PortlandPublicSchools = 16;
			public const int LaneTransitDistrict = 17;
		}

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


		/* Key names for session data */
		public static class SessionKeys
		{
			public const string LocationId = "locId";
			public const string Files = "myFiles";
			public const string ListingType = "listingType";
		}


		/* Make this into a singleton class, for handling configuration from appsettings.json */
		private static readonly Defines _instance = new Defines();
		public static Defines Instance
		{
			get { return _instance; }
		}

		private Defines()
 		{
			// place for instance initialization code
		}

	}
	
}
