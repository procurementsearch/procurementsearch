using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SearchProcurement.Controllers;

namespace SearchProcurement.Helpers
{
	public class Address
	{
		public string Address1 { get; set; }
		public string Address2 { get; set; }
		public string City { get; set; }
		public string State { get; set; }
		public string Country { get; set; }
        public string Postal { get; set; }
		public string Phone { get; set; }
	}

}
