using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Silo.Extensions;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Aevatar.Silo;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.secrets.json", optional: true)
            .Build();
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting Silo");
            var builder = CreateHostBuilder(args);
            var app = builder.Build();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    internal static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Configure OpenTelemetry
                services.AddAevatarOpenTelemetry(hostContext.Configuration);
                services.AddApplication<SiloModule>();
            })
            .UseOrleansConfiguration()
            .UseAutofac()
            .UseSerilog((context, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext();

                // Add OpenTelemetry logging support
                configuration.WriteTo.OpenTelemetry(options =>
                {
                    var serviceName = context.Configuration["OpenTelemetry:ServiceName"] ?? "Aevatar.Silo";
                    var serviceVersion = context.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0";
                    var endpoint = context.Configuration["OpenTelemetry:CollectorEndpoint"] ?? OpenTelemetryExtensions.DefaultCollectorEndpoint;
                    
                    options.Endpoint = endpoint;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = serviceName,
                        ["service.version"] = serviceVersion
                    };
                });
            });
}