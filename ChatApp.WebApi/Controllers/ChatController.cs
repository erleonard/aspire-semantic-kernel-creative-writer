// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ChatApp.WebApi.Agents;
using ChatApp.WebApi.Interfaces;
using ChatApp.WebApi.Model;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ChatApp.WebApi.Controllers;

[ApiController, Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly ISemanticKernelApp _semanticKernelApp;
    private readonly CreativeWriterApp _creativeWriterApp;
    private readonly IDeserializer _yamlDeserializer;


    public ChatController(ISemanticKernelApp semanticKernelApp, CreativeWriterApp creativeWriterApp)
    {
        _semanticKernelApp = semanticKernelApp;
        _creativeWriterApp = creativeWriterApp;

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> ProcessMessage(AIChatRequest request)
    {
        var session = request.SessionState switch
        {
            string sessionId => await _semanticKernelApp.GetSession(sessionId),
            _ => await _semanticKernelApp.CreateSession()
        };
        var response = await session.ProcessRequest(request);
        return Ok(response);
    }

    [HttpPost("stream")]
    [Consumes("application/json")]
    public async Task ProcessStreamingMessage(AIChatRequest request)
    {
        var response = Response;
        response.Headers.Append("Content-Type", "application/x-ndjson");

        try
        {
            var userInput = request.Messages.Last();
            CreateWriterRequest createWriterRequest = _yamlDeserializer.Deserialize<CreateWriterRequest>(userInput.Content);

            var session = await _creativeWriterApp.CreateSessionAsync(Response);

            await foreach (var delta in session.ProcessStreamingRequest(createWriterRequest))
            {
                await response.WriteAsync($"{JsonSerializer.Serialize(delta)}\r\n");
                await response.Body.FlushAsync();
            }
        }
        catch (YamlException ex) // TODO: very bad hack, the UI needs to be adopted
        {
            var session = request.SessionState switch
            {
                string sessionId => await _semanticKernelApp.GetSession(sessionId),
                _ => await _semanticKernelApp.CreateSession()
            };

            await foreach (var delta in session.ProcessStreamingRequest(request))
            {
                await response.WriteAsync($"{JsonSerializer.Serialize(delta)}\r\n");
                await response.Body.FlushAsync();
            }
        }
    }
}
