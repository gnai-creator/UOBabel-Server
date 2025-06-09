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

        // Construtor base
        private void InitCreatureManager()
        {
            CreatureManager = new CreatureManager(this);
            CreatureManager.EnsureAllFeatures();
        }

        public CustomCreature(AIType ai, FightMode mode, int perceptionRange, int fightRange)
            : base(ai, mode, perceptionRange, fightRange)
        {
            InitCreatureManager();
        }

        public CustomCreature(AIType ai) : base(ai)
        {
            InitCreatureManager();
        }

        public CustomCreature(AIType ai, FightMode mode) : base(ai, mode)
        {
            InitCreatureManager();
        }

        public CustomCreature(AIType ai, FightMode mode, int perceptionRange) : base(ai, mode, perceptionRange)
        {
            InitCreatureManager();
        }

        public CustomCreature(Serial serial) : base(serial)
        {
            InitCreatureManager();
        }

        public void Enrage()
        {
            if (CreatureManager?.Features.TryGetValue("rage", out var rageFeature) == true)
            {
                if (rageFeature is RageFeature rf)
                    rf.TriggerEnrage();
            }
        }


        public override void OnThink()
        {
            CreatureManager?.OnThink();
            base.OnThink();
        }

        public override void OnDeath(Container c)
        {
            CreatureManager?.OnDeath();
            var lastKiller = this.LastKiller;
            if (lastKiller is CustomPlayer player &&
                player.Manager?.Features.TryGetValue("ironman", out var ironmanFeature) == true)
            {
                (ironmanFeature as dynamic)?.OnKill(this, player);
            }
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
