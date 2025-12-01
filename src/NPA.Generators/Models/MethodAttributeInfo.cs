namespace NPA.Generators.Models;

internal class MethodAttributeInfo
{
    public bool HasQuery { get; set; }
    public string? QuerySql { get; set; }
    public bool NativeQuery { get; set; }

    public bool HasNamedQuery { get; set; }
    public string? NamedQueryName { get; set; }

    public bool HasStoredProcedure { get; set; }
    public string? ProcedureName { get; set; }
    public string? Schema { get; set; }

    public bool HasMultiMapping { get; set; }
    public string? KeyProperty { get; set; }
    public string? SplitOn { get; set; }

    public bool HasBulkOperation { get; set; }
    public int BatchSize { get; set; }
    public bool UseTransaction { get; set; }

    public int? CommandTimeout { get; set; }
    public bool Buffered { get; set; } = true;

    // New custom generator attributes
    public bool HasGeneratedMethod { get; set; }
    public bool IncludeNullCheck { get; set; } = true;
    public bool GenerateAsync { get; set; }
    public bool GenerateSync { get; set; }
    public string? CustomSql { get; set; }
    public bool IncludeLogging { get; set; }
    public bool IncludeErrorHandling { get; set; }
    public string? MethodDescription { get; set; }

    public bool IgnoreInGeneration { get; set; }
    public string? IgnoreReason { get; set; }

    public bool HasCustomImplementation { get; set; }
    public bool GeneratePartialStub { get; set; } = true;
    public string? ImplementationHint { get; set; }
    public bool CustomImplementationRequired { get; set; } = true;

    public bool HasCacheResult { get; set; }
    public int CacheDuration { get; set; } = 300;
    public string? CacheKeyPattern { get; set; }
    public string? CacheRegion { get; set; }
    public bool CacheNulls { get; set; }
    public int CachePriority { get; set; }
    public bool CacheSlidingExpiration { get; set; }

    public bool HasValidateParameters { get; set; }
    public bool ThrowOnNull { get; set; } = true;
    public bool ValidateStringsNotEmpty { get; set; }
    public bool ValidateCollectionsNotEmpty { get; set; }
    public bool ValidatePositive { get; set; }
    public string? ValidationErrorMessage { get; set; }

    public bool HasRetryOnFailure { get; set; }
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 100;
    public bool RetryExponentialBackoff { get; set; } = true;
    public int RetryMaxDelayMilliseconds { get; set; } = 30000;
    public bool LogRetries { get; set; } = true;

    public bool HasTransactionScope { get; set; }
    public bool TransactionRequired { get; set; } = true;
    public string? TransactionIsolationLevel { get; set; } = "ReadCommitted";
    public int TransactionTimeoutSeconds { get; set; } = 30;
    public bool TransactionAutoRollback { get; set; } = true;
    public bool TransactionJoinAmbient { get; set; } = true;

    // PerformanceMonitor attribute
    public bool HasPerformanceMonitor { get; set; }
    public bool IncludeParameters { get; set; }
    public int WarnThresholdMs { get; set; }
    public string? MetricCategory { get; set; }
    public bool TrackMemory { get; set; }
    public bool TrackQueryCount { get; set; }
    public string? MetricName { get; set; }

    // Audit attribute
    public bool HasAudit { get; set; }
    public bool AuditIncludeOldValue { get; set; }
    public bool AuditIncludeNewValue { get; set; } = true;
    public string AuditCategory { get; set; } = "Data";
    public string AuditSeverity { get; set; } = "Normal";
    public bool AuditIncludeParameters { get; set; } = true;
    public bool AuditCaptureUser { get; set; } = true;
    public string? AuditDescription { get; set; }
    public bool AuditCaptureIpAddress { get; set; }
}

