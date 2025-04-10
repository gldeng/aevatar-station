using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Orleans.Streams;
using System.Diagnostics;
using System;

namespace Aevatar.TracedStreamProcessing;

public  class TracedGAgentAsyncObserver : GAgentAsyncObserver, IAsyncObserver<EventWrapperBase>
{
    private static readonly ActivitySource ActivitySource = new ActivitySource("Aevatar.TracedStreamProcessing");

    public TracedGAgentAsyncObserver(List<EventWrapperBaseAsyncObserver> observers) : base(observers)
    {
    }

    public async Task OnNextAsync(EventWrapperBase item, StreamSequenceToken? token = null)
    {
        using var activity = ActivitySource.StartActivity(
            $"ProcessEvent_{item.GetType().Name}", 
            ActivityKind.Internal);
        var eventType = (EventBase) item.GetType().GetProperty("Event")?.GetValue((object) item);
        // Add event details to the activity
        activity?.SetTag("event.type", item.GetType().FullName);
        activity?.SetTag("event.correlationid", eventType?.CorrelationId);
        activity?.SetTag("event.publishergrainid", eventType?.PublisherGrainId);
        activity?.SetTag("stream.token", token?.ToString());
            
        try
        {
            var startTime = Stopwatch.GetTimestamp();
            
            await base.OnNextAsync(item, token);
            
            var elapsed = Stopwatch.GetElapsedTime(startTime);
            activity?.SetTag("event.processing.time_ms", elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().FullName);
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
    }
}