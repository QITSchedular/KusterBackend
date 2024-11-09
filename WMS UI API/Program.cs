using System;
using Microsoft.AspNetCore.Hosting;
using NLog;
using NLog.Web;
using Serilog;
using Serilog.Events;

namespace WMS_UI_API
{
    public class Program
    {
        //https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-7.0
        //https://stackoverflow.com/questions/67793589/asp-net-core-api-self-hosted-logging-to-file
        public static void Main(string[] args)
        {
            Logger nLogger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

            try
            {
                nLogger.Debug("NLogeer initilized in 'Main'");
                //var config = new ConfigurationBuilder()
                //.AddJsonFile("appsettings.json", optional: false)
                //.Build();

                //var path = config.GetValue<string>("Logging:FilePath");

                //Log.Logger = new LoggerConfiguration()
                //    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                //    .Enrich.FromLogContext()
                //    .WriteTo.File(path)
                //    .CreateLogger();

                CreateHostBuilder(args).Build().Run();

            }
            catch (Exception e)
            {
                nLogger.Error(e, "Application terminated unexpectdly");
                throw;
            }
            finally
            {
                LogManager.Shutdown();
            } 
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;

                config.SetBasePath(env.ContentRootPath)
                    .AddJsonFile("Secrets.json", optional: true, reloadOnChange: true);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging
                .AddConsole()
                .AddDebug()
                .AddEventLog();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureLogging(logging =>
                {
                    //To remove or default logging providers
                    logging.ClearProviders();

                    //To set the log level for recording in file and console
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);

                    //Enable logging to console.
                    logging.AddConsole();
                })
                .UseNLog()
                .UseStartup<Startup>();
            });
    }
} 