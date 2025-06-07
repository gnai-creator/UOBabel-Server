using System;
using Server;
using Server.Custom.Interfaces;

namespace Server.Custom.Features
{
    public class ExampleFeature : IPlayerFeature
    {
        public void OnLogin()
        {
            Console.WriteLine("ExampleFeature OnLogin");
        }

        public void OnDeath()
        {
            Console.WriteLine("ExampleFeature OnDeath");
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