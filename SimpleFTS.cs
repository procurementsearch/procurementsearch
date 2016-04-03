using System;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

using SearchProcurement;

namespace SteveHavelka.SimpleFTS
{

    public class SimpleFTS
	{
		public int limit { get; set; } = 100;
		public int offset  { get; set; } = 0;
	
		public string[] words { get; private set; }
	
		public string searchString { get; private set; }
		public string searchUrl { get; private set; }
		public string resultCount { get; private set; }
	
		public string searchUrlSeparator { get; set; } = "&";
	
		public bool absoluteMatch  { get; set; } = true;
		public bool buildPartial  { get; set; } = false;
		public string kwTable  { get; set; } = "kw";
	
		/* for internal use */
		private string query { get; set; }


		/* Prepares the words based on a search string */
		public string[] prepareWords(string stg)
		{
			/* strip out junk, split on spaces, remove empty elements */
			stg = Regex.Replace(stg, @"(\s|-)+", " ");
			stg = Regex.Replace(stg, @"'+", "");
			stg = Regex.Replace(stg, @"[^\w ]+", "", RegexOptions.None);

			/* failsafe */
			if( stg.Length == 0 || stg == " " )
				return null;

			/* ok, we've caught the empty string, so now we must have at least some words */
			words = stg.Split(null as char[], StringSplitOptions.RemoveEmptyEntries);

			/* set the search URL */
			searchUrl = "kw=" + WebUtility.UrlEncode(String.Join(" ", words)) + searchUrlSeparator;
			searchString = String.Join(", ", words);
			return words;
		}



		/*
		 * Prepare bits of the query as needed
		 */
		private string limitq()
		{
			if( limit > 0 ) {
			    if( offset > 0 )
			        return "limit " + offset + ", " + limit;
			      else
			        return "limit " + limit;
		    } else
				return "";
		}







		/*
		 * Return a count of search results for the given search query
		 */
		public int count()
		{
			/* failsafe */
			if( words.Length == 0 )
				return 0;

			using(MySql.Data.MySqlClient.MySqlConnection my_dbh = new MySqlConnection())
			{
				// create the DB connection
			    my_dbh.ConnectionString = Defines.myConnectionString;
			    my_dbh.Open();

				/* Our database command */
				using(MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand())
				{
					cmd.Connection = my_dbh;

					/* build our keyword query */
					string subq = "";
					int subq_idx = 0;
					foreach(string w in words)
					{
						/* add the parameters */
						subq = subq + "word=@w" + subq_idx + " or ";
						cmd.Parameters.AddWithValue("@w" + subq_idx, w);
						/* and be sure to increment the parameter index!! */
						subq_idx++;
					}
		
					/* ok!  we have a query, so let's go */
					subq = "(" + subq.Substring(0,subq.Length-4) + ")";
		
					/* now it gets a little trickier */
					string havingq = "";
					if( absoluteMatch ) {
						havingq = "having c=@c";
						cmd.Parameters.AddWithValue("@c", words.Length);
					}
		
					/* limit? */
					string limitq = "";
					if( limit > 0 ) {
						if( offset > 0 )
							limitq = "limit " + offset + ", " + limit;
						else
							limitq = "limit " + limit;
					}
		
					/* let's get a total possible count */
					cmd.CommandText =
						"select count(*) from (" +
							"select count(weight) as c,id from " + kwTable +
							" where " + subq + " group by id " + havingq +
						") as z";
					cmd.Prepare();
		
					/* And we're done */
					return Convert.ToInt32(cmd.ExecuteScalar());
				}

			}

		}



		/*
		 * Return the search results for the given query
		 */
		public int[] search()
		{
			/* failsafe */
			if( words.Length == 0 )
				return null;

			using(MySql.Data.MySqlClient.MySqlConnection my_dbh = new MySqlConnection())
			{
				// create the DB connection
			    my_dbh.ConnectionString = Defines.myConnectionString;
			    my_dbh.Open();

				/* Our database command */
				using(MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand())
				{
					cmd.Connection = my_dbh;

					/* build our keyword query */
					string subq = "";
					int subq_idx = 0;
					foreach(string w in words)
					{
						/* add the parameters */
						subq = subq + "word=@w" + subq_idx + " or ";
						cmd.Parameters.AddWithValue("@w" + subq_idx, w);
						/* and be sure to increment the parameter index!! */
						subq_idx++;
					}
		
					/* ok!  we have a query, so let's go */
					subq = "(" + subq.Substring(0,subq.Length-4) + ")";
		
					/* now it gets a little trickier */
					string havingq = "";
					if( absoluteMatch ) {
						havingq = "having c=@c";
						cmd.Parameters.AddWithValue("@c", words.Length);
					}
		
					/* limit? */
					string limitq = "";
					if( limit > 0 ) {
						if( offset > 0 )
							limitq = "limit " + offset + ", " + limit;
						else
							limitq = "limit " + limit;
					}
		
					/* let's do our keyword match */
					cmd.CommandText =
						"select count(weight) as c,sum(weight) as w,id from " +
							kwTable + " where " + subq + " group by id " +
							havingq + " order by c desc,w desc " + limitq;
					cmd.Prepare();

					/* And read out our data */
					using(MySqlDataReader r = cmd.ExecuteReader())
					{
						List<int> ids = new List<int>();
						while( r.Read() )
						{
							ids.Add(Convert.ToInt32(r.GetString(2)));
						}
			
						/* And we're done */
						return ids.ToArray();
						
					}
		
				}

			}

		}



//   function clearKeywords( $id ) {
//     global $db;
//     $db->query("delete from {$this->kwTable} where id=?",$id);
//   }
// 
// 
//   function removeKeywords( $id, $text ) {
//     global $db;
// 
//     $id = (int)$id;
// 
//     /* process text */
//     $text = preg_replace("/(\s|-)+/"," ",$text);
//     $text = preg_replace("/'+/","",$text);
//     $text = preg_replace("/[^a-zA-Z0-9 ]+/","",$text);
//     $text = trim(strtolower($text));
// 
//     /* we may not want to split here */
//     if( $nosplit )
//       $words[] = $text;
//     else
//       $words = explode(' ',$text);
// 
//     if( $words ) {
//       foreach( $words as $w ) {
// 
//       if( strlen($w) < 3 )
//         continue;
// 
//       $res = $db->query("select count(*) from {$this->kwTable} where word=? and id=?", array($w,$id) );
//       $res->fetchInto($row,DB_FETCHMODE_ASSOC);
// 
//       if( $row['count'] > 1 )
//         $db->query("update {$this->kwTable} set count=count-1 where word=? and id=?", array($w,$id) );
//       else
//         $db->query("delete from {$this->kwTable} where word=?' and id=?", array($w,$id) );
// 
//       }
// 
//     }
// 
//   }
// 
// 
// 
//   function buildKeywords( $id, $text, $allowshort = FALSE ) {
//     global $db;
// 
//     /* process text */
//     $text = preg_replace("/[^a-zA-Z0-9 ]+/"," ",$text);
//     $text = preg_replace("/(\s|-)+/"," ",$text);
//     $text = preg_replace("/'+/","",$text);
//     $text = trim(strtolower($text));
// 
//     /* we may not want to split here */
//     $words = explode(' ',$text);
// 
//     /* if we still have words to process .. */
//     if( $words ) {
// 
//       $uniq = array();
// 
//       /* build the unique word array */
//       foreach( $words as $w ) {
// 
//         if( strlen($w) < 3 and !$allowshort )
//           continue;
// 
//         if( !isset($uniq[$w]) )
//           $uniq[$w] = 1;
//         else
//           $uniq[$w]++;
// 
//       }
// 
// 
//       /* and, if we've got unique words, process them */
//       if( count($uniq) ) {
// 
//         foreach( $uniq as $w => $ct ) {
// 
//           $res = $db->query("select count from {$this->kwTable} where word=? and id=?", array($w, $id) );
//           $res->fetchInto($row, DB_FETCHMODE_ASSOC);
//           $cur_ct = $row['count'];
// 
//           if( $cur_ct )
//             $db->query("update {$this->kwTable} set count=? where word=? and id=?", array($ct + $cur_ct, $w, $id) );
//           else
//             $db->query("insert into {$this->kwTable} (word,count,weight,id) values (?,?,1,?)", array($w, $ct, $id) );
// 
// 
//           /* now rebuild the weights */
//           if( !$this->buildPartial ) {
//             $res = $db->query("select max(count) as max from {$this->kwTable} where word=?", $w);
//             $res->fetchInto($row, DB_FETCHMODE_ASSOC);
//             $max = $row['max'];
// 
//             $db->query("update {$this->kwTable} set weight=count/? where word=?", array($max,$w) );
//           }
// 
//         }
// 
//       }
// 
//     }
// 
//   }
// 
// 
// 
//   function updateCounts() {
//     global $db;
// 
//     $res = $db->query("select distinct word from {$this->kwTable}");
//     while( $res->fetchInto($row) ) {
// 
//       list($w) = $row;
// 
//       $r2 = $db->query("select max(count) as max from {$this->kwTable} where word=?", $w);
//       $r2->fetchInto($row, DB_FETCHMODE_ASSOC);
//       $max = $row['max'];
// 
//       $db->query("update {$this->kwTable} set weight=count/? where word=?", array($max, $w) );
// 
//     }
// 
//   }


	}

}
