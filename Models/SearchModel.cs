using System;
using System.Collections.Generic;

using SteveHavelka.SphinxFTS;
using SearchProcurement.Helpers;

namespace SearchProcurement.Models
{
	public class Search
	{
		public int searchCount { get; private set; }
		public string searchString { get; private set; }
		public string searchUrl { get; private set; }
		public string searchUrlEncoded { get; private set; }

		/* The search results themselves */
		public searchItem[] searchResults { get; private set; }


		public Search(string kw)
		{
            // Instantiate an empty Search object, in case we want
            // to load it up elsewhere
            if( kw == null )
                return;

			// Instantiate the search object
			SphinxFTS s = new SphinxFTS();
			s.kwTable = Defines.mySphinxTable;
			s.searchUrlSeparator = "";
			s.locationId = Defines.LocationSettings.myLocationId;
			s.setWords(kw);

			// Pull out the search data
			searchString = s.searchString;
			searchUrl = s.searchUrl;
			searchUrlEncoded = s.searchUrlEncoded;
			searchCount = s.count();

			// no result?
			if( searchCount == 0 ) {

				// empty search result array
				searchResults = new searchItem[0];

			}
			else
			{

				// Run the search
				int[] searchIds = s.search();

				// And filter down to allowed agencies
				searchCount = searchIds.Length;

				// Load the data from the search IDs
				List<searchItem> items = new List<searchItem>();
				foreach(int id in searchIds)
				{
					items.Add(SearchHelper.loadItem(id));
				}

				// save the search results
				searchResults = items.ToArray();

			}

		}


        /**
         * Load procurement opportunities by agency ID
         * @param agency The agency ID
         * @return The search object with procurement data
         */
		public static Search loadByAgency(int agency)
		{
			// Instantiate the search object
            Search s = new Search(null);
            int[] ids_from_agency = SearchHelper.findByAgencyId(agency);

			// Pull out the search data
			s.searchString = SearchHelper.loadAgencyName(agency);
			s.searchUrl = "agency=" + agency;
			s.searchCount = ids_from_agency.Length;

			// no result?
			if( s.searchCount == 0 ) {

				// empty search result array
				s.searchResults = new searchItem[0];

			}
			else
			{
				// Load the data from the search IDs
				List<searchItem> items = new List<searchItem>();
				foreach(int id in ids_from_agency)
				{
					items.Add(SearchHelper.loadItem(id));
				}

				// save the search results
				s.searchResults = items.ToArray();

			}

            // And return the search object
            return s;

		}


	}
}