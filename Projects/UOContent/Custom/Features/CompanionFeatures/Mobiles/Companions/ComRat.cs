using ModernUO.Serialization;
using Server.Custom.Mobiles;
using Server.Mobiles;
using Server.ContextMenus;
using Server.Custom.Features.CompanionFeatures;
using Server.Collections;

namespace Server.Custom.Companions
{
    [SerializationGenerator(0, false)]
    public class ComRat : CustomCreature
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

        public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, ref list);
            list.Add(new CompanionStatusEntry(this));
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            _ = reader.ReadInt();
        }
    }
}
