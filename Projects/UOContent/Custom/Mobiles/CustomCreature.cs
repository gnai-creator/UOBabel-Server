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

        protected double m_KillersDropMultiplier = 1.0;

        [CommandProperty(AccessLevel.GameMaster)]
        public int EnrageDamageThreshold { get; set; } = 21;

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

        public override void OnSpeech(SpeechEventArgs e)
        {
            CreatureManager?.OnSpeech(e);
            base.OnSpeech(e);
        }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            if (amount > EnrageDamageThreshold)
            {
                if (CreatureManager?.Features.TryGetValue("rage", out var feature) == true && feature is RageFeature rf)
                {
                    rf.TriggerEnrage();
                }
            }

            base.OnDamage(amount, from, willKill);
        }

        protected double GetKillerDropMultiplier()
        {
            var rights = GetLootingRights(DamageEntries, HitsMax);
            DamageStore highest = null;

            for (var i = 0; i < rights.Count; ++i)
            {
                var ds = rights[i];

                if (ds.m_HasRight && (highest == null || ds.m_Damage > highest.m_Damage))
                {
                    highest = ds;
                }
            }

            if (highest?.m_Mobile is CustomPlayer cp &&
                cp.Manager.Features.TryGetValue("dropboost", out var f) &&
                f is DropBoostFeature boost && boost.IsActive)
            {
                return boost.Multiplier;
            }

            return 1.0;
        }

        public override void GenerateLoot(bool spawning)
        {
            m_Spawning = spawning;

            if (!spawning)
            {
                m_KillersLuck = LootPack.GetLuckChanceForKiller(this);
                m_KillersDropMultiplier = GetKillerDropMultiplier();
            }

            base.GenerateLoot();

            if (IsParagon)
            {
                if (Fame < 1250)
                {
                    AddLoot(LootPack.Meager);
                }
                else if (Fame < 2500)
                {
                    AddLoot(LootPack.Average);
                }
                else if (Fame < 5000)
                {
                    AddLoot(LootPack.Rich);
                }
                else if (Fame < 10000)
                {
                    AddLoot(LootPack.FilthyRich);
                }
                else
                {
                    AddLoot(LootPack.UltraRich);
                }
            }

            m_Spawning = false;
            m_KillersLuck = 0;
            m_KillersDropMultiplier = 1.0;
        }

        public override void AddLoot(LootPack pack)
        {
            if (Summoned)
            {
                return;
            }

            var backpack = Backpack ?? new Backpack { Movable = false };
            AddItem(backpack);

            pack.Generate(this, backpack, m_Spawning, m_KillersLuck, m_KillersDropMultiplier);
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
