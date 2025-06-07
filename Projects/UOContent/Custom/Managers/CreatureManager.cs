using System.Collections.Generic;
using Server;
using Server.Custom.Features;
using Server.Custom.Interfaces;
using Server.Mobiles;

namespace Server.Custom.Mobiles
{
    public class CreatureManager
    {
        public BaseCreature Owner { get; private set; }

        public Dictionary<string, ICreatureFeature> Features { get; set; } = new();

        public CreatureManager(BaseCreature owner)
        {
            Owner = owner;

            Features["rage"] = new RageFeature();
            // Features["memory"] = new MemoryFeature();
        }

        public void RegisterFeature(string name, ICreatureFeature feature)
        {
            Features[name] = feature;
        }


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
            writer.Write(0);
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
                ICreatureFeature feature = key switch
                {
                    "rage" => new RageFeature(),
                    // "memory" => new MemoryFeature(),
                    _ => null
                };

                feature?.Deserialize(reader);
                if (feature != null)
                    Features[key] = feature;
            }
        }
    }

}