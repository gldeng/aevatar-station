using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Silo.Extensions;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Threading.Tasks;

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
                services.AddOpenTelemetry()
                    .WithTracing(builder =>
                    {
                        builder
                            .AddSource("Aevatar.TracedStreamProcessing") // Add your ActivitySource
                            // .AddSource("Aevatar.Silo") // Add Silo's ActivitySource
                            // .SetResourceBuilder(ResourceBuilder
                            //     .CreateDefault()
                            //     .AddService("Aevatar.Silo"))
                            // Orleans instrumentation - directly include Orleans activity sources
                            // .AddSource("Orleans.Runtime")
                            // .AddSource("Orleans.Messaging")
                            // .AddSource("Microsoft.Orleans")
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            // Add console exporter for debugging
                            .AddConsoleExporter();
                    });

                services.AddApplication<SiloModule>();
            })
            .UseOrleansConfiguration()
            .UseAutofac()
            .UseSerilog();
}