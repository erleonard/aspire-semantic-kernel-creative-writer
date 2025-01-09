// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.AI.OpenAI;
using ChatApp.WebApi.Interfaces;
using Microsoft.SemanticKernel;

namespace ChatApp.WebApi.Agents;

internal record AzureOpenAIConfig(string Deployment, string Endpoint);

public class SemanticKernelApp : ISemanticKernelApp
{
    private readonly ISecretStore _secretStore;
    private readonly IStateStore<string> _stateStore;
    private readonly Lazy<Task<Kernel>> _kernel;
    private readonly AzureOpenAIClient _openAIClient;
    private async Task<Kernel> InitKernel()
    {
        var config = await SemanticKernelConfig.CreateAsync(_secretStore, CancellationToken.None);
        var builder = Kernel.CreateBuilder();
        if (config.AzureOpenAIConfig is AzureOpenAIConfig azureOpenAIConfig)
        {
            if (azureOpenAIConfig.Deployment is null || azureOpenAIConfig.Endpoint is null)
            {
                throw new InvalidOperationException("AzureOpenAI is enabled but AzureDeployment and AzureEndpoint are not set.");
            }
            builder.AddAzureOpenAIChatCompletion(azureOpenAIConfig.Deployment, _openAIClient);
        }
        return builder.Build();
    }

    public SemanticKernelApp(ISecretStore secretStore, IStateStore<string> stateStore, AzureOpenAIClient openAIClient)
    {
        _secretStore = secretStore;
        _stateStore = stateStore;
        _openAIClient = openAIClient;
        _kernel = new(() => Task.Run(InitKernel));
    }

    public async Task<ISemanticKernelSession> CreateSession()
    {
        var kernel = await _kernel.Value;
        return new SemanticKernelSession(kernel, _stateStore, Guid.NewGuid());
    }

    public async Task<ISemanticKernelSession> GetSession(string threadId)
    {
        var sessionId = Guid.Parse(threadId);
        var kernel = await _kernel.Value;
        var state = await _stateStore.GetStateAsync(sessionId);
        if (state is null)
        {
            throw new KeyNotFoundException($"Session {sessionId} not found.");
        }
        return new SemanticKernelSession(kernel, _stateStore, sessionId);
    }
}
