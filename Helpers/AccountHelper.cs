using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SearchProcurement.Controllers;

namespace SearchProcurement.Helpers
{

    public static class AgencyTypes
    {
        public const string GovernmentNP = "government_np";
        public const string Private = "private";
    }

    public static class PaymentTokenType
    {
        public const string Single = "single";
        public const string Umbrella = "umbrella";
    }

    public static class AccountHelper
    {
        /**
         * Return the name identifier from the claim.  If we're logged in via auth0,
         * we'll alwys have this and it will be unique for every auth0 user.
         * @return string The name identifier
         */
        public static string readNameIdentifier(this Controller a)
        {
            return a.User.Claims.
                Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").
                Select(v => v.Value).
                FirstOrDefault();
        }


    }
}
