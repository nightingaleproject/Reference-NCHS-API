using Microsoft.Extensions.FileProviders;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// using System;
using messaging.Models;

Console.WriteLine("Booting status_api Program.cs");

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

// Load configuration, with latter sources overriding former
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json")
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// Specify MSSQL DB URL in NVSSMessagingDatabase
var connectionString =
    builder.Configuration.GetConnectionString("NVSSMessagingDatabase")
        ?? throw new InvalidOperationException("Connection string 'NVSSMessagingDatabase' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddMemoryCache();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

    // null check the MaxRequestBodySizeFeature, this feature is null in the dotnet test instance and will throw a null error in our testing framework
    // IHttpMaxRequestBodySizeFeature feat = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    // if (feat != null)
    // {
    //     feat.MaxRequestBodySize = Int32.Parse(Configuration.GetSection("AppSettings").GetSection("MaxPayloadSize").Value);
    // }

    // The StatusUI React app requires its own CSP headers
    if (context.Request.Path.StartsWithSegments("/StatusUI"))
    {
        context.Response.Headers.Add("Content-Security-Policy", "default-src 'self' 'unsafe-inline';");
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

