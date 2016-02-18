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

	}
}