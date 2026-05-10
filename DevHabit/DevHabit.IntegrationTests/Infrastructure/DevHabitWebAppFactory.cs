using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;
using WireMock.Server;

namespace DevHabit.IntegrationTests.Infrastructure;

public sealed class DevHabitWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17.2")
        .WithDatabase("devhabit")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WireMockServer _wireMockServer;

    public WireMockServer GetWireMockServer() => _wireMockServer;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // To override the dbcontext definition and introduce another one for the test container.
        builder.UseSetting("ConnectionStrings:Database", _postgresContainer.GetConnectionString());

        // For the MockServer
        builder.UseSetting("GitHub:BaseUrl", _wireMockServer.Urls[0]);
        builder.UseSetting("Encryption:Key", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        Quartz.Logging.LogContext.SetCurrentLogProvider(NullLoggerFactory.Instance);
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        // Using the WebApplicationFactory's it also run the migrations and seed the database, but an alternative could be run an script
        //_postgresContainer.ExecScriptAsync();
        _wireMockServer = WireMockServer.Start();
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.StopAsync();
        _wireMockServer.Stop();
    }
}
