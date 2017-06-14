using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Stripe;
using Amazon;
using Amazon.S3;
using Newtonsoft.Json;

using SearchProcurement.Models;
using SearchProcurement.Helpers;

namespace SearchProcurement.Controllers
{
    public class AccountController : Controller
    {

//        IAmazonS3 S3Client { get; set; }
        private IHostingEnvironment _environment;


        /**
         * Constructor
         */
        public AccountController(IHostingEnvironment environment /*IAmazonS3 s3Client*/)
        {
            // Inject the IHostingEnvironment
            _environment = environment;

            // Dependency-inject the s3 client
            //this.S3Client = s3Client;
        }



        public IActionResult Index()
        {
            // Have we seen this unique identifier before?  If no, send them to the new account page
            string uniq = this.readNameIdentifier();
            if( !Agency.isKnownAgency(uniq) )
                return Redirect("/account/NewAccount");

            // Yep, they're good, they can stay here
            Agency a = new Agency();
            a.loadIdByAgencyIdentifier(uniq);
            a.loadDataByAgencyIdentifier(uniq);

            // Load up the listings
            Listing[] activeListings = a.getActiveListings();
            Listing[] inactiveListings = a.getInactiveListings();

            // And stash the data
            ViewBag.nameidentifier = uniq;
            ViewBag.activeListings = activeListings;
            ViewBag.inactiveListings = inactiveListings;

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
        public IActionResult MyAccount()
        {
            // Never seen 'em before?  They shouldn't be here
            string uniq = this.readNameIdentifier();
            if( !Agency.isKnownAgency(uniq) )
                return Redirect("/account/NewAccount");

            // Yep, they're good, they can stay here
            Agency a = new Agency();
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
        public IActionResult MyAccountPost(Agency agency)
        {
            // Have we seen this unique identifier before?
            string uniq = this.readNameIdentifier();
            if( !Agency.isKnownAgency(uniq) )
                return Redirect("/account/NewAccount");

            // So we have a valid model in account now...  Let's just save it
            // and bump them to their account page
            agency.loadIdByAgencyIdentifier(uniq);
            agency.update();

            // Did we get a new logo?
            if( HttpContext.Request.Form["logoData"] != "" )
            {
                // Remove the old one first
                if( !string.IsNullOrEmpty(agency.AgencyLogo) )
                    agency.removeLogo();

                // And save the new logo to s3
                agency.saveLogo(HttpContext.Request.Form["logoData"]);
            }
            else if( HttpContext.Request.Form["removeOldLogo"] == "1" )
            {
                // Remove the old logo as requested
                if( !string.IsNullOrEmpty(agency.AgencyLogo) )
                    agency.removeLogo();
            }

            // And show the account
            ViewBag.message = "I've saved your information!";
            return View(agency);
        }









        [Authorize]
        public IActionResult NewAccount()
        {
            // Have we seen this unique identifier before?  If so, send 'em to their account page
            string uniq = this.readNameIdentifier();
            if( Agency.isKnownAgency(uniq) )
                return Redirect("/account");

            // Yep, they're good, they can stay here
            Agency a = new Agency();
            return View(a);
        }




        /**
         * The POST endpoint for adding new accounts.
         */
        [Authorize]
        [HttpPost]
        [ActionName("NewAccount")]
        [ValidateAntiForgeryToken]
        public IActionResult NewAccountPost(Agency agency)
        {
            // Have we seen this unique identifier before?  If so, send 'em to their account page
            string uniq = this.readNameIdentifier();
            if( Agency.isKnownAgency(uniq) )
                return Redirect("/account");

            // So we have a valid model in account now...  Let's just save it
            // and bump them to their account page
            agency.add(uniq, HttpContext.Features.Get<IHttpRequestFeature>().Headers["X-Real-IP"]);
            if( !string.IsNullOrEmpty(HttpContext.Request.Form["logoData"]) )
                agency.saveLogo(HttpContext.Request.Form["logoData"]);

            return View();
        }



        [Authorize]
        public IActionResult checkEmail(string email)
        {
            // Do we have a logged-in user, maybe updating their email?
            // If so, then their own email shouldn't match as an existing email...
            string uniq = this.readNameIdentifier();
            if( Agency.isKnownAgency(uniq) )
            {
                // Yep, they're good, they can stay here
                Agency a = new Agency();
                a.loadDataByAgencyIdentifier(uniq);

                // Saving the same email?  Then we pass the email-in-use check..
                if( a.UserEmailAddress == email )
                    return StatusCode(200);
                else
                    // New email?  Then check if the email exists, and return a good/bad status code as needed
                    return Agency.emailExists(email) ? StatusCode(418) : StatusCode(200);

            }
            else
                // Just check to see if the email exists, and return a good/bad status code as needed
                return Agency.emailExists(email) ? StatusCode(418) : StatusCode(200);

        }


    }
}
