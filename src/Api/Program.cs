using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

const string ApiKeyScheme = "ApiKeyBearer";

builder.Services
    .AddAuthentication(ApiKeyScheme)
    .AddScheme<ApiKeyBearerOptions, ApiKeyBearerHandler>(ApiKeyScheme, o =>
    {
        // Recommended: environment variable (App Service setting / Container App secret)
        // Example value: "key1,key2" (supports rotation)
        var raw = builder.Configuration["Shortener:ApiKeys"]
                  ?? Environment.GetEnvironmentVariable("SHORTENER_API_KEYS")
                  ?? "";
        o.ValidKeys = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    });

builder.Services.AddAuthorization();


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddAzureTableClient("strTables");

builder.Services.AddTransient<ILogger>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return loggerFactory.CreateLogger("shortenerLogger");
});


var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapShortenerEnpoints();

app.Run();

