using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using Serilog;
using messaging.Models;

namespace status_api
{
    public class Program
    {
        public static async Task Main(String[] args)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                EnvironmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            });

            try {
                Console.WriteLine("DEBUGGING - status_api/Program.cs - start of try block");

                var env = builder.Environment.EnvironmentName;
                Console.WriteLine("DEBUGGING - status_api/Program.cs - ENV: " + env);

                builder.Configuration
                        .AddJsonFile($"status_api.appsettings.json")
                        .AddJsonFile($"status_api.appsettings.{env}.json");

                // if (Environment.GetEnvironmentVariable("CI")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false)
                // {
                //     builder.Environment.EnvironmentName = "Test";
                // }

                // builder automatically loads config from appsettings.<Environment>.json and appsettings.json. See for details:
                // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0#default-application-configuration-sources
                if (builder.Configuration == null)
                {
                    throw new InvalidOperationException("Configuration is null");
                }

                // XXX DEBUG
                // Print all configuration key-value pairs
                Console.WriteLine("Application Configuration:");
                var configuration = builder.Configuration;
                foreach (var kvp in configuration.AsEnumerable())
                {
                    Console.WriteLine($"\t{kvp.Key}: {kvp.Value}");
                }

                Log.Logger = new LoggerConfiguration()
                                .ReadFrom.Configuration(builder.Configuration)
                                .CreateLogger();
                Log.Information("Initialized Logger !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                Log.Fatal("Initialized Logger @@@@@@@@@@@@@@@@@@@@@@");

                // Fetch MSSQL DB from config ConnectionStrings' NVSSMessagingDatabase
                var connectionString =
                    builder.Configuration.GetConnectionString("NVSSMessagingDatabase")
                        ?? throw new InvalidOperationException("Connection string 'NVSSMessagingDatabase' not found.");

                builder.Host.UseSerilog(Log.Logger);

                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlServer(connectionString);
                });

                // Use options pattern to bind configuration
                // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0#default-application-configuration-sources
                builder.Services.Configure<AppSettings>(
                    builder.Configuration.GetSection("AppSettings")
                );

                builder.Services.AddMemoryCache();

                builder.Services.AddControllers();

                // Use Swagger/OpenAPI
                // https://aka.ms/aspnetcore/swashbuckle
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                builder.Services.AddMiniProfiler(options => {
                                    options.RouteBasePath = "/profiler";
                                })
                                .AddEntityFramework();

                // ======================== Configure middleware for HTTP handling ================================
                var app = builder.Build();

                app.UseHttpLogging();
                app.UseHttpsRedirection();

                // Harden HTTP Headers for security
                app.Use(async (context, next) =>
                {
                    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                    context.Response.Headers.Add("X-XSS-Protection", "1;mode=block");
                    context.Response.Headers.Add("Cache-Control", "no-store");

                    // The StatusUI React app requires its own CSP headers
                    if (context.Request.Path.StartsWithSegments("/StatusUI"))
                    {
                        context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'; style-src 'self' 'unsafe-inline';");
                    }
                    // Miniprofiler & Swagger UI fail strict CSP, so loosen it for Dev only
                    // see: https://github.com/swagger-api/swagger-ui/issues/5817
                    else if( (context.Request.Path.StartsWithSegments("/profiler") ||context.Request.Path.StartsWithSegments("/swagger"))
                            && app.Environment.IsDevelopment() )
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

                if (app.Environment.IsDevelopment())
                {
                    // GET /Error
                    app.UseExceptionHandler("/Error");
                    app.UseHsts();

                    // GET /profiler/results-index for profiling
                    app.UseMiniProfiler();

                    // GET /swagger for Swagger OpenAPI Docs
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseAuthorization();
                app.MapControllers();

                // GET /StatusUI/index.html renders StatusUI react app from `npm run build`
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "StatusUI")),
                    RequestPath = "/StatusUI"
                });

                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("status_api/Program.cs - catch block start");

                // Handles .NET 6 logging bug: https://github.com/dotnet/runtime/issues/60600
                // Also see: https://stackoverflow.com/a/70256808
                string type = ex.GetType().Name;
                if (type.Equals("StopTheHostException", StringComparison.Ordinal))
                {
                throw;
                }

                Log.Fatal(ex, "Unhandled exception");
                Console.WriteLine("DEBUGGING - status_api/Program.cs - catch block end");
                Console.WriteLine(ex);
            }
            finally
            {
                Console.WriteLine("DEBUGGING - status_api/Program.cs - finally block");
                Log.CloseAndFlush();
            }

            Console.WriteLine("DEBUGGING - status_api/Program.cs - near end of file");
        }
    }
}


// Required for tests:
// public partial class Program { }

