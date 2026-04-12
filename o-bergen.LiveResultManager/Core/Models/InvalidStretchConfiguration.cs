namespace o_bergen.LiveResultManager.Core.Models;

/// <summary>
/// Configuration file structure for invalid stretches
/// </summary>
public class InvalidStretchConfiguration
{
    /// <summary>
    /// List of all configured invalid stretches across all events
    /// </summary>
    public List<InvalidStretch> Stretches { get; set; } = new();
}
