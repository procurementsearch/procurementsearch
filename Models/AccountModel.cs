using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;

using SearchProcurement.Helpers;

namespace SearchProcurement.Models
{
    /* The address struct */
	public struct Address
	{
		public string Address1;
		public string Address2;
		public string City;
		public string State;
		public string Country;
        public string Postal;
	}

    public enum AgencyTypes
    {
        [Display(Name="Government/Non-profit")]
        GovernmentNP,
        [Display(Name="Private Sector (General Contractor, etc.)")]
        Private
    }

	public class Account {

        [Display(Name="Your name")]
        public string UserRealName { get; private set; }
        [Display(Name="Your email address")]
        public string UserEmailAddress { get; private set; }
        [Display(Name="The name of your agency")]
        public string AgencyName { get; private set; }

        public AgencyTypes AgencyType { get; private set; }
        [Display(Name="A few words about your agency")]
        public string AgencyAboutText { get; private set; }
        [Display(Name="Your Website")]
        public string AgencyUrl { get; private set; }
        public string AgengyLogo { get; private set; }

        [Display(Name="Contact Person")]
        public string AgencyContactName { get; private set; }
        [Display(Name="Contact Email")]
        public string AgencyContactEmail { get; private set; }
        [Display(Name="Phone Number")]
        public string AgencyPhone { get; private set; }
        [Display(Name="Fax Number")]
        public string AgencyFax { get; private set; }
        public Address BillingAddress { get; private set; }
        public Address ShippingAddress { get; private set; }


        // For the HTML
        public List<SelectListItem> States { get; private set; } = Library.StateListItems();

    }

}