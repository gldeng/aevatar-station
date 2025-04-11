using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Diagnostics;

namespace Aevatar.Silo.Extensions;

public static class OpenTelemetryExtensions
{
    public const string DefaultCollectorEndpoint = "http://localhost:4315";

    public static IServiceCollection AddAevatarOpenTelemetry(this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "Aevatar.Silo";
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0";
        var endpoint = configuration["OpenTelemetry:CollectorEndpoint"] ?? DefaultCollectorEndpoint;

        return services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: serviceName,
                serviceVersion: serviceVersion))
            .WithTracing(tracing =>
            {
                tracing.SetSampler(new AlwaysOnSampler())
                    .AddSource(serviceName)
                    .AddSource("Aevatar.Messaging") // Make sure this matches the source name exactly
                    .AddSource("Orleans.Runtime")
                    .AddSource("Orleans.Messaging")
                    .AddSource("Microsoft.Orleans")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter() // Add console exporter for debugging
                    .AddOtlpExporter(exporter =>
                    {
                        exporter.Endpoint = new Uri(endpoint);
                    });
            })
            .WithMetrics(metrics => metrics
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddMeter("Microsoft.Orleans")
                .AddMeter(serviceName)
                .AddOtlpExporter(exporter => exporter.Endpoint = new Uri(endpoint)))
            .Services;
    }

    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder, IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "Aevatar.Silo";
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0";
        var endpoint = configuration["OpenTelemetry:CollectorEndpoint"] ?? DefaultCollectorEndpoint;

        return builder.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
            options.ParseStateValues = true;
            options
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion))
                .AddOtlpExporter(exporter => exporter.Endpoint = new Uri(endpoint));
        });
    }
}