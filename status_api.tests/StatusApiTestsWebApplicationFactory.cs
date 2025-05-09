namespace status_api.tests;

public class StatusApiTestsWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        /*
        var connectionString =
                builder.Configuration.GetConnectionString("NVSSMessagingDatabase")
                    ?? throw new InvalidOperationException("Connection string 'NVSSMessagingDatabase' not found.");
        */
        
        builder.ConfigureServices(services =>
        {
            /*
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                    typeof(DbContextOptions<ApplicationDbContext>));

            services.Remove(dbContextDescriptor);

            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                    typeof(DbConnection));

            services.Remove(dbConnectionDescriptor);
            
            // Create open SqliteConnection so EF won't automatically close it.
            services.AddService<DbConnection>(container =>
            {
                var connection = 
                connection.Open();

                return connection;
            });

            services.AddDbContext<ApplicationDbContext>((container, options) =>
            {
                var connection = container.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });
            */

            /*
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString)
            );
            */
        });

        builder.UseEnvironment("Test");
    }
}
