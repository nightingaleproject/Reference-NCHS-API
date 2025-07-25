using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using messaging.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace messaging
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            services.AddMemoryCache();

            services.AddMiniProfiler(options => options.RouteBasePath = "/profiler").AddEntityFramework();

            services.AddDbContext<ApplicationDbContext>(opt =>
                opt.UseSqlServer(Configuration.GetConnectionString("NVSSMessagingDatabase"))
            );

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new BundleConverter());
                });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "NVSSMessaging", Version = "v1"});
                // using System.Reflection;
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });

            services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressMapClientErrors = true;
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseHttpLogging();
            app.UseHttpsRedirection();
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("Cache-Control", "no-store");

                // null check the MaxRequestBodySizeFeature, this feature is null in the dotnet test instance and will throw a null error in our testing framework
                IHttpMaxRequestBodySizeFeature feat = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
                if (feat != null)
                {
                    feat.MaxRequestBodySize = Int32.Parse(Configuration.GetSection("AppSettings").GetSection("MaxPayloadSize").Value);
                }

                // Miniprofiler & Swagger UI fail strict CSP, so loosen it for Dev only
                // see: https://github.com/swagger-api/swagger-ui/issues/5817
                if( (context.Request.Path.StartsWithSegments("/profiler") ||context.Request.Path.StartsWithSegments("/swagger"))
                        && env.IsDevelopment() )
                {
                    context.Response.Headers.Add("Content-Security-Policy", "Content-Security-Policy: default-src 'self' 'unsafe-inline' 'unsafe-eval'");
                }
                else
                {
                    context.Response.Headers.Add("Content-Type", "application/json");
                    context.Response.Headers.Add("Content-Security-Policy", "default-src");
                }
                
                await next.Invoke();
            });
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger(c =>
                {
                    c.PreSerializeFilters.Add((swagger, httpReq) =>
                    {
                        swagger.Servers = new List<OpenApiServer> { new OpenApiServer { Url = "https://test.ASTV-NVSS-API.cdc.gov" } };
                    });
                });
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NVSSMessaging v1"));
            }
            app.UseMiniProfiler();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}