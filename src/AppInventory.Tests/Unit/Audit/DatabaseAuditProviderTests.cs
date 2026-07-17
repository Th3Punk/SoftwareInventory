using AppInventory.Core.Entities;
using AppInventory.Infrastructure.Audit;
using AppInventory.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppInventory.Tests.Unit.Audit;

public class DatabaseAuditProviderTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AppInventoryDbContext _dbContext;
    private readonly DatabaseAuditProvider _provider;

    public DatabaseAuditProviderTests()
    {
        var options = new DbContextOptionsBuilder<AppInventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppInventoryDbContext(options);

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddScoped<AppInventoryDbContext>();
        _serviceProvider = services.BuildServiceProvider();

        _provider = new DatabaseAuditProvider(_serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }

    [Fact]
    public void IsAvailable_ReturnsTrue()
    {
        _provider.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task LogAsync_PersistsAuditLogEntryAsync()
    {
        await _provider.LogAsync("Created", "Application", "42",
            userId: 1,
            newValueJson: "{\"name\":\"MyApp\"}",
            ipAddress: "127.0.0.1",
            userAgent: "TestAgent/1.0");

        var dbContext = _serviceProvider.GetRequiredService<AppInventoryDbContext>();
        var log = await dbContext.AuditLogs.SingleAsync();

        log.Action.Should().Be("Created");
        log.ResourceType.Should().Be("Application");
        log.ResourceId.Should().Be("42");
        log.UserId.Should().Be(1);
        log.NewValueJson.Should().Be("{\"name\":\"MyApp\"}");
        log.IpAddress.Should().Be("127.0.0.1");
        log.UserAgent.Should().Be("TestAgent/1.0");
        log.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LogAsync_WithNullOptionals_PersistsEntryAsync()
    {
        await _provider.LogAsync("Deleted", "Documentation", "7");

        var dbContext = _serviceProvider.GetRequiredService<AppInventoryDbContext>();
        var log = await dbContext.AuditLogs.SingleAsync();

        log.UserId.Should().BeNull();
        log.OldValueJson.Should().BeNull();
        log.NewValueJson.Should().BeNull();
        log.IpAddress.Should().BeNull();
        log.UserAgent.Should().BeNull();
    }

    [Fact]
    public async Task LogAsync_MultipleEntries_AllPersistedAsync()
    {
        await _provider.LogAsync("Created", "Application", "1");
        await _provider.LogAsync("Updated", "Application", "1");
        await _provider.LogAsync("Deleted", "Application", "1");

        var dbContext = _serviceProvider.GetRequiredService<AppInventoryDbContext>();
        var count = await dbContext.AuditLogs.CountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task LogAsync_StoresOldAndNewValueAsync()
    {
        await _provider.LogAsync("Updated", "Application", "5",
            oldValueJson: "{\"name\":\"OldName\"}",
            newValueJson: "{\"name\":\"NewName\"}");

        var dbContext = _serviceProvider.GetRequiredService<AppInventoryDbContext>();
        var log = await dbContext.AuditLogs.SingleAsync();

        log.OldValueJson.Should().Be("{\"name\":\"OldName\"}");
        log.NewValueJson.Should().Be("{\"name\":\"NewName\"}");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }
}
