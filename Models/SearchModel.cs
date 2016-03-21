using System;
using System.Collections.Generic;

using SteveHavelka.SimpleFTS;
using SearchProcurement.Helpers;

namespace SearchProcurement.Models
{
	public class Search
	{
		public int searchCount { get; private set; }
		public string searchString { get; private set; }
		public string searchUrl { get; private set; }

		/* The search results themselves */
		public searchItem[] searchResults { get; private set; }


		public Search(string kw)
		{
            // Instantiate an empty Search object, in case we want
            // to load it up elsewhere
            if( kw == null )
                return;

			// Instantiate the search object
			SimpleFTS s = new SimpleFTS();
			s.kwTable = Defines.myTable;
			s.searchUrlSeparator = "";
			s.prepareWords(kw);

			// Pull out the search data
			searchString = s.searchString;
			searchUrl = s.searchUrl;
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

				// And filter down to allowed sources
				searchIds = SearchHelper.filter(searchIds, Defines.mySources);
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
         * Load procurement opportunities by source ID
         * @param source The source ID
         * @return The search object with procurement data
         */
		public static Search loadBySource(int source)
		{
			// Instantiate the search object
            Search s = new Search(null);
            int[] ids_from_source = SearchHelper.findBySourceId(source);

			// Pull out the search data
			s.searchString = SearchHelper.loadSourceName(source);
			s.searchUrl = "source=" + source;
			s.searchCount = ids_from_source.Length;

			// no result?
			if( s.searchCount == 0 ) {

				// empty search result array
				s.searchResults = new searchItem[0];

			}
			else
			{
				// Load the data from the search IDs
				List<searchItem> items = new List<searchItem>();
				foreach(int id in ids_from_source)
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