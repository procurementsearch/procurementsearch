using System;
using System.Text.RegularExpressions;

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


	}

}