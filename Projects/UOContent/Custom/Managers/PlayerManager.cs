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

        private PlayerFeatureThinkTimer _thinkTimer;

        public PlayerManager(Mobile owner)
        {
            Owner = owner;

            var ironmanFeature = new IronmanFeature();
            ironmanFeature.Initialize(owner); // você já chamou aqui
            Features["ironman"] = ironmanFeature;

            Console.WriteLine($"[PlayerManager] Criado: {Owner.Name}");
        }

        public void OnLogin()
        {
            foreach (var feature in Features.Values)
                feature.OnLogin();

            // Inicia o timer de pensamento
            _thinkTimer?.Stop();
            _thinkTimer = new PlayerFeatureThinkTimer((CustomPlayer)Owner, this);
            _thinkTimer.Start();
        }

        public void OnDeath()
        {
            foreach (var feature in Features.Values)
                feature.OnDeath();

            // Opcional: para o timer ao morrer
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
                        IPlayerFeature feature = key switch
                        {
                            "ironman" => new IronmanFeature(),
                            _ => new ExampleFeature()
                        };
                        feature?.Deserialize(reader);
                        if (feature != null)
                            Features[key] = feature;
                    }
                    break;
            }
        }
    }

}