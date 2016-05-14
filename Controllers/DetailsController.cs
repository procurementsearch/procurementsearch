using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

using SearchProcurement.Models;
using SearchProcurement.Helpers;

namespace SearchProcurement.Controllers
{
    public class DetailsController : Controller
    {
        public IActionResult Index(int id)
        {
            // Get the details model
            Details d = new Details(id);
            ViewBag.extraTitle = d.title;

            // Load the model
            return View(d);
        }

        public IActionResult Iframe(int id)
        {
            // Get the model, so we can get the source from it
            Details d = new Details(id);
            frameDetails f = d.loadFrameData();

            // Update the access count
            LogHelper.updateDetailsAccesses(id);

            // Load the model
            switch(f.sourceId)
            {
                // PDC gets a custom template
                case Defines.PDC:
                    ViewBag.contents = f.contents;
                    return View("~/Views/Details/Templates/Pdc.cshtml");

                // PDC gets a custom template
                case Defines.PortOfPortland:
                    ViewBag.contents = f.contents;
                    return View("~/Views/Details/Templates/Portofportland.cshtml");

                // Orpin-sourced data gets a modified raw_contents template
                case Defines.Metro:
                case Defines.PortlandStateUniversity:
                case Defines.OregonDepartmentOfCorrections:
                    ViewBag.rawContents = f.rawContents.Replace("<head>", "<head><base href=\"http://orpin.oregon.gov/\">");
                    return View("~/Views/Details/Templates/Orpin.cshtml");

                // Otherwise, we're probably doing a redirect..
                default:
                    if( f.originUrl != "" )
                        return Redirect(f.originUrl);
                    else
                    {
                        ViewBag.rawContents = f.rawContents;
                        return View("~/Views/Details/Templates/Raw.cshtml");
                    }

            }

        }

    }
}
