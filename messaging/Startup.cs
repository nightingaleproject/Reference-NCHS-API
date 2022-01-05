using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using messaging.Models;
using Microsoft.Extensions.Logging;

namespace messaging
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.AddMemoryCache();
            services.AddMiniProfiler(options => options.RouteBasePath = "/profiler").AddEntityFramework();
            services.AddDbContext<ApplicationDbContext>(opt =>
                opt.UseSqlServer(Configuration.GetConnectionString("NVSSMessagingDatabase"))
            );
            services.AddControllers().AddNewtonsoftJson(
                options => options.SerializerSettings.Converters.Add(new BundleConverter())
            );
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "NVSSMessaging", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseHttpLogging();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NVSSMessaging v1"));
            }
            app.UseMiniProfiler();
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Uncomment the following line to enable logging when running under IIS
            // Serilog.Extensions.Logging.File must also be enabled in messaging.csproj
            // loggerFactory.AddFile("logs/nvssmessaging-{Date}.txt");
        }
    }
}
