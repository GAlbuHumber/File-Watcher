using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileWatcher;
//using FileWatcherService;
using FileWatcherWorkerService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FileWatcher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        })
        .ConfigureServices((hostContext, services) =>
        {
            services.AddHostedService<Worker>();
        });


    }
}
