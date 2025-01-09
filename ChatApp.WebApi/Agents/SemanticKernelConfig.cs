// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ChatApp.WebApi.Interfaces;

namespace ChatApp.WebApi.Agents;

internal struct SemanticKernelConfig
{
    internal AzureOpenAIConfig AzureOpenAIConfig { get; private init; }

    internal static async Task<SemanticKernelConfig> CreateAsync(ISecretStore secretStore, CancellationToken cancellationToken)
    {
        var azureDeployment = await secretStore.GetSecretAsync("AzureDeployment", cancellationToken);
        var azureEndpoint = await secretStore.GetSecretAsync("AzureEndpoint", cancellationToken);

        return new SemanticKernelConfig
        {
            AzureOpenAIConfig = new AzureOpenAIConfig(azureDeployment, azureEndpoint),
        };
    }
}
