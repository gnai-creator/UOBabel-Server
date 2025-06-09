using System;
using System.Collections.Generic;
using Server;
using Server.Custom.Features;
using Server.Custom.Interfaces;

namespace Server.Custom.Mobiles
{
    public class PlayerManager
    {
        public Mobile Owner { get; private set; }
        public Dictionary<string, IPlayerFeature> Features { get; set; } = new();

        public static readonly Dictionary<string, Func<IPlayerFeature>> FeatureFactories =
            new Dictionary<string, Func<IPlayerFeature>>
            {
                { "ironman", () => new IronmanFeature() },
                { "dropboost", () => new DropBoostFeature() },
                // { "pets", () => new PetFeature() },
                // { "dungeonrun", () => new DungeonRunFeature() },
                // ... adicione mais
            };

        private PlayerFeatureThinkTimer _thinkTimer;

        public PlayerManager(Mobile owner)
        {
            Owner = owner;
        }

        public void EnsureAllFeatures()
        {
            foreach (var pair in FeatureFactories)
            {
                if (!Features.ContainsKey(pair.Key))
                {
                    var feature = pair.Value();
                    feature.Initialize(Owner);
                    Features[pair.Key] = feature;
                }
            }
        }

        public void OnLogin()
        {
            EnsureAllFeatures();
            foreach (var feature in Features.Values)
                feature.OnLogin();

            _thinkTimer?.Stop();
            _thinkTimer = new PlayerFeatureThinkTimer((CustomPlayer)Owner, this);
            _thinkTimer.Start();
        }

        public void OnDeath()
        {
            foreach (var feature in Features.Values)
                feature.OnDeath();

            _thinkTimer?.Stop();
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

            switch (version)
            {
                case 0:
                    int count = reader.ReadInt();
                    Features = new();
                    for (int i = 0; i < count; i++)
                    {
                        string key = reader.ReadString();
                        IPlayerFeature feature = FeatureFactories.ContainsKey(key) ? FeatureFactories[key]() : null;

                        if (feature != null)
                        {
                            feature.Initialize(Owner); // Initialize before deserializing
                            feature.Deserialize(reader);
                            Features[key] = feature;
                        }
                    }
                    break;
            }
        }
    }
}
