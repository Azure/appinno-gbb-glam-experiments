using Azure.Core;
using Azure.Identity;
using ui_backend;
using ui_backend.Models;
using ui_backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/// Instantiates DefaultAzureCredentialOptions to be used by DefaultAzureCredential. The exclude flags are set to ensure that only
/// the ManagedIdentityCredential (User Assigned) or AzureCliCredential are used.
var credentialOptions = new DefaultAzureCredentialOptions { ManagedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"),
                                                            ExcludeInteractiveBrowserCredential = true,
                                                            ExcludeSharedTokenCacheCredential = true,
                                                            ExcludeVisualStudioCodeCredential = true,
                                                            ExcludeVisualStudioCredential = true,
                                                            ExcludeEnvironmentCredential = true,
                                                            ExcludeAzurePowerShellCredential = true,
                                                            ExcludeWorkloadIdentityCredential = true };

var credential = new DefaultAzureCredential(credentialOptions);
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

builder.Services.AddHttpClient(Constants.NAMED_HTTP_CLIENT);

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