namespace WediFrame.Modules.Media.Services;

/// <summary>Tuning for the ZIP export worker. Bound from the "Export" config section.</summary>
public sealed class ExportOptions
{
    public const string SectionName = "Export";

    /// <summary>Master switch (kept on by default; mirrors the thumbnail worker).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Delay before the first poll after startup.</summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>How often to look for pending jobs when idle.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// A Running job older than this is assumed dead (worker crashed) and is
    /// re-claimed on the next poll.
    /// </summary>
    public TimeSpan StaleRunningAfter { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>How long a finished ZIP stays downloadable before it must be regenerated.</summary>
    public TimeSpan ZipTtl { get; set; } = TimeSpan.FromHours(24);
}
