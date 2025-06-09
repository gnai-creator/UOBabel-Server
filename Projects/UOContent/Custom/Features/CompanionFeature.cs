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
        }

        public override void OnInteract(Mobile player)
        {
            // Exemplo: menu de contexto ou click direito
            Owner.PublicOverheadMessage(Server.MessageType.Regular, 1151, false, "Como posso ajudar?");
        }

        public override void OnCommand(string command, Mobile from)
        {
            CommandHistory.Add($"{DateTime.UtcNow:HH:mm} {from.Name}: {command}");
            Memory?.AddMemory($"Recebeu comando: \"{command}\" de {from.Name}.", "neutra");
        }

        public override void OnIdle() { }
        public override void OnFollow(Mobile target)
        {
            Memory?.AddMemory($"Seguindo {target.Name}.", "afeto");
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
            writer.Write(0); // version
            writer.Write(CompanionName);
            writer.Write(Personality);
            writer.Write(Happiness);
            writer.Write(Mood);
            writer.Write(CommandHistory.Count);
            foreach (var cmd in CommandHistory)
                writer.Write(cmd);
            writer.Write(LastInteraction);
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
            // OwnerPlayer pode ser restaurado via Initialize em runtime
        }
    }
}
