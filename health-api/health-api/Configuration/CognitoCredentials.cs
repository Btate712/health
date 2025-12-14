using System.Text.Json;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace health_api.Configuration;

public record CognitoSecrets(string UserPoolId, string ClientId, string Region, string Authority);

public static class CognitoCredentials
{
    public static async Task<CognitoSecrets> GetCognitoSecrets(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.TryAddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
        builder.Services.AddAWSService<IAmazonSecretsManager>();

        string? region = builder.Configuration["aws-region"];

        if (region == null)
        {
            throw new Exception("No AWS region found in the appsettings.json");
        }

        RegionEndpoint regionEndpoint = RegionEndpoint.GetBySystemName(region);

        builder.Services.AddSingleton<IAmazonSecretsManager>(_ =>
            new AmazonSecretsManagerClient(regionEndpoint));

        WebApplication app = builder.Build();
        
        using (var scope = app.Services.CreateScope())
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
        
        string authority = $"https://cognito-idp.{region}.amazonaws.com/{userPoolIdKey}";
        
        return new CognitoSecrets(userPoolIdKey, clientIdKey, region, authority);
    }
}