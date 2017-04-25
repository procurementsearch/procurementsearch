using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Authentication;

using SearchProcurement.Models;

namespace SearchProcurement.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login(string returnUrl = "/account")
        {
            return new ChallengeResult("Auth0", new AuthenticationProperties() { RedirectUri = returnUrl });
        }

        public async Task Logout()
        {
            await HttpContext.Authentication.SignOutAsync("Auth0", new AuthenticationProperties
            {
                RedirectUri = Url.Action("Index", "Home")
            });
            await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }


        public IActionResult NewAccount()
        {
            Account a = new Account();
            return View(a);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NewAccountPost(Account account)
        {
            // So we have a valid model in account now...  Let's just save it
            // and bump them to their account page
            account.AgencyLogo = HttpContext.Request.Form["logo_data"];
            account.add();
            return Content("OK");
        }



        public IActionResult checkEmail(string email)
        {
            // Just check to see if the email exists, and return a good/bad status code as needed
            return Account.emailExists(email) ? StatusCode(418) : StatusCode(200);
        }

    }
}
