using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using SearchProcurement.Models;
using SearchProcurement.Helpers;

namespace SearchProcurement.Controllers
{
    public class DetailsController : Controller
    {
        public IActionResult Index(int id, string kw)
        {
            // Get the details model
            Details d = new Details(id, kw);
            ViewBag.extraTitle = d.title;
            ViewBag.kw = kw;

            // Draft, published (but not yet live), disabled -- check to see if they're logged in,
            // and this is their listing..  if it is, let them see it
            if( d.status == ListingStatus.Draft || d.status == ListingStatus.Published || d.status == ListingStatus.Disabled )
            {
                // What's the agency?
                Agency a = new Agency(this.readNameIdentifier());

                // No agency?  Or not the owning agency?  Don't show the listing
                if( a.AgencyId == 0 || a.AgencyId != d.agencyId )
                    return Redirect("/");
            }

            // Load the model
            if( d.isExternalFeed )
                return View("IndexIframe", d);
            else
                return View(d);
        }

        public IActionResult Iframe(int id)
        {
            // Get the model, so we can get the agency name from it
            Details d = new Details(id, null);
            frameDetails f = d.loadFrameData();

            // Update the access count
            LogHelper.updateDetailsAccesses(id);

            // Are we using a custom redirect URL?  That overrides all
            if( f.redirectUrl != "" )
                return Redirect(f.redirectUrl);
            else
            {
                ViewBag.contents = f.contents;
                return View(f.viewPath);
            }

        }

    }
}
