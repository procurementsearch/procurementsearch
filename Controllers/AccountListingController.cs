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
            if( !Account.isKnownAgency(uniq) )
                return Redirect("/account/NewAccount");

            // Yep, they're good, they can start an RFP
            Account a = new Account();
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
        [Route("/Account/setupListing", Name = "Account")]
        public IActionResult SetupListing(int? listingId)
        {
            // Have we seen this unique identifier before?
            string uniq = this.readNameIdentifier();
            if( !Account.isKnownAgency(uniq) )
                return Redirect("/account/NewAccount");


            // Is this a new listing?  If it is, we need to verify
            // the payment token..
            if( listingId == null )
            {
                Account a = new Account();
                a.loadIdByAgencyIdentifier(uniq);

                // Make sure they've selected a location Id
                int? locId = HttpContext.Session.GetInt32(Defines.SessionKeys.LocationId);
                // REMOVE THE FOLLOWING !!!
                locId = 1;
                // REMOVE THE PRECEDING !!!
                if( locId == null )
                    return Redirect("/account/newListing");

                // If they can't post here, send them to the payment screen
                if( !a.hasAvailablePaymentToken(locId.Value) )
                    return Redirect("/account/newListing?locId=" + locId.Value);

                // OK, they've picked a location and they've paid for it ..
                Listing l = new Listing();

                // Get the item
                ViewBag.listingLocation = LocationHelper.getNameForId(locId.Value);
                TempData["locId"] = locId.Value;

                return View(l);

            }
            else
            {
                // They're editing an existing listing .. let's load it before
                // showing the view
                Listing l = new Listing();
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
        public IActionResult SetupListingPost(Listing listing)
        {
            // Have we seen this unique identifier before?  If so, send 'em to their account page
            string uniq = this.readNameIdentifier();
            if( Account.isKnownAgency(uniq) )
                return Redirect("/account");

            // They have an account ..
            Account a = new Account();
            a.loadIdByAgencyIdentifier(uniq);

            listing.AgencyId = a.AgencyId;

            // Add the listing with the assigned status
            listing.add(
                HttpContext.Request.Form["action"] == "add_listing" ? ListingStatus.AddNow : ListingStatus.SaveForLater,
                HttpContext.Features.Get<IHttpRequestFeature>().Headers["X-Real-IP"]
            );

            // And set the location and, if requested, secondary location
            listing.addLocationById(HttpContext.Session.GetInt32(Defines.SessionKeys.LocationId).Value);

            if( HttpContext.Request.Form["secondary_location_id"] != "" )
                listing.addLocationById(Convert.ToInt32(HttpContext.Request.Form["secondary_location_id"]));

            return View("SetupListingDone");
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
            System.IO.File.Copy(uploadTempFile, Defines.UploadStoragePath + "/" + myFile.FileName);

            // And get the file size
            FileInfo f = new FileInfo(Defines.UploadStoragePath + "/" + myFile.FileName);
            myFile.Size = f.Length;

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
                        size = myFile.Size,
                        error = "",
                        uploadId = myFile.Guid
                    }
                }
            };

            return Json(myFiles);


            // var viewModel = new MyViewModel();
            // var bindingSuccessful = await TryUpdateModelAsync(viewModel, prefix: "", valueProvider: formModel);
            // if (!bindingSuccessful)
            // {
            //     if (!ModelState.IsValid)
            //     {
            //         return BadRequest(ModelState);
            //     }
            // }
            // return Ok(viewModel);
        }





        [Authorize]
        [Route("/Account/removeUpload")]
        public IActionResult RemoveUpload(string id)
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
                    System.IO.File.Delete(Defines.UploadStoragePath + "/" + myFile.FileName);
                    break;
                }
            }

            return StatusCode(200);
        }


    }
}
