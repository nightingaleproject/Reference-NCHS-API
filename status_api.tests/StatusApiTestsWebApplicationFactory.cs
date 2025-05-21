using Microsoft.AspNetCore.TestHost;

namespace status_api.tests;

public class StatusApiTestsWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    public IConfiguration Configuration { get; }

    private static readonly object _lock = new();

    /// <summary>
    /// There is a problem with using Serilog's "CreateBootstrapLogger" when trying to initialize a web host.
    /// This is because in tests, multiple hosts are created in parallel, and Serilog's static logger is not thread-safe.
    /// The way around this without touching the host code is to lock the creation of the host to a single thread at a time.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    protected override TestServer CreateServer(IWebHostBuilder builder)
    {
        lock (_lock)
            return base.CreateServer(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, builder) => {
            builder.AddJsonFile("appsettings.Test.json");
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
    }
}
