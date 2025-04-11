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
        activity?.SetTag("event.correlation_id", eventObj?.CorrelationId);
        activity?.SetTag("event.publisher_grain_id", eventObj?.PublisherGrainId);
        activity?.SetTag("stream.sequence_number", token?.SequenceNumber.ToString());

        // Add more descriptive tags
        activity?.SetTag("event.timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        try
        {
            await base.OnNextAsync(item, token);
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