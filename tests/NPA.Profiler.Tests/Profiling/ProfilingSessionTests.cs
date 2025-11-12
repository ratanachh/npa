using FluentAssertions;
using NPA.Profiler.Profiling;
using Xunit;

namespace NPA.Profiler.Tests.Profiling;

public class ProfilingSessionTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Act
        var session = new ProfilingSession();

        // Assert
        session.SessionId.Should().NotBeEmpty();
        session.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        session.EndTime.Should().BeNull();
        session.Queries.Should().BeEmpty();
        session.TotalQueries.Should().Be(0);
    }

    [Fact]
    public void Start_ShouldStartSession()
    {
        // Arrange
        var session = new ProfilingSession();

        // Act
        session.Start();

        // Assert
        session.Duration.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void Stop_ShouldSetEndTime()
    {
        // Arrange
        var session = new ProfilingSession();
        session.Start();

        // Act
        session.Stop();

        // Assert
        session.EndTime.Should().NotBeNull();
        session.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddQuery_ShouldAddQueryToSession()
    {
        // Arrange
        var session = new ProfilingSession();
        var query = new QueryProfile
        {
            Sql = "SELECT * FROM Users",
            QueryType = QueryType.Select,
            Duration = TimeSpan.FromMilliseconds(50)
        };

        // Act
        session.AddQuery(query);

        // Assert
        session.Queries.Should().HaveCount(1);
        session.Queries.First().Should().BeSameAs(query);
        session.TotalQueries.Should().Be(1);
    }

    [Fact]
    public void CacheHitRate_WithNoQueries_ShouldBeZero()
    {
        // Arrange
        var session = new ProfilingSession();

        // Act & Assert
        session.CacheHitRate.Should().Be(0);
    }

    [Fact]
    public void CacheHitRate_WithCachedQueries_ShouldCalculateCorrectly()
    {
        // Arrange
        var session = new ProfilingSession();
        session.AddQuery(new QueryProfile { FromCache = true });
        session.AddQuery(new QueryProfile { FromCache = false });
        session.AddQuery(new QueryProfile { FromCache = true });
        session.AddQuery(new QueryProfile { FromCache = false });

        // Act & Assert
        session.CacheHitRate.Should().Be(0.5); // 2 out of 4
    }

    [Fact]
    public void GetStatistics_ShouldCalculateCorrectly()
    {
        // Arrange
        var session = new ProfilingSession();
        session.AddQuery(new QueryProfile { Duration = TimeSpan.FromMilliseconds(100), RowsAffected = 10 });
        session.AddQuery(new QueryProfile { Duration = TimeSpan.FromMilliseconds(50), RowsAffected = 5 });
        session.AddQuery(new QueryProfile { Duration = TimeSpan.FromMilliseconds(150), RowsAffected = 15 });

        // Act
        var stats = session.GetStatistics();

        // Assert
        stats.TotalQueries.Should().Be(3);
        stats.TotalDuration.Should().Be(300);
        stats.AverageDuration.Should().Be(100);
        stats.MinDuration.Should().Be(50);
        stats.MaxDuration.Should().Be(150);
        stats.TotalRowsAffected.Should().Be(30);
    }

    [Fact]
    public void GetStatistics_ShouldIdentifySlowQueries()
    {
        // Arrange
        var session = new ProfilingSession();
        session.AddQuery(new QueryProfile { Duration = TimeSpan.FromMilliseconds(50) });
        session.AddQuery(new QueryProfile { Duration = TimeSpan.FromMilliseconds(150) }); // Slow
        session.AddQuery(new QueryProfile { Duration = TimeSpan.FromMilliseconds(200) }); // Slow

        // Act
        var stats = session.GetStatistics();

        // Assert
        stats.SlowQueries.Should().HaveCount(2);
    }

    [Fact]
    public void GetStatistics_P95Duration_ShouldCalculateCorrectly()
    {
        // Arrange
        var session = new ProfilingSession();
        for (int i = 1; i <= 100; i++)
        {
            session.AddQuery(new QueryProfile { Duration = TimeSpan.FromMilliseconds(i) });
        }

        // Act
        var stats = session.GetStatistics();

        // Assert
        stats.P95Duration.Should().BeApproximately(95, 5); // Should be close to 95ms
    }

    [Fact]
    public void GetStatistics_WithNoQueries_ShouldReturnZeroValues()
    {
        // Arrange
        var session = new ProfilingSession();

        // Act
        var stats = session.GetStatistics();

        // Assert
        stats.TotalQueries.Should().Be(0);
        stats.TotalDuration.Should().Be(0);
        stats.AverageDuration.Should().Be(0);
        stats.MinDuration.Should().Be(0);
        stats.MaxDuration.Should().Be(0);
    }
}
