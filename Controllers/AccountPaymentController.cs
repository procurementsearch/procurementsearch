using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

using Stripe;

using SearchProcurement.Models;
using SearchProcurement.Helpers;

namespace SearchProcurement.Controllers
{
    public class AccountPaymentController : Controller
    {

//        IAmazonS3 S3Client { get; set; }
        private IHostingEnvironment _environment;


        /**
         * Constructor
         */
        public AccountPaymentController(IHostingEnvironment environment /*IAmazonS3 s3Client*/)
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
        [Route("/account/ChargeSimple")]
        public IActionResult ChargeSimple(string stripeEmail, string stripeToken)
        {
            return Charge(ListingTypes.Simple, stripeEmail, stripeToken);
        }


        /**
         * Process a Stripe token to charge a credit card when someone
         * purchases an umbrella RFP.
         */
        [Authorize]
        [Route("/account/ChargeUmbrella")]
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
                Description = "",
                Currency = "usd",
                CustomerId = customer.Id
            });

            // OK!  They've paid, so let's give them a payment token
            // for the listing they've just paid for
            a.addPaymentToken(listingType, Price.loadPrice(a.AgencyType, listingType), stripeToken);

            return Redirect("/account/setupListing");
        }


    }
}
