using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Custom.Interfaces;
using Server.Custom.Mobiles;
using Server.Ethics;
using System.Text.Json;
using System.Net.Http;
using System.Text;
using Server.Custom.Features.Ironman;
using System.Threading.Tasks;

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

        public int IronmanPvMKillStreak { get; set; } = 0;
        public int IronmanPvPKillStreak { get; set; } = 0;
        public double IronmanScoreMultiplier { get; set; } = 1.0;
        public string IronmanStartRegion { get; set; } = "Unknown";
        public string IronmanCauseOfDeath { get; set; } = "Unknown";

        public List<string> IronmanAchievements { get; set; } = new();

        public int IronmanPvPDeaths { get; set; } = 0;
        public int IronmanPVPKills { get; set; } = 0;
        public int IronmanPVMKills { get; set; } = 0;
        public int IronmanPVMDeaths { get; set; } = 0;

        private DateTime _lastAfkPenalty = DateTime.MinValue;

        public IronmanFeature()
        {
            IronmanSurvivalTime = TimeSpan.Zero;
        }

        public void Initialize(Mobile owner)
        {
            Owner = owner;
            IronmanMonsterKills ??= new();
            IronmanPlayerKills ??= new();
            IronmanAchievements ??= new();
        }

        public void StartRun()
        {
            IsActive = true;
            UpdateDropBoost(true);
            IronmanStartTime = Core.Now;
            IronmanSurvivalTime = TimeSpan.Zero;
            IronmanScore = 0;
            IronmanPvMKillStreak = 0;
            IronmanPvPKillStreak = 0;
            IronmanScoreMultiplier = 1.0;
            IronmanAchievements.Clear();
            IronmanPvPDeaths = 0;
            IronmanPVPKills = 0;
            IronmanPVMKills = 0;
            IronmanPVMDeaths = 0;
            IronmanMonsterKills.Clear();
            IronmanPlayerKills.Clear();
            IronmanStartRegion = Owner?.Region?.ToString() ?? "Unknown";
            IronmanCauseOfDeath = "Unknown";
  

        }

        public void StopRun()
        {
            IsActive = false;
            UpdateDropBoost(false);
            IronmanStartTime = DateTime.MinValue;
            IronmanSurvivalTime = TimeSpan.Zero;
            IronmanScore = 0;
            IronmanPvMKillStreak = 0;
            IronmanPvPKillStreak = 0;
            IronmanScoreMultiplier = 1.0;
            IronmanAchievements.Clear();
            IronmanPvPDeaths = 0;
            IronmanPVPKills = 0;
            IronmanPVMKills = 0;
            IronmanPVMDeaths = 0;
            IronmanMonsterKills.Clear();
            IronmanPlayerKills.Clear();
            IronmanStartRegion = "Unknown";
            IronmanCauseOfDeath = "Unknown";
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
                UpdateDropBoost(false);
                AtualizarSobrevivencia();
                Console.WriteLine("Ironman morreu.");
                AtualizaRankingIronmanAsync(new IronmanRankingEntry
                {
                    PlayerName = Owner.Name,
                    Score = IronmanScore,
                    SurvivalTime = IronmanSurvivalTime.ToString(),
                    KillPvPKillStreak = IronmanPvPKillStreak,
                    KillPvMKillStreak = IronmanPvMKillStreak,
                    Achievements = IronmanAchievements,
                    CauseOfDeath = IronmanCauseOfDeath,
                    StartRegion = IronmanStartRegion,
                    PvPKills = IronmanPVPKills,
                    PvPDeaths = IronmanPvPDeaths,
                    PvMKills = IronmanPVMKills,
                    PvMDeaths = IronmanPVMDeaths,
                    IsActive = IsActive,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        public void AtualizarSobrevivencia()
        {
            if (IronmanStartTime > DateTime.MinValue)
                IronmanSurvivalTime = Core.Now - IronmanStartTime;
            else
                IronmanSurvivalTime = TimeSpan.Zero;
        }

        private void UpdateDropBoost(bool active)
        {
            if (Owner is CustomPlayer cp &&
                cp.Manager.Features.TryGetValue("dropboost", out var feat) &&
                feat is DropBoostFeature boost)
            {
                boost.IsActive = active;
            }
        }

        public void OnThink()
        {
            if (!IsActive || Owner is not CustomPlayer cp)
                return;

            var lastMove = new DateTime(cp.LastMoveTime);
            TimeSpan afkTime = Core.Now - lastMove;

            if (afkTime > TimeSpan.FromHours(12))
            {
                if ((Core.Now - _lastAfkPenalty) > TimeSpan.FromHours(1))
                {
                    IronmanScore -= 10;
                    cp.SendMessage(33, "[Ironman] Você está AFK há muito tempo. Penalidade de -10 pontos.");
                    _lastAfkPenalty = Core.Now;
                }
            }
        }

        public void OnKill(Mobile victim, Mobile killer)
        {
            // Atualiza tempo de sobrevivência antes de salvar
            AtualizarSobrevivencia();

            if (killer is CustomPlayer killerPlayer && victim is CustomPlayer victimPlayer)
            {
                ((IronmanFeature)victimPlayer.Manager.Features["ironman"]).IsActive = false;
                ((IronmanFeature)victimPlayer.Manager.Features["ironman"]).IronmanPvPDeaths++;
                ((IronmanFeature)victimPlayer.Manager.Features["ironman"]).IronmanPvMKillStreak = 0;
                ((IronmanFeature)victimPlayer.Manager.Features["ironman"]).IronmanPvPKillStreak = 0;
                ((IronmanFeature)victimPlayer.Manager.Features["ironman"]).IronmanCauseOfDeath = killerPlayer.Name;
                ((IronmanFeature)victimPlayer.Manager.Features["ironman"]).IronmanStartRegion = victimPlayer.Region.ToString();
                ((IronmanFeature)victimPlayer.Manager.Features["ironman"]).AtualizarSobrevivencia();
                ((IronmanFeature)victimPlayer.Manager.Features["ironman"]).IronmanAchievements = new List<string>();
                ((IronmanFeature)victimPlayer.Manager.Features["ironman"]).IronmanScore = 0;
                ((IronmanFeature)victimPlayer.Manager.Features["ironman"]).IronmanStartTime = Core.Now;
                ((IronmanFeature)victimPlayer.Manager.Features["ironman"]).IronmanScoreMultiplier = 1.0;

                ((IronmanFeature)killerPlayer.Manager.Features["ironman"]).IronmanPVPKills++;
                ((IronmanFeature)killerPlayer.Manager.Features["ironman"]).IronmanPvPKillStreak++;
                ((IronmanFeature)victimPlayer.Manager.Features["ironman"]).IronmanScoreMultiplier = 1.0 + ((IronmanFeature)victimPlayer.Manager.Features["ironman"]).IronmanPvPKillStreak * 0.05;
                int killerScore = (int)(victimPlayer.Fame * ((IronmanFeature)victimPlayer.Manager.Features["ironman"]).IronmanScoreMultiplier) / 100;
                ((IronmanFeature)killerPlayer.Manager.Features["ironman"]).IronmanScore += killerScore;

                Console.WriteLine($"[IronmanRanking] Atualizando ranking para {killerPlayer.Name} com score {((IronmanFeature)killerPlayer.Manager.Features["ironman"]).IronmanScore}");
            }
            else if (killer is CustomPlayer customPlayer && victim is Mobile creature)
            {
                ((IronmanFeature)customPlayer.Manager.Features["ironman"]).IronmanPVMKills++;
                ((IronmanFeature)customPlayer.Manager.Features["ironman"]).IronmanPvMKillStreak++;
                ((IronmanFeature)customPlayer.Manager.Features["ironman"]).IronmanScoreMultiplier = 1.0 + ((IronmanFeature)customPlayer.Manager.Features["ironman"]).IronmanPvMKillStreak * 0.05;
                ((IronmanFeature)customPlayer.Manager.Features["ironman"]).IronmanScore = (int)(creature.Fame * ((IronmanFeature)customPlayer.Manager.Features["ironman"]).IronmanScoreMultiplier) / 100;
            }
            if (killer is CustomPlayer player)
                AtualizaRankingIronmanAsync(new IronmanRankingEntry
                {
                    PlayerName = player.Name,
                    Score = ((IronmanFeature)player.Manager.Features["ironman"]).IronmanScore,
                    SurvivalTime = ((IronmanFeature)player.Manager.Features["ironman"]).IronmanSurvivalTime.ToString(),
                    KillPvPKillStreak = ((IronmanFeature)player.Manager.Features["ironman"]).IronmanPvPKillStreak,
                    KillPvMKillStreak = ((IronmanFeature)player.Manager.Features["ironman"]).IronmanPvMKillStreak,
                    Achievements = ((IronmanFeature)player.Manager.Features["ironman"]).IronmanAchievements,
                    CauseOfDeath = ((IronmanFeature)player.Manager.Features["ironman"]).IronmanCauseOfDeath,
                    StartRegion = ((IronmanFeature)player.Manager.Features["ironman"]).IronmanStartRegion,
                    PvPKills = ((IronmanFeature)player.Manager.Features["ironman"]).IronmanPVPKills,
                    PvPDeaths = ((IronmanFeature)player.Manager.Features["ironman"]).IronmanPvPDeaths,
                    PvMKills = ((IronmanFeature)player.Manager.Features["ironman"]).IronmanPVMKills,
                    PvMDeaths = ((IronmanFeature)player.Manager.Features["ironman"]).IronmanPVMDeaths,
                    IsActive = ((IronmanFeature)player.Manager.Features["ironman"]).IsActive,
                    Timestamp = DateTime.UtcNow
                });
        }

        public static async Task AtualizaRankingIronmanAsync(IronmanRankingEntry entry)
        {
            var httpClient = new HttpClient();
            var url = "https://uobabel.com/api/update-ironman-ranking";
            var json = JsonSerializer.Serialize(entry);
            Console.WriteLine("[IronmanRanking] JSON enviado: " + json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Lê a chave secreta da env
            var serverKey = Environment.GetEnvironmentVariable("IRONMAN_SECRET_KEY") ?? "";
            Console.WriteLine($"[IronmanRanking] Chave secreta: {serverKey}");
            httpClient.DefaultRequestHeaders.Add("X-Server-Key", serverKey);

            try
            {
                Console.WriteLine($"[IronmanRanking] Enviando para {url} com conteúdo: {content}");
                var result = await httpClient.PostAsync(url, content);
                if (!result.IsSuccessStatusCode)
                    Console.WriteLine($"[IronmanRanking] Falha ao atualizar: {result.StatusCode}");
                Console.WriteLine($"[IronmanRanking] Resposta: {await result.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IronmanRanking] Erro: {ex.Message}");
            }
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(2); // version
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

            writer.Write(IronmanPvPKillStreak);
            writer.Write(IronmanPvMKillStreak);
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
                case 2:
                    IsActive = reader.ReadBool();
                    IronmanScore = reader.ReadInt();
                    try
                    {
                        IronmanStartTime = reader.ReadDateTime();
                        if (IronmanStartTime < DateTime.MinValue || IronmanStartTime > DateTime.MaxValue)
                        {
                            IronmanStartTime = DateTime.UtcNow;
                        }
                    }
                    catch
                    {
                        IronmanStartTime = DateTime.UtcNow;
                    }

                    try
                    {
                        IronmanSurvivalTime = reader.ReadTimeSpan();
                    }
                    catch
                    {
                        IronmanSurvivalTime = TimeSpan.Zero;
                    }

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

                    IronmanPvPKillStreak = reader.ReadInt();
                    IronmanPvMKillStreak = reader.ReadInt();
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
                    break;
            }
        }
    }
}
