using Microsoft.AspNetCore.TestHost;

namespace status_api.tests;

public class StatusApiTestsWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    public IConfiguration Configuration { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Console.WriteLine("DEBUGGING - status_api.tests/...WebApplicationFactory - start of ConfigureWebHost");

        builder.ConfigureAppConfiguration((context, builder) =>
        {
            builder.AddJsonFile("status_api.appsettings.json");
            builder.AddJsonFile("status_api.appsettings.Test.json");
        });

        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            var buildServiceProvider = services.BuildServiceProvider();

            using (var scope = buildServiceProvider.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                var logger = scopedServices.GetRequiredService<ILogger<StatusApiTestsWebApplicationFactory<TProgram>>>();

                db.Database.EnsureCreated();
            }
        });

        Console.WriteLine("DEBUGGING - status_api.tests/...WebApplicationFactory - end of ConfigureWebHost");
    }
}
