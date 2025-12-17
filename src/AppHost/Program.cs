
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var customDomain = builder.AddParameter("CustomDomain");
var customApiDomain = builder.AddParameter("CustomApiDomain");
var customAdminDomain = builder.AddParameter("CustomAdminDomain");
var defaultRedirectUrl = builder.AddParameter("DefaultRedirectUrl");

// Secret for API keys
var apiKeys = builder.AddParameter("ApiKeys");

var urlStorage = builder.AddAzureStorage("url-data");

if (builder.Environment.IsDevelopment())
{
    urlStorage.RunAsEmulator();
}

var strTables = urlStorage.AddTables("strTables");

var azFuncLight = builder.AddAzureFunctionsProject<Projects.Cloud5mins_ShortenerTools_FunctionsLight>("azfunc-light")
	.WithReference(strTables)
	.WaitFor(strTables)
	.WithEnvironment("DefaultRedirectUrl",defaultRedirectUrl)
	.WithExternalHttpEndpoints();

var manAPI = builder.AddProject<Projects.Cloud5mins_ShortenerTools_Api>("api")
	.WithReference(strTables)
	.WaitFor(strTables)
	.WithEnvironment("CustomDomain",customDomain)
    .WithEnvironment("CustomApiDomain", customApiDomain)
    .WithEnvironment("DefaultRedirectUrl",defaultRedirectUrl)
    .WithEnvironment("SHORTENER_API_KEYS", apiKeys)
    .WithExternalHttpEndpoints(); // If you want to access the API directly

builder.AddProject<Projects.Cloud5mins_ShortenerTools_TinyBlazorAdmin>("admin")
	.WithEnvironment("CustomAdminDomain", customAdminDomain)
    .WithExternalHttpEndpoints()
	.WithReference(manAPI);

builder.Build().Run();
