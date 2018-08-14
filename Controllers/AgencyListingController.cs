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

using Newtonsoft.Json;

using SearchProcurement.Models;
using SearchProcurement.Helpers;

namespace SearchProcurement.Controllers
{
    public class AgencyListingController : Controller
    {

//        IAmazonS3 S3Client { get; set; }
        private IHostingEnvironment _environment;


        /**
         * Constructor
         */
        public AgencyListingController(IHostingEnvironment environment /*IAmazonS3 s3Client*/)
        {
            // Inject the IHostingEnvironment
            _environment = environment;

            // Dependency-inject the s3 client
            //this.S3Client = s3Client;
        }






        /**
         * Show the umbrella listing overlay
         * @param int id The listing ID to show subcontracts for
         */
        [Authorize]
        [Route("/Agency/setupUmbrella")]
        public IActionResult SetupUmbrella(int id)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/newAccount");

            // Let's try to load the listing
            Listing l = new Listing();
            l.loadById(id);

            // Make sure we own it
            if( l.AgencyId != a.AgencyId )
                return Redirect("/Agency");

            return View(l);

        }










        [Authorize]
        [Route("/Agency/newListing")]
        public IActionResult NewListing(int? locId, string listingType)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/newAccount");

            // Try to use the session location, if we didn't get one in the query string.
            // If the session location is also empty, this'll do nothing, which is the
            // right thing anyway.
            if( locId == null )
                locId = HttpContext.Session.GetInt32(Defines.SessionKeys.LocationId);


            // Yep, they're good.  So, did we get a location ID?
            if( locId != null )
            {
                // Save the location for the redirect
                HttpContext.Session.SetInt32(Defines.SessionKeys.LocationId, locId.Value);

                // Did they select a location AND a listing type?  OR ... are they
                // a government agency, and can only use simple listing types, and
                // have one available?
                if (a.AgencyType == AgencyTypes.GovernmentNP && a.getPaymentTokens(locId.Value, ListingTypes.Simple) > 0)
                {
                    // They are a government agency, so they can only use simple listings, no umbrella listings
                    // TODO See if this varies by state, and is an Oregon-specific thing, or if it is true everywhere
                    HttpContext.Session.SetString(Defines.SessionKeys.ListingType, ListingTypes.Simple);
                    return Redirect("/Agency/addListing");
                }
                else if(
                    (listingType == ListingTypes.Simple && a.getPaymentTokens(locId.Value, ListingTypes.Simple) > 0) ||
                    (listingType == ListingTypes.Umbrella && a.getPaymentTokens(locId.Value, ListingTypes.Umbrella) > 0) )
                {
                    // OK!  They've selected a listing type AND they have a payment token for it
                    HttpContext.Session.SetString(Defines.SessionKeys.ListingType, listingType);
                    return Redirect("/Agency/addListing");
                }
                else
                {
                    // They can post here--let's let them
                    ViewBag.simpleTokens = a.getPaymentTokens(locId.Value, ListingTypes.Simple);
                    ViewBag.umbrellaTokens = a.getPaymentTokens(locId.Value, ListingTypes.Umbrella);
                    ViewBag.locationName = LocationHelper.getNameForId(locId.Value);
                    ViewBag.locationId = locId.Value;

                    if( a.AgencyType == AgencyTypes.GovernmentNP )
                        return View("NewListingPayPub", a);
                    else
                        return View("NewListingPayPriv", a);
                }

            }
            else
                // No location ID means no whiskey!
                return View(a);

        }






        [Authorize]
        [Route("/Agency/newListingActivate")]
        public IActionResult NewListingActivate(string listingType)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/NewAccount");

            // Failsafe
            int? locId = HttpContext.Session.GetInt32(Defines.SessionKeys.LocationId);
            if( locId == null )
                return Redirect("/Agency/newListing");

            // And set up the view
            ViewBag.simpleTokens = a.getPaymentTokens(locId.Value, ListingTypes.Simple);
            ViewBag.umbrellaTokens = a.getPaymentTokens(locId.Value, ListingTypes.Umbrella);

            return View(a);

        }





        [Authorize]
        [Route("/Agency/closeListing")]
        public IActionResult closeListing(int? id)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/NewAccount");

            // Load the listing
            Listing l = new Listing();
            l.loadById(id.Value);

            // Make sure we own it
            if( l.AgencyId != a.AgencyId )
                return Redirect("/Agency");

            // Update the status
            l.updateStatus(ListingStatus.Closed);

            return View("CloseListing");

        }





        [Authorize]
        [Route("/Agency/addListing")]
        public IActionResult addListing(int? id)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/NewAccount");

            // Make sure they've selected a location Id
            int? locId = HttpContext.Session.GetInt32(Defines.SessionKeys.LocationId);
            if( locId == null )
                return Redirect("/Agency/newListing");

            // If they can't post here, send them to the payment screen
            if( !a.hasAvailablePaymentToken(locId.Value) )
                return Redirect("/Agency/newListing?locId=" + locId.Value);

            // OK, they've picked a location and they've paid for it ..
            Listing l = new Listing();

            // Get the item
            ViewBag.listingLocation = LocationHelper.getNameForId(locId.Value);
            ViewBag.locId = locId.Value;
            return View("~/Views/AgencyListing/SetupListing.cshtml", l);

        }






        [Authorize]
        [Route("/Agency/editListing")]
        public IActionResult editListing(int id)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/NewAccount");

            // They're editing an existing listing .. let's load it before showing the view
            Listing l = new Listing();
            l.loadById(id);

            // Now save these files to the session, so we can access them by GUID
            HttpContext.Session.SetString(Defines.SessionKeys.Files, JsonConvert.SerializeObject(l.BidDocuments));

            // Get the item top-level location ID
            ViewBag.locId = l.PrimaryLocationId;
            ViewBag.listingLocation = LocationHelper.getNameForId(l.PrimaryLocationId);
            ViewBag.id = id;
            return View("~/Views/AgencyListing/SetupListing.cshtml", l);

        }






        /**
         * The POST endpoint for adding a new RFP listing
         */
        [Authorize]
        [HttpPost]
        [Route("/Agency/setupListing")]
        [ValidateAntiForgeryToken]
        public IActionResult SetupListingPost(int? id, Listing listing)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/NewAccount");

            // Which button did they click on?
            string action = HttpContext.Request.Form["action"];

            // Cancel!
            if( action == "cancel" )
                return Redirect("/Agency");

            // Are we adding, or updating?
            if( id == null )
            {
                // First, a failsafe, to make sure they have a location
                int? locId = HttpContext.Session.GetInt32(Defines.SessionKeys.LocationId);
                if( locId == null )
                    return Redirect("/Agency/newListing");

                // And a failsafe, to make sure they have a payment token when they're
                // adding a new listing
                string listingType = HttpContext.Session.GetString(Defines.SessionKeys.ListingType);
                if( listingType == null || a.getPaymentTokens(locId.Value, listingType) == 0 )
                    return Redirect("/Agency/newListing");

                // No ID means it's a new listing
                listing.AgencyId = a.AgencyId;

                // Add the listing with the assigned status
                listing.add(
                    action == "draft" ? ListingStatus.Draft : ListingStatus.Published,
                    listingType,
                    HttpContext.Features.Get<IHttpRequestFeature>().Headers["X-Real-IP"]
                );

                // And set the location(s)
                listing.addLocationById(locId.Value);
                if( !string.IsNullOrEmpty(HttpContext.Request.Form["secondary_location_id"]) )
                    listing.addLocationById(Convert.ToInt32(HttpContext.Request.Form["secondary_location_id"]));

                // And add the attachments
                string sessionFilesJson = HttpContext.Session.GetString(Defines.SessionKeys.Files);
                if( sessionFilesJson != null )
                    AttachmentHelper.processFiles(JsonConvert.DeserializeObject<Attachment []>(sessionFilesJson), HttpContext, listing);

                // And at last, do we need to register a payment token as having been used?
                a.usePaymentToken(locId.Value, listingType, HttpContext.Features.Get<IHttpRequestFeature>().Headers["X-Real-IP"]);

                // And show the right breadcrumbs
                ViewBag.isNew = true;

            }
            else
            {
                // Be sure to set the old listing ID, since the model binding doesn't
                // catch this
                listing.ListingId = id.Value;

                // A failsafe, in case they're trying to be tricky
                if( listing.getAgencyId() != a.AgencyId )
                    return Redirect("/Agency");  // This should never happen!


                // They are saving an existing listing, so we have a somewhat complicated
                // scheme for determining its status:
                // Draft -> Save as draft // Publish now
                // Published, not live -> Save as draft // Save as revision
                // Published, live -> Save as addendum // Save as revision
                string updateMode = "";
                string origStatus = ListingHelper.getStatus(listing.ListingId);

                if( origStatus == ListingStatus.Draft || origStatus == ListingStatus.Published )
                {
                    if( action == "draft" )
                        listing.Status = ListingStatus.Draft;
                    else
                        listing.Status = ListingStatus.Published;
                    updateMode = ListingUpdateMode.Revision;
                }
                else
                {
                    // If it's not a draft and it's not in published mode, it's got to be an open listing
                    listing.Status = origStatus;
                    if( action == "revision" )
                        updateMode = ListingUpdateMode.Revision;
                    else
                        updateMode = ListingUpdateMode.Addendum;
                }

                // And, update!
                listing.update(updateMode);

                // Then, update the listing secondary location
                listing.loadLocations();

                // Remove the old location listing and save the new one
                if( listing.SecondaryLocationIds.Length != 0 )
                {
                    foreach (int myId in listing.SecondaryLocationIds)
                        listing.removeLocationById(myId);
                }

                // Save the new one only if we've actually set a new one
                if( !string.IsNullOrEmpty(HttpContext.Request.Form["secondary_location_id"]) )
                    listing.addLocationById(Convert.ToInt32(HttpContext.Request.Form["secondary_location_id"]));

                // OK!  Now, this is a little tricky, saving out the addenda...
                string[] oldTitles = AttachmentHelper.getAttachmentTitles(listing.ListingId);

                // And save new attachments, if we have any new attachments
                string sessionFilesJson = HttpContext.Session.GetString(Defines.SessionKeys.Files);
                if( sessionFilesJson != null )
                    AttachmentHelper.processFiles(JsonConvert.DeserializeObject<Attachment []>(sessionFilesJson), HttpContext, listing);


                // Continuing the attachment diffing...
                string[] newTitles = AttachmentHelper.getAttachmentTitles(listing.ListingId);
                if( updateMode == ListingUpdateMode.Addendum )
                    if( Library.diffStringArrays(oldTitles, newTitles).Length > 0 )
                        ListingHelper.logAddendum(listing.ListingId, "attachments", String.Join("\n", oldTitles), String.Join("\n", newTitles));

            }

            // Empty out the session data
            HttpContext.Session.Remove(Defines.SessionKeys.LocationId);
            HttpContext.Session.Remove(Defines.SessionKeys.ListingType);
            HttpContext.Session.Remove(Defines.SessionKeys.Files);

            return View();
        }









        [Authorize]
        [Route("/Agency/addSublisting")]
        public IActionResult AddSublisting(int parentId)
        {
            // Have we seen this unique identifier before?  If not, they really shouldn't be here
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/NewAccount");

            // Make sure they gave us a parent ID
            if( parentId == 0 )
                return Redirect("/Agency");

            // Make sure that the listing they're attaching this subcontract to,
            // they own that listing ...
            Listing parent = new Listing();
            parent.loadById(parentId);
            if( parent.AgencyId != a.AgencyId )
                return Redirect("/Agency");

            // Get the item
            Listing l = new Listing();
            ViewBag.projectTitle = parent.Title;
            ViewBag.parentId = parentId;
            return View("~/Views/AgencyListing/SetupSublisting.cshtml", l);

        }





        [Authorize]
        [Route("/Agency/editSublisting")]
        public IActionResult EditSublisting(int parentId, int id)
        {
            // Have we seen this unique identifier before?  If not, they really shouldn't be here
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/NewAccount");

            // Load the parent, to get the title/project description
            Listing parent = new Listing();
            parent.loadById(parentId);

            // They're editing an existing listing .. let's load it and verify ownership
            Listing l = new Listing();
            l.loadById(id);
            if( l.AgencyId != a.AgencyId )
                return Redirect("/Agency");

            // Now save these files to the session, so we can remove them by GUID
            HttpContext.Session.SetString(Defines.SessionKeys.Files, JsonConvert.SerializeObject(l.BidDocuments));

            // Get the item
            ViewBag.projectTitle = parent.Title;
            ViewBag.parentId = parentId;
            ViewBag.id = id;
            return View("~/Views/AgencyListing/SetupSublisting.cshtml", l);

        }






        /**
         * The POST endpoint for saving a subcontract
         */
        [Authorize]
        [HttpPost]
        [Route("/Agency/setupSublisting")]
        [ValidateAntiForgeryToken]
        public IActionResult SetupSublistingPost(int parentId, int? id, Listing listing)
        {
            // Have we seen this unique identifier before?  If not, they really shouldn't be here
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/NewAccount");

            // Which button did they click on?
            string action = HttpContext.Request.Form["action"];

            // Cancel!
            if( action == "cancel" )
                return Redirect("/Agency");

            if( id == null )
            {
                // No ID means we're adding a listing
                listing.AgencyId = a.AgencyId;

                // Add the listing with the assigned status
                listing.add("", ListingTypes.Simple, HttpContext.Features.Get<IHttpRequestFeature>().Headers["X-Real-IP"]);
                listing.setParent(parentId);
            }
            else
            {
                // Be sure to set the old listing ID, since the model binding doesn't
                // catch this
                listing.ListingId = id.Value;

                // A failsafe, in case they're trying to be tricky
                if( listing.getAgencyId() != a.AgencyId )
                    return Redirect("/Agency");  // This should never happen!

                // And, update!
                listing.update(ListingUpdateMode.Revision);
            }


            // And save new attachments, if we have any new attachments
            string sessionFilesJson = HttpContext.Session.GetString(Defines.SessionKeys.Files);
            if( sessionFilesJson != null )
                AttachmentHelper.processFiles(JsonConvert.DeserializeObject<Attachment []>(sessionFilesJson), HttpContext, listing);

            // Empty out the session data
            HttpContext.Session.Remove(Defines.SessionKeys.Files);

            return View();
        }





        /**
         * Remove the sublisting from an umbrella contract
         * @param int id The listing ID of the subcontract to remove
         */
        [Authorize]
        [Route("/Agency/removeSublisting")]
        public IActionResult RemoveSublisting(int id)
        {
            // Have we seen this unique identifier before?  If no, send them to the new account page
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/NewAccount");

            // Let's try to load the listing
            Listing l = new Listing();
            l.loadById(id);

            // Is it really ours?
            if( l.getAgencyId() == a.AgencyId )
            {
                // OK!  Remove the listing
                l.removeListing();
                return StatusCode(200);
            }
            else
                // It is not!  Do nothing...
                return StatusCode(418);

        }




        /**
         * Show the screen to add an intent to award
         * @param int id The listing ID to show intent to award for
         */
        [Authorize]
        [Route("/Agency/addIntentToAward")]
        public IActionResult AddIntentToAward(int id)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/newAccount");

            // Let's try to load the listing
            Listing l = new Listing();
            l.loadById(id);

            // Make sure we own it
            if( l.AgencyId != a.AgencyId )
                return Redirect("/Agency");

            // Now save these files to the session, so we can access them by GUID
            Attachment[] documents = AttachmentHelper.filterIntentToAward(l.BidDocuments);
            HttpContext.Session.SetString(Defines.SessionKeys.Files, JsonConvert.SerializeObject(documents));

            // Stash some data in the ViewBag
            ViewBag.id = id;
            ViewBag.awardDocuments = documents;

            return View(l);

        }





        /**
         * The POST endpoint for adding an intent to award
         */
        [Authorize]
        [HttpPost]
        [Route("/Agency/addIntentToAward")]
        [ValidateAntiForgeryToken]
        public IActionResult AddIntentToAwardPost(int? id, Listing listing)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/NewAccount");

            // Cancel!  Also, no ID?  No whiskey!
            if( id == null || HttpContext.Request.Form["action"] == "cancel" )
                return Redirect("/Agency");

            // Be sure to set the old listing ID, since the model binding doesn't
            // catch this
            listing.loadById(id.Value);

            // A failsafe, in case they're trying to be tricky
            if( listing.getAgencyId() != a.AgencyId )
                return Redirect("/Agency");  // This should never happen!

            // And, update!
            listing.IntentToAward = HttpContext.Request.Form["IntentToAward"];
            listing.update(ListingUpdateMode.Revision);

            // And save new attachments, if we have any new attachments
            string sessionFilesJson = HttpContext.Session.GetString(Defines.SessionKeys.Files);
            if( sessionFilesJson != null )
            {
                AttachmentHelper.processFiles(
                    AttachmentHelper.toggleIntentToAward(
                        JsonConvert.DeserializeObject<Attachment []>(sessionFilesJson), true), HttpContext, listing);
            }

            // Empty out the session data
            HttpContext.Session.Remove(Defines.SessionKeys.Files);
            return View("AddIntentToAwardPost");

        }


        /**
         * Show the screen to add a notice to proceed
         * @param int id The listing ID to show notice to proceed for
         */
        [Authorize]
        [Route("/Agency/addNoticeToProceed")]
        public IActionResult AddNoticeToProceed(int id)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/newAccount");

            // Let's try to load the listing
            Listing l = new Listing();
            l.loadById(id);

            // Make sure we own it
            if( l.AgencyId != a.AgencyId )
                return Redirect("/Agency");

            // Stash some data in the ViewBag
            ViewBag.id = id;

            return View(l);

        }


        /**
         * The POST endpoint for adding a notice to proceed
         */
        [Authorize]
        [HttpPost]
        [Route("/Agency/addNoticeToProceed")]
        [ValidateAntiForgeryToken]
        public IActionResult AddNoticeToProceed(int? id, Listing listing)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/Agency/NewAccount");

            // Cancel!  Also, no ID?  No whiskey!
            if( id == null || HttpContext.Request.Form["action"] == "cancel" )
                return Redirect("/Agency");

            // Be sure to set the old listing ID, since the model binding doesn't
            // catch this
            listing.loadById(id.Value);

            // A failsafe, in case they're trying to be tricky
            if( listing.getAgencyId() != a.AgencyId )
                return Redirect("/Agency");  // This should never happen!

            // And, update!
            listing.IntentToAward = HttpContext.Request.Form["NoticeToProceed"];
            listing.update(ListingUpdateMode.Revision);

            // And save new attachments, if we have any new attachments
            string sessionFilesJson = HttpContext.Session.GetString(Defines.SessionKeys.Files);
            if( sessionFilesJson != null )
            {
                AttachmentHelper.processFiles(
                    AttachmentHelper.toggleNoticeToProceed(
                        JsonConvert.DeserializeObject<Attachment []>(sessionFilesJson), true), HttpContext, listing);
            }

            // Empty out the session data
            HttpContext.Session.Remove(Defines.SessionKeys.Files);
            return View("AddNoticeToProceedPost");

        }



    }
}
