using health_api.Configuration;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

await builder.AddAuthAsync(args);

WebApplication app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/data", () =>
    Results.Ok(new { data = "something secret" })
).RequireAuthorization();

app.Run();
