using System;
using Server;
using Server.Custom.Interfaces;

namespace Server.Custom.Features
{
    public class RageFeature : ICreatureFeature
    {
        public bool IsEnraged { get; private set; } = false;
        public int RageLevel { get; private set; } = 0; // 0 a 100
        public DateTime LastCombatTime { get; private set; } = DateTime.UtcNow;

        private const int MaxRage = 100;
        private const int RageDecayPerSecond = 5;
        private const int RageGainPerCombat = 15;

        public void OnSpeech(SpeechEventArgs e)
        {
            // Empty implementation as this feature doesn't need speech handling
        }

        public void OnThink()
        {
            if (!IsEnraged)
                return;

            TimeSpan timeSinceLastCombat = DateTime.UtcNow - LastCombatTime;
            int decay = (int)(timeSinceLastCombat.TotalSeconds * RageDecayPerSecond);

            RageLevel = Math.Max(0, RageLevel - decay);

            if (RageLevel <= 0)
            {
                IsEnraged = false;
                Console.WriteLine("[RageFeature] Creature calmed down.");
            }
        }

        public void OnDeath()
        {
            IsEnraged = false;
            RageLevel = 0;
        }

        public void OnCombat(Mobile target)
        {
            LastCombatTime = DateTime.UtcNow;

            RageLevel = Math.Min(MaxRage, RageLevel + RageGainPerCombat);

            if (!IsEnraged && RageLevel >= 50)
            {
                IsEnraged = true;
                Console.WriteLine("[RageFeature] Creature has entered rage mode!");
            }

            if (IsEnraged)
            {
                // Aplica efeito durante o ataque — ex: buff de dano, velocidade, etc
                if (target != null && target.Alive)
                {
                    // exemplo simbólico, aplique o efeito real na IA da criatura
                    Console.WriteLine("[RageFeature] Enraged attack on " + target.Name);
                }
            }
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // version
            writer.Write(IsEnraged);
            writer.Write(RageLevel);
            writer.Write(LastCombatTime);
        }

        public void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    IsEnraged = reader.ReadBool();
                    RageLevel = reader.ReadInt();
                    LastCombatTime = reader.ReadDateTime();
                    break;
            }
        }
    }
}
