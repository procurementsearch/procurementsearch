using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Amazon;
using Amazon.S3;

using SearchProcurement.Helpers;

namespace SearchProcurement
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        // public static void RegisterBundles(BundleCollection bundles)
        // {
        //     bundles.Add(new StyleBundle("~/css").Include("~/css/styles.css"));
        // 
        //     // Code removed for clarity.
        //     // BundleTable.EnableOptimizations = true;
        // }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add authentication services
            services.AddAuthentication(options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
            services.AddMvc();
            services.AddMemoryCache();
            services.AddSession();
            services.AddOptions();
//            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
//            services.AddAWSService<IAmazonS3>();
            services.Configure<Auth0Settings>(Configuration.GetSection("Auth0"));
            services.Configure<StripeSettings>(Configuration.GetSection("Stripe"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<Auth0Settings> auth0Settings)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Internationalization
/*            var supportedCultures = new[]
            {
                new CultureInfo("en"),
                new CultureInfo("en-US"),
                new CultureInfo("es"),
                new CultureInfo("es-ES")
            };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });
*/
            // Make sure we can serve up the static content
            app.UseStaticFiles();

            // And the session
            app.UseSession();

            // Add the Stripe configuration
            StripeConfiguration.SetApiKey(Configuration.GetSection("Stripe")["SecretKey"]);

            // Add the cookie middleware
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                LoginPath = new PathString("/account/login"),
                LogoutPath = new PathString("/account/logout")
            });

            // Add the OIDC middleware
            var options = new OpenIdConnectOptions("Auth0")
            {
                // Set the authority to your Auth0 domain
                Authority = $"https://{auth0Settings.Value.Domain}",

                // Configure the Auth0 Client ID and Client Secret
                ClientId = auth0Settings.Value.ClientId,
                ClientSecret = auth0Settings.Value.ClientSecret,

                // Do not automatically authenticate and challenge
                AutomaticAuthenticate = false,
                AutomaticChallenge = false,

                // Set response type to code
                ResponseType = "code",

                // Set the callback path, so Auth0 will call back to http://localhost:5000/signin-auth0 
                // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard 
                CallbackPath = new PathString("/signin-auth0"),

                // Configure the Claims Issuer to be Auth0
                ClaimsIssuer = "Auth0",

                // Saves tokens to the AuthenticationProperties
                SaveTokens = true,

                Events = new OpenIdConnectEvents
                {
                    OnTicketReceived = context =>
                    {
                        // Get the ClaimsIdentity
                        var identity = context.Principal.Identity as ClaimsIdentity;
                        if (identity != null)
                        {
                            // Add the Name ClaimType. This is required if we want User.Identity.Name to actually return something!
                            if (!context.Principal.HasClaim(c => c.Type == ClaimTypes.Name) &&
                                identity.HasClaim(c => c.Type == "name"))
                                identity.AddClaim(new Claim(ClaimTypes.Name, identity.FindFirst("name").Value));
                        }

                        return Task.CompletedTask;
                    },
                    // handle the logout redirection 
                    OnRedirectToIdentityProviderForSignOut = (context) =>
                    {
                        var logoutUri = $"https://{auth0Settings.Value.Domain}/v2/logout?client_id={auth0Settings.Value.ClientId}";

                        var postLogoutUri = context.Properties.RedirectUri;
                        if (!string.IsNullOrEmpty(postLogoutUri))
                        {
                            if (postLogoutUri.StartsWith("/"))
                            {
                                // transform to absolute
                                var request = context.Request;
                                postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
                            }
                            logoutUri += $"&returnTo={ Uri.EscapeDataString(postLogoutUri)}";
                        }

                        context.Response.Redirect(logoutUri);
                        context.HandleResponse();

                        return Task.CompletedTask;
                    }
                }

            };
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("name");
            options.Scope.Add("email");
            options.Scope.Add("picture");
            app.UseOpenIdConnectAuthentication(options);

            // And set up our routes
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "Home page",
                    template: "",
                    defaults: new { controller = "Home", action = "Index" });
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
                routes.MapRoute(
                    name: "details",
                    template: "details/{id}/{action}",
                    defaults: new { controller = "Details", action = "Index" });
                routes.MapRoute(
                    name: "Search for keywords",
                    template: "search",
                    defaults: new { controller = "Search", action = "Index" });
                routes.MapRoute(
                    name: "Rss with Search",
                    template: "rss/kw={kw}",
                    defaults: new { controller = "Rss", action = "Index" });
                routes.MapRoute(
                    name: "Rss for Source",
                    template: "rss/source={source}",
                    defaults: new { controller = "Rss", action = "Index" });
                routes.MapRoute(
                    name: "Rss feed",
                    template: "rss",
                    defaults: new { controller = "Rss", action = "Index" });

                // Special routes for agency-specific RSS feeds
                routes.MapRoute(
                    name: "Rss feed for City of Beaverton",
                    template: "rss_cityofbeaverton",
                    defaults: new { controller = "Rss", action = "Index", source = Defines.CityOfBeaverton });
                routes.MapRoute(
                    name: "Rss feed for City of Portland",
                    template: "rss_portland",
                    defaults: new { controller = "Rss", action = "Index", source = Defines.CityOfPortland });
                routes.MapRoute(
                    name: "Rss feed for Port of Portland",
                    template: "rss_portofportland",
                    defaults: new { controller = "Rss", action = "Index", source = Defines.PortOfPortland });
                routes.MapRoute(
                    name: "Rss feed for Oregon Department of Corrections",
                    template: "rss_odoc",
                    defaults: new { controller = "Rss", action = "Index", source = Defines.OregonDepartmentOfCorrections });
                routes.MapRoute(
                    name: "Rss feed for Portland State University",
                    template: "rss_psu",
                    defaults: new { controller = "Rss", action = "Index", source = Defines.PortlandStateUniversity });
                routes.MapRoute(
                    name: "Rss feed for Portland Public Schools",
                    template: "rss_pps",
                    defaults: new { controller = "Rss", action = "Index", source = Defines.PortlandPublicSchools });
                routes.MapRoute(
                    name: "Rss feed for Oregon Department of Transportation",
                    template: "rss_odot",
                    defaults: new { controller = "Rss", action = "Index", source = Defines.OregonDepartmentOfTransportation });
            });
        }
    }
}
