using AspNetCoreRateLimit;
using Blog.Core.Common;
using Microsoft.AspNetCore.Builder;
using System;
using Serilog;
using Blog.Core.Common.Helper;

namespace Blog.Core.Extensions.Middlewares
{
    /// <summary>
    /// ip 限流
    /// </summary>
    public static class IpLimitMiddleware
    {
        public static void UseIpLimitMiddle(this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            try
            {
                if (AppSettings.app("Middleware", "IpRateLimit", "Enabled").ObjToBool())
                {
                    app.UseIpRateLimiting();
                }
            }
            catch (Exception e)
            {
                LogHelper.Error($"Error occured limiting ip rate.\n{e.Message}");
                throw;
            }
        }
    }
}
