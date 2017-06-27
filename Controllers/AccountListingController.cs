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
    public class AccountListingController : Controller
    {

//        IAmazonS3 S3Client { get; set; }
        private IHostingEnvironment _environment;


        /**
         * Constructor
         */
        public AccountListingController(IHostingEnvironment environment /*IAmazonS3 s3Client*/)
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
        [Route("/Account/setupUmbrella")]
        public IActionResult SetupUmbrella(int id)
        {
            // Have we seen this unique identifier before?  If no, send them to the new account page
            if( !Agency.isKnownAgency(this.readNameIdentifier()) )
                return Redirect("/account/NewAccount");

            // Let's try to load the listing
            Listing l = new Listing();
            l.loadById(id);

            return View(l);

        }










        [Authorize]
        [Route("/Account/newListing")]
        public IActionResult NewListing(int? locId, string listingType)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/account/newAccount");

            // Yep, they're good.  So, did we get a location ID?
            if( locId != null )
            {
                // Save the location for the redirect
                HttpContext.Session.SetInt32(Defines.SessionKeys.LocationId, locId.Value);

                // Did they select a location AND a listing type?
                if( (listingType == ListingTypes.Simple && a.getPaymentTokens(locId.Value, ListingTypes.Simple) > 0) ||
                    (listingType == ListingTypes.Umbrella && a.getPaymentTokens(locId.Value, ListingTypes.Umbrella) > 0) )
                {
                    // OK!  They've selected a listing type AND they have a payment token for it
                    HttpContext.Session.SetString(Defines.SessionKeys.ListingType, listingType);
                    return Redirect("/Account/addListing");
                }
                else
                {
                    // They can post here--let's let them
                    ViewBag.simpleTokens = a.getPaymentTokens(locId.Value, ListingTypes.Simple);
                    ViewBag.umbrellaTokens = a.getPaymentTokens(locId.Value, ListingTypes.Umbrella);
                    ViewBag.locationName = LocationHelper.getNameForId(locId.Value);
                    return View("NewListingPay", a);
                }

            }
            else
                // No location ID means no whiskey!
                return View(a);

        }






        [Authorize]
        [Route("/Account/newListingActivate")]
        public IActionResult NewListingActivate(string listingType)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/account/NewAccount");

            // Failsafe
            int? locId = HttpContext.Session.GetInt32(Defines.SessionKeys.LocationId);
            if( locId == null )
                return Redirect("/account/newListing");

            // And set up the view
            ViewBag.simpleTokens = a.getPaymentTokens(locId.Value, ListingTypes.Simple);
            ViewBag.umbrellaTokens = a.getPaymentTokens(locId.Value, ListingTypes.Umbrella);

            return View(a);

        }






        [Authorize]
        [Route("/Account/addListing")]
        public IActionResult addListing(int? id)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/account/NewAccount");

            // Make sure they've selected a location Id
            int? locId = HttpContext.Session.GetInt32(Defines.SessionKeys.LocationId);
            if( locId == null )
                return Redirect("/account/newListing");

            // If they can't post here, send them to the payment screen
            if( !a.hasAvailablePaymentToken(locId.Value) )
                return Redirect("/account/newListing?locId=" + locId.Value);

            // OK, they've picked a location and they've paid for it ..
            Listing l = new Listing();

            // Get the item
            ViewBag.listingLocation = LocationHelper.getNameForId(locId.Value);
            ViewBag.locId = locId.Value;
            return View("~/Views/AccountListing/SetupListing.cshtml", l);

        }






        [Authorize]
        [Route("/Account/editListing")]
        public IActionResult editListing(int id)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/account/NewAccount");

            // They're editing an existing listing .. let's load it before showing the view
            Listing l = new Listing();
            l.loadById(id);

            // Now save these files to the session, so we can access them by GUID
            HttpContext.Session.SetString(Defines.SessionKeys.Files, JsonConvert.SerializeObject(l.BidDocuments));

            // Get the item top-level location ID
            ViewBag.locId = l.PrimaryLocationId;
            ViewBag.listingLocation = LocationHelper.getNameForId(l.PrimaryLocationId);
            ViewBag.id = id;
            return View("~/Views/AccountListing/SetupListing.cshtml", l);

        }






        /**
         * The POST endpoint for adding a new RFP listing
         */
        [Authorize]
        [HttpPost]
        [Route("/Account/setupListing")]
        [ValidateAntiForgeryToken]
        public IActionResult SetupListingPost(int? id, Listing listing)
        {
            // Have we seen this unique identifier before?
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/account/NewAccount");

            // First, a failsafe, to make sure they have a location
            int? locId = HttpContext.Session.GetInt32(Defines.SessionKeys.LocationId);
            if( locId == null )
                return Redirect("/account/newListing");

            // Which button did they click on?
            string action = HttpContext.Request.Form["action"];

            // Cancel!
            if( action == "cancel" )
                return Redirect("/account");


            // Are we adding, or updating?
            if( id == null )
            {
                // And a failsafe, to make sure they have a payment token when they're
                // adding a new listing
                string listingType = HttpContext.Session.GetString(Defines.SessionKeys.ListingType);
                if( listingType == null || a.getPaymentTokens(locId.Value, listingType) == 0 )
                    return Redirect("/account/newListing");

                // No ID means it's a new listing
                listing.AgencyId = a.AgencyId;

                // Add the listing with the assigned status
                listing.add(
                    action == "draft" ? ListingStatus.Draft : ListingStatus.Published,
                    ListingTypes.Simple,
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

            }
            else
            {
                // Be sure to set the old listing ID, since the model binding doesn't
                // catch this
                listing.ListingId = id.Value;

                // A failsafe, in case they're trying to be tricky
                if( listing.getAgencyId() != a.AgencyId )
                    return Redirect("/account");  // This should never happen!


                // They are saving an existing listing, so we have a somewhat complicated
                // scheme for determining its status:
                // Draft -> Save as draft // Publish now
                // Published, not live -> Save as draft // Save as revision
                // Published, live -> Save as addendum // Save as revision
                string updateMode = "";

                if( listing.Status == ListingStatus.Draft || listing.Status == ListingStatus.Published )
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
                    listing.Status = ListingHelper.getStatus(listing.ListingId);
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
        [Route("/Account/addSublisting")]
        public IActionResult AddSublisting(int parentId)
        {
            // Have we seen this unique identifier before?  If not, they really shouldn't be here
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/account/NewAccount");

            // Make sure they gave us a parent ID
            if( parentId == 0 )
                return Redirect("/account");

            // Make sure that the listing they're attaching this subcontract to,
            // they own that listing ...
            Listing parent = new Listing();
            parent.loadById(parentId);
            if( parent.AgencyId != a.AgencyId )
                return Redirect("/account");

            // Get the item
            Listing l = new Listing();
            ViewBag.projectTitle = parent.Title;
            ViewBag.parentId = parentId;
            return View("~/Views/AccountListing/SetupSublisting.cshtml", l);

        }





        [Authorize]
        [Route("/Account/editSublisting")]
        public IActionResult EditSublisting(int parentId, int id)
        {
            // Have we seen this unique identifier before?  If not, they really shouldn't be here
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/account/NewAccount");

            // Load the parent, to get the title/project description
            Listing parent = new Listing();
            parent.loadById(parentId);

            // They're editing an existing listing .. let's load it and verify ownership
            Listing l = new Listing();
            l.loadById(id);
            if( l.AgencyId != a.AgencyId )
                return Redirect("/account");

            // Now save these files to the session, so we can remove them by GUID
            HttpContext.Session.SetString(Defines.SessionKeys.Files, JsonConvert.SerializeObject(l.BidDocuments));

            // Get the item
            ViewBag.projectTitle = parent.Title;
            ViewBag.parentId = parentId;
            ViewBag.id = id;
            return View("~/Views/AccountListing/SetupSublisting.cshtml", l);

        }






        /**
         * The POST endpoint for saving a subcontract
         */
        [Authorize]
        [HttpPost]
        [Route("/Account/setupSublisting")]
        [ValidateAntiForgeryToken]
        public IActionResult SetupSublistingPost(int parentId, int? id, Listing listing)
        {
            // Have we seen this unique identifier before?  If not, they really shouldn't be here
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/account/NewAccount");

            // Which button did they click on?
            string action = HttpContext.Request.Form["action"];

            // Cancel!
            if( action == "cancel" )
                return Redirect("/account");

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
                    return Redirect("/account");  // This should never happen!

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













        [Authorize]
        [HttpPost]
        [DisableFormValueModelBinding]
        [Route("/Account/saveUpload")]
        public async Task<IActionResult> SaveUpload()
        {
            Attachment myFile = new Attachment {};
            FormValueProvider formModel;

            // Get a GUID for naming the file temporarily
            Guid myUploadId = Guid.NewGuid();
            string uploadTempFile = Path.GetTempFileName();

            using (var stream = System.IO.File.Create(uploadTempFile))
            {
                formModel = await Request.StreamFile(stream);
            }


            // OK, we've got a file.  Let's copy it to the on-server temp storage
            myFile.DocumentName = formModel.GetValue("uploadFilename").ToString();
            myFile.FileName = myUploadId.ToString() + Path.GetExtension(myFile.DocumentName);
            myFile.Url = Defines.UploadStorageUrl + myFile.FileName;
            myFile.IsStaged = true;

            // Copy the file to the staging area and delete the uploaded file
            string myFilePath = Defines.UploadStoragePath + "/" + myFile.FileName;
            System.IO.File.Copy(uploadTempFile, myFilePath);
            System.IO.File.Delete(uploadTempFile);

            // And get the file size
            FileInfo f = new FileInfo(myFilePath);

            // And save the GUID
            myFile.Guid = myUploadId.ToString();

            // And now, do we already have some files for this session?  If so, let's get them
            string sessionFilesJson;
            List<Attachment> sessionFiles;

            sessionFilesJson = HttpContext.Session.GetString(Defines.SessionKeys.Files);
            if( sessionFilesJson != null )
                sessionFiles = JsonConvert.DeserializeObject<Attachment []>(sessionFilesJson).ToList();
            else
                sessionFiles = new List<Attachment>();

            // Add this file to the list
            sessionFiles.Add(myFile);

            // Save the list of files back to the session
            HttpContext.Session.SetString(Defines.SessionKeys.Files, JsonConvert.SerializeObject(sessionFiles.ToArray()));

            // And send the file data back
            UploadFiles myFiles = new UploadFiles
            {
                files = new UploadFile []
                {
                    new UploadFile
                    {
                        name = myFile.DocumentName,
                        type = MimeTypes.MimeTypeMap.GetMimeType(Path.GetExtension(myFile.DocumentName)),
                        size = f.Length,
                        error = "",
                        uploadId = myFile.Guid
                    }
                }
            };

            return Json(myFiles);

        }





        [Authorize]
        [Route("/Account/removeAttachment")]
        public IActionResult RemoveAttachment(string id)
        {
            // And now, do we already have some files for this session?  If so, let's get them
            string sessionFilesJson;
            List<Attachment> sessionFiles;

            sessionFilesJson = HttpContext.Session.GetString(Defines.SessionKeys.Files);
            if( sessionFilesJson == null )
                return StatusCode(418); // No session files??  what are they trying to remove?

            sessionFiles = JsonConvert.DeserializeObject<Attachment []>(sessionFilesJson).ToList();
            foreach (var myFile in sessionFiles)
            {
                // Is this the one?
                if( myFile.Guid == id )
                {
                    // Are we deleting an attachment that hasn't yet been saved
                    // to a listing?  If so, we can just remove the file itself, and
                    // remove the file from our attachment listing..
                    if( myFile.AttachmentId == 0 )
                    {
                        System.IO.File.Delete(Defines.UploadStoragePath + "/" + myFile.FileName);
                        sessionFiles.Remove(myFile);
                    }
                    else
                        // Otherwise, mark it for deletion when they save their changes
                        myFile.ToDelete = true;

                    break;
                }
            }

            // Save the list of files back to the session
            HttpContext.Session.SetString(Defines.SessionKeys.Files, JsonConvert.SerializeObject(sessionFiles.ToArray()));

            return StatusCode(200);
        }





        /**
         * Remove the sublisting from an umbrella contract
         * @param int id The listing ID of the subcontract to remove
         */
        [Authorize]
        [Route("/Account/removeSublisting")]
        public IActionResult RemoveSublisting(int id)
        {
            // Have we seen this unique identifier before?  If no, send them to the new account page
            Agency a = new Agency(this.readNameIdentifier());
            if( a.AgencyId == 0 )
                return Redirect("/account/NewAccount");

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



    }
}
