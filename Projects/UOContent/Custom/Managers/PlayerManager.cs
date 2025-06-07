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

        public PlayerManager(Mobile owner)
        {
            Owner = owner;

            // Registra todos os módulos do jogador
            Features["ironman"] = new IronmanFeature();
            // Features["pvp"] = new PvpStatsFeature();
            // Features["rep"] = new ReputationFeature();
            Console.WriteLine($"[PlayerManager] Criado: {Owner.Name}");
        }

        public void OnLogin()
        {
            foreach (var feature in Features.Values)
                feature.OnLogin();
        }

        public void OnDeath()
        {
            foreach (var feature in Features.Values)
                feature.OnDeath();
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // version
            writer.Write(Features.Count);
            foreach (var pair in Features)
            {
                writer.Write(pair.Key); // Nome
                pair.Value.Serialize(writer); // Dados
            }
        }

        public void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    {
                        int count = reader.ReadInt();
                        Features = new();
                        for (int i = 0; i < count; i++)
                        {
                            string key = reader.ReadString();
                            IPlayerFeature feature = key switch
                            {
                                "ironman" => new IronmanFeature(),
                                // "pvp" => new PvpStatsFeature(),
                                _ => null
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
}