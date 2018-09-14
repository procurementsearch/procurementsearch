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
    public class AgencyController : Controller
    {

//        IAmazonS3 S3Client { get; set; }
        private IHostingEnvironment _environment;
        private readonly IHttpContextAccessor _context;

        // The unique ID from Auth0
        private string auth0Id { get; set; }

        /**
         * Constructor
         */
        public AgencyController(IHostingEnvironment environment, IHttpContextAccessor context /*IAmazonS3 s3Client*/)
        {
            // Inject the IHostingEnvironment
            _environment = environment;
            _context = context;

            // Dependency-inject the s3 client
            //this.S3Client = s3Client;

            // Get the unique ID because we'll use it everywhere.
            // If it's null, we're not logged in.
            auth0Id = Auth0Helper.getAuth0UniqueIdFromContext(context.HttpContext);

        }



        [Authorize(Policy="VerifiedKnown")]
        public IActionResult Index()
        {
            // Yep, they're good, they can stay here
            Agency a = new Agency(auth0Id);

            // Load up the listings
            Listing[] activeListings = a.getActiveListings();
            Listing[] inactiveListings = a.getInactiveListings();

            // Make sure we're starting fresh
            HttpContext.Session.Remove(Defines.SessionKeys.LocationId);

            // And stash the data
            ViewBag.nameidentifier = auth0Id;
            ViewBag.activeListings = activeListings;
            ViewBag.inactiveListings = inactiveListings;

            return View(a);

        }

        public IActionResult Login(string returnUrl = "/Agency")
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
            // Okay, so they're authenticated but not verified -- send them to
            // the awaiting email verification view
            if( User.Identity.IsAuthenticated && !this.isEmailVerified() )
            {
                return Redirect("/Agency/Unverified");
            }
            if( User.Identity.IsAuthenticated && this.isEmailVerified() )
            {
                if( !AgencyHelper.isKnownLogin(auth0Id) )
                {
                    return Redirect("/Agency/NewAccount");
                }
            }

            return View();
        }


        [Authorize]
        public IActionResult Unverified()
        {
            // If it turns out they land here and they are verified,
            // just send them to the agency page and it'll route them
            // to the right place
            if( this.isEmailVerified() )
            {
                return Redirect("/Agency");
            }
            return View();
        }






        [Authorize(Policy="VerifiedKnown")]
        public IActionResult MyAccount()
        {
            // Yep, they're good, they can stay here
            Agency a = new Agency(auth0Id);
            return View(a);
        }



        /**
         * The POST endpoint for updating an existing account.
         */
        [HttpPost]
        [Authorize(Policy="VerifiedKnown")]
        [ValidateAntiForgeryToken]
        [ActionName("MyAccount")]
        public IActionResult MyAccountPost(Agency agency)
        {
            // So we have a valid model in account now...  Let's just save it
            // and bump them to their account page
            agency.AgencyId = AgencyHelper.getIdForAgencyIdentifier(auth0Id);
            agency.update();

            // Did we get a new logo?
            if(
                !string.IsNullOrEmpty(HttpContext.Request.Form["logoName"]) &&
                !string.IsNullOrEmpty(HttpContext.Request.Form["logoData"])
            )
            {
                // Remove the old one first
                if( !string.IsNullOrEmpty(agency.AgencyLogo) )
                    agency.removeLogo();

                // And save the new logo to s3
                agency.saveLogo(HttpContext.Request.Form["logoName"], HttpContext.Request.Form["logoData"]);
            }
            else if( HttpContext.Request.Form["removeOldLogo"] == "1" )
            {
                // Remove the old logo as requested
                if( !string.IsNullOrEmpty(agency.AgencyLogo) )
                    agency.removeLogo();
            }
            else
                agency.loadLogo();  // We just need to load the logo if we haven't done anything else to it

            // And show the account
            ViewBag.message = "I've saved your information!";
            return View(agency);
        }








        [Authorize(Policy="Verified")]
        public IActionResult NewAccount()
        {
            // Have we seen this unique identifier before?  If so, send 'em to their account page
            if( AgencyHelper.isKnownLogin(auth0Id) )
                return Redirect("/Agency");

            // Has someone invited this email to a team?
            string email = this.readEmailAddress();
            if( AgencyTeam.isPendingTeamInvitation(email) )
                ViewBag.joinAgency = AgencyHelper.getTeamInvitationAgencyName(email);

            ViewBag.verifiedEmail = email;

            // Show the new-account page
            AgencyTeam at = new AgencyTeam();
            return View(at);
        }




        /**
         * The POST endpoint for adding new accounts.
         */
        [HttpPost]
        [Authorize(Policy="Verified")]
        [ValidateAntiForgeryToken]
        public IActionResult NewAccountPost(AgencyTeam agencyteam)
        {
            // Have we seen this unique identifier before?  If so, send 'em to their account page
            if( AgencyHelper.isKnownLogin(auth0Id) )
                return Redirect("/Agency");

            // Has someone invited this email to a team?
            string email = this.readEmailAddress();
            if( AgencyTeam.isPendingTeamInvitation(email) )
                ViewBag.joinAgency = AgencyHelper.getTeamInvitationAgencyName(email);


            // So we have a valid model in account now...  Let's just save it
            // and bump them to their account page
            if( !string.IsNullOrEmpty(HttpContext.Request.Form["joinTeam"]) )
            {

}

            agencyteam.add(auth0Id, HttpContext.Features.Get<IHttpRequestFeature>().Headers["X-Real-IP"]);


            // if(
            //     !string.IsNullOrEmpty(HttpContext.Request.Form["logoName"]) &&
            //     !string.IsNullOrEmpty(HttpContext.Request.Form["logoData"])
            // )
            //     agency.saveLogo(HttpContext.Request.Form["logoName"], HttpContext.Request.Form["logoData"]);

            return View("NewAccountPost");
        }



        [Authorize]
        public IActionResult checkEmail(string email)
        {
            // Do we have a logged-in user, maybe updating their email?
            // If so, then their own email shouldn't match as an existing email...
            if( AgencyHelper.isKnownLogin(auth0Id) )
            {
                // Yep, they're good, they can stay here
                Agency a = new Agency(auth0Id);

                // Saving the same email?  Then we pass the email-in-use check..
                if( a.MyLogin.UserEmailAddress == email )
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
