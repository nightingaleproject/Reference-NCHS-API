using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using messaging.Services;
using Microsoft.Extensions.Options;
using System;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace messaging
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // old files will be cleaned up as per the retainedFileCountLimit (every 31 days)
            //dev -5.10.2023
            var config = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json")
                .Build();

            Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(config)
                    .CreateLogger();
            try
            {
                Log.Information("Starting the NVSS FHIR API");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
            
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                services.AddHostedService<QueuedHostedService>();
                    services.AddSingleton<IBackgroundTaskQueue>(ctx => {
                        if (!int.TryParse(hostContext.Configuration["QueueCapacity"], out var queueCapacity))
                            queueCapacity = 100;
                        return new BackgroundTaskQueue(queueCapacity);
                    });
                    services.AddScoped<ConvertToIJEBackgroundWork.Worker>();
                });
    }
}
