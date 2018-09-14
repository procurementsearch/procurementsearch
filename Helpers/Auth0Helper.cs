using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;


using System.Security.Claims;
using System.Threading.Tasks;




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
         * Given an HttpContext, return the name identifier.  If we're logged in
         * via auth0, we'll alwys have this and it will be unique for every auth0 user.
         * @return string The name identifier
         */
        public static string getAuth0UniqueIdFromContext(HttpContext context)
        {
            return context.User.Claims.
                Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").
                Select(v => v.Value).
                FirstOrDefault();
        }



        /**
         * Return the name identifier from the claim, usable from within a controller.
         * If we're logged in via auth0, this will always be unique for every user.
         * @return string The name identifier
         */
        public static string getAuth0UniqueId(this Controller a)
        {
            return a.User.Claims.
                Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").
                Select(v => v.Value).
                FirstOrDefault();
        }


        /**
         * Return the email address from the claim.
         * @return string The email address
         */
        public static string readEmailAddress(this Controller a)
        {
            return a.User.Claims.
                Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress").
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




    public class Auth0KnownUniqueIdRequirement : IAuthorizationRequirement
    {
        public Auth0KnownUniqueIdRequirement() {}
    }



    public class Auth0KnownUniqueIdHandler : AuthorizationHandler<Auth0KnownUniqueIdRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                    Auth0KnownUniqueIdRequirement requirement)
        {
            string uniq = context.User.Claims.
                Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").
                Select(v => v.Value).
                FirstOrDefault();

            if( AgencyHelper.isKnownLogin(uniq) )
                context.Succeed(requirement);

            //TODO: Use the following if targeting a version of
            //.NET Framework older than 4.6:
            //      return Task.FromResult(0);
            return Task.CompletedTask;

        }
    }


}