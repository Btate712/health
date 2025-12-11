using System.Text.Json;
using Amazon;
using Amazon.SecretsManager;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Amazon.SecretsManager.Model;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.TryAddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSecretsManager>();

string? region = builder.Configuration["aws-region"];

RegionEndpoint? regionEndpoint = !string.IsNullOrWhiteSpace(region) ? RegionEndpoint.GetBySystemName(region) : null;

builder.Services.AddSingleton<IAmazonSecretsManager>(_ =>
    region is null ? new AmazonSecretsManagerClient()
        : new AmazonSecretsManagerClient(regionEndpoint));

WebApplication tmpApp = builder.Build();

using (var scope = tmpApp.Services.CreateScope())
{
    IAmazonSecretsManager secretsClient = scope.ServiceProvider.GetRequiredService<IAmazonSecretsManager>();

    string? secretName = builder.Configuration["auth-secret-key"];

    GetSecretValueResponse response = await secretsClient.GetSecretValueAsync(new GetSecretValueRequest
    {
        SecretId = secretName
    });

    string secretsJson = response.SecretString;
    Dictionary<string, string>? secretsDict = JsonSerializer.Deserialize<Dictionary<string, string>>(secretsJson);

    if (secretsDict != null)
    {
        foreach (KeyValuePair<string, string> kvp in secretsDict)
        {
            builder.Configuration[kvp.Key] = kvp.Value;
        }
    }
}

bool isDev = builder.Environment.IsDevelopment();

string userPoolIdKey = isDev ? "Cognito__UserPoolId-dev-test" : "Cognito__UserPoolId";
string clientIdKey = isDev ? "Cognito__ClientId-dev-test" : "Cognito__ClientId";

string? userPoolId = builder.Configuration[userPoolIdKey];
string? clientId = builder.Configuration[clientIdKey];

var builder2 = WebApplication.CreateBuilder(args);

builder2.Services.AddOpenApi();
builder2.Services.AddEndpointsApiExplorer();

string authority = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";

builder2.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = clientId;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudience = clientId,
            ValidateLifetime = true
        };
    });

builder2.Services.AddAuthorization();

WebApplication app = builder2.Build();

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/data", () =>
    Results.Ok(new { data = "something secret" })
).RequireAuthorization();

app.Run();
