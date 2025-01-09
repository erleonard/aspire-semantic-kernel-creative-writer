// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ChatApp.WebApi.Converters;
using System.Text.Json.Serialization;

namespace ChatApp.WebApi.Model;

[JsonConverter(typeof(JsonCamelCaseEnumConverter<AIChatRole>))]
public enum AIChatRole
{
    System,
    Assistant,
    User
}
