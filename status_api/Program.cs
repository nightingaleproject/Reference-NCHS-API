using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;

using messaging.Models;
using status_api;

Console.WriteLine("Booting status_api Program.cs");

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

// builder automatically loads config from appsettings.<Environment>.json and appsettings.json. See for details:
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0#default-application-configuration-sources

// Specify MSSQL DB URL in NVSSMessagingDatabase
var connectionString =
    builder.Configuration.GetConnectionString("NVSSMessagingDatabase")
        ?? throw new InvalidOperationException("Connection string 'NVSSMessagingDatabase' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

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


// Print all Services for debugging
Console.WriteLine("Services:");
foreach (var service in builder.Services)
{
    Console.WriteLine($"{service.ServiceType.Name} - {service.ImplementationType?.Name ?? "Unknown"}");
}


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
        context.Response.Headers.Add("Content-Security-Policy", "default-src 'self' 'unsafe-inline';");
    }
    else if( context.Request.Path.StartsWithSegments("/swagger") && app.Environment.IsDevelopment() )
    {
        // TODO: enable Swagger UI for production?
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

    // GET /results to view profiling results
    app.UseMiniProfiler();

    // GET /swagger for Swagger OpenAPI Docs
    // TODO: do we enable this for production?
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

// Required for tests:
public partial class Program { }

