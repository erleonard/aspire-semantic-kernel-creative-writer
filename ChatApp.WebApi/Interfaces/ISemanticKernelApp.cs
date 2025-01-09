// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ChatApp.WebApi.Interfaces;

public interface ISemanticKernelApp
{
    Task<ISemanticKernelSession> CreateSession();
    Task<ISemanticKernelSession> GetSession(string threadId);
}
