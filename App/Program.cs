using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using App.Strategies;
using Lib.AzureBlobStorage;
using Lib.AzureSearchStorage;
using Lib.Configuration;
using Lib.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace App
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "DEV";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.Configure<Settings>(configuration.GetSection(nameof(Settings)));
            services.AddSingleton(typeof(IAzureSearchClient<>), typeof(AzureSearchClient<>));
            services.AddSingleton<IAzureBlobClient, AzureBlobClient>();
            services.AddSingleton<IProcessor, ChannelProcessor>();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole(options =>
                {
                    options.DisableColors = false;
                    options.TimestampFormat = "[HH:mm:ss:fff] ";
                });
                loggingBuilder.AddNonGenericLogger();
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            });

            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            var processors = serviceProvider.GetServices<IProcessor>();
            var azureSearchClient = serviceProvider.GetRequiredService<IAzureSearchClient<PersonIndex>>();

            await azureSearchClient.DeleteIndexAndDocumentsAsync();
            await azureSearchClient.CreateIndexWhenNotExistsAsync();

            var stopWatch = new Stopwatch();

            foreach (var processor in processors)
            {
                stopWatch.Start();
                await processor.LaunchAsync();
                stopWatch.Stop();

                logger.LogInformation("Elapsed time for {processor} is {duration}", processor.GetType().Name, stopWatch.Elapsed.ToString("g"));
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
            var count = await azureSearchClient.CountAsync();

            logger.LogInformation("Found {count} items in azure search storage", count);

            Console.WriteLine("Press any key to exit !");
            Console.ReadKey();
        }

        private static void AddNonGenericLogger(this ILoggingBuilder loggingBuilder)
        {
            var services = loggingBuilder.Services;
            services.AddSingleton(serviceProvider =>
            {
                const string categoryName = nameof(Program);
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                return loggerFactory.CreateLogger(categoryName);
            });
        }
    }
}
