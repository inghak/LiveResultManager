using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace o_bergen.LiveResultManager.Infrastructure.Destinations;

/// <summary>
/// Minimal Supabase model for the runners table.
/// Contains all historical runner IDs across all seasons — one row per unique runner ID.
/// This is a registry of known participants, not tied to web accounts.
/// The runner_id matches the local eTiming participant ID (Name.id).
/// </summary>
[Table("runners")]
public class Runner : BaseModel
{
    [PrimaryKey("id", false)]
    [JsonProperty("id")]
    public string? Id { get; set; }

    [Column("runner_id")]
    [JsonProperty("runner_id")]
    public string RunnerId { get; set; } = string.Empty;
}
