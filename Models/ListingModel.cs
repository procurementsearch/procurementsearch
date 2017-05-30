using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;

using SearchProcurement.Helpers;

namespace SearchProcurement.Models
{

	public class Listing {

        int ListingId;

        [Display(Name="Listing Title")]
        public string Title { get; set; }
        [Display(Name="Listing Description")]
        public string Description { get; set; }
        [Display(Name="Open Date - when the listing should go live")]
        public DateTime OpenDate { get; set; }
        [Display(Name="Close Date - when the listing closes")]
        public DateTime CloseDate { get; set; }
        [Display(Name="Contact Information")]
        public string Contact { get; set; }
        [Display(Name="Listing Action Steps")]
        public string ActionSteps { get; set; }

        // The bid documents
        public Attachment[] BidDocuments { get; set; }

    }

}