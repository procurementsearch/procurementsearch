namespace SearchProcurement
{

	static class Defines
	{
		/* The database connection string */
		public const string myConnectionString = "server=127.0.0.1;uid=root;pwd=toor;database=rss_procure;";

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

	    /* site-specific sources */
	    public static readonly int[] Pdx_Sources = { 1, 2, 3, 6, 8, 10, 11, 13 };
	    public static readonly int[] Oregon_Sources = { 1, 2, 3, 6, 8, 10, 11, 12, 13 };

		/* My sources */
		public static readonly int[] mySources = Oregon_Sources;

		/* The keyword table I'm using */
		public const string myTable = "kw_oregon";

		/* The RSS data for this specific site */
		public const string RssTitle = "Oregon Procurement Opportunities";
		public const string RssDescription = "This is a searchable, RSSable database of procurement opportunities in Oregon, including cities, counties, regional governments, state-level agencies, and private companies.";
		public const string RssUrl = "http://OregonProcurementSearch.com";
		public const string RssDetailsUrl = "http://OregonProcurementSearch.com/details";

	}
	
}
