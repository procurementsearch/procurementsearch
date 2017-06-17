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

using SearchProcurement.AWS;
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





        [Authorize]
        [Route("/Account/newListing")]
        public IActionResult NewListing(int? locId)
        {
            // Have we seen this unique identifier before?
            string uniq = this.readNameIdentifier();
            if( !Agency.isKnownAgency(uniq) )
                return Redirect("/account/NewAccount");

            // Yep, they're good, they can start an RFP
            Agency a = new Agency();
            a.loadIdByAgencyIdentifier(uniq);
            a.loadDataByAgencyIdentifier(uniq);

            // Did we get a location ID?
            if( locId != null )
            {
                // They can post here--let's let them
                if( a.hasAvailablePaymentToken(locId.Value) )
                {
                    // Save the location for the redirect
                    HttpContext.Session.SetInt32(Defines.SessionKeys.LocationId, locId.Value);

                    // And send them to the listing setup page
                    return Redirect("/account/setupListing");
                }
                else
                    return View("NewListingPay", a);  // Nope, need to pay first
            }
            else
                return View(a);

        }




        [Authorize]
        [Route("/Account/setupListing")]
        public IActionResult SetupListing(int? id)
        {
            // Have we seen this unique identifier before?
            string uniq = this.readNameIdentifier();
            if( !Agency.isKnownAgency(uniq) )
                return Redirect("/account/NewAccount");

            // Is this a new listing?  If it is, we need to verify
            // the payment token..
            if( id == null )
            {
                Agency a = new Agency();
                a.loadIdByAgencyIdentifier(uniq);

                // Make sure they've selected a location Id
                int? locId = HttpContext.Session.GetInt32(Defines.SessionKeys.LocationId);
                if( locId == null )
                    return Redirect("/account/newListing");

                // If they can't post here, send them to the payment screen
                if( !a.hasAvailablePaymentToken(locId.Value) )
                    return Redirect("/account/newListing?locId=" + locId.Value);

                // OK, they've picked a location and they've paid for it ..
                Listing l = new Listing();

                // Flush out any old session files from this session, in case they
                // started and clicked away from a previous listing
                string sessionFilesJson = HttpContext.Session.GetString(Defines.SessionKeys.Files);
                if( sessionFilesJson != null )
                {
                    // Delete any unsaved uploaded files
                    List<Attachment> sessionFiles = JsonConvert.DeserializeObject<Attachment []>(sessionFilesJson).ToList();
                    foreach (Attachment att in sessionFiles)
                    {
                        System.IO.File.Delete(Defines.UploadStoragePath + "/" + att.FileName);
                    }

                    // And empty the session files list
                    HttpContext.Session.SetString(Defines.SessionKeys.Files, null);

                }

                // Get the item
                ViewBag.listingLocation = LocationHelper.getNameForId(locId.Value);
                ViewBag.locId = locId.Value;

                return View(l);

            }
            else
            {
                // They're editing an existing listing .. let's load it before
                // showing the view
                Listing l = new Listing();
                l.loadById(id.Value);

                // Go through the attachments and assign a GUID to each of them
                for (int i = 0; i < l.BidDocuments.Length; i++)
                    l.BidDocuments[i].Guid = Guid.NewGuid().ToString();

                // Now save these files to the session, so we can remove
                // them by GUID
                HttpContext.Session.SetString(Defines.SessionKeys.Files, JsonConvert.SerializeObject(l.BidDocuments));

                // Get the item top-level location ID
                ViewBag.locId = l.PrimaryLocationId;
                ViewBag.listingLocation = LocationHelper.getNameForId(l.PrimaryLocationId);
                ViewBag.id = id.Value;

                return View(l);
            }

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
            // Have we seen this unique identifier before?  If not, they really shouldn't be here
            string uniq = this.readNameIdentifier();
            if( !Agency.isKnownAgency(uniq) )
                return Redirect("/account/NewAccount");

            // Which button did they click on?
            string action = HttpContext.Request.Form["action"];

            // Cancel!
            if( action == "cancel" )
                return Redirect("/account");


            // Are we adding, or updating?
            if( id == null )
            {
                // No ID means it's a new listing
                Agency a = new Agency();
                a.loadIdByAgencyIdentifier(uniq);

                listing.AgencyId = a.AgencyId;

                // Add the listing with the assigned status
                listing.add(
                    HttpContext.Request.Form["action"] == "publish_now" ? ListingStatus.Open : ListingStatus.SaveForLater,
                    HttpContext.Features.Get<IHttpRequestFeature>().Headers["X-Real-IP"]
                );

                // And set the location
                listing.addLocationById(HttpContext.Session.GetInt32(Defines.SessionKeys.LocationId).Value);

                // And, if requested, secondary location
                if( !string.IsNullOrEmpty(HttpContext.Request.Form["secondary_location_id"]) )
                    listing.addLocationById(Convert.ToInt32(HttpContext.Request.Form["secondary_location_id"]));

                // And add the attachments
                string sessionFilesJson = HttpContext.Session.GetString(Defines.SessionKeys.Files);
                if( sessionFilesJson != null )
                {
                    List<Attachment> sessionFiles = JsonConvert.DeserializeObject<Attachment []>(sessionFilesJson).ToList();
                    foreach (Attachment att in sessionFiles)
                    {
                        // Add the attachment
                        Attachment myAtt = att;
                        myAtt.RedirectUrl = HttpContext.Request.Form["redir-" + myAtt.Guid];
                        listing.addAttachment(myAtt);
                    }
                }

            }
            else
            {
                // They are saving an existing listing
                listing.ListingId = id.Value;

                // First, update the listing itself
                listing.update(action == "addendum" ? ListingUpdateMode.Addendum : ListingUpdateMode.Revision);

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
                {
                    listing.addLocationById(Convert.ToInt32(HttpContext.Request.Form["secondary_location_id"]));
                }

                // And save new attachments, if we have any new attachments
                string sessionFilesJson = HttpContext.Session.GetString(Defines.SessionKeys.Files);
                if( sessionFilesJson != null )
                {
                    List<Attachment> sessionFiles = JsonConvert.DeserializeObject<Attachment []>(sessionFilesJson).ToList();
                    foreach (Attachment att in sessionFiles)
                    {
                        string redirectUrl = HttpContext.Request.Form["redir-" + att.Guid];

                        // If it hasn't got an ID, we want to save it
                        if( att.AttachmentId == 0 )
                        {
                            // Add the attachment
                            Attachment myAtt = att;
                            myAtt.RedirectUrl = redirectUrl;
                            listing.addAttachment(myAtt);
                        }
                        else
                        {
                            // Otherwise, just update its redirect URL
                            AttachmentHelper.updateRedirectUrl(att.AttachmentId, redirectUrl);
                        }
                    }
                }


            }

            // Empty out the session data
            HttpContext.Session.Remove(Defines.SessionKeys.LocationId);
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
                    // Yes! We're done!
                    sessionFiles.Remove(myFile);

                    // Are we deleting from the filesystem, S3, ...?
                    if( myFile.AttachmentId != 0 )
                    {
                        // Is this an upload for a listing that we haven't yet saved?  If so,
                        // this document will have its filename set, because we just saved that
                        // from the previous upload action.
                        // If this is an upload from a listing we've already saved, however,
                        // the filename won't be set, because we're not storing that in the database ..
                        // or not as such, anyway.  Once it's in the DB, we store it as the
                        // deletion identifier, because it could refer either to a file on disk
                        // or to an S3 object.
                        // So there are three possible conditions for a file, that we're removing ..
                        // Just uploaded, listing not saved- the file is on disk, name is in the session
                        // Uploaded and previously saved- the file is on disk, name is not in the session
                        // Uploaded and moved to S3- the file is not on disk, name is not in session
                        string del = AttachmentHelper.getDeletionIdentifier(myFile.AttachmentId);
                        if( myFile.IsStaged )
                            // Has an ID, is staged, it's still on disk, but we don't have its name handy
                            System.IO.File.Delete(Defines.UploadStoragePath + "/" + del);
                        else
                        {
                            // Has an ID, not staged, it's been moved to S3
                            S3 s3 = new S3();
                            s3.Delete(Defines.s3Bucket, Defines.s3AttachmentPath + "/" + del);
                        }

                        // And remove the attachment from the database
                        AttachmentHelper.deleteById(myFile.AttachmentId);

                    }
                    else
                        // No ID, so we do have its name handy
                        System.IO.File.Delete(Defines.UploadStoragePath + "/" + myFile.FileName);

                    break;
                }
            }

            return StatusCode(200);
        }


    }
}
