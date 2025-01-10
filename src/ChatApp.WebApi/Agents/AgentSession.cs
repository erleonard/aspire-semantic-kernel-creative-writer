using System;
using System.Runtime.CompilerServices;
using System.Text;
using Azure;
using ChatApp.WebApi.Interfaces;
using ChatApp.WebApi.Model;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatApp.WebApi.Agents;

public class AgentSession(OpenAIAssistantAgent agent, string threadId) : ISemanticKernelSession
{
    public string ThreadId => threadId;

    public async Task<AIChatCompletion> ProcessRequest(AIChatRequest request)
    {
        var userInput = request.Messages.Last();
        ChatMessageContent message = new(AuthorRole.User, userInput.Content);
        await agent.AddChatMessageAsync(threadId, message);

        ChatMessageContent response = await agent.InvokeAsync(threadId).FirstAsync();

        string authorExpression = response.Role == AuthorRole.User ? string.Empty : $" - {response.AuthorName ?? "*"}";
        string contentExpression = response.Content ?? string.Empty;

        return new AIChatCompletion(Message: new AIChatMessage
        {
            Role = AIChatRole.Assistant,
            Content = $"{response.Role}{authorExpression}: {contentExpression}",
        })
        {
            SessionState = ThreadId,
        };
    }

    public async IAsyncEnumerable<AIChatCompletionDelta> ProcessStreamingRequest(AIChatRequest request)
    {
        var userInput = request.Messages.Last();
        ChatMessageContent message = new(AuthorRole.User, userInput.Content);
        await agent.AddChatMessageAsync(threadId, message);

        var streamedResponses = agent.InvokeStreamingAsync(threadId);
        var first = true;
        await foreach (var response in streamedResponses)
        {
            string contentExpression = response.Content ?? string.Empty;
            if (first)
            {
                first = false;
                string authorExpression = response.Role == AuthorRole.User ? string.Empty : $" - {response.AuthorName ?? "*"}";
                contentExpression = $"{response.Role}{authorExpression}: {contentExpression}";
            }

            yield return new AIChatCompletionDelta(Delta: new AIChatMessageDelta
            {
                Role = AIChatRole.Assistant,
                Content = contentExpression,
            })
            {
                SessionState = ThreadId,
            };
        }
    }
}
