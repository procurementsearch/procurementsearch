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
    public class VendorController : Controller
    {

//        IAmazonS3 S3Client { get; set; }
        private IHostingEnvironment _environment;


        /**
         * Constructor
         */
        public VendorController(IHostingEnvironment environment /*IAmazonS3 s3Client*/)
        {
            // Inject the IHostingEnvironment
            _environment = environment;

            // Dependency-inject the s3 client
            //this.S3Client = s3Client;
        }



        public IActionResult Index()
        {
            // Have we seen this unique identifier before?  If no, send them to the new account page
            string uniq = this.getAuth0UniqueId();
            if( !Vendor.isKnownVendor(uniq) )
                return Redirect("/vendor/NewAccount");

            // Yep, they're good, they can stay here
            Vendor v = new Vendor();
            v.loadIdByVendorIdentifier(uniq);
            v.loadDataByVendorIdentifier(uniq);

            // Make sure we're starting fresh
            HttpContext.Session.Remove(Defines.SessionKeys.LocationId);

            // And stash the data
            ViewBag.nameidentifier = uniq;

            return View(v);

        }

        public IActionResult Login(string returnUrl = "/vendor")
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
            string uniq = this.getAuth0UniqueId();
            if( !Vendor.isKnownVendor(uniq) )
                return Redirect("/vendor/NewAccount");

            // Yep, they're good, they can stay here
            Vendor v = new Vendor();
            v.loadDataByVendorIdentifier(uniq);
            return View(v);
        }



        /**
         * The POST endpoint for updating an existing account.
         */
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("MyAccount")]
        public IActionResult MyAccountPost(Vendor vendor)
        {
            // Have we seen this unique identifier before?
            string uniq = this.getAuth0UniqueId();
            if( !Vendor.isKnownVendor(uniq) )
                return Redirect("/vendor/NewAccount");

            // So we have a valid model in account now...  Let's just save it
            // and bump them to their account page
            vendor.loadIdByVendorIdentifier(uniq);
            vendor.update();

            // And show the account
            ViewBag.message = "I've saved your information!";
            return View(vendor);
        }









        [Authorize]
        public IActionResult NewAccount()
        {
            // Have we seen this unique identifier before?  If so, send 'em to their account page
            string uniq = this.getAuth0UniqueId();
            if( Vendor.isKnownVendor(uniq) )
                return Redirect("/vendor");

            // Yep, they're good, they can stay here
            Vendor v = new Vendor();
            return View(v);
        }




        /**
         * The POST endpoint for adding new accounts.
         */
        [Authorize]
        [HttpPost]
        [ActionName("NewAccount")]
        [ValidateAntiForgeryToken]
        public IActionResult NewAccountPost(Vendor vendor)
        {
            // Have we seen this unique identifier before?  If so, send 'em to their account page
            string uniq = this.getAuth0UniqueId();
            if( Vendor.isKnownVendor(uniq) )
                return Redirect("/vendor");

            // So we have a valid model in account now...  Let's just save it
            // and bump them to their account page
            vendor.add(uniq, HttpContext.Features.Get<IHttpRequestFeature>().Headers["X-Real-IP"]);
            return View("NewAccountPost");
        }



        [Authorize]
        public IActionResult checkEmail(string email)
        {
            // Do we have a logged-in user, maybe updating their email?
            // If so, then their own email shouldn't match as an existing email...
            string uniq = this.getAuth0UniqueId();
            if( Vendor.isKnownVendor(uniq) )
            {
                // Yep, they're good, they can stay here
                Vendor v = new Vendor();
                v.loadDataByVendorIdentifier(uniq);

                // Saving the same email?  Then we pass the email-in-use check..
                if( v.VendorEmailAddress == email )
                    return StatusCode(200);
                else
                    // New email?  Then check if the email exists, and return a good/bad status code as needed
                    return Vendor.emailExists(email) ? StatusCode(418) : StatusCode(200);

            }
            else
                // Just check to see if the email exists, and return a good/bad status code as needed
                return Vendor.emailExists(email) ? StatusCode(418) : StatusCode(200);

        }



        /**
         * Get a list of certifications for the given state
         */
        public IActionResult getCertsForState(string state)
        {
            var certs = Vendor.getPossibleCertifications(state);
            return Json(certs);
        }

    }
}
