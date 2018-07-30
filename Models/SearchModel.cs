using System;
using System.Data;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

		/* The Sphinx search object */
		private SphinxFTS mySphinx;

		public Search(string kw)
		{
            // Instantiate an empty Search object, in case we want
            // to load it up elsewhere
            if( kw == null )
                return;

			// Instantiate the search object
			mySphinx = new SphinxFTS();
			mySphinx.kwTable = Defines.mySphinxTable;
			mySphinx.searchUrlSeparator = "";
			mySphinx.locationId = Defines.LocationSettings.myLocationId;
			mySphinx.setWords(kw);

			// Pull out the search data
			searchString = mySphinx.searchString;
			searchUrl = mySphinx.searchUrl;
			searchUrlEncoded = mySphinx.searchUrlEncoded;

		}


		public void setAgencies(int[] agencies)
		{
			mySphinx.agencyLimit = agencies;
		}

		public void setSortBy(string sortBy)
		{
			mySphinx.sortBy = SearchParam.SortByOptions[sortBy];
		}

		public void setShow(string show)
		{
			mySphinx.show = SearchParam.ShowOptions[show];
		}



		/**
		 * Run the search itself
		 * @return void Returns nothing but populates the search
		 */
		public void run()
		{
			searchCount = mySphinx.count();

			// no result?
			if( searchCount == 0 ) {

				// empty search result array
				searchResults = new searchItem[0];

			}
			else
			{

				// Run the search
				int[] searchIds = mySphinx.search();

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
			s.searchUrlEncoded = WebUtility.UrlEncode(s.searchUrl);
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