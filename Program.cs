using System;
using FileWatcherWorkerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FileWatcher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json") // Load other settings from appsettings.json
                .Build();

            // Prompt the user for input and update the configuration values
            Console.WriteLine("File Watcher Configuration");

            Console.Write("Enter source directory path: ");
            string sourcePath = Console.ReadLine();
            configuration["SourcePath"] = sourcePath;

            /*Console.Write("Enter destination directory path: ");
            string destinationPath = Console.ReadLine();
            configuration["DestinationPath"] = destinationPath;
            */
            Console.Write("Enter delay1 in milliseconds: ");
            string delay1 = Console.ReadLine();
            configuration["Delay1"] = delay1;

            CreateHostBuilder(args, configuration).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddConfiguration(configuration); // Use the provided configuration
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                });
    }
}
