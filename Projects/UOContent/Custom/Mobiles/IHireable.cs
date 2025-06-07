using Server;
using Server.Items;
using System;

namespace Server.Mobiles
{
    public interface IHireable
    {
        bool IsHired { get; set; }
        int Pay { get; }
        int HoldGold { get; set; }
        DateTime NextHirePay { get; set; }
        bool AddHire(Mobile m);
        void SayHireCost();
        Mobile GetOwner();
    }
} 