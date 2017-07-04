using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;


namespace SearchProcurement
{

	public static class Library
	{
		/**
		 * Get the last insert ID from MariaDB
		 * @param dbh The DB handle
		 * @return integer The insert ID
		 */
		static public int lastInsertId(MySqlConnection dbh)
		{
			using(MySqlCommand cmd = new MySqlCommand())
			{
				cmd.Connection = dbh;
				cmd.CommandText = "SELECT LAST_INSERT_ID()";

				// Get the new agency ID
				return Convert.ToInt32(cmd.ExecuteScalar());
			}

		}


		/**
		 * Diff a string array
		 * @param string[] a The first array
		 * @param string[] b The second array
		 * @return string[] The difference
		 */
		static public string[] diffStringArrays(string[] a, string[] b)
		{
			return a.Except(b).Union(b.Except(a)).ToArray();
		}



		/**
		 * Try to get the text from a document
		 * @param string file The file
		 * @return string The text
		 */
		static public string getTextFromDocument(string file)
		{
			// Get the filetype
			string filetype = MimeTypes.MimeTypeMap.GetMimeType(Path.GetExtension(file));
			string text = "";

			ProcessStartInfo psi = new ProcessStartInfo();
	        psi.UseShellExecute = true;
	        psi.RedirectStandardOutput = true;

			bool needs_exec = false;

			// What type of file is it?
			switch(filetype)
			{
				case "application/pdf":
					psi.FileName = "pdftotext";
					psi.Arguments = "-q \"" + file + "\" -";
					needs_exec = true;
					break;
				case "application/msword":
				case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":
				case "application/vnd.openxmlformats-officedocument.wordprocessingml.template":
				case "application/vnd.ms-word.document.macroEnabled.12":
				case "application/vnd.ms-word.template.macroEnabled.12":
					psi.FileName = "wvHtml";
					psi.Arguments = "\"" + file + "\" -";
					needs_exec = true;
					break;
				case "text/plain":
					text = File.ReadAllText(file);
					break;
				default:
					break;
			}

			// Did we need to run something?
			if( needs_exec )
			{
				// Yes, run the command
				Process proc = new Process
		        {
		            StartInfo = psi
		        };
		        proc.Start();

				// And capture its output
				text = proc.StandardOutput.ReadToEnd();
				proc.WaitForExit();
			}

			// And we're done
			return text;

		}



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
		 * @return A list of states
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




		/**
		 * Build a list of countries, for the account signup page
		 * @return Returns a list of countries
		 */
		static public List<SelectListItem> CountryListItems()
		{
			var items = new List<SelectListItem>
			{
				new SelectListItem() { Value = "AF", Text = "Afghanistan"},
				new SelectListItem() { Value = "AL", Text = "Albania"},
				new SelectListItem() { Value = "DZ", Text = "Algeria"},
				new SelectListItem() { Value = "AS", Text = "American Samoa"},
				new SelectListItem() { Value = "AD", Text = "Andorra"},
				new SelectListItem() { Value = "AO", Text = "Angola"},
				new SelectListItem() { Value = "AI", Text = "Anguilla"},
				new SelectListItem() { Value = "AQ", Text = "Antarctica"},
				new SelectListItem() { Value = "AG", Text = "Antigua and Barbuda"},
				new SelectListItem() { Value = "AR", Text = "Argentina"},
				new SelectListItem() { Value = "AM", Text = "Armenia"},
				new SelectListItem() { Value = "AW", Text = "Aruba"},
				new SelectListItem() { Value = "AU", Text = "Australia"},
				new SelectListItem() { Value = "AT", Text = "Austria"},
				new SelectListItem() { Value = "AZ", Text = "Azerbaijan"},
				new SelectListItem() { Value = "BS", Text = "Bahamas"},
				new SelectListItem() { Value = "BH", Text = "Bahrain"},
				new SelectListItem() { Value = "BD", Text = "Bangladesh"},
				new SelectListItem() { Value = "BB", Text = "Barbados"},
				new SelectListItem() { Value = "BY", Text = "Belarus"},
				new SelectListItem() { Value = "BE", Text = "Belgium"},
				new SelectListItem() { Value = "BZ", Text = "Belize"},
				new SelectListItem() { Value = "BJ", Text = "Benin"},
				new SelectListItem() { Value = "BM", Text = "Bermuda"},
				new SelectListItem() { Value = "BT", Text = "Bhutan"},
				new SelectListItem() { Value = "BO", Text = "Bolivia"},
				new SelectListItem() { Value = "BA", Text = "Bosnia and Herzegovina"},
				new SelectListItem() { Value = "BW", Text = "Botswana"},
				new SelectListItem() { Value = "BV", Text = "Bouvet Island"},
				new SelectListItem() { Value = "BR", Text = "Brazil"},
				new SelectListItem() { Value = "BQ", Text = "British Antarctic Territory"},
				new SelectListItem() { Value = "IO", Text = "British Indian Ocean Territory"},
				new SelectListItem() { Value = "VG", Text = "British Virgin Islands"},
				new SelectListItem() { Value = "BN", Text = "Brunei"},
				new SelectListItem() { Value = "BG", Text = "Bulgaria"},
				new SelectListItem() { Value = "BF", Text = "Burkina Faso"},
				new SelectListItem() { Value = "BI", Text = "Burundi"},
				new SelectListItem() { Value = "KH", Text = "Cambodia"},
				new SelectListItem() { Value = "CM", Text = "Cameroon"},
				new SelectListItem() { Value = "CA", Text = "Canada"},
				new SelectListItem() { Value = "CT", Text = "Canton and Enderbury Islands"},
				new SelectListItem() { Value = "CV", Text = "Cape Verde"},
				new SelectListItem() { Value = "KY", Text = "Cayman Islands"},
				new SelectListItem() { Value = "CF", Text = "Central African Republic"},
				new SelectListItem() { Value = "TD", Text = "Chad"},
				new SelectListItem() { Value = "CL", Text = "Chile"},
				new SelectListItem() { Value = "CN", Text = "China"},
				new SelectListItem() { Value = "CX", Text = "Christmas Island"},
				new SelectListItem() { Value = "CC", Text = "Cocos [Keeling] Islands"},
				new SelectListItem() { Value = "CO", Text = "Colombia"},
				new SelectListItem() { Value = "KM", Text = "Comoros"},
				new SelectListItem() { Value = "CG", Text = "Congo - Brazzaville"},
				new SelectListItem() { Value = "CD", Text = "Congo - Kinshasa"},
				new SelectListItem() { Value = "CK", Text = "Cook Islands"},
				new SelectListItem() { Value = "CR", Text = "Costa Rica"},
				new SelectListItem() { Value = "HR", Text = "Croatia"},
				new SelectListItem() { Value = "CU", Text = "Cuba"},
				new SelectListItem() { Value = "CY", Text = "Cyprus"},
				new SelectListItem() { Value = "CZ", Text = "Czech Republic"},
				new SelectListItem() { Value = "CI", Text = "Côte d’Ivoire"},
				new SelectListItem() { Value = "DK", Text = "Denmark"},
				new SelectListItem() { Value = "DJ", Text = "Djibouti"},
				new SelectListItem() { Value = "DM", Text = "Dominica"},
				new SelectListItem() { Value = "DO", Text = "Dominican Republic"},
				new SelectListItem() { Value = "NQ", Text = "Dronning Maud Land"},
				new SelectListItem() { Value = "DD", Text = "East Germany"},
				new SelectListItem() { Value = "EC", Text = "Ecuador"},
				new SelectListItem() { Value = "EG", Text = "Egypt"},
				new SelectListItem() { Value = "SV", Text = "El Salvador"},
				new SelectListItem() { Value = "GQ", Text = "Equatorial Guinea"},
				new SelectListItem() { Value = "ER", Text = "Eritrea"},
				new SelectListItem() { Value = "EE", Text = "Estonia"},
				new SelectListItem() { Value = "ET", Text = "Ethiopia"},
				new SelectListItem() { Value = "FK", Text = "Falkland Islands"},
				new SelectListItem() { Value = "FO", Text = "Faroe Islands"},
				new SelectListItem() { Value = "FJ", Text = "Fiji"},
				new SelectListItem() { Value = "FI", Text = "Finland"},
				new SelectListItem() { Value = "FR", Text = "France"},
				new SelectListItem() { Value = "GF", Text = "French Guiana"},
				new SelectListItem() { Value = "PF", Text = "French Polynesia"},
				new SelectListItem() { Value = "TF", Text = "French Southern Territories"},
				new SelectListItem() { Value = "FQ", Text = "French Southern and Antarctic Territories"},
				new SelectListItem() { Value = "GA", Text = "Gabon"},
				new SelectListItem() { Value = "GM", Text = "Gambia"},
				new SelectListItem() { Value = "GE", Text = "Georgia"},
				new SelectListItem() { Value = "DE", Text = "Germany"},
				new SelectListItem() { Value = "GH", Text = "Ghana"},
				new SelectListItem() { Value = "GI", Text = "Gibraltar"},
				new SelectListItem() { Value = "GR", Text = "Greece"},
				new SelectListItem() { Value = "GL", Text = "Greenland"},
				new SelectListItem() { Value = "GD", Text = "Grenada"},
				new SelectListItem() { Value = "GP", Text = "Guadeloupe"},
				new SelectListItem() { Value = "GU", Text = "Guam"},
				new SelectListItem() { Value = "GT", Text = "Guatemala"},
				new SelectListItem() { Value = "GG", Text = "Guernsey"},
				new SelectListItem() { Value = "GN", Text = "Guinea"},
				new SelectListItem() { Value = "GW", Text = "Guinea-Bissau"},
				new SelectListItem() { Value = "GY", Text = "Guyana"},
				new SelectListItem() { Value = "HT", Text = "Haiti"},
				new SelectListItem() { Value = "HM", Text = "Heard Island and McDonald Islands"},
				new SelectListItem() { Value = "HN", Text = "Honduras"},
				new SelectListItem() { Value = "HK", Text = "Hong Kong SAR China"},
				new SelectListItem() { Value = "HU", Text = "Hungary"},
				new SelectListItem() { Value = "IS", Text = "Iceland"},
				new SelectListItem() { Value = "IN", Text = "India"},
				new SelectListItem() { Value = "ID", Text = "Indonesia"},
				new SelectListItem() { Value = "IR", Text = "Iran"},
				new SelectListItem() { Value = "IQ", Text = "Iraq"},
				new SelectListItem() { Value = "IE", Text = "Ireland"},
				new SelectListItem() { Value = "IM", Text = "Isle of Man"},
				new SelectListItem() { Value = "IL", Text = "Israel"},
				new SelectListItem() { Value = "IT", Text = "Italy"},
				new SelectListItem() { Value = "JM", Text = "Jamaica"},
				new SelectListItem() { Value = "JP", Text = "Japan"},
				new SelectListItem() { Value = "JE", Text = "Jersey"},
				new SelectListItem() { Value = "JT", Text = "Johnston Island"},
				new SelectListItem() { Value = "JO", Text = "Jordan"},
				new SelectListItem() { Value = "KZ", Text = "Kazakhstan"},
				new SelectListItem() { Value = "KE", Text = "Kenya"},
				new SelectListItem() { Value = "KI", Text = "Kiribati"},
				new SelectListItem() { Value = "KW", Text = "Kuwait"},
				new SelectListItem() { Value = "KG", Text = "Kyrgyzstan"},
				new SelectListItem() { Value = "LA", Text = "Laos"},
				new SelectListItem() { Value = "LV", Text = "Latvia"},
				new SelectListItem() { Value = "LB", Text = "Lebanon"},
				new SelectListItem() { Value = "LS", Text = "Lesotho"},
				new SelectListItem() { Value = "LR", Text = "Liberia"},
				new SelectListItem() { Value = "LY", Text = "Libya"},
				new SelectListItem() { Value = "LI", Text = "Liechtenstein"},
				new SelectListItem() { Value = "LT", Text = "Lithuania"},
				new SelectListItem() { Value = "LU", Text = "Luxembourg"},
				new SelectListItem() { Value = "MO", Text = "Macau SAR China"},
				new SelectListItem() { Value = "MK", Text = "Macedonia"},
				new SelectListItem() { Value = "MG", Text = "Madagascar"},
				new SelectListItem() { Value = "MW", Text = "Malawi"},
				new SelectListItem() { Value = "MY", Text = "Malaysia"},
				new SelectListItem() { Value = "MV", Text = "Maldives"},
				new SelectListItem() { Value = "ML", Text = "Mali"},
				new SelectListItem() { Value = "MT", Text = "Malta"},
				new SelectListItem() { Value = "MH", Text = "Marshall Islands"},
				new SelectListItem() { Value = "MQ", Text = "Martinique"},
				new SelectListItem() { Value = "MR", Text = "Mauritania"},
				new SelectListItem() { Value = "MU", Text = "Mauritius"},
				new SelectListItem() { Value = "YT", Text = "Mayotte"},
				new SelectListItem() { Value = "FX", Text = "Metropolitan France"},
				new SelectListItem() { Value = "MX", Text = "Mexico"},
				new SelectListItem() { Value = "FM", Text = "Micronesia"},
				new SelectListItem() { Value = "MI", Text = "Midway Islands"},
				new SelectListItem() { Value = "MD", Text = "Moldova"},
				new SelectListItem() { Value = "MC", Text = "Monaco"},
				new SelectListItem() { Value = "MN", Text = "Mongolia"},
				new SelectListItem() { Value = "ME", Text = "Montenegro"},
				new SelectListItem() { Value = "MS", Text = "Montserrat"},
				new SelectListItem() { Value = "MA", Text = "Morocco"},
				new SelectListItem() { Value = "MZ", Text = "Mozambique"},
				new SelectListItem() { Value = "MM", Text = "Myanmar [Burma]"},
				new SelectListItem() { Value = "NA", Text = "Namibia"},
				new SelectListItem() { Value = "NR", Text = "Nauru"},
				new SelectListItem() { Value = "NP", Text = "Nepal"},
				new SelectListItem() { Value = "NL", Text = "Netherlands"},
				new SelectListItem() { Value = "AN", Text = "Netherlands Antilles"},
				new SelectListItem() { Value = "NT", Text = "Neutral Zone"},
				new SelectListItem() { Value = "NC", Text = "New Caledonia"},
				new SelectListItem() { Value = "NZ", Text = "New Zealand"},
				new SelectListItem() { Value = "NI", Text = "Nicaragua"},
				new SelectListItem() { Value = "NE", Text = "Niger"},
				new SelectListItem() { Value = "NG", Text = "Nigeria"},
				new SelectListItem() { Value = "NU", Text = "Niue"},
				new SelectListItem() { Value = "NF", Text = "Norfolk Island"},
				new SelectListItem() { Value = "KP", Text = "North Korea"},
				new SelectListItem() { Value = "VD", Text = "North Vietnam"},
				new SelectListItem() { Value = "MP", Text = "Northern Mariana Islands"},
				new SelectListItem() { Value = "NO", Text = "Norway"},
				new SelectListItem() { Value = "OM", Text = "Oman"},
				new SelectListItem() { Value = "PC", Text = "Pacific Islands Trust Territory"},
				new SelectListItem() { Value = "PK", Text = "Pakistan"},
				new SelectListItem() { Value = "PW", Text = "Palau"},
				new SelectListItem() { Value = "PS", Text = "Palestinian Territories"},
				new SelectListItem() { Value = "PA", Text = "Panama"},
				new SelectListItem() { Value = "PZ", Text = "Panama Canal Zone"},
				new SelectListItem() { Value = "PG", Text = "Papua New Guinea"},
				new SelectListItem() { Value = "PY", Text = "Paraguay"},
				new SelectListItem() { Value = "YD", Text = "People's Democratic Republic of Yemen"},
				new SelectListItem() { Value = "PE", Text = "Peru"},
				new SelectListItem() { Value = "PH", Text = "Philippines"},
				new SelectListItem() { Value = "PN", Text = "Pitcairn Islands"},
				new SelectListItem() { Value = "PL", Text = "Poland"},
				new SelectListItem() { Value = "PT", Text = "Portugal"},
				new SelectListItem() { Value = "PR", Text = "Puerto Rico"},
				new SelectListItem() { Value = "QA", Text = "Qatar"},
				new SelectListItem() { Value = "RO", Text = "Romania"},
				new SelectListItem() { Value = "RU", Text = "Russia"},
				new SelectListItem() { Value = "RW", Text = "Rwanda"},
				new SelectListItem() { Value = "RE", Text = "Réunion"},
				new SelectListItem() { Value = "BL", Text = "Saint Barthélemy"},
				new SelectListItem() { Value = "SH", Text = "Saint Helena"},
				new SelectListItem() { Value = "KN", Text = "Saint Kitts and Nevis"},
				new SelectListItem() { Value = "LC", Text = "Saint Lucia"},
				new SelectListItem() { Value = "MF", Text = "Saint Martin"},
				new SelectListItem() { Value = "PM", Text = "Saint Pierre and Miquelon"},
				new SelectListItem() { Value = "VC", Text = "Saint Vincent and the Grenadines"},
				new SelectListItem() { Value = "WS", Text = "Samoa"},
				new SelectListItem() { Value = "SM", Text = "San Marino"},
				new SelectListItem() { Value = "SA", Text = "Saudi Arabia"},
				new SelectListItem() { Value = "SN", Text = "Senegal"},
				new SelectListItem() { Value = "RS", Text = "Serbia"},
				new SelectListItem() { Value = "CS", Text = "Serbia and Montenegro"},
				new SelectListItem() { Value = "SC", Text = "Seychelles"},
				new SelectListItem() { Value = "SL", Text = "Sierra Leone"},
				new SelectListItem() { Value = "SG", Text = "Singapore"},
				new SelectListItem() { Value = "SK", Text = "Slovakia"},
				new SelectListItem() { Value = "SI", Text = "Slovenia"},
				new SelectListItem() { Value = "SB", Text = "Solomon Islands"},
				new SelectListItem() { Value = "SO", Text = "Somalia"},
				new SelectListItem() { Value = "ZA", Text = "South Africa"},
				new SelectListItem() { Value = "GS", Text = "South Georgia and the South Sandwich Islands"},
				new SelectListItem() { Value = "KR", Text = "South Korea"},
				new SelectListItem() { Value = "ES", Text = "Spain"},
				new SelectListItem() { Value = "LK", Text = "Sri Lanka"},
				new SelectListItem() { Value = "SD", Text = "Sudan"},
				new SelectListItem() { Value = "SR", Text = "Suriname"},
				new SelectListItem() { Value = "SJ", Text = "Svalbard and Jan Mayen"},
				new SelectListItem() { Value = "SZ", Text = "Swaziland"},
				new SelectListItem() { Value = "SE", Text = "Sweden"},
				new SelectListItem() { Value = "CH", Text = "Switzerland"},
				new SelectListItem() { Value = "SY", Text = "Syria"},
				new SelectListItem() { Value = "ST", Text = "São Tomé and Príncipe"},
				new SelectListItem() { Value = "TW", Text = "Taiwan"},
				new SelectListItem() { Value = "TJ", Text = "Tajikistan"},
				new SelectListItem() { Value = "TZ", Text = "Tanzania"},
				new SelectListItem() { Value = "TH", Text = "Thailand"},
				new SelectListItem() { Value = "TL", Text = "Timor-Leste"},
				new SelectListItem() { Value = "TG", Text = "Togo"},
				new SelectListItem() { Value = "TK", Text = "Tokelau"},
				new SelectListItem() { Value = "TO", Text = "Tonga"},
				new SelectListItem() { Value = "TT", Text = "Trinidad and Tobago"},
				new SelectListItem() { Value = "TN", Text = "Tunisia"},
				new SelectListItem() { Value = "TR", Text = "Turkey"},
				new SelectListItem() { Value = "TM", Text = "Turkmenistan"},
				new SelectListItem() { Value = "TC", Text = "Turks and Caicos Islands"},
				new SelectListItem() { Value = "TV", Text = "Tuvalu"},
				new SelectListItem() { Value = "UM", Text = "U.S. Minor Outlying Islands"},
				new SelectListItem() { Value = "PU", Text = "U.S. Miscellaneous Pacific Islands"},
				new SelectListItem() { Value = "VI", Text = "U.S. Virgin Islands"},
				new SelectListItem() { Value = "UG", Text = "Uganda"},
				new SelectListItem() { Value = "UA", Text = "Ukraine"},
				new SelectListItem() { Value = "SU", Text = "Union of Soviet Socialist Republics"},
				new SelectListItem() { Value = "AE", Text = "United Arab Emirates"},
				new SelectListItem() { Value = "GB", Text = "United Kingdom"},
				new SelectListItem() { Value = "US", Text = "United States"},
				new SelectListItem() { Value = "ZZ", Text = "Unknown or Invalid Region"},
				new SelectListItem() { Value = "UY", Text = "Uruguay"},
				new SelectListItem() { Value = "UZ", Text = "Uzbekistan"},
				new SelectListItem() { Value = "VU", Text = "Vanuatu"},
				new SelectListItem() { Value = "VA", Text = "Vatican City"},
				new SelectListItem() { Value = "VE", Text = "Venezuela"},
				new SelectListItem() { Value = "VN", Text = "Vietnam"},
				new SelectListItem() { Value = "WK", Text = "Wake Island"},
				new SelectListItem() { Value = "WF", Text = "Wallis and Futuna"},
				new SelectListItem() { Value = "EH", Text = "Western Sahara"},
				new SelectListItem() { Value = "YE", Text = "Yemen"},
				new SelectListItem() { Value = "ZM", Text = "Zambia"},
				new SelectListItem() { Value = "ZW", Text = "Zimbabwe"},
				new SelectListItem() { Value = "AX", Text = "Åland Islands"}
			};
			return items;
		}


	}


	//Copyright (C) Microsoft Corporation.  All rights reserved.
	// See the ReadMe.html for additional information
	public class ObjectDumper {

		public static void Write(object element)
		{
			Write(element, 0);
		}

		public static void Write(object element, int depth)
		{
			Write(element, depth, Console.Out);
		}

		public static void Write(object element, int depth, TextWriter log)
		{
			ObjectDumper dumper = new ObjectDumper(depth);
			dumper.writer = log;
			dumper.WriteObject(null, element);
		}

		TextWriter writer;
		int pos;
		int level;
		int depth;

		private ObjectDumper(int depth)
		{
			this.depth = depth;
		}

		private void Write(string s)
		{
			if (s != null) {
				writer.Write(s);
				pos += s.Length;
			}
		}

		private void WriteIndent()
		{
			for (int i = 0; i < level; i++) writer.Write("  ");
		}

		private void WriteLine()
		{
			writer.WriteLine();
			pos = 0;
		}

		private void WriteTab()
		{
			Write("  ");
			while (pos % 8 != 0) Write(" ");
		}

		private void WriteObject(string prefix, object element)
		{
			if (element == null || element is ValueType || element is string) {
				WriteIndent();
				Write(prefix);
				WriteValue(element);
				WriteLine();
			}
			else {
				IEnumerable enumerableElement = element as IEnumerable;
				if (enumerableElement != null) {
					foreach (object item in enumerableElement) {
						if (item is IEnumerable && !(item is string)) {
							WriteIndent();
							Write(prefix);
							Write("...");
							WriteLine();
							if (level < depth) {
								level++;
								WriteObject(prefix, item);
								level--;
							}
						}
						else {
							WriteObject(prefix, item);
						}
					}
				}
				else {
					MemberInfo[] members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
					WriteIndent();
					Write(prefix);
					bool propWritten = false;
					foreach (MemberInfo m in members) {
						FieldInfo f = m as FieldInfo;
						PropertyInfo p = m as PropertyInfo;
						if (f != null || p != null) {
							if (propWritten) {
								WriteTab();
							}
							else {
								propWritten = true;
							}
							Write(m.Name);
							Write("=");
							Type t = f != null ? f.FieldType : p.PropertyType;
							if (t.GetTypeInfo().IsValueType || t == typeof(string)) {
								WriteValue(f != null ? f.GetValue(element) : p.GetValue(element, null));
							}
							else {
								if (typeof(IEnumerable).IsAssignableFrom(t)) {
									Write("...");
								}
								else {
									Write("{ }");
								}
							}
						}
					}
					if (propWritten) WriteLine();
					if (level < depth) {
						foreach (MemberInfo m in members) {
							FieldInfo f = m as FieldInfo;
							PropertyInfo p = m as PropertyInfo;
							if (f != null || p != null) {
								Type t = f != null ? f.FieldType : p.PropertyType;
								if (!(t.GetTypeInfo().IsValueType || t == typeof(string))) {
									object value = f != null ? f.GetValue(element) : p.GetValue(element, null);
									if (value != null) {
										level++;
										WriteObject(m.Name + ": ", value);
										level--;
									}
								}
							}
						}
					}
				}
			}
		}

		private void WriteValue(object o)
		{
			if (o == null) {
				Write("null");
			}
			else if (o is DateTime) {
				Write(((DateTime)o).ToString("d"));
			}
			else if (o is ValueType || o is string) {
				Write(o.ToString());
			}
			else if (o is IEnumerable) {
				Write("...");
			}
			else {
				Write("{ }");
			}
		}
	}


}