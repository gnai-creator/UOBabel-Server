using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Custom.Features;
using Server.Collections;
using Server.ContextMenus;
using System.Threading.Tasks;
using UOContent.Custom.Patreon;
using System.Collections.Generic;

namespace Server.Custom.Mobiles
{

    public enum NpcRole
    {

        Vendor,
        Hireable
    }
    public class CustomCreature : BaseCreature
    {
        public CreatureManager CreatureManager { get; private set; }

        public CustomCreature(AIType ai, FightMode mode, int perceptionRange, int fightRange)
        : base(ai, mode, perceptionRange, fightRange)
        {
            CreatureManager = new CreatureManager(this);
            CreatureManager.RegisterFeature("ai", new NpcIntelligenceFeature(this));

        }
        public CustomCreature(AIType ai) : base(ai)
        {
            CreatureManager = new CreatureManager(this);
            CreatureManager.RegisterFeature("ai", new NpcIntelligenceFeature(this));
        }
        public CustomCreature(AIType ai, FightMode mode) : base(ai, mode)
        {
            CreatureManager = new CreatureManager(this);
            CreatureManager.RegisterFeature("ai", new NpcIntelligenceFeature(this));
        }
        public CustomCreature(Serial serial) : base(serial)
        {
            CreatureManager = new CreatureManager(this);
            CreatureManager.RegisterFeature("ai", new NpcIntelligenceFeature(this));
        }

        public override void OnThink()
        {
            CreatureManager?.OnThink();
            base.OnThink();
        }

        public override void OnDeath(Container c)
        {
            CreatureManager?.OnDeath();
            base.OnDeath(c);
        }

        public override void OnCombatantChange()
        {
            if (Combatant != null)
                CreatureManager?.OnCombat(Combatant);

            base.OnCombatantChange();
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.Write(0);
            base.Serialize(writer);
            CreatureManager.Serialize(writer);
        }

        public override void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            base.Deserialize(reader);
            CreatureManager = new CreatureManager(this);
            CreatureManager.Deserialize(reader);
        }
    }
}
