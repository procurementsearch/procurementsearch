using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SearchProcurement.Controllers;

namespace SearchProcurement.Helpers
{
    public class Auth0Settings
    {
        public string Domain { get; set; }
        public string CallbackUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }


    public static class Auth0Helper
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