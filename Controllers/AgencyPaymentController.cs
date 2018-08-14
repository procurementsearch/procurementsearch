using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;



using Stripe;

using SearchProcurement.Models;
using SearchProcurement.Helpers;

namespace SearchProcurement.Controllers
{
    public class AgencyPaymentController : Controller
    {

//        IAmazonS3 S3Client { get; set; }
        private IHostingEnvironment _environment;


        /**
         * Constructor
         */
        public AgencyPaymentController(IHostingEnvironment environment /*IAmazonS3 s3Client*/)
        {
            // Inject the IHostingEnvironment
            _environment = environment;

            // Dependency-inject the s3 client
            //this.S3Client = s3Client;
        }



        /**
         * Process a Stripe token to charge a credit card when someone
         * purchases a single RFP.
         */
        [Authorize]
        [Route("/Agency/ChargeSimple")]
        public IActionResult ChargeSimple(string stripeEmail, string stripeToken)
        {
            return Charge(ListingTypes.Simple, stripeEmail, stripeToken);
        }



        /**
         * Process a Stripe token to charge a credit card when someone
         * purchases a block of 10 RFPs.
         */
        [Authorize]
        [Route("/Agency/ChargeSimple10")]
        public IActionResult ChargeSimple10(string stripeEmail, string stripeToken)
        {
            return Charge(ListingTypes.Simple10, stripeEmail, stripeToken);
        }

        /**
         * Process a Stripe token to charge a credit card when someone
         * purchases an umbrella RFP.
         */
        [Authorize]
        [Route("/Agency/ChargeUmbrella")]
        public IActionResult ChargeUmbrella(string stripeEmail, string stripeToken)
        {
            return Charge(ListingTypes.Umbrella, stripeEmail, stripeToken);
        }




        /**
         * Process a Stripe token to charge a credit card when someone
         * purchases an umbrella RFP.
         */
        public IActionResult Charge(string listingType, string stripeEmail, string stripeToken)
        {
            // Have we seen this unique identifier before?  If not, they shouldn't be submitting a payment token at all
            string uniq = this.readNameIdentifier();
            if( !Agency.isKnownAgency(uniq) )
                return StatusCode(401);

            // Yep, they're good, they can stay here
            Agency a = new Agency();
            a.loadDataByAgencyIdentifier(uniq);
            a.loadIdByAgencyIdentifier(uniq);

            // Now, process the Stripe customer and charge
            var customers = new StripeCustomerService();
            var charges = new StripeChargeService();

            var customer = customers.Create(new StripeCustomerCreateOptions {
                Email = stripeEmail,
                SourceToken = stripeToken
            });

            var charge = charges.Create(new StripeChargeCreateOptions {
                Amount = Decimal.ToInt32(Price.loadPrice(a.AgencyType, listingType) * 100),
                Description = "RFP/ITB Listing on ProcurementSearch.com",
                Currency = "usd",
                CustomerId = customer.Id
            });

            // OK!  They've paid, so let's give them a payment token
            // for the listing they've just paid for
            if( listingType == ListingTypes.Simple10 )
            {
                // The type is really the Simple listing type ..
                listingType = ListingTypes.Simple;

                // But do it 10 times instead of once
                for (int i=0; i < 10; i++)
                    a.addPaymentToken(listingType, Price.loadPrice(a.AgencyType, listingType), stripeToken);

            }
            else
                a.addPaymentToken(listingType, Price.loadPrice(a.AgencyType, listingType), stripeToken);

            // And save the type of listing they just paid for, because we're
            // assuming they'll use this first
            HttpContext.Session.SetString(Defines.SessionKeys.ListingType, listingType);

            return Redirect("/Agency/addListing");
        }


    }
}
