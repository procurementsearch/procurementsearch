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

}
