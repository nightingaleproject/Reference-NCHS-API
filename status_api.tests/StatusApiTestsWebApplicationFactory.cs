namespace status_api.tests;

public class StatusApiTestsWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    public IConfiguration Configuration { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, builder) =>
        {
// builder.AddJsonFile("status_api.appsettings.json, optional: true");
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
    }
}
