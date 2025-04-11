using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Orleans.Streams;

namespace Aevatar.TracedStreamProcessing;

public class TracedGAgentConsumerFactory : IGAgentConsumerFactory
{
    public IAsyncObserver<EventWrapperBase> CreateConsumer(IReadOnlyList<EventWrapperBaseAsyncObserver> observers, string consumerId)
    {
        return new TracedGAgentAsyncObserver(observers.ToList(), consumerId);
    }
}