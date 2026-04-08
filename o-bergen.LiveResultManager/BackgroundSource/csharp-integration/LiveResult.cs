using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OBergen.LiveResults
{
    /// <summary>
    /// Datamodell for live_results tabell i Supabase
    /// Matcher TypeScript interface LiveResult
    /// </summary>
    [Table("live_results")]
    public class LiveResult : BaseModel
    {
        [PrimaryKey("id", false)]
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [Column("competition_date")]
        [JsonProperty("competition_date")]
        public string CompetitionDate { get; set; } = string.Empty;

        [Column("runner_id")]
        [JsonProperty("runner_id")]
        public string? RunnerId { get; set; }

        [Column("ecard")]
        [JsonProperty("ecard")]
        public string Ecard { get; set; } = string.Empty;

        [Column("ecard2")]
        [JsonProperty("ecard2")]
        public string? Ecard2 { get; set; }

        [Column("first_name")]
        [JsonProperty("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [Column("last_name")]
        [JsonProperty("last_name")]
        public string LastName { get; set; } = string.Empty;

        [Column("class")]
        [JsonProperty("class")]
        public string Class { get; set; } = string.Empty;

        [Column("course")]
        [JsonProperty("course")]
        public string? Course { get; set; }

        [Column("time_seconds")]
        [JsonProperty("time_seconds")]
        public int? TimeSeconds { get; set; }

        [Column("status")]
        [JsonProperty("status")]
        public string Status { get; set; } = "OK";

        [Column("status_message")]
        [JsonProperty("status_message")]
        public string? StatusMessage { get; set; }

        [Column("points")]
        [JsonProperty("points")]
        public int Points { get; set; }

        [Column("team_id")]
        [JsonProperty("team_id")]
        public string? TeamId { get; set; }

        [Column("team_name")]
        [JsonProperty("team_name")]
        public string? TeamName { get; set; }

        [Column("split_times")]
        [JsonProperty("split_times")]
        public List<SplitTime>? SplitTimes { get; set; }

        [Column("finished_at")]
        [JsonProperty("finished_at")]
        public DateTime FinishedAt { get; set; } = DateTime.UtcNow;

        [Column("created_at")]
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Strekktid for en enkelt post
    /// </summary>
    public class SplitTime
    {
        [JsonProperty("controlCode")]
        public string ControlCode { get; set; } = string.Empty;

        [JsonProperty("punchTime")]
        public string PunchTime { get; set; } = string.Empty;

        [JsonProperty("splitSeconds")]
        public int SplitSeconds { get; set; }

        [JsonProperty("cumulativeSeconds")]
        public int CumulativeSeconds { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("behind")]
        public int Behind { get; set; }
    }
}
