using Server;
using Server.Mobiles;
using Server.Custom.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Server.Custom.Features
{
    public class MemoryFeature : CreatureFeatureBase
    {
        public string MemoryId { get; set; }
        public List<MemoryEntry> Entries { get; set; } = new();
        public int MaxMemoryEntries { get; set; } = 100;

        private static readonly string SavePath = "Data/CreatureMemory/";
        private static readonly object _fileLock = new();

        public MemoryFeature() { }

        public MemoryFeature(BaseCreature owner)
        {
            Owner = owner;
            MemoryId = owner.Serial.ToString();
        }

        public override void Initialize()
        {
            Load();
        }

        public void AddMemory(string content, string emotion = "neutra")
        {
            Entries.Add(new MemoryEntry
            {
                Timestamp = DateTime.UtcNow,
                Content = content,
                Emocao = emotion
            });

            // Limita tamanho do histórico
            if (Entries.Count > MaxMemoryEntries)
                Entries = Entries.Skip(Entries.Count - MaxMemoryEntries).ToList();

            Save();
        }

        public List<string> GetRecentMemories(int count = 20)
        {
            return Entries
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .Select(m => $"[{m.Timestamp:HH:mm:ss}] {m.Content}")
                .ToList();
        }

        public string SearchMemory(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return "Você precisa dizer o que procura.";

            foreach (var entry in Entries.OrderByDescending(m => m.Timestamp))
            {
                if (!string.IsNullOrWhiteSpace(entry?.Content) &&
                    entry.Content.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return $"[{entry.Timestamp:HH:mm:ss}] {entry.Content}";
                }
            }
            return $"Nada encontrado sobre \"{query}\".";
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            AddMemory($"{e.Mobile.Name}: \"{e.Speech}\"");
        }

        public override void OnThink() { }
        public override void OnDeath()
        {
            AddMemory("Morreu em combate!", "medo");
        }
        public override void OnCombat(Mobile target)
        {
            AddMemory($"Entrou em combate com {target.Name}.", "raiva");
        }
        public override void OnInteract(Mobile player) { }
        public override void OnCommand(string command, Mobile from)
        {
            AddMemory($"Recebeu comando: \"{command}\" de {from.Name}.", "neutra");
        }
        public override void OnIdle() { }
        public override void OnFollow(Mobile target)
        {
            AddMemory($"Seguindo {target.Name}.", "afeto");
        }
        public override void OnEmotionChanged(string newEmotion)
        {
            AddMemory($"Mudou de emoção para {newEmotion}.", newEmotion);
        }
        public override void OnDespawn() { }
        public override void OnSaved() => Save();
        public override void OnLoaded() => Load();

        // Serialização customizada da feature:
        public override void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // version

            writer.Write(MemoryId);
            writer.Write(Entries.Count);
            foreach (var mem in Entries)
            {
                writer.Write(mem.Timestamp);
                writer.Write(mem.Content ?? "");
                writer.Write(mem.Emocao ?? "neutra");
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    MemoryId = reader.ReadString();
                    int count = reader.ReadInt();
                    Entries = new List<MemoryEntry>();
                    for (int i = 0; i < count; i++)
                    {
                        var ts = reader.ReadDateTime();
                        var content = reader.ReadString();
                        var emocao = reader.ReadString();
                        Entries.Add(new MemoryEntry
                        {
                            Timestamp = ts,
                            Content = content,
                            Emocao = emocao
                        });
                    }
                    break;
                default:
                    throw new Exception("Versão desconhecida do MemoryFeature");
            }
        }

        // Persiste no disco (opcional, para memórias longas e cross-session)
        public void Save()
        {
            lock (_fileLock)
            {
                Directory.CreateDirectory(SavePath);
                var path = Path.Combine(SavePath, $"{MemoryId}.json");
                var json = JsonSerializer.Serialize(this.Entries, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
        }

        public void Load()
        {
            lock (_fileLock)
            {
                Directory.CreateDirectory(SavePath);
                var path = Path.Combine(SavePath, $"{MemoryId}.json");
                if (File.Exists(path))
                {
                    try
                    {
                        var json = File.ReadAllText(path);
                        var data = JsonSerializer.Deserialize<List<MemoryEntry>>(json);
                        if (data != null)
                            Entries = data;
                    }
                    catch
                    {
                        Entries = new List<MemoryEntry>();
                    }
                }
            }
        }

    }
}
