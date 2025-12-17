
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var customDomain = builder.AddParameter("CustomDomain");
var customApiDomain = builder.AddParameter("CustomApiDomain");
var customAdminDomain = builder.AddParameter("CustomAdminDomain");
var defaultRedirectUrl = builder.AddParameter("DefaultRedirectUrl");

// Secret for API keys
var apiKeys = builder.AddParameter("ApiKeys");


// Check if we have a connection string configured for local testing
var hasConnectionString = !string.IsNullOrEmpty(builder.Configuration.GetConnectionString("url-data"));
bool usingExistingStorage = builder.Environment.IsDevelopment() && hasConnectionString;
var urlStorage = builder.AddAzureStorage("url-data");

if(usingExistingStorage)
{
    urlStorage = urlStorage
		.RunAsExisting(
            builder.AddParameter("StorageAccountName"),
            builder.AddParameter("ResourceGroup")
        );

}
else if(builder.Environment.IsDevelopment()) {  // Use real Azure Storage in other environments
    urlStorage = urlStorage.RunAsEmulator();
}

var strTables = urlStorage.AddTables("strTables");

var azFuncLight = builder.AddAzureFunctionsProject<Projects.Cloud5mins_ShortenerTools_FunctionsLight>("azfunc-light")
	.WithReference(strTables)
	.WithEnvironment("DefaultRedirectUrl",defaultRedirectUrl)
	.WithExternalHttpEndpoints();

var manAPI = builder.AddProject<Projects.Cloud5mins_ShortenerTools_Api>("api")
    .WithReference(strTables)
	.WithEnvironment("CustomDomain",customDomain)
    .WithEnvironment("CustomApiDomain", customApiDomain)
    .WithEnvironment("DefaultRedirectUrl",defaultRedirectUrl)
    .WithEnvironment("SHORTENER_API_KEYS", apiKeys)
    .WithExternalHttpEndpoints(); // If you want to access the API directly

if(!usingExistingStorage)
{
    azFuncLight = azFuncLight.WaitFor(strTables);
    manAPI = manAPI.WaitFor(strTables);
}

builder.AddProject<Projects.Cloud5mins_ShortenerTools_TinyBlazorAdmin>("admin")
	.WithEnvironment("CustomAdminDomain", customAdminDomain)
    .WithExternalHttpEndpoints()
	.WithReference(manAPI);

builder.Build().Run();
