using DevHabit.Api;
using DevHabit.Api.Extensions;
using DevHabit.Api.Settings;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
    .AddApiServices()
    .AddErrorHandling()
    .AddDatabase()
    .AddObservability()
    .AddApplicationServices()
    .AddAuthenticationServices()
    .AddBackgroundJobs()
    .AddCorsPolicy()
    .AddRateLimiting();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    await app.ApplyMigrationsAsync();

    await app.SeedInitialDataAsync();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors(CorsOptions.PolicyName);

//app.UseResponseCaching();
//app.UseOutputCache();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter(); // Implemented here to ensure to be applied after authentication and authorization, so that rate limits can be applied based on user identity or other factors.

app.UseUserContextEnrichment();
//app.UseMiddleware<ETagMiddleware>();

app.MapControllers();

await app.RunAsync();

public partial class Program;
