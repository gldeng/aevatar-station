using System.ComponentModel;
using Aevatar.Application.Grains.Agents.TestAgent;
using Aevatar.Code;
using Aevatar.Code.GEvents;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.Code;

[GenerateSerializer]
public class PingMessage : EventBase
{
    [Id(0)] public string Message { get; set; }
}

[Description("Handle Agent Combination")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class CodeGAgent : GAgentBase<CodeGAgentState, CodeAgentGEvent>, ICodeGAgent
{
    public CodeGAgent(ILogger<CodeGAgent> logger)
    {
    }

    [EventHandler]
    public async Task HandlePingMessageAsync(PingMessage message)
    {
        await PublishAsync(new TestRequest()
        {
            Details = message.Message
        });
        Console.WriteLine($"received message: {message.Message}");
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "This agent is used to store the code needed to create a webhook.");
    }

    public async Task UploadCodeAsync(string webhookId, string version, byte[] codeBytes)
    {
        var addCodeAgentGEvent = new AddCodeAgentGEvent
        {
            Ctime = DateTime.UtcNow,
            WebhookId = webhookId,
            WebhookVersion = version,
            Code = codeBytes
        };
        RaiseEvent(addCodeAgentGEvent);
        await ConfirmEvents();
    }
}

public interface ICodeGAgent : IStateGAgent<CodeGAgentState>
{
    Task UploadCodeAsync(string webhookId, string version, byte[] codeBytes);
}