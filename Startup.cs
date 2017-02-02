using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            // Add framework services.
            services.AddMvc();
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

            app.UseStaticFiles();

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
