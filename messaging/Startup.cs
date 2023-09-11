using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using messaging.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Security.Claims;

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
            var settings = Configuration.GetSection("AppSettings");
            // remove the cookies?
            services.AddAuthentication("cookie")
            .AddCookie("cookie")
            .AddOAuth("github", o =>
            {
                o.SignInScheme = "cookie";
                o.ClientId = settings["ClientId"];
                o.ClientSecret = settings["ClientSecret"];
                o.AuthorizationEndpoint = settings["AuthEndpoint"];
                o.TokenEndpoint = settings["TokenEndpoint"];
                o.UserInformationEndpoint = settings["UserInfo"];
                o.SaveTokens = true;
                o.CallbackPath = settings["CallbackPath"];

                o.ClaimActions.MapJsonKey("sub","id");
                o.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");

                o.Events.OnCreatingTicket = async ctx =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, ctx.Options.UserInformationEndpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AccessToken);
                    using var result = await ctx.Backchannel.SendAsync(request);
                    var user = await result.Content.ReadFromJsonAsync<JsonElement>(); 
                    ctx.RunClaimActions(user);
                };

            });

            services.AddControllers().AddNewtonsoftJson(
                options => options.SerializerSettings.Converters.Add(new BundleConverter())
            );
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

            // auth must be added between app.UseRouting and app.UseEndpoints
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // the default redirect when the request is unauthorized is /Account/Login
                endpoints.MapGet("/Account/Login", (HttpContext ctx) => 
                {   return Results.Challenge(
                    new AuthenticationProperties()
                    {
                        RedirectUri = "https://localhost:5001/TT/metadata"
                    },
                    authenticationSchemes: new List<string>() { "github" }
                    ); 
                });
                // require authorization for all endpoints
                endpoints.MapControllers().RequireAuthorization();
            });
        }
    }
}