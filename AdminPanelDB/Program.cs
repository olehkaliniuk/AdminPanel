using AdminPanelDB.Repository;
using AdminPanelDB.Services;
using System.Diagnostics;
using Serilog;

namespace AdminPanelDB
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    "Logs/app.log",
                    rollingInterval: RollingInterval.Infinite,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
                )
                .CreateLogger();


            builder.Host.UseSerilog();


            if (!EventLog.SourceExists("AdminPanel"))
            {
                EventLog.CreateEventSource("AdminPanel", "Application");
            }

            builder.Logging.AddEventLog(options =>
            {
                options.SourceName = "AdminPanel";
            });


            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // ConfigRepository.
            builder.Services.AddScoped<ConfigRepository>();

            // ConfigLogRepository.
            builder.Services.AddScoped<ConfigLogRepository>();


            // ConfigRepository.
            builder.Services.AddScoped<AdresseRepository>();

            // Load config for Admin Panel.
            builder.Services.AddScoped<ConfigService>();

            // Session.
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }



            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            // Session.
            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Auth}/{action=Login}/{id?}");

            app.Run();
        }
    }
}