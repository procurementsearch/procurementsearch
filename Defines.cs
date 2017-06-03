namespace SearchProcurement
{

	static class Defines
	{
		/* The database connection string */
		public const string myConnectionString = "server=127.0.0.1;uid=root;pwd=toor;database=rss_procure;sslmode=none;";

		/* The sources */
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
		public const int Skanska = 17;
		public const int Bremik = 18;

	    /* site-specific sources */
		public static readonly int[] All_Sources = { 1, 2, 3, 4, 6, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
	    public static readonly int[] Pdx_Sources = { 1, 2, 3, 4, 6, 8, 10, 11, 13, 14, 15, 16, 18 };
	    public static readonly int[] Oregon_Sources = { 1, 2, 3, 4, 6, 8, 10, 11, 12, 13, 14, 15, 16, 18 };
		public static readonly int[] Washington_Sources = { 17 };

		/* My sources */
		public static readonly int[] mySources = All_Sources;

		/* The keyword table I'm using */
		public const string mySphinxTable = "search_all";
		public const string mySphinxIndex = "kw_all";

		/* The dreamobjects access and secret keys */
		public const string s3ServiceUrl = "https://objects-us-west-1.dream.io/";
		public const string s3AccessKey = "hF0qM1vBICnpZeGF_oC5";
        public const string s3SecretKey = "G-u6q1uEsAJRiNgJx2kfGTFyfoWc2ltyO3YlJqo3";
		public const string s3Bucket = "procurementsearch-dev";
		public const string s3LogoPath = "agency_logos";

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
		}

	}
	
}
