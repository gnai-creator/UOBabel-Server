using Server;
using Server.Mobiles;
using Server.Custom.Features;
using System;
using System.Collections.Generic;
using Server.Custom.Mobiles;


namespace Server.Custom.Companions
{
    public class CompanionFeature : CreatureFeatureBase
    {
        public string CompanionName { get; set; }
        public string Personality { get; set; } = "leal";
        public int Happiness { get; set; } = 100;
        public string Mood { get; set; } = "neutra";
        public List<string> CommandHistory { get; set; } = new();
        public DateTime LastInteraction { get; set; } = DateTime.UtcNow;
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public Dictionary<string, int> Skills { get; set; } = new();
        public DateTime LastLevelUp { get; set; } = DateTime.UtcNow;

        private const int BaseExpToLevel = 100;
        private DateTime _nextXpGain = DateTime.UtcNow;

        // OwnerPlayer é opcional: você pode associar em tempo de execução
        public CustomPlayer OwnerPlayer { get; set; }

        // Exemplo de acesso à memória modular
        public MemoryFeature Memory
        {
            get
            {
                var custom = Owner as CustomCreature;
                if (custom?.CreatureManager?.Features.TryGetValue("memory", out var mem) == true)
                    return mem as MemoryFeature;
                return null;
            }
        }

        public CompanionFeature() { }

        private int ExpToLevel() => Level * BaseExpToLevel;

        private void AddExperience(int amount)
        {
            if (amount <= 0)
                return;

            Experience += amount;
            if (Experience >= ExpToLevel())
            {
                Experience -= ExpToLevel();
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Level++;
            LastLevelUp = DateTime.UtcNow;
            Memory?.AddMemory($"Subiu para o nível {Level}.", "afeto");
            Owner?.PublicOverheadMessage(MessageType.Regular, 1161, false, $"{CompanionName} agora é nível {Level}!");
        }

        public override void Initialize()
        {
            CompanionName = Owner?.Name ?? "Companion";
            // Detecção automática de dono, se possível (exemplo para pets)
            if (Owner is BaseCreature bc && bc.ControlMaster is CustomPlayer player)
                OwnerPlayer = player;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            // Exemplo: responde se chamado pelo nome do companion
            if (e.Speech.Contains(CompanionName, StringComparison.OrdinalIgnoreCase))
            {
                // Registra comando na memória e histórico
                Memory?.AddMemory($"Dono falou: \"{e.Speech}\"", "afeto");
                CommandHistory.Add($"{DateTime.UtcNow:HH:mm} {e.Mobile.Name}: {e.Speech}");
                Owner.PublicOverheadMessage(Server.MessageType.Regular, 1161, false, $"Estou ouvindo, {e.Mobile.Name}!");
            }
        }

        public override void OnThink()
        {
            // Exemplo: aumenta felicidade se estiver perto do dono
            if (OwnerPlayer != null && Owner.InRange(OwnerPlayer, 2))
            {
                Happiness = Math.Min(Happiness + 1, 100);
                if (DateTime.UtcNow > _nextXpGain)
                {
                    AddExperience(1);
                    _nextXpGain = DateTime.UtcNow + TimeSpan.FromMinutes(1);
                }
            }
            else
            {
                Happiness = Math.Max(Happiness - 1, 0);
            }
        }

        public override void OnDeath()
        {
            Memory?.AddMemory("Companion morreu!", "medo");
        }

        public override void OnCombat(Mobile target)
        {
            Memory?.AddMemory($"Entrou em combate com {target.Name}.", "raiva");
            AddExperience(5);
        }

        public override void OnInteract(Mobile player)
        {
            // Exemplo: menu de contexto ou click direito
            Owner.PublicOverheadMessage(Server.MessageType.Regular, 1151, false, "Como posso ajudar?");
            Owner.PublicOverheadMessage(MessageType.Regular, 1151, false, $"Nível {Level} | Felicidade {Happiness}% | Humor {Mood}");
        }

        public override void OnCommand(string command, Mobile from)
        {
            CommandHistory.Add($"{DateTime.UtcNow:HH:mm} {from.Name}: {command}");
            Memory?.AddMemory($"Recebeu comando: \"{command}\" de {from.Name}.", "neutra");

            string lower = command.ToLower();

            if (lower.Contains("status"))
            {
                Owner.PublicOverheadMessage(MessageType.Regular, 1151, false,
                    $"Nível {Level} | XP {Experience}/{ExpToLevel()} | Felicidade {Happiness}%");
                return;
            }

            if (lower.StartsWith("treinar "))
            {
                string skill = lower.Substring(8).Trim();
                if (!Skills.ContainsKey(skill))
                    Skills[skill] = 0;
                Skills[skill]++;
                Memory?.AddMemory($"Treinou {skill}.", "afeto");
                Owner.PublicOverheadMessage(MessageType.Regular, 1151, false,
                    $"{skill} agora está em {Skills[skill]}");
            }
        }

        public override void OnIdle() { }
        public override void OnFollow(Mobile target)
        {
            Memory?.AddMemory($"Seguindo {target.Name}.", "afeto");
            AddExperience(1);
        }
        public override void OnEmotionChanged(string newEmotion)
        {
            Mood = newEmotion;
            Memory?.AddMemory($"Mudou de emoção para {newEmotion}.", newEmotion);
        }
        public override void OnDespawn() { }
        public override void OnSaved() { }
        public override void OnLoaded() { }

        public override void Serialize(IGenericWriter writer)
        {
            writer.Write(1); // version
            writer.Write(CompanionName);
            writer.Write(Personality);
            writer.Write(Happiness);
            writer.Write(Mood);
            writer.Write(CommandHistory.Count);
            foreach (var cmd in CommandHistory)
                writer.Write(cmd);
            writer.Write(LastInteraction);
            writer.Write(Level);
            writer.Write(Experience);
            writer.Write(Skills.Count);
            foreach (var kv in Skills)
            {
                writer.Write(kv.Key);
                writer.Write(kv.Value);
            }
            writer.Write(LastLevelUp);
            // OwnerPlayer pode ser restaurado via Initialize em runtime
        }

        public override void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            CompanionName = reader.ReadString();
            Personality = reader.ReadString();
            Happiness = reader.ReadInt();
            Mood = reader.ReadString();
            int cmdCount = reader.ReadInt();
            CommandHistory = new List<string>();
            for (int i = 0; i < cmdCount; i++)
                CommandHistory.Add(reader.ReadString());
            LastInteraction = reader.ReadDateTime();

            if (version >= 1)
            {
                Level = reader.ReadInt();
                Experience = reader.ReadInt();
                int skillCount = reader.ReadInt();
                Skills = new();
                for (int i = 0; i < skillCount; i++)
                {
                    string key = reader.ReadString();
                    int val = reader.ReadInt();
                    Skills[key] = val;
                }
                LastLevelUp = reader.ReadDateTime();
            }
            else
            {
                Level = 1;
                Experience = 0;
                Skills = new();
                LastLevelUp = DateTime.UtcNow;
            }
            // OwnerPlayer pode ser restaurado via Initialize em runtime
        }
    }
}
