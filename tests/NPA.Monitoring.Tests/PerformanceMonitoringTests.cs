using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NPA.Monitoring;
using Xunit;

namespace NPA.Monitoring.Tests;

public class PerformanceMonitoringTests
{
    [Fact]
    public void RecordMetric_ShouldAddMetric()
    {
        // Arrange
        var collector = new InMemoryMetricCollector(NullLogger<InMemoryMetricCollector>.Instance);

        // Act
        collector.RecordMetric("TestMethod", TimeSpan.FromMilliseconds(100), "TestCategory");

        // Assert
        var metrics = collector.GetAllMetrics();
        metrics.Should().HaveCount(1);
        metrics[0].MetricName.Should().Be("TestMethod");
        metrics[0].Category.Should().Be("TestCategory");
        metrics[0].Duration.Should().Be(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void RecordMetric_WithParameters_ShouldStoreParameters()
    {
        // Arrange
        var collector = new InMemoryMetricCollector(NullLogger<InMemoryMetricCollector>.Instance);
        var parameters = new Dictionary<string, object>
        {
            { "userId", 123 },
            { "action", "GetUser" }
        };

        // Act
        collector.RecordMetric("TestMethod", TimeSpan.FromMilliseconds(50), "Users", parameters);

        // Assert
        var metrics = collector.GetAllMetrics();
        metrics[0].Parameters.Should().NotBeNull();
        metrics[0].Parameters!["userId"].Should().Be(123);
        metrics[0].Parameters["action"].Should().Be("GetUser");
    }

    [Fact]
    public void GetStatistics_WithNoMetrics_ShouldReturnEmptyStats()
    {
        // Arrange
        var collector = new InMemoryMetricCollector(NullLogger<InMemoryMetricCollector>.Instance);

        // Act
        var stats = collector.GetStatistics("NonExistent");

        // Assert
        stats.CallCount.Should().Be(0);
        stats.MetricName.Should().Be("NonExistent");
    }

    [Fact]
    public void GetStatistics_WithMetrics_ShouldCalculateCorrectly()
    {
        // Arrange
        var collector = new InMemoryMetricCollector(NullLogger<InMemoryMetricCollector>.Instance);
        collector.RecordMetric("TestMethod", TimeSpan.FromMilliseconds(100));
        collector.RecordMetric("TestMethod", TimeSpan.FromMilliseconds(200));
        collector.RecordMetric("TestMethod", TimeSpan.FromMilliseconds(150));

        // Act
        var stats = collector.GetStatistics("TestMethod");

        // Assert
        stats.CallCount.Should().Be(3);
        stats.MinDuration.Should().Be(TimeSpan.FromMilliseconds(100));
        stats.MaxDuration.Should().Be(TimeSpan.FromMilliseconds(200));
        stats.AverageDuration.Should().Be(TimeSpan.FromMilliseconds(150));
        stats.TotalDuration.Should().Be(TimeSpan.FromMilliseconds(450));
    }

    [Fact]
    public void GetStatistics_ShouldCalculateP95()
    {
        // Arrange
        var collector = new InMemoryMetricCollector(NullLogger<InMemoryMetricCollector>.Instance);
        
        // Record 100 metrics with values from 1-100ms
        for (int i = 1; i <= 100; i++)
        {
            collector.RecordMetric("TestMethod", TimeSpan.FromMilliseconds(i));
        }

        // Act
        var stats = collector.GetStatistics("TestMethod");

        // Assert
        stats.CallCount.Should().Be(100);
        // P95 should be around 95ms (95th percentile of 1-100)
        stats.P95Duration.TotalMilliseconds.Should().BeInRange(94, 96);
    }

    [Fact]
    public void GetStatistics_WithCategoryFilter_ShouldFilterCorrectly()
    {
        // Arrange
        var collector = new InMemoryMetricCollector(NullLogger<InMemoryMetricCollector>.Instance);
        collector.RecordMetric("GetUser", TimeSpan.FromMilliseconds(100), "UserRepo");
        collector.RecordMetric("GetUser", TimeSpan.FromMilliseconds(200), "UserRepo");
        collector.RecordMetric("GetUser", TimeSpan.FromMilliseconds(300), "ProductRepo");

        // Act
        var stats = collector.GetStatistics("GetUser", "UserRepo");

        // Assert
        stats.CallCount.Should().Be(2);
        stats.AverageDuration.Should().Be(TimeSpan.FromMilliseconds(150));
    }

    [Fact]
    public void Clear_ShouldRemoveAllMetrics()
    {
        // Arrange
        var collector = new InMemoryMetricCollector(NullLogger<InMemoryMetricCollector>.Instance);
        collector.RecordMetric("TestMethod", TimeSpan.FromMilliseconds(100));
        collector.RecordMetric("TestMethod", TimeSpan.FromMilliseconds(200));

        // Act
        collector.Clear();

        // Assert
        collector.GetAllMetrics().Should().BeEmpty();
    }

    [Fact]
    public void RecordWarning_ShouldNotThrow()
    {
        // Arrange
        var collector = new InMemoryMetricCollector(NullLogger<InMemoryMetricCollector>.Instance);

        // Act
        var action = () => collector.RecordWarning("SlowMethod", TimeSpan.FromMilliseconds(2000), 1000, "TestCategory");

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void GetAllMetrics_ShouldReturnAllRecordedMetrics()
    {
        // Arrange
        var collector = new InMemoryMetricCollector(NullLogger<InMemoryMetricCollector>.Instance);
        collector.RecordMetric("Method1", TimeSpan.FromMilliseconds(100), "Cat1");
        collector.RecordMetric("Method2", TimeSpan.FromMilliseconds(200), "Cat2");
        collector.RecordMetric("Method3", TimeSpan.FromMilliseconds(300), "Cat1");

        // Act
        var metrics = collector.GetAllMetrics();

        // Assert
        metrics.Should().HaveCount(3);
        metrics.Should().Contain(m => m.MetricName == "Method1");
        metrics.Should().Contain(m => m.MetricName == "Method2");
        metrics.Should().Contain(m => m.MetricName == "Method3");
    }

    [Fact]
    public void MetricStatistics_ShouldHandleSingleMetric()
    {
        // Arrange
        var collector = new InMemoryMetricCollector(NullLogger<InMemoryMetricCollector>.Instance);
        collector.RecordMetric("SingleMethod", TimeSpan.FromMilliseconds(100));

        // Act
        var stats = collector.GetStatistics("SingleMethod");

        // Assert
        stats.CallCount.Should().Be(1);
        stats.MinDuration.Should().Be(TimeSpan.FromMilliseconds(100));
        stats.MaxDuration.Should().Be(TimeSpan.FromMilliseconds(100));
        stats.AverageDuration.Should().Be(TimeSpan.FromMilliseconds(100));
        stats.P95Duration.Should().Be(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void RecordMetric_ThreadSafety_ShouldHandleConcurrentCalls()
    {
        // Arrange
        var collector = new InMemoryMetricCollector(NullLogger<InMemoryMetricCollector>.Instance);
        var tasks = new List<Task>();

        // Act - Record metrics from multiple threads
        for (int i = 0; i < 10; i++)
        {
            int threadNum = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    collector.RecordMetric($"Method{threadNum}", TimeSpan.FromMilliseconds(j));
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var metrics = collector.GetAllMetrics();
        metrics.Should().HaveCount(1000); // 10 threads * 100 metrics each
    }
}
