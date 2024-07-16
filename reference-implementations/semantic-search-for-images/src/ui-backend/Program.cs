using Azure.Core;
using Azure.Identity;
using Polly;
using Polly.Retry;
using ui_backend;
using ui_backend.Models;
using ui_backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var credential = new ChainedTokenCredential(new AzureCliCredential(), new ManagedIdentityCredential(clientId: Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")));
var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();

builder.Services.AddSingleton(appSettings!);
builder.Services.AddSingleton<TokenCredential>(credential);
builder.Services.AddTransient<IImageService, ImageService>();

if(appSettings!.DatabaseTargeted == Constants.DATABASE_TARGETED_COSMOSDB)
{
    builder.Services.AddSingleton<IDatabaseService, CosmoDbService>();
}
else if(appSettings!.DatabaseTargeted == Constants.DATABASE_TARGETED_AI_SEARCH)
{
    builder.Services.AddSingleton<IDatabaseService, AiSearchService>();
}

builder.Services.AddHttpClient();
builder.Services.AddHttpClient(Constants.NAMED_HTTP_CLIENT_AI_SERVICES, c =>
{
    c.BaseAddress = new Uri(appSettings!.AiServices.Uri);   
});
builder.Services.AddResiliencePipeline(Constants.NAMED_RESILIENCE_PIPELINE, builder =>
{
    builder
        .AddRetry(new RetryStrategyOptions{
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Linear
        
        })
        .AddTimeout(TimeSpan.FromSeconds(10));
});

builder.Services.AddCors();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Note: when running in a non-development environment, we expect the host resource will manage 
    // (e.g., Azure Container Apps: https://learn.microsoft.com/en-us/azure/container-apps/cors)
    app.UseCors(policy => policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .SetIsOriginAllowed(origin => true) // allow any origin
        .AllowCredentials());
}

app.UseAuthorization();

app.MapControllers();

app.Run();