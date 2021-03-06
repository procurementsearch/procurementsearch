using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        private IHostingEnvironment _environment;
        private readonly IHttpContextAccessor _context;

        // The unique ID from Auth0
        private string auth0Id { get; set; }

        /**
         * Constructor
         */
        public AgencyController(IHostingEnvironment environment, IHttpContextAccessor context)
        {
            // Inject the IHostingEnvironment
            _environment = environment;
            _context = context;

            // Get the unique ID because we'll use it everywhere.
            // If it's null, we're not logged in.
            auth0Id = Auth0Helper.getAuth0UniqueIdFromContext(context.HttpContext);

        }



        [Authorize(Policy="VerifiedKnown")]
        public IActionResult Index()
        {
            // load up the agency team member, to make sure they've
            // been assigned to an agency
            AgencyTeam at = new AgencyTeam(auth0Id);
            if( !at.hasAgency() )
                return Redirect("/Agency/NewAccountStage2");

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

        /**
         * Auth0 Login and Logout methods
         */
        public async Task Login(string returnUrl = "/Agency")
        {
            await HttpContext.ChallengeAsync("Auth0", new AuthenticationProperties() { RedirectUri = returnUrl });
        }

        public async Task Logout()
        {
            await HttpContext.SignOutAsync("Auth0", new AuthenticationProperties
            {
                // Indicate here where Auth0 should redirect the user after a logout.
                // Note that the resulting absolute Uri must be whitelisted in the
                // **Allowed Logout URLs** settings for the app.
                RedirectUri = Url.Action("Index", "Home")
            });
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
        public IActionResult MyAgency()
        {
            // Yep, they're good, they can stay here
            Agency a = new Agency(auth0Id);
            return View(a);
        }



        /**
         * The POST endpoint for updating an existing agency.
         */
        [HttpPost]
        [Authorize(Policy="VerifiedKnown")]
        [ValidateAntiForgeryToken]
        public IActionResult MyAgencyPost(Agency agency)
        {
            // Load up the existing one, to retrieve two properties
            Agency my_a = new Agency(auth0Id);

            // So we have a valid model in account now...  Let's just save it
            // and bump them to their account page
            agency.AgencyId = my_a.AgencyId;
            agency.MyLogin = my_a.MyLogin;
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
            return View(agency);
        }






        [Authorize(Policy="VerifiedKnown")]
        public IActionResult MyAccount()
        {
            // Yep, they're good, they can stay here
            AgencyTeam at = new AgencyTeam(auth0Id);
            return View(at);
        }



        /**
         * The POST endpoint for updating an existing account.
         */
        [HttpPost]
        [Authorize(Policy="VerifiedKnown")]
        [ValidateAntiForgeryToken]
        public IActionResult MyAccountPost(AgencyTeam agencyteam)
        {
            // Load up the existing one, to retrieve two properties
            AgencyTeam my_at = new AgencyTeam(auth0Id);

            // So we have a valid model in account now...  Let's just save it
            // and bump them to their account page
            agencyteam.AgencyTeamId = my_at.AgencyTeamId;
            agencyteam.AgencyId = my_at.AgencyId;
            agencyteam.isAdmin = my_at.isAdmin;
            agencyteam.update();

            // And show the account
            return View(agencyteam);
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

            agencyteam.add(auth0Id, HttpContext.Features.Get<IHttpRequestFeature>().Headers["X-Real-IP"]);

            // Has someone invited this email to a team?
            string email = this.readEmailAddress();
            if( AgencyTeam.isPendingTeamInvitation(email) )
            {
                // Get the joined agency name
                ViewBag.joinAgency = AgencyHelper.getTeamInvitationAgencyName(email);

                // Did they join it?
                if( !string.IsNullOrEmpty(HttpContext.Request.Form["joinTeam"]) )
                {
                    // reassign them to this agency, and send them to the
                    // "thanks for joining this agency" view
                    int agencyId = AgencyHelper.getTeamInvitationAgencyId(email);
                    agencyteam.updateAssignedAgency(agencyId);
                    agencyteam.acceptTeamInvitation();

                    return View("NewAccountTeamJoined");
                }
            }

            // Okay, either they didn't join a team, or they weren't invited to
            // join a team, so let's proceed to stage 2
            return Redirect("/Agency/NewAccountStage2");
        }



        [Authorize(Policy="VerifiedKnown")]
        public IActionResult NewAccountStage2()
        {
            AgencyTeam at = new AgencyTeam(auth0Id);
            Agency a = new Agency();

            // Stash the team member object into the ViewBag
            ViewBag.agencyTeam = at;

            // Show the new-account page
            return View(a);
        }



        /**
         * The POST endpoint for adding new accounts.
         */
        [HttpPost]
        [Authorize(Policy="VerifiedKnown")]
        [ValidateAntiForgeryToken]
        public IActionResult NewAccountStage2Post(Agency agency)
        {
            AgencyTeam at = new AgencyTeam(auth0Id);

            // is this account assigned to an agency already?  If so, send 'em to their dashboard
            if( at.hasAgency() )
                return Redirect("/Agency");


            // So we have a valid model in account now...  Let's just save it
            // and bump them to their account page
            agency.add(HttpContext.Features.Get<IHttpRequestFeature>().Headers["X-Real-IP"]);
            if(
                !string.IsNullOrEmpty(HttpContext.Request.Form["logoName"]) &&
                !string.IsNullOrEmpty(HttpContext.Request.Form["logoData"])
            )
            {
                agency.saveLogo(HttpContext.Request.Form["logoName"], HttpContext.Request.Form["logoData"]);
            }

            // And last, update the team account to belong to this agency
            at.updateAssignedAgency(agency.AgencyId);
            at.setAdminStatus(true);

            // that's it!
            return View();
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
