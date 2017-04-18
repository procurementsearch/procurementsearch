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

            // Load the model
            return View(d);
        }

        public IActionResult Iframe(int id)
        {
            // Get the model, so we can get the source from it
            Details d = new Details(id, null);
            frameDetails f = d.loadFrameData();

            // Update the access count
            LogHelper.updateDetailsAccesses(id);

            // Load the model
            switch(f.sourceId)
            {
                // Oregon Department of Transportation EBIDS gets a custom template
                case Defines.OregonDepartmentOfTransportationEBIDS:
                    ViewBag.contents = f.contents;
                    return View("~/Views/Details/Templates/OdotEBIDS.cshtml");

                // Port of Portland gets a custom template
                case Defines.Bremik:
                    ViewBag.contentsFormatted = Library.nl2br(f.contents);
                    return View("~/Views/Details/Templates/Bremik.cshtml");

                // Port of Portland gets a custom template
                case Defines.PortOfPortland:
                    ViewBag.contents = f.contents;
                    return View("~/Views/Details/Templates/Portofportland.cshtml");

                // PDC gets a custom template
                case Defines.PDC:
                    ViewBag.contents = f.contents;
                    return View("~/Views/Details/Templates/Pdc.cshtml");

                // PPS gets a custom template
                case Defines.PortlandPublicSchools:
                    ViewBag.contents = f.contents;
                    return View("~/Views/Details/Templates/Pps.cshtml");

                // Orpin-sourced data gets a modified raw_contents template
                case Defines.Metro:
                case Defines.PortlandStateUniversity:
                case Defines.OregonDepartmentOfCorrections:
                case Defines.OregonDepartmentOfTransportation:
                    ViewBag.rawContents = f.rawContents.Replace("<head>", "<head><base href=\"http://orpin.oregon.gov/\">");
                    return View("~/Views/Details/Templates/Orpin.cshtml");

                // Otherwise, we're probably doing a redirect..
                default:
                    if( f.redirectUrl != "" )
                        return Redirect(f.redirectUrl);
                    else
                    {
                        ViewBag.rawContents = f.rawContents;
                        return View("~/Views/Details/Templates/Raw.cshtml");
                    }

            }

        }

    }
}
