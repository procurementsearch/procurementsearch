using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Session;
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
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            // Load some defines before doing anything else
            Defines.AppSettings.myConnectionString = Configuration.GetSection("MariaDB")["connection"];
            Defines.AppSettings.UploadStoragePath = Configuration.GetSection("Upload")["StoragePath"];
            Defines.AppSettings.UploadStorageUrl = Configuration.GetSection("Upload")["StorageUrl"];
            Defines.AppSettings.s3ServiceUrl = Configuration.GetSection("S3")["ServiceURL"];
            Defines.AppSettings.s3AccessKey = Configuration.GetSection("S3")["AccessKey"];
            Defines.AppSettings.s3SecretKey = Configuration.GetSection("S3")["SecretKey"];
            Defines.AppSettings.s3Bucket = Configuration.GetSection("S3")["DefaultBucket"];
            Defines.AppSettings.StripeKey = Configuration.GetSection("Stripe")["PublishableKey"];
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // Add authentication services
            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                options.LoginPath = new PathString("/Agency/Login");
                options.LogoutPath = new PathString("/Agency/Logout");
                options.AccessDeniedPath = new PathString("/Agency/AccessDenied");
            })
            .AddOpenIdConnect("Auth0", options => {
                // Set the authority to your Auth0 domain
                options.Authority = $"https://{Configuration["Auth0:Domain"]}";

                // Configure the Auth0 Client ID and Client Secret
                options.ClientId = Configuration["Auth0:ClientId"];
                options.ClientSecret = Configuration["Auth0:ClientSecret"];

                // Set response type to code
                options.ResponseType = "code";
                options.ResponseMode = "query";

                // Configure the scope
                options.Scope.Clear();
                options.Scope.Add("openid");

                // Set the callback path, so Auth0 will call back to http://localhost:5000/signin-auth0
                // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard
                options.CallbackPath = new PathString("/signin-auth0");

                // Configure the Claims Issuer to be Auth0
                options.ClaimsIssuer = "Auth0";

                options.Events = new OpenIdConnectEvents
                {
                    // handle the logout redirection
                    OnRedirectToIdentityProviderForSignOut = (context) =>
                    {
                        var logoutUri = $"https://{Configuration["Auth0:Domain"]}/v2/logout?client_id={Configuration["Auth0:ClientId"]}";

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
                };   
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Add session services
            services.AddDistributedMemoryCache();
            services.AddSession();

            // and register the Stripe Settings configuration injection
            services.Configure<StripeSettings>(Configuration.GetSection("Stripe"));

            // Inject Http Context into controllers
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Add in some custom authentication levels, since we're dealing with
            // unverified vs verified users from auth0
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Verified", policy =>
                    policy.RequireClaim("email_verified", "true"));
                options.AddPolicy("VerifiedKnown", policy =>
                    policy.Requirements.Add(new Auth0KnownUniqueIdRequirement()));
            });

            services.AddSingleton<IAuthorizationHandler, Auth0KnownUniqueIdHandler>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<Auth0Settings> auth0Settings)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseSession();
            app.UseAuthentication();

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
                    defaults: new { controller = "Rss", action = "Search" });
                routes.MapRoute(
                    name: "Rss for Agency",
                    template: "rss/agency={agency}",
                    defaults: new { controller = "Rss", action = "ByAgency" });
                routes.MapRoute(
                    name: "Rss for Agency (deprecated)",
                    template: "rss/source={agency}",
                    defaults: new { controller = "Rss", action = "ByAgency" });
                routes.MapRoute(
                    name: "Rss feed",
                    template: "rss",
                    defaults: new { controller = "Rss", action = "Index" });

                // Special routes for agency-specific RSS feeds
                routes.MapRoute(
                    name: "Rss for Agency (by short name)",
                    template: "rss_{agencyshortname}",
                    defaults: new { controller = "Rss", action = "ByAgencyShortname" });
            });
        }
    }
}
