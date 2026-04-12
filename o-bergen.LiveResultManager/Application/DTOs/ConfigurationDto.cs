namespace o_bergen.LiveResultManager.Application.DTOs;

/// <summary>
/// Application configuration DTO
/// Maps to appsettings.json structure
/// </summary>
public record ConfigurationDto
{
    public AccessDbConfig AccessDb { get; init; } = new();
    public SupabaseConfig Supabase { get; init; } = new();
    public ArchiveConfig Archive { get; init; } = new();
    public TransferConfig Transfer { get; init; } = new();
    public LoggingConfig Logging { get; init; } = new();
}

/// <summary>
/// Access Database configuration
/// </summary>
public record AccessDbConfig
{
    public string Path { get; init; } = string.Empty;
    public string ConnectionString { get; init; } = string.Empty;
}

/// <summary>
/// Supabase configuration
/// </summary>
public record SupabaseConfig
{
    public string Url { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string TableName { get; init; } = "live_results";
}

/// <summary>
/// Archive configuration
/// </summary>
public record ArchiveConfig
{
    public string BasePath { get; init; } = string.Empty;
    public int KeepDays { get; init; } = 90;

    /// <summary>
    /// Encoding for Sploype CSV files: "windows-1252" (legacy WinSplits) or "utf-8-bom" (modern)
    /// Default: "windows-1252" for compatibility with World of O WinSplits
    /// </summary>
    public string SploypeCsvEncoding { get; init; } = "windows-1252";
}

/// <summary>
/// Transfer operation configuration
/// </summary>
public record TransferConfig
{
    public int IntervalSeconds { get; init; } = 30;
    public int RetryAttempts { get; init; } = 3;
    public int RetryDelaySeconds { get; init; } = 5;
    public bool EnableAutoStart { get; init; } = false;
}

/// <summary>
/// Logging configuration
/// </summary>
public record LoggingConfig
{
    public string LogLevel { get; init; } = "Information";
    public bool EnableFileLogging { get; init; } = true;
    public string LogPath { get; init; } = string.Empty;
}
