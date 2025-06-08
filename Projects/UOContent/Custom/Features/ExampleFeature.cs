using System;
using Server;
using Server.Custom.Interfaces;

namespace Server.Custom.Features
{
    public class ExampleFeature : IPlayerFeature
    {
        private Mobile Owner { get; set; }

        public void Initialize(Mobile owner)
        {
            Owner = owner;
        }

        public void OnLogin()
        {
            Console.WriteLine("ExampleFeature OnLogin");
        }

        public void OnDeath()
        {
            Console.WriteLine("ExampleFeature OnDeath");
        }

        public void OnKill(Mobile victim, Mobile killer)
        {
            Console.WriteLine($"ExampleFeature OnKill - Victim: {victim.Name}, Killer: {killer.Name}");
        }

        public void OnThink()
        {
            // Periodic updates can be handled here
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // version
        }

        public void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
        }
    }
}