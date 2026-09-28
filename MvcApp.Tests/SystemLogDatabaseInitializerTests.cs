using Microsoft.Extensions.Logging.Abstractions;
using MvcApp.Services;
using Xunit;

namespace MvcApp.Tests;

public class SystemLogDatabaseInitializerTests
{
    [Theory]
    [InlineData("serilogsDb", true)]
    [InlineData("MyApp_logs", true)]
    [InlineData("my-app_2$logs", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    [InlineData("bad`name", false)]
    [InlineData("bad name", false)]
    [InlineData("bad;name", false)]
    [InlineData("bad'name", false)]
    [InlineData("-leading", false)]
    [InlineData(".leading", false)]
    [InlineData("../../etc", false)]
    public void IsValidDatabaseName_AcceptsOnlySafeIdentifiers(string? name, bool expected)
    {
        Assert.Equal(expected, SystemLogDatabaseInitializer.IsValidDatabaseName(name));
    }

    [Fact]
    public void IsValidDatabaseName_RejectsOverlyLongName()
    {
        Assert.False(SystemLogDatabaseInitializer.IsValidDatabaseName(new string('a', 65)));
        Assert.True(SystemLogDatabaseInitializer.IsValidDatabaseName(new string('a', 64)));
    }

    [Fact]
    public void BuildServerConnectionString_DropsTheDatabase()
    {
        var result = SystemLogDatabaseInitializer.BuildServerConnectionString(
            "Server=db.example.com;Database=MyApp_logs;Uid=user;Pwd=secret;");

        Assert.NotNull(result);
        Assert.DoesNotContain("MyApp_logs", result);
        Assert.Contains("db.example.com", result);
    }

    [Fact]
    public void BuildServerConnectionString_ReturnsNullForBlankInput()
    {
        Assert.Null(SystemLogDatabaseInitializer.BuildServerConnectionString(null));
        Assert.Null(SystemLogDatabaseInitializer.BuildServerConnectionString("   "));
    }

    [Fact]
    public void EnsureExistsAsync_DoesNothingWithoutAConnectionString()
    {
        var initializer = new SystemLogDatabaseInitializer(NullLogger<SystemLogDatabaseInitializer>.Instance);

        Assert.False(initializer.EnsureExistsAsync(null).GetAwaiter().GetResult());
        Assert.False(initializer.EnsureExistsAsync("  ").GetAwaiter().GetResult());
    }

    [Fact]
    public void EnsureExistsAsync_SkipsWhenTheDatabaseNameIsNotAValidIdentifier()
    {
        var initializer = new SystemLogDatabaseInitializer(NullLogger<SystemLogDatabaseInitializer>.Instance);

        // No database name at all: nothing to create, and no connection attempted.
        Assert.False(initializer.EnsureExistsAsync("Server=localhost;Uid=u;Pwd=p;").GetAwaiter().GetResult());
    }

    [Fact]
    public async Task EnsureExistsAsync_ReportsFalseWhenTheServerIsUnreachable()
    {
        var initializer = new SystemLogDatabaseInitializer(NullLogger<SystemLogDatabaseInitializer>.Instance);

        // Port 1 is not listening; the initializer must swallow the failure so the app still starts.
        var created = await initializer.EnsureExistsAsync(
            "Server=127.0.0.1;Port=1;Database=SomeApp_logs;Uid=u;Pwd=p;ConnectionTimeout=2;");

        Assert.False(created);
    }
}
