using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SearchProcurement.Controllers;

namespace SearchProcurement.Helpers
{

    public static class AgencyTypes
    {
        public const string GovernmentNP = "governmentnp";
        public const string Private = "private";
        public const string TribalGovt = "tribalgov";
    }

    public static class PaymentTokenType
    {
        public const string Single = "single";
        public const string Umbrella = "umbrella";
    }

	public class Address
	{
		public string Address1 { get; set; }
		public string Address2 { get; set; }
		public string City { get; set; }
		public string State { get; set; }
		public string Country { get; set; }
        public string Postal { get; set; }
	}

}
