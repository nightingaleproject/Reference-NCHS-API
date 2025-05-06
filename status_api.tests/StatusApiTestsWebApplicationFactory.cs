namespace status_api.tests;

public class StatusApiTestsWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    public IConfiguration Configuration { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Console.WriteLine("==== 1 =====");

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
                // TODO logging:
                //var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                db.Database.EnsureCreated();

            }
        });
    }
}
