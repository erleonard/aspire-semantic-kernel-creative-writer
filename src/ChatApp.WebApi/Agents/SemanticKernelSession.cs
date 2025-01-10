// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ChatApp.WebApi.Interfaces;
using ChatApp.WebApi.Model;
using Microsoft.SemanticKernel;
using System.Text;

namespace ChatApp.WebApi.Agents;

internal class SemanticKernelSession : ISemanticKernelSession
{
    private readonly Kernel _kernel;
    private readonly IStateStore<string> _stateStore;
    private readonly Guid _sessiondId;

    public string ThreadId { get => _sessiondId.ToString(); }

    internal SemanticKernelSession(Kernel kernel, IStateStore<string> stateStore, Guid sessionId)
    {
        _kernel = kernel;
        _stateStore = stateStore;
        _sessiondId = sessionId;
    }

    const string prompt = @"
        ChatBot can have a conversation with you about any topic.
        It can give explicit instructions or say 'I don't know' if it does not know the answer.

        {{$history}}
        User: {{$userInput}}
        ChatBot:";

    public async Task<AIChatCompletion> ProcessRequest(AIChatRequest message)
    {
        var chatFunction = _kernel.CreateFunctionFromPrompt(prompt);
        var userInput = message.Messages.Last();
        string history = await _stateStore.GetStateAsync(_sessiondId) ?? "";
        var arguments = new KernelArguments()
        {
            ["history"] = history,
            ["userInput"] = userInput.Content,
        };
        var botResponse = await chatFunction.InvokeAsync(_kernel, arguments);
        var updatedHistory = $"{history}\nUser: {userInput.Content}\nChatBot: {botResponse}";
        await _stateStore.SetStateAsync(_sessiondId, updatedHistory);
        return new AIChatCompletion(Message: new AIChatMessage
        {
            Role = AIChatRole.Assistant,
            Content = $"{botResponse}",
        })
        {
            SessionState = _sessiondId.ToString(),
        };
    }

    public async IAsyncEnumerable<AIChatCompletionDelta> ProcessStreamingRequest(AIChatRequest message)
    {
        var chatFunction = _kernel.CreateFunctionFromPrompt(prompt);
        var userInput = message.Messages.Last();
        string history = await _stateStore.GetStateAsync(_sessiondId) ?? "";
        var arguments = new KernelArguments()
        {
            ["history"] = history,
            ["userInput"] = userInput.Content,
        };
        var streamedBotResponse = chatFunction.InvokeStreamingAsync(_kernel, arguments);
        StringBuilder response = new();
        await foreach (var botResponse in streamedBotResponse)
        {
            response.Append(botResponse);
            yield return new AIChatCompletionDelta(Delta: new AIChatMessageDelta
            {
                Role = AIChatRole.Assistant,
                Content = $"{botResponse}",
            })
            {
                SessionState = _sessiondId.ToString(),
            };
        }
        var updatedHistory = $"{history}\nUser: {userInput.Content}\nChatBot: {response}";
        await _stateStore.SetStateAsync(_sessiondId, updatedHistory);
    }

}
