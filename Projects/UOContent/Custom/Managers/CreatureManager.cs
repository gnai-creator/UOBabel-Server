using System;
using System.Collections.Generic;
using Server;
using Server.Custom.Features;
using Server.Mobiles;
using Server.Custom.Companions;

namespace Server.Custom.Mobiles
{
    public class CreatureManager
    {
        public BaseCreature Owner { get; private set; }
        public Dictionary<string, ICreatureFeature> Features { get; set; } = new();

        // Factory centralizada: mapeie todas as features possíveis
        public static readonly Dictionary<string, Func<ICreatureFeature>> FeatureFactories =
            new Dictionary<string, Func<ICreatureFeature>>
            {
                { "ai", () => new NpcIntelligenceFeature() },
                { "rage", () => new RageFeature() },
                { "memory", () => new MemoryFeature() },
                { "companion", () => new CompanionFeature() },
                // ...adicione mais aqui!
            };

        public CreatureManager(BaseCreature owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// Garante que todas as features da factory estejam presentes (útil para inicialização).
        /// </summary>
        public void EnsureAllFeatures()
        {
            foreach (var pair in FeatureFactories)
            {
                if (!Features.ContainsKey(pair.Key))
                {
                    var feature = pair.Value();
                    feature.Owner = Owner;
                    feature.Initialize();
                    Features[pair.Key] = feature;
                }
            }
        }

        public void RegisterFeature(string name, ICreatureFeature feature)
        {
            feature.Owner = Owner;
            feature.Initialize();
            Features[name] = feature;
        }

        // Hooks para eventos (igual antes)
        public void OnSpeech(SpeechEventArgs e)
        {
            foreach (var feature in Features.Values)
                feature.OnSpeech(e);
        }
        public void OnThink()
        {
            foreach (var feature in Features.Values)
                feature.OnThink();
        }
        public void OnDeath()
        {
            foreach (var feature in Features.Values)
                feature.OnDeath();
        }
        public void OnCombat(Mobile target)
        {
            foreach (var feature in Features.Values)
                feature.OnCombat(target);
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // version
            writer.Write(Features.Count);
            foreach (var pair in Features)
            {
                writer.Write(pair.Key);
                pair.Value.Serialize(writer);
            }
        }

        public void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            int count = reader.ReadInt();
            Features = new();

            for (int i = 0; i < count; i++)
            {
                string key = reader.ReadString();
                ICreatureFeature feature = FeatureFactories.ContainsKey(key) ? FeatureFactories[key]() : null;

                if (feature != null)
                {
                    feature.Owner = Owner;
                    feature.Initialize(); // Inicializa antes de deserializar!
                    feature.Deserialize(reader);
                    Features[key] = feature;
                }
            }

            // Garantir que todas as novas features sejam registradas
            // mesmo que não existam no save anterior
            EnsureAllFeatures();
        }
    }
}
