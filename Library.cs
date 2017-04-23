using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace SearchProcurement
{

	static class Library
	{


        /**
         * Strip HTML from a string
         * @param string html The HTML
         * @return string The text minus the HTML
         */
		static public string stripHtml(string html)
		{
			html = replace("<script[^>]*?>.*?</script>", " ", html);
            html = replace(@"<[\/\!]*?[^<>]*?>", " ", html);
            html = replace(@"([\r\n])[\s]+", "\\1", html);
            html = replace("&(quot|#34);", "\"", html);
            html = replace("&(amp|#38);", "&", html);
            html = replace("&(lt|#60);", "<", html);
            html = replace("&(gt|#62);", ">", html);
            html = replace("&(nbsp|#160);", " ", html);
            html = replace("&(iexcl|#161);", "\xA1", html);
            html = replace("&(cent|#162);", "\xA2", html);
            html = replace("&(pound|#163);", "\xA3", html);
            html = replace("&(copy|#169);", "\xA9", html);
			return replace_entities(html);
		}


		static private string replace(string mat, string rep, string stg)
		{
			return new Regex(mat, RegexOptions.IgnoreCase).Replace(stg, rep);
		}

		static private string replace_entities(string stg)
		{
			return new Regex(@"&#(?<number>\d+);").Replace(stg, delegate(Match m) { return m.Groups["number"].ToString(); });
		}


		/**
		 * Like PHP's nl2br()
		 * @param string stg The source string
		 * @return string The br-ified string
		 */
		static public string nl2br(string stg)
		{
			stg = stg.Replace("\r\n", "<br>");
			stg = stg.Replace("\n\r", "<br>");
			stg = stg.Replace("\n", "<br>");
			stg = stg.Replace("\r", "<br>");
			return stg;
		}


        /**
         * Generate an excerpt of max length l from the text
         * @param string stg The source text
		 * @param int l The maximum length of the excerpt
         * @return string The excerpted text
         */
		static public string makeExcerpt(string stg, int l)
		{
			// Does the whole string fit?  Then we're done
			if( stg.Length <= l )
				return stg;

			// Remove extraneous whitespace, just in case ..
			stg = stg.Trim();

			Regex r = new Regex(@"\s");
			Match m = r.Match(stg, l);

			// Did it find whitespace?
			if( m.Value == "" ) {

				// No...  so, if the string is 10 letters longer than max, truncate it there
				if( stg.Length > l + 10 )
					return stg.Substring(0, l + 10) + " ...";
				else
					return stg;

			} else {

				// Yes... so truncate the string at the whitespace
				return stg.Substring(0, m.Index) + " ...";

			}

		}




		/**
		 * Build a list of states, for the account signup page
		 */
		static public List<SelectListItem> StateListItems()
		{
			var items = new List<SelectListItem>
			{
				new SelectListItem() {Text = "Alabama", Value = "AL"},
				new SelectListItem() {Text = "Alaska", Value = "AK"},
				new SelectListItem() {Text = "Arizona", Value = "AZ"},
				new SelectListItem() {Text = "Arkansas", Value = "AR"},
				new SelectListItem() {Text = "California", Value = "CA"},
				new SelectListItem() {Text = "Colorado", Value = "CO"},
				new SelectListItem() {Text = "Connecticut", Value = "CT"},
				new SelectListItem() {Text = "District of Columbia", Value = "DC"},
				new SelectListItem() {Text = "Delaware", Value = "DE"},
				new SelectListItem() {Text = "Florida", Value = "FL"},
				new SelectListItem() {Text = "Georgia", Value = "GA"},
				new SelectListItem() {Text = "Hawaii", Value = "HI"},
				new SelectListItem() {Text = "Idaho", Value = "ID"},
				new SelectListItem() {Text = "Illinois", Value = "IL"},
				new SelectListItem() {Text = "Indiana", Value = "IN"},
				new SelectListItem() {Text = "Iowa", Value = "IA"},
				new SelectListItem() {Text = "Kansas", Value = "KS"},
				new SelectListItem() {Text = "Kentucky", Value = "KY"},
				new SelectListItem() {Text = "Louisiana", Value = "LA"},
				new SelectListItem() {Text = "Maine", Value = "ME"},
				new SelectListItem() {Text = "Maryland", Value = "MD"},
				new SelectListItem() {Text = "Massachusetts", Value = "MA"},
				new SelectListItem() {Text = "Michigan", Value = "MI"},
				new SelectListItem() {Text = "Minnesota", Value = "MN"},
				new SelectListItem() {Text = "Mississippi", Value = "MS"},
				new SelectListItem() {Text = "Missouri", Value = "MO"},
				new SelectListItem() {Text = "Montana", Value = "MT"},
				new SelectListItem() {Text = "Nebraska", Value = "NE"},
				new SelectListItem() {Text = "Nevada", Value = "NV"},
				new SelectListItem() {Text = "New Hampshire", Value = "NH"},
				new SelectListItem() {Text = "New Jersey", Value = "NJ"},
				new SelectListItem() {Text = "New Mexico", Value = "NM"},
				new SelectListItem() {Text = "New York", Value = "NY"},
				new SelectListItem() {Text = "North Carolina", Value = "NC"},
				new SelectListItem() {Text = "North Dakota", Value = "ND"},
				new SelectListItem() {Text = "Ohio", Value = "OH"},
				new SelectListItem() {Text = "Oklahoma", Value = "OK"},
				new SelectListItem() {Text = "Oregon", Value = "OR"},
				new SelectListItem() {Text = "Pennsylvania", Value = "PA"},
				new SelectListItem() {Text = "Rhode Island", Value = "RI"},
				new SelectListItem() {Text = "South Carolina", Value = "SC"},
				new SelectListItem() {Text = "South Dakota", Value = "SD"},
				new SelectListItem() {Text = "Tennessee", Value = "TN"},
				new SelectListItem() {Text = "Texas", Value = "TX"},
				new SelectListItem() {Text = "Utah", Value = "UT"},
				new SelectListItem() {Text = "Vermont", Value = "VT"},
				new SelectListItem() {Text = "Virginia", Value = "VA"},
				new SelectListItem() {Text = "Washington", Value = "WA"},
				new SelectListItem() {Text = "West Virginia", Value = "WV"},
				new SelectListItem() {Text = "Wisconsin", Value = "WI"},
				new SelectListItem() {Text = "Wyoming", Value = "WY"}
			};
			return items;
		}

	}

}