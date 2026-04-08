using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace o_bergen.LiveResultManager.Infrastructure.Destinations;

/// <summary>
/// Supabase model for live_competitions table
/// Matches existing LiveCompetition from OBergen.LiveResults
/// </summary>
[Table("live_competitions")]
public class LiveCompetition : BaseModel
{
    [PrimaryKey("id", false)]
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }

    [Column("competition_date")]
    [JsonProperty("competition_date")]
    public string CompetitionDate { get; set; } = string.Empty;

    [Column("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [Column("date")]
    [JsonProperty("date")]
    public string Date { get; set; } = string.Empty;

    [Column("organizer")]
    [JsonProperty("organizer")]
    public string Organizer { get; set; } = string.Empty;

    [Column("location")]
    [JsonProperty("location")]
    public string Location { get; set; } = string.Empty;

    [Column("courses")]
    [JsonProperty("courses")]
    public List<SupabaseCourse> Courses { get; set; } = new List<SupabaseCourse>();

    [Column("eventType")]
    [JsonProperty("eventType")]
    public string EventType { get; set; } = "Normal";

    [Column("terrainType")]
    [JsonProperty("terrainType")]
    public string TerrainType { get; set; } = "Skog";

    [Column("status")]
    [JsonProperty("status")]
    public string Status { get; set; } = "live";

    [Column("created_at")]
    [JsonProperty("created_at", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    [JsonProperty("updated_at", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Course for Supabase JSONB (matches existing format)
/// </summary>
public class SupabaseCourse
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("length")]
    public int Length { get; set; }

    [JsonProperty("level")]
    public string Level { get; set; } = string.Empty;

    [JsonProperty("controls")]
    public List<SupabaseControl> Controls { get; set; } = new List<SupabaseControl>();
}

/// <summary>
/// Control for Supabase JSONB (matches existing format)
/// </summary>
public class SupabaseControl
{
    [JsonProperty("no")]
    public int No { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;
}
