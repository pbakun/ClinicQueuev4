using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Extensions
{
    public sealed class LoggingExceptionHandler
    {
        private readonly RequestDelegate _next;

        public LoggingExceptionHandler(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                //_next goes to next middleware method in Startup.cs
                await _next(context);
            }
            catch(Exception ex)
            {
                try
                {
                    //if exception log it together with stack trace and re-throw the error so error page can be rendered
                    Log.Error($"ErrorException | {ex.Message}");
                    Log.Error($"ErrorStackTrace | => \n {ex.StackTrace}");
                }
                catch(Exception ex2)
                {
                    Log.Fatal($"ErrorException while catching exception | {ex2.Message}");
                }
                throw;
            }
            }
    }
}
