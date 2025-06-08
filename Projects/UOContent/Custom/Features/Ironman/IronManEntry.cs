using System;
using System.Collections.Generic;

namespace Server.Custom.Features.Ironman
{
    public class IronmanRankingEntry
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public string SurvivalTime { get; set; } // pode ser string ou segundos
        public int KillPvPKillStreak { get; set; }
        public int KillPvMKillStreak { get; set; }
        public List<string> Achievements { get; set; }
        public string CauseOfDeath { get; set; }
        public string StartRegion { get; set; }
        public int PvPKills { get; set; }
        public int PvPDeaths { get; set; }
        public int PvMKills { get; set; }
        public int PvMDeaths { get; set; }
        public bool IsActive { get; set; }
        public DateTime Timestamp { get; set; }
    }

}