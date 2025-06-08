using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Server.Custom.Features.Ironman
{
    public class IronmanRankingEntry
    {
        [JsonPropertyName("PlayerName")]
        public string PlayerName { get; set; }
        [JsonPropertyName("Score")]
        public int Score { get; set; }
        [JsonPropertyName("SurvivalTime")]
        public string SurvivalTime { get; set; } // pode ser string ou segundos
        [JsonPropertyName("KillPvPKillStreak")]
        public int KillPvPKillStreak { get; set; }
        [JsonPropertyName("KillPvMKillStreak")]
        public int KillPvMKillStreak { get; set; }
        [JsonPropertyName("Achievements")]
        public List<string> Achievements { get; set; }
        [JsonPropertyName("CauseOfDeath")]
        public string CauseOfDeath { get; set; }
        [JsonPropertyName("StartRegion")]
        public string StartRegion { get; set; }
        [JsonPropertyName("PvPKills")]
        public int PvPKills { get; set; }
        [JsonPropertyName("PvPDeaths")]
        public int PvPDeaths { get; set; }
        [JsonPropertyName("PvMKills")]
        public int PvMKills { get; set; }
        [JsonPropertyName("PvMDeaths")]
        public int PvMDeaths { get; set; }
        [JsonPropertyName("IsActive")]
        public bool IsActive { get; set; }
        [JsonPropertyName("Timestamp")]
        public DateTime Timestamp { get; set; }
    }

}