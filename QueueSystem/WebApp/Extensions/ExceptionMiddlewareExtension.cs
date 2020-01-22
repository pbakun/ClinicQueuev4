using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.Extensions
{
    public static class ExceptionMiddlewareExtension
    {
        public static void ConfigureExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    //context.Response.ContentType = "application/json";
                    context.Response.ContentType = "text/html";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if(contextFeature != null)
                    {
                        Log.Error($"ErrorException | {contextFeature.Error}");

                        //Find way to write fancy html page
                        //await context.Response.WriteAsync();

                        //await context.Response.WriteAsync(new ErrorDetails()
                        //{
                        //    StatusCode = context.Response.StatusCode,
                        //    Message = "Internal server error"
                        //}.ToString());
                    }
                });
            });

        }
    }
}
