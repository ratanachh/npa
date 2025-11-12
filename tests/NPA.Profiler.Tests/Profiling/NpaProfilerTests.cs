using FluentAssertions;
using NPA.Profiler.Profiling;
using Xunit;

namespace NPA.Profiler.Tests.Profiling;

public class NpaProfilerTests
{
    [Fact]
    public void StartSession_ShouldCreateNewSession()
    {
        // Arrange
        var profiler = new NpaProfiler();

        // Act
        var session = profiler.StartSession();

        // Assert
        session.Should().NotBeNull();
        profiler.CurrentSession.Should().BeSameAs(session);
        profiler.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void StartSession_WhenAlreadyActive_ShouldThrowException()
    {
        // Arrange
        var profiler = new NpaProfiler();
        profiler.StartSession();

        // Act
        Action act = () => profiler.StartSession();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already active*");
    }

    [Fact]
    public void StopSession_ShouldReturnCurrentSession()
    {
        // Arrange
        var profiler = new NpaProfiler();
        var session = profiler.StartSession();

        // Act
        var result = profiler.StopSession();

        // Assert
        result.Should().BeSameAs(session);
        result.EndTime.Should().NotBeNull();
        profiler.IsEnabled.Should().BeFalse();
        profiler.CurrentSession.Should().BeNull();
    }

    [Fact]
    public void StopSession_WhenNotActive_ShouldReturnNull()
    {
        // Arrange
        var profiler = new NpaProfiler();

        // Act
        var result = profiler.StopSession();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void BeginProfileQuery_WhenDisabled_ShouldReturnNull()
    {
        // Arrange
        var profiler = new NpaProfiler();

        // Act
        var scope = profiler.BeginProfileQuery("SELECT * FROM Users");

        // Assert
        scope.Should().BeNull();
    }

    [Fact]
    public void BeginProfileQuery_WhenEnabled_ShouldReturnScope()
    {
        // Arrange
        var profiler = new NpaProfiler();
        profiler.StartSession();

        // Act
        using var scope = profiler.BeginProfileQuery(
            "SELECT * FROM Users",
            QueryType.Select,
            "User");

        // Assert
        scope.Should().NotBeNull();
    }

    [Fact]
    public void BeginProfileQuery_ShouldCaptureQueryInSession()
    {
        // Arrange
        var profiler = new NpaProfiler();
        var session = profiler.StartSession();
        var sql = "SELECT * FROM Users WHERE Id = @id";

        // Act
        using (profiler.BeginProfileQuery(
            sql,
            QueryType.Select,
            "User",
            new Dictionary<string, object?> { ["@id"] = 123 }))
        {
            // Simulate query execution
            Thread.Sleep(10);
        }

        // Assert
        session.Queries.Should().HaveCount(1);
        var query = session.Queries.First();
        query.Sql.Should().Be(sql);
        query.QueryType.Should().Be(QueryType.Select);
        query.EntityType.Should().Be("User");
        query.Parameters.Should().ContainKey("@id").WhoseValue.Should().Be(123);
    }

    [Fact]
    public void Dispose_ShouldStopSession()
    {
        // Arrange
        var profiler = new NpaProfiler();
        profiler.StartSession();

        // Act
        profiler.Dispose();

        // Assert
        profiler.IsEnabled.Should().BeFalse();
        profiler.CurrentSession.Should().BeNull();
    }
}
