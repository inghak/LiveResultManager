using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OBergen.LiveResults
{
    /// <summary>
    /// Datamodell for results.json fra ET2002/Emit-systemet
    /// Tilpasset det eksisterende formatet du allerede genererer
    /// </summary>
    public class EmitResult
    {
        [JsonProperty("place")]
        public int Place { get; set; }

        [JsonProperty("ecard")]
        public string Ecard { get; set; } = string.Empty;

        [JsonProperty("ecard2")]
        public string? Ecard2 { get; set; }

        [JsonProperty("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonProperty("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonProperty("startTime")]
        public string? StartTime { get; set; }

        [JsonProperty("class")]
        public string Class { get; set; } = string.Empty;

        [JsonProperty("course")]
        public string Course { get; set; } = string.Empty;

        [JsonProperty("timeSeconds")]
        public int? TimeSeconds { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } = "OK";

        [JsonProperty("statusMessage")]
        public string? StatusMessage { get; set; }

        [JsonProperty("points")]
        public int Points { get; set; }

        [JsonProperty("teamId")]
        public string? TeamId { get; set; }

        [JsonProperty("teamName")]
        public string? TeamName { get; set; }

        [JsonProperty("splitTimes")]
        public List<EmitSplitTime>? SplitTimes { get; set; }
    }

    public class EmitSplitTime
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
