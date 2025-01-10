using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var azureDeployment = Environment.GetEnvironmentVariable("AzureDeployment") ?? "chatdeploymentnew";
var embeddingModelDeployment = Environment.GetEnvironmentVariable("EmbeddingModelDeployment") ?? "text-embedding-3-large";
var azureEndpoint = Environment.GetEnvironmentVariable("AzureEndpoint");

var vectorStoreCollectionName = Environment.GetEnvironmentVariable("VectorStoreCollectionName") ?? "products";

var openAi = builder.AddAzureOpenAI("openAi")
    .AddDeployment(new AzureOpenAIDeployment(azureDeployment, "gpt-4o", "2024-05-13", "Standard", 10))
    .AddDeployment(new AzureOpenAIDeployment(embeddingModelDeployment, "text-embedding-3-large", "1"));

var vectorSearch = builder.AddAzureSearch("vectorSearch");

var bingSearch = builder.AddBicepTemplate("bingSearch", "./BicepTemplates/bingSearch.bicep")
    .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName);
var bingAPIKey = bingSearch.GetSecretOutput("bingAPIKey");

var backend = builder.AddProject<Projects.ChatApp_WebApi>("backend")
    .WithReference(openAi)
    .WithReference(vectorSearch)
    .WithEnvironment("AzureDeployment", azureDeployment)
    .WithEnvironment("EmbeddingModelDeployment", embeddingModelDeployment)
    .WithEnvironment("AzureEndpoint", azureEndpoint)
    .WithEnvironment("BingAPIKey", bingAPIKey)
    .WithEnvironment("VectorStoreCollectionName", vectorStoreCollectionName)
    .WithExternalHttpEndpoints();

var frontend = builder.AddNpmApp("frontend", "../ChatApp.React")
    .WithReference(backend)
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
