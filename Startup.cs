using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;

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
            services.AddAuthentication(
                options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

            // Add framework services.
            services.AddMvc();

            // Add functionality to inject IOptions<T>
            services.AddOptions();

            // Add the Auth0 Settings object so it can be injected
            services.Configure<Auth0Settings>(Configuration.GetSection("Auth0"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
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



            // Add the cookie middleware
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true
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

                Events = new OpenIdConnectEvents
                {
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
            app.UseOpenIdConnectAuthentication(options);


            // And set up our routes
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "Home page",
                    template: "",
                    defaults: new { controller = "Home", action = "Index" });
                routes.MapRoute(
                    name: "details",
                    template: "details/{id}/{action}",
                    defaults: new { controller = "Details", action = "Index" });
                routes.MapRoute(
                    name: "Search for keywords",
                    template: "search",
                    defaults: new { controller = "Search", action = "Index" });
                routes.MapRoute(
                    name: "Post RFPs",
                    template: "post",
                    defaults: new { controller = "Post", action = "Index" });
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
