using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

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


        /**
         * Return true if we're a logged-in user with a verified email
         * @return bool Whether this user has verified their email
         */
        public static bool isEmailVerified(this Controller a)
        {
            return a.User.Claims.
                Where(c => c.Type == "email_verified").
                Select(v => v.Value).
                FirstOrDefault() != "false";
        }

    }




public class AccessDeniedAuthorizeAttribute : AuthorizeAttribute
{
    public override void OnAuthorization(AuthorizationContext filterContext)
    {
        base.OnAuthorization(filterContext);
        if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
        {
            filterContext.Result = new RedirectResult("~/Account/Logon");
            return;
        }

        if (filterContext.Result is HttpUnauthorizedResult)
        {
            filterContext.Result = new RedirectResult("~/Account/Denied");
        }
    }
}



}