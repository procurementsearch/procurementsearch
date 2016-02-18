using System;
using System.Text.RegularExpressions;

namespace SearchProcurement
{

	static class Library
	{


		// Strip HTML from a string
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

	}

}