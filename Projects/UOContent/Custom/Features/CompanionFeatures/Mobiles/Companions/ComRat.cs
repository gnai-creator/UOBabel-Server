using ModernUO.Serialization;
using Server.Custom.Mobiles;
using Server.Mobiles;

namespace Server.Custom.Companions
{
    [SerializationGenerator(0, false)]
    public partial class ComRat : CustomCreature
    {
        [Constructible]
        public ComRat() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Name = "Ratinho";
            Body = 238;
            BaseSoundID = 0xCC;

            SetStr(25);
            SetDex(25);
            SetInt(5);

            SetHits(15);
            SetDamage(2, 4);

            Tamable = false;
            ControlSlots = 1;
        }

        public override string DefaultName => "companheiro rato";

        public ComRat(Serial serial) : base(serial)
        {
        }
    }
}
