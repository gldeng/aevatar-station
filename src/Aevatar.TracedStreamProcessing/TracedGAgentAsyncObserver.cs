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

        // Use attributes in the activity name to ensure they'll be captured in metrics
        using var activity = ActivitySource.StartActivity(
            $"ProcessNextGrainEvent/{eventTypeName}",
            ActivityKind.Internal);

        // Add event details to the activity
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
            var processingTimeMs = elapsed.TotalMilliseconds;
            
            // Record the processing time as a span attribute
            activity?.SetTag("event.processing.time_ms", processingTimeMs);

            // Add performance categorization tag
            if (processingTimeMs > 1000)
            {
                activity?.SetTag("performance.category", "slow");
            }
            else if (processingTimeMs > 300)
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