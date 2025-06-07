using System;
using Server;
using Server.Custom.Interfaces;

namespace Server.Custom.Features
{
    public class IronmanFeature : IPlayerFeature
    {
        public bool IsActive { get; set; } = false;
        public int Score { get; set; } = 0;

        public void OnLogin()
        {
            Console.WriteLine($"[IronmanFeature] Ativo: {IsActive}");
            if (IsActive)
                Console.WriteLine("Entrou no modo Ironman.");
        }

        public void OnDeath()
        {
            if (IsActive)
            {
                IsActive = false;
                Console.WriteLine("Ironman morreu.");
            }
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // version
            writer.Write(IsActive);
            writer.Write(Score);
        }

        public void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    {   
                        IsActive = reader.ReadBool();
                        Score = reader.ReadInt();
                        break;
                    }
            }
        }
    }

}