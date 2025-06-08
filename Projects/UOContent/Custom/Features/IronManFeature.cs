using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Custom.Interfaces;
using Server.Custom.Mobiles;
using Server.Ethics;

namespace Server.Custom.Features
{
    public class IronmanFeature : IPlayerFeature
    {
        private Mobile Owner { get; set; }

        public bool IsActive { get; set; } = false;
        public int IronmanScore { get; set; } = 0;
        public DateTime IronmanStartTime { get; set; } = DateTime.MinValue;
        public TimeSpan IronmanSurvivalTime { get; set; }

        public Dictionary<string, int> IronmanMonsterKills { get; set; } = new();
        public Dictionary<string, int> IronmanPlayerKills { get; set; } = new();

        public int IronmanKillStreak { get; set; } = 0;
        public double IronmanScoreMultiplier { get; set; } = 1.0;
        public string IronmanStartRegion { get; set; } = "Unknown";
        public string IronmanCauseOfDeath { get; set; } = "Unknown";

        public List<string> IronmanAchievements { get; set; } = new();

        public int IronmanPvPDeaths { get; set; } = 0;
        public int IronmanPVPKills { get; set; } = 0;
        public int IronmanPVMKills { get; set; } = 0;
        public int IronmanPVMDeaths { get; set; } = 0;

        public IronmanFeature()
        {
            IronmanSurvivalTime = Core.Now - IronmanStartTime;
        }

        public void Initialize(Mobile owner)
        {
            Owner = owner;
            IronmanMonsterKills ??= new();
            IronmanPlayerKills ??= new();
            IronmanAchievements ??= new();
        }

        public void OnLogin()
        {
            Console.WriteLine($"[IronmanFeature] Ativo: {IsActive}");
            if (IsActive)
                Console.WriteLine("Entrou no modo Ironman.");
        }

        public void OnDeath()
        {
            if (IsActive)
            {
                IsActive = false;
                Console.WriteLine("Ironman morreu.");
            }
        }

        public void OnThink()
        {
            if (!IsActive || Owner is not CustomPlayer cp)
                return;

            var lastMove = new DateTime(cp.LastMoveTime);
            TimeSpan afkTime = Core.Now - lastMove;

            if (afkTime > TimeSpan.FromMinutes(60))
            {
                IronmanScore -= 100;
                cp.SendMessage(33, "[Ironman] Você está AFK há muito tempo. Penalidade de -100 pontos.");
            }
        }

        public void OnKill(Mobile victim, Mobile killer)
        {
            if (killer is CustomPlayer killerPlayer && victim is CustomPlayer victimPlayer)
            {
                int killerScore = (int)(victimPlayer.Fame / 100);
                ((IronmanFeature)killerPlayer.Manager.Features["ironman"]).IronmanScore += killerScore;
            }
            else if (killer is CustomPlayer player && victim is CustomCreature creature)
            {
                int killerScore = (int)(creature.Fame / 100);
                ((IronmanFeature)player.Manager.Features["ironman"]).IronmanScore += killerScore;
            }
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(1); // version
            writer.Write(IsActive);
            writer.Write(IronmanScore);
            writer.Write(IronmanStartTime);
            writer.Write(IronmanSurvivalTime);

            var validMonsterKills = IronmanMonsterKills.Where(kv => !string.IsNullOrWhiteSpace(kv.Key)).ToList();
            writer.Write(validMonsterKills.Count);
            foreach (var kv in validMonsterKills)
            {
                writer.Write(kv.Key);
                writer.Write(kv.Value);
            }

            var validPlayerKills = IronmanPlayerKills.Where(kv => !string.IsNullOrWhiteSpace(kv.Key)).ToList();
            writer.Write(validPlayerKills.Count);
            foreach (var kv in validPlayerKills)
            {
                writer.Write(kv.Key);
                writer.Write(kv.Value);
            }

            writer.Write(IronmanKillStreak);
            writer.Write(IronmanScoreMultiplier);
            writer.Write(IronmanStartRegion ?? "Unknown");
            writer.Write(IronmanCauseOfDeath ?? "Unknown");

            var validAchievements = IronmanAchievements.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
            writer.Write(validAchievements.Count);
            foreach (var achievement in validAchievements)
            {
                writer.Write(achievement);
            }

            writer.Write(IronmanPvPDeaths);
            writer.Write(IronmanPVPKills);
            writer.Write(IronmanPVMKills);
            writer.Write(IronmanPVMDeaths);
        }

        public void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    IronmanScore = reader.ReadInt();
                    IronmanStartTime = reader.ReadDateTime();
                    IronmanSurvivalTime = reader.ReadTimeSpan();

                    int monsterKillsCount = reader.ReadInt();
                    for (int i = 0; i < monsterKillsCount; i++)
                    {
                        string monsterName = reader.ReadString();
                        int monsterKills = reader.ReadInt();
                        if (!string.IsNullOrWhiteSpace(monsterName))
                        {
                            IronmanMonsterKills[monsterName] = monsterKills;
                        }
                    }

                    int playerKillsCount = reader.ReadInt();
                    for (int i = 0; i < playerKillsCount; i++)
                    {
                        string playerName = reader.ReadString();
                        int playerKills = reader.ReadInt();
                        if (!string.IsNullOrWhiteSpace(playerName))
                        {
                            IronmanPlayerKills[playerName] = playerKills;
                        }
                    }

                    IronmanKillStreak = reader.ReadInt();
                    IronmanScoreMultiplier = reader.ReadDouble();
                    IronmanStartRegion = reader.ReadString() ?? "Unknown";
                    IronmanCauseOfDeath = reader.ReadString() ?? "Unknown";

                    int achievementsCount = reader.ReadInt();
                    for (int i = 0; i < achievementsCount; i++)
                    {
                        string achievement = reader.ReadString();
                        if (!string.IsNullOrWhiteSpace(achievement))
                        {
                            IronmanAchievements.Add(achievement);
                        }
                    }

                    IronmanPvPDeaths = reader.ReadInt();
                    IronmanPVPKills = reader.ReadInt();
                    IronmanPVMKills = reader.ReadInt();
                    IronmanPVMDeaths = reader.ReadInt();

                    IsActive = reader.ReadBool();
                    break;
            }
        }
    }
}
