using System;
using Server;
using Server.Custom.Interfaces;

namespace Server.Custom.Features
{
    public class DropBoostFeature : IPlayerFeature
    {
        private Mobile Owner { get; set; }

        public bool IsActive { get; set; } = false;
        public double Multiplier { get; set; } = 100.0;

        public void Initialize(Mobile owner)
        {
            Owner = owner;
        }

        public void OnLogin()
        {
        }

        public void OnDeath()
        {
        }

        public void OnKill(Mobile victim, Mobile killer)
        {
        }

        public void OnThink()
        {
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // version
            writer.Write(IsActive);
            writer.Write(Multiplier);
        }

        public void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    IsActive = reader.ReadBool();
                    Multiplier = reader.ReadDouble();
                    break;
            }
        }
    }
}
