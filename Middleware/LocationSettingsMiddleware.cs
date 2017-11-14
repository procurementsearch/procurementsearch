using Microsoft.AspNetCore.Builder;

namespace SearchProcurement
{
    public static class LocationSettingsMiddlewareExtensions
    {
        public static IApplicationBuilder UseLocationSettings(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LocationSettingsMiddleware>();
        }
    }
}
