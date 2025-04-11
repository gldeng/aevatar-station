using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Orleans.Streams;
using System.Diagnostics;

namespace Aevatar.TracedStreamProcessing;

public class TracedGAgentAsyncObserver : GAgentAsyncObserver, IAsyncObserver<EventWrapperBase>
{
    private static readonly ActivitySource ActivitySource = new ActivitySource("Aevatar.Messaging");

    public TracedGAgentAsyncObserver(List<EventWrapperBaseAsyncObserver> observers) : base(observers)
    {
    }

    public async Task OnNextAsync(EventWrapperBase item, StreamSequenceToken? token = null)
    {
        // Extract the actual event for better naming and context
        var eventProperty = item.GetType().GetProperty("Event");
        var eventObj = eventProperty?.GetValue((object)item) as EventBase;
        var eventTypeName = eventObj?.GetType().FullName ?? "UnknownEvent";

        using var activity = ActivitySource.StartActivity(
            $"ProcessNextGrainEvent/{eventTypeName}",
            ActivityKind.Internal);

        // Add event details to the activity
        // TODO: Should we add grain id
        activity?.SetTag("event.type", eventTypeName);
        activity?.SetTag("event.correlationid", eventObj?.CorrelationId);
        activity?.SetTag("event.publishergrainid", eventObj?.PublisherGrainId);
        activity?.SetTag("stream.sequencenumber", token?.SequenceNumber.ToString());

        // Add more descriptive tags
        activity?.SetTag("event.timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        try
        {
            var startTime = Stopwatch.GetTimestamp();

            // Consider wrapping in another activity for more detailed tracing
            using (activity?.Source.StartActivity("CoreEventProcessing"))
            {
                await base.OnNextAsync(item, token);
            }

            var elapsed = Stopwatch.GetElapsedTime(startTime);
            activity?.SetTag("event.processing.time_ms", elapsed.TotalMilliseconds);

            // Add performance categorization tag
            if (elapsed.TotalMilliseconds > 1000)
            {
                activity?.SetTag("performance.category", "slow");
            }
            else if (elapsed.TotalMilliseconds > 300)
            {
                activity?.SetTag("performance.category", "medium");
            }
            else
            {
                activity?.SetTag("performance.category", "fast");
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().FullName);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.stack_trace", ex.StackTrace);
            throw;
        }
    }
}