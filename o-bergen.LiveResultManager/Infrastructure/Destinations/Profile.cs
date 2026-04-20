using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace o_bergen.LiveResultManager.Infrastructure.Destinations;

/// <summary>
/// Minimal Supabase model for the profiles table.
/// Profiles represent authenticated web users who have explicitly chosen
/// to link their account to a specific runner_id from the historical registry.
/// This is the strongest signal for which runner ID is the "canonical" identity
/// for a given person.
/// </summary>
[Table("profiles")]
public class Profile : BaseModel
{
    [PrimaryKey("id", false)]
    [JsonProperty("id")]
    public string? Id { get; set; }

    [Column("runner_id")]
    [JsonProperty("runner_id")]
    public string? RunnerId { get; set; }
}
