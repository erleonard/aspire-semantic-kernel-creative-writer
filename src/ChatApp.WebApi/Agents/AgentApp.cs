using System.Net;
using Azure.AI.OpenAI;
using ChatApp.WebApi.Interfaces;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.OpenAI;
using OpenTelemetry.Logs;
using System.Reflection;
using System.Text.Json;

namespace ChatApp.WebApi.Agents;

public class AgentApp(ISecretStore secretStore, IStateStore<string> stateStore, AzureOpenAIClient openAIClient, IConfiguration configuration) : ISemanticKernelApp
{
    const string prompt = @"
        ChatBot can have a conversation with you about any topic.
        It can give explicit instructions or say 'I don't know' if it does not know the answer.";

    public async Task<ISemanticKernelSession> CreateSession()
    {
        OpenAIAssistantAgent agent = await CreateAgentAsync();
        string threadId = await agent.CreateThreadAsync();
        return new AgentSession(agent, threadId);
    }

    public async Task<ISemanticKernelSession> GetSession(string threadId)
    {
        OpenAIAssistantAgent agent = await CreateAgentAsync();
        return new AgentSession(agent, threadId);
    }

    private async Task<OpenAIAssistantAgent> CreateAgentAsync()
    {
        var agentName = "ChatBot";

        var azureDeployment = await secretStore.GetSecretAsync("AzureDeployment");
        var clientProvider = OpenAIClientProvider.FromClient(openAIClient);
        var kernel = CreateKernel();

        await foreach (var exisitingAgent in OpenAIAssistantAgent.ListDefinitionsAsync(clientProvider))
        {
            if (exisitingAgent.Name == agentName)
            {
                var existingAgent = await OpenAIAssistantAgent.RetrieveAsync(clientProvider, exisitingAgent.Id, kernel);
                FixLoggingFactoryBug(kernel, existingAgent);
                return existingAgent;
            }
        }

        var agent = await OpenAIAssistantAgent.CreateAsync(
                    clientProvider: OpenAIClientProvider.FromClient(openAIClient),
                    definition: new OpenAIAssistantDefinition(azureDeployment)
                    {
                        Name = agentName,
                        Instructions = prompt
                    },
                    kernel: kernel);
        FixLoggingFactoryBug(kernel, agent);
        return agent;
    }

    private static void FixLoggingFactoryBug(Kernel kernel, OpenAIAssistantAgent agent)
    {
        // This is a workaround for a bug in the OpenAIAssistantAgent that does not set the LoggerFactory property
        // https://github.com/microsoft/semantic-kernel/issues/10110
        var loggerFactoryProperty = typeof(OpenAIAssistantAgent).GetProperty("LoggerFactory", BindingFlags.Public | BindingFlags.Instance);
        if (loggerFactoryProperty != null)
        {
            loggerFactoryProperty.SetValue(agent, kernel.LoggerFactory);
        }
    }

    private sealed class FunctionInvocationFilter() : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            if (context.Function.PluginName == "SearchPlugin")
            {
                Console.WriteLine($"{context.Function.Name}:{JsonSerializer.Serialize(context.Arguments)}\n");
            }
            await next(context);
        }
    }

    private Kernel CreateKernel()
    {
        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.ConfigureOpenTelemetry(configuration);

        builder.Services.AddSingleton<IFunctionInvocationFilter, FunctionInvocationFilter>();

        builder.Services.ConfigureHttpClientDefaults(c =>
        {
            c.AddStandardResilienceHandler();
        });
        return builder.Build();
    }
}
