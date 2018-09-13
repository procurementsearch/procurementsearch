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
    public class AgencyAttachmentController : Controller
    {
        [HttpPost]
        [Authorize(Policy="VerifiedKnown")]
        [DisableFormValueModelBinding]
        [Route("/Agency/saveUpload")]
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

            // Just to be clear, the filename is:  the tidied original document name (minus
            // extension), the document GUID, and the extension...
            myFile.FileName = Library.tidyString(Path.GetFileNameWithoutExtension(myFile.DocumentName)) + "-" + myUploadId.ToString() + Path.GetExtension(myFile.DocumentName);
            myFile.Url = Defines.AppSettings.UploadStorageUrl + Defines.UploadDocumentPath + "/" + myFile.FileName;
            myFile.IsStaged = true;

            // Copy the file to the staging area and delete the uploaded file
            string myFilePath = Defines.AppSettings.UploadStoragePath + Defines.UploadDocumentPath + "/" + myFile.FileName;
            System.IO.File.Copy(uploadTempFile, myFilePath);
            System.IO.File.Delete(uploadTempFile);

            // Set the file permissions on the new file
            Chmod.chmod644(myFilePath);

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





        [Authorize(Policy="VerifiedKnown")]
        [Route("/Agency/removeAttachment")]
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
                        System.IO.File.Delete(Defines.AppSettings.UploadStoragePath + Defines.UploadDocumentPath + "/" + myFile.FileName);
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

    }
}
