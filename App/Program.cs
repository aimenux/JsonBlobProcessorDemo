﻿using System;
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
            services.AddSingleton<IAzureBlobClient, AzureBlobClient>();
            services.AddSingleton<IProcessor, ChannelProcessor>();
            services.AddSingleton<IProcessor, ChannelExtensionsProcessor>();
            services.AddSingleton(typeof(IAzureSearchClient<>), typeof(AzureSearchClient<>));

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


            foreach (var processor in processors)
            {
                await azureSearchClient.DeleteIndexAndDocumentsAsync();
                await azureSearchClient.CreateIndexWhenNotExistsAsync();

                logger.LogInformation("Running strategy '{strategy}'", processor.Name);

                var stopWatch = new Stopwatch();

                stopWatch.Start();
                await processor.LaunchAsync();
                stopWatch.Stop();

                logger.LogInformation("Elapsed time for '{processor}' is '{duration}'", processor.Name, stopWatch.Elapsed.ToString("g"));

                await Task.Delay(TimeSpan.FromSeconds(10));
                var count = await azureSearchClient.CountAsync();

                logger.LogInformation("Found '{count}' items in azure search storage", count);
            }

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
