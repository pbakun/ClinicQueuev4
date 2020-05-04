using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Extensions
{
    public static class SwaggerExtension
    {
        public static void AddSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc("v1", new Swashbuckle.AspNetCore.Swagger.Info
                {
                    Title = "Queue System API",
                    Version = "v1"
                });

                var security = new Dictionary<string, IEnumerable<string>>
                {
                    {"Bearer", new string[] { }},
                };


                x.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    Description = "JWT Token Auth",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });

                x.AddSecurityRequirement(security);
            });
        }

        public static void UseCustomSwagger(this IApplicationBuilder app, string description)
        {
            app.UseSwagger();

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", description);
            });
        }
    }
}
