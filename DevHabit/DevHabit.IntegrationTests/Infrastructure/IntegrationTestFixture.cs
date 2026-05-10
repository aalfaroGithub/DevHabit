using System.Net.Http.Headers;
using System.Net.Http.Json;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WireMock.Server;

namespace DevHabit.IntegrationTests.Infrastructure;

//[Collection(nameof(IntegrationTestCollection))]
// Using the IClassFixture interface with WebApplicationFactory has some implications
// You get one instance of your container, having multiple test classes creating multiple test containers, consuming resources.
// One solution is to use a collection fixture, using the same collection for all the test cases, but it could cause issues with test parallelization and test isolation, as all tests would share the same container instance.
public abstract class IntegrationTestFixture(DevHabitWebAppFactory factory) : IClassFixture<DevHabitWebAppFactory>
{
    private HttpClient? _authorizedClient;

    public WireMockServer WireMockServer => factory.GetWireMockServer();

    public HttpClient CreateClient() => factory.CreateClient();

    protected async Task CleanupDatabaseAsync()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IConfiguration configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        string? connectionString = configuration.GetConnectionString("Database");
        if (connectionString is null)
        {
            throw new InvalidOperationException("Database connection string not found in configuration");
        }

        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();

        await using NpgsqlCommand command = new(@"
            DO $$
            BEGIN
                -- Truncate application tables
                TRUNCATE TABLE dev_habit.entries CASCADE;
                TRUNCATE TABLE dev_habit.entry_import_jobs CASCADE;
                TRUNCATE TABLE dev_habit.tags CASCADE;
                TRUNCATE TABLE dev_habit.habits CASCADE;
                TRUNCATE TABLE dev_habit.users CASCADE;

                -- Truncate identity tables
                TRUNCATE TABLE identity.asp_net_users CASCADE;
                TRUNCATE TABLE identity.refresh_tokens CASCADE;
            END $$;", connection);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    ///  To obtein an authenticated client to use on the tests.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="forceNewClient"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = "test@test.com",
        string password = "Test123!",
        bool forceNewClient = false)
    {
        if (_authorizedClient is not null && !forceNewClient)
        {
            return _authorizedClient;
        }

        HttpClient client = CreateClient();

        // Check if user exists
        bool userExists;
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            userExists = await dbContext.Users.AnyAsync(u => u.Email == email);
        }

        if (!userExists)
        {
            // Register a new user
            HttpResponseMessage registerResponse = await client.PostAsJsonAsync(Routes.Auth.Register,
                new RegisterUserDto
                {
                    Email = email,
                    Name = email,
                    Password = password,
                    ConfirmPassword = password
                });

            registerResponse.EnsureSuccessStatusCode();
        }

        // Login to get the token
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(Routes.Auth.Login,
            new LoginUserDto
            {
                Email = email,
                Password = password
            });

        loginResponse.EnsureSuccessStatusCode();

        AccessTokensDto? loginResult = await loginResponse.Content.ReadFromJsonAsync<AccessTokensDto>();

        if (loginResult?.AccessToken is null)
        {
            throw new InvalidOperationException("Failed to get authentication token");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);
        if (!forceNewClient)
        {
            _authorizedClient = client;
        }

        return client;
    }
}
