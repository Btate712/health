using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace health_api.Configuration;

public static class AuthConfiguration
{
    public static async Task AddAuthAsync(this WebApplicationBuilder builder, string[] args)
    {
        CognitoSecrets cognitoSecrets = await CognitoCredentials.GetCognitoSecrets(args);
        
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = cognitoSecrets.Authority;
                options.Audience = cognitoSecrets.ClientId;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = cognitoSecrets.Authority,
                    ValidateAudience = true,
                    ValidAudience = cognitoSecrets.ClientId,
                    ValidateLifetime = true
                };
            });

        builder.Services.AddAuthorization();
    }
    
}