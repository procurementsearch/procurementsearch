using System;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Amazon;
using Amazon.S3;

using SearchProcurement.Models;

namespace SearchProcurement.Controllers
{
    public class AccountController : Controller
    {

//        IAmazonS3 S3Client { get; set; }

        /**
         * Constructor
         */
        public AccountController(/*IAmazonS3 s3Client*/)
        {
            // Dependency-inject the s3 client
            //this.S3Client = s3Client;
        }



        public IActionResult Index()
        {
            // Have we seen this unique identifier before?
            string uniq = readNameIdentifier();

            // Nope, send 'em to the new account page
            if( !Account.isKnownAgency(uniq) )
                return Redirect("/account/NewAccount");

            // Yep, they're good, they can stay here
            Account a = new Account();
            a.loadDataByAgencyIdentifier(uniq);

            ViewBag.nameidentifier = uniq;
            return View(a);

        }

        public IActionResult Login(string returnUrl = "/account")
        {
            return new ChallengeResult("Auth0", new AuthenticationProperties() { RedirectUri = returnUrl });
        }

        public async Task Logout()
        {
            await HttpContext.Authentication.SignOutAsync("Auth0", new AuthenticationProperties
            {
                RedirectUri = Url.Action("Index", "Home")
            });
            await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }




        [Authorize]
        public IActionResult NewRFP()
        {
            // Have we seen this unique identifier before?
            string uniq = readNameIdentifier();

            // Never seen 'em before?  They shouldn't be here
            if( !Account.isKnownAgency(uniq) )
                return Redirect("/account/NewAccount");

            // Yep, they're good, they can start an RFP
            return View();
        }





        [Authorize]
        public IActionResult MyAccount()
        {
            // Have we seen this unique identifier before?
            string uniq = readNameIdentifier();

            // Never seen 'em before?  They shouldn't be here
            if( !Account.isKnownAgency(uniq) )
                return Redirect("/account/NewAccount");

            // Yep, they're good, they can stay here
            Account a = new Account();
            a.loadDataByAgencyIdentifier(uniq);
            return View(a);
        }



        /**
         * The POST endpoint for updating an existing account.
         */
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("MyAccount")]
        public IActionResult MyAccountPost(Account account)
        {
            // Have we seen this unique identifier before?
            string uniq = readNameIdentifier();
            if( !Account.isKnownAgency(uniq) )
                return Redirect("/account/NewAccount");

            // So we have a valid model in account now...  Let's just save it
            // and bump them to their account page
            account.loadIdByAgencyIdentifier(uniq);
            account.update();

            // Did we get a new logo?
            if( HttpContext.Request.Form["logoData"] != "" )
            {
                // Remove the old one first
                if( !string.IsNullOrEmpty(account.AgencyLogo) )
                    account.removeLogo();

                // And save the new logo to s3
                account.saveLogo(HttpContext.Request.Form["logoData"]);
            }
            else if( HttpContext.Request.Form["removeOldLogo"] == "1" )
            {
                // Remove the old logo as requested
                if( !string.IsNullOrEmpty(account.AgencyLogo) )
                    account.removeLogo();
            }

            // And show the account
            ViewBag.message = "I've saved your information!";
            return View(account);
        }









        [Authorize]
        public IActionResult NewAccount()
        {
            // Have we seen this unique identifier before?
            string uniq = readNameIdentifier();

            // Yep, send 'em to their account page
            if( Account.isKnownAgency(uniq) )
                return Redirect("/account");

            // Yep, they're good, they can stay here
            Account a = new Account();
            return View(a);
        }



        /**
         * If someone hits /account/NewAccountPost as a GET request,
         * just redirect them to /account.  That shouldn't ever happen,
         * and there's nothing we can sensibly do with it.
         */
        [Authorize]
        [HttpGet]
        [ActionName("NewAccountPost")]
        public IActionResult NewAccountNonPost(Account account)
        {
            return Redirect("/account");
        }



        /**
         * The POST endpoint for adding new accounts.
         */
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NewAccountPost(Account account)
        {
            // Have we seen this unique identifier before?
            string uniq = readNameIdentifier();

            // Yep, send 'em to their account page
            if( Account.isKnownAgency(uniq) )
                return Redirect("/account");

            // So we have a valid model in account now...  Let's just save it
            // and bump them to their account page
            account.add(uniq, HttpContext.Features.Get<IHttpRequestFeature>().Headers["X-Real-IP"]);
            account.saveLogo(HttpContext.Request.Form["logoData"]);

            return View();
        }



        [Authorize]
        public IActionResult checkEmail(string email)
        {
            // Do we have a logged-in user, maybe updating their email?
            // If so, then their own email shouldn't match as an existing email...
            string uniq = readNameIdentifier();
            if( Account.isKnownAgency(uniq) )
            {
                // Yep, they're good, they can stay here
                Account a = new Account();
                a.loadDataByAgencyIdentifier(uniq);

                // Saving the same email?  Then we pass the email-in-use check..
                if( a.UserEmailAddress == email )
                    return StatusCode(200);
                else
                    // New email?  Then check if the email exists, and return a good/bad status code as needed
                    return Account.emailExists(email) ? StatusCode(418) : StatusCode(200);

            }
            else
                // Just check to see if the email exists, and return a good/bad status code as needed
                return Account.emailExists(email) ? StatusCode(418) : StatusCode(200);

        }


        /**
         * Return the name identifier from the claim.  If we're logged in via auth0,
         * we'll alwys have this and it will be unique for every auth0 user.
         * @return string The name identifier
         */
        private string readNameIdentifier()
        {
            return User.Claims.
                Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").
                Select(v => v.Value).
                FirstOrDefault();
        }

    }
}
