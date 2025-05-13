namespace status_api.tests;

public class StatusApiTestsWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    public IConfiguration Configuration { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, builder) => {
            builder.AddJsonFile("appsettings.Test.json");
        });

        var connectionString =
                Configuration.GetConnectionString("NVSSMessagingDatabase")
                    ?? throw new InvalidOperationException("Connection string 'NVSSMessagingDatabase' not found.");
        
        builder.ConfigureServices(services =>
        {
            var buildServiceProvider = services.BuildServiceProvider();

            using (var scope = buildServiceProvider.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                // TODO logging:
                //var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                db.Database.EnsureCreated();
            }
        });

        builder.UseEnvironment("Test");
    }
}
