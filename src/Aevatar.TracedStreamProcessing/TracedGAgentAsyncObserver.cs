using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Orleans.Streams;
using System.Diagnostics;

namespace Aevatar.TracedStreamProcessing;

public class TracedGAgentAsyncObserver : GAgentAsyncObserver, IAsyncObserver<EventWrapperBase>
{
    private static readonly ActivitySource ActivitySource = new ActivitySource("Aevatar.Messaging");
    private readonly string _consumerId;

    public TracedGAgentAsyncObserver(List<EventWrapperBaseAsyncObserver> observers, string consumerId) : base(observers)
    {
        _consumerId = consumerId;
    }

    public async Task OnNextAsync(EventWrapperBase item, StreamSequenceToken? token = null)
    {
        // Extract the actual event for better naming and context
        var eventProperty = item.GetType().GetProperty("Event");
        var eventObj = eventProperty?.GetValue((object)item) as EventBase;
        using var scope = OpenTelemetryScope.Start(_consumerId, eventObj, token);
        try
        {
            await base.OnNextAsync(item, token);
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            throw;
        }
    }
}