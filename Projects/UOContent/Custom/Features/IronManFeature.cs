using System;
using System.Collections.Generic;
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

        public Dictionary<string, int> IronmanMonsterKills { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> IronmanPlayerKills { get; set; } = new Dictionary<string, int>();

        public int IronmanKillStreak { get; set; } = 0;

        public double IronmanScoreMultiplier { get; set; } = 1.0;

        public string IronmanStartRegion { get; set; } = "Unknown";

        public string IronmanCauseOfDeath { get; set; } = "Unknown";

        public List<string> IronmanAchievements { get; set; } = new List<string>();

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
            int monsterKillsCount = IronmanMonsterKills.Count;
            writer.Write(monsterKillsCount);
            foreach (var monsterKill in IronmanMonsterKills)
            {
                writer.Write(monsterKill.Key);
                writer.Write(monsterKill.Value);
            }
            int playerKillsCount = IronmanPlayerKills.Count;
            writer.Write(playerKillsCount);
            foreach (var playerKill in IronmanPlayerKills)
            {
                writer.Write(playerKill.Key);
                writer.Write(playerKill.Value);
            }
            writer.Write(IronmanKillStreak);
            writer.Write(IronmanScoreMultiplier);
            writer.Write(IronmanStartRegion);
            writer.Write(IronmanCauseOfDeath);
            int achievementsCount = IronmanAchievements.Count;
            writer.Write(achievementsCount);
            foreach (var achievement in IronmanAchievements)
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
                    {
                        IronmanScore = reader.ReadInt();
                        IronmanStartTime = reader.ReadDateTime();
                        IronmanSurvivalTime = reader.ReadTimeSpan();
                        int monsterKillsCount = reader.ReadInt();
                        for (int i = 0; i < monsterKillsCount; i++)
                        {
                            string monsterName = reader.ReadString();
                            int monsterKills = reader.ReadInt();
                            IronmanMonsterKills[monsterName] = monsterKills;
                        }
                        int playerKillsCount = reader.ReadInt();
                        for (int i = 0; i < playerKillsCount; i++)
                        {
                            string playerName = reader.ReadString();
                            int playerKills = reader.ReadInt();
                            IronmanPlayerKills[playerName] = playerKills;
                        }
                        IronmanKillStreak = reader.ReadInt();
                        IronmanScoreMultiplier = reader.ReadDouble();
                        IronmanStartRegion = reader.ReadString();
                        IronmanCauseOfDeath = reader.ReadString();
                        int achievementsCount = reader.ReadInt();
                        for (int i = 0; i < achievementsCount; i++)
                        {
                            string achievement = reader.ReadString();
                            IronmanAchievements.Add(achievement);
                        }
                        IronmanPvPDeaths = reader.ReadInt();
                        IronmanPVPKills = reader.ReadInt();
                        IronmanPVMKills = reader.ReadInt();
                        IronmanPVMDeaths = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        IsActive = reader.ReadBool();
                        break;
                    }
            }
        }
    }
}