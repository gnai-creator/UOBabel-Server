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
    public class CustomCreature : BaseCreature, IHireable
    {

        public NpcRole CurrentRole { get; private set; } = NpcRole.Vendor;
        private NpcRole OriginalRole { get; set; } = NpcRole.Vendor;
        private bool _isHired;
        private Mobile _owner;
        private DateTime _nextPay;
        private int _holdGold;

        public bool IsHired { get => _isHired; set => _isHired = value; }
        public int Pay => PerDayCost();
        public int HoldGold { get => _holdGold; set => _holdGold = value; }
        public DateTime NextHirePay { get => _nextPay; set => _nextPay = value; }

        public virtual bool RemoveHire(Mobile m)
        {
            _owner = GetOwner();
            if (_owner != null)
            {
                m.SendLocalizedMessage(1043284, _owner.Name); // I am no longer following ~1_NAME~.
                SetControlMaster(null);
                IHireable hireable = this;
                hireable.IsHired = false;
                return false;
            }
            return true;
        }
        public virtual bool AddHire(Mobile m)
        {
            _owner = GetOwner();

            if (_owner != null)
            {
                m.SendLocalizedMessage(1043283, _owner.Name); // I am following ~1_NAME~.
                return false;
            }

            if (SetControlMaster(m))
            {
                IHireable hireable = this;
                hireable.IsHired = true;

                return true;
            }

            return false;
        }        public void SayHireCost() => Say($"I charge {HoldGold} gold coins per day.");
        public Mobile GetOwner() => _owner;

        private int PerDayCost() =>
            (int)(Skills[SkillName.Anatomy].Value +
                Skills[SkillName.Tactics].Value +
                Skills[SkillName.Macing].Value +
                Skills[SkillName.Swords].Value +
                Skills[SkillName.Fencing].Value +
                Skills[SkillName.Archery].Value +
                Skills[SkillName.MagicResist].Value +
                Skills[SkillName.Healing].Value +
                Skills[SkillName.Magery].Value +
                Skills[SkillName.Parry].Value) / 35 + 1;

         private bool OnHireDragDrop(Mobile from, Item item)
        {
            if (Pay == 0)
            {
                SayTo(from, 500200); // I have no need for that.
                return false;
            }

            // Is the creature already hired
            if (Controlled)
            {
                SayTo(from, 1042495); // I have already been hired.
                return false;
            }

            // Is the item the payment in gold
            if (item is not Gold)
            {
                SayTo(from, 1043268); // Tis crass of me, but I want gold
                return false;
            }

            // Is the payment in gold sufficient
            if (item.Amount < Pay)
            {
                SayHireCost();
                return false;
            }

            if (from.Followers + ControlSlots > from.FollowersMax)
            {
                SayTo(from, 500896); // I see you already have an escort.
                return false;
            }

            // Try to add the hireling as a follower
            if (!AddHire(from))
            {
                return false;
            }

            if (CurrentRole != NpcRole.Hireable)
            {
                ChangeRole(NpcRole.Hireable);
            }

            // I thank thee for paying me.  I will work for thee for ~1_NUMBER~ days.
            SayTo(from, 1043258, $"{item.Amount / Pay}"); // Stupid that they don't have "day" cliloc

            IHireable hireable = this;
            hireable.HoldGold += item.Amount;
            hireable.NextHirePay = Core.Now + PayTimer.GetInterval();

            PayTimer.RegisterTimer(this);
            return true;
        }

        public CreatureManager CreatureManager { get; private set; }
        
        public CustomCreature(AIType ai, FightMode mode, int rangePerception, int rangeFight)
            : base(ai, mode, rangePerception, rangeFight)
        {
            OriginalRole = CurrentRole;

            CreatureManager = new CreatureManager(this);
            CreatureManager.RegisterFeature("npc_intel", new NpcIntelligenceFeature(this));

        }

        public CustomCreature(Serial serial) : base(serial)
        {
            OriginalRole = CurrentRole;
            CreatureManager = new CreatureManager(this);
            CreatureManager.RegisterFeature("npc_intel", new NpcIntelligenceFeature(this));

        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            CreatureManager?.OnSpeech(e);
            base.OnSpeech(e);
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

        protected bool CheckVendorAccess(Mobile from)
        {
            return !Deleted && Alive && from.CheckAlive() && !IsDeadBondedPet;
        }

        public class HireEntry : ContextMenuEntry
        {
            public HireEntry() : base(6120, 3)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (target is IHireable hire)
                {
                    hire.SayHireCost();
                }
            }
        }

        public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
            if (Deleted)
            {
                return;
            }

            if (!Controlled)
            {
                if (CanPaperdollBeOpenedBy(from))
                {
                    list.Add(new PaperdollEntry());
                }

                list.Add(new HireEntry());
                list.Add(new VendorBuyEntry(CheckVendorAccess(from)));
                list.Add(new VendorSellEntry(CheckVendorAccess(from)));
            }
            else
            {
                base.GetContextMenuEntries(from, ref list);
                list.RemoveAt(list.Count - 1);
                list.RemoveAt(list.Count - 1);
            }
        }

        public void ChangeRole(NpcRole role)
        {
            CurrentRole = role;

            switch (role)
            {
                case NpcRole.Vendor:
                    AI = AIType.AI_Vendor;
                    FightMode = FightMode.None;
                    InitVendor();
                    break;

                case NpcRole.Hireable:
                    AI = AIType.AI_Melee;
                    FightMode = FightMode.Aggressor;
                    ControlSlots = 2;
                    InitHireable();
                    break;

                default:
                    AI = AIType.AI_Vendor;
                    FightMode = FightMode.None;
                    break;
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.Write(0);
            base.Serialize(writer);
            CreatureManager.Serialize(writer);
            writer.Write(_isHired);
            writer.Write(_nextPay);
            writer.Write(_holdGold);
        }

        public override void OnDelete()
        {
            PayTimer.RemoveTimer(this);
            base.OnDelete();
        }

        public Container BuyPack
        {
            get
            {
                if (FindItemOnLayer(Layer.ShopBuy) is not Container pack)
                {
                    pack = new Backpack { Layer = Layer.ShopBuy, Visible = false };
                    AddItem(pack);
                }
                return pack;
            }
        }

        public Container ResellPack
        {
            get
            {
                if (FindItemOnLayer(Layer.ShopResale) is not Container pack)
                {
                    pack = new Backpack { Layer = Layer.ShopResale, Visible = false };
                    AddItem(pack);
                }
                return pack;
            }
        }
        public override void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            base.Deserialize(reader);
            CreatureManager = new CreatureManager(this);
            CreatureManager.Deserialize(reader);
            _isHired = reader.ReadBool();
            _nextPay = reader.ReadDateTime();
            _holdGold = reader.ReadInt();
        }

         public DateTime LastRestock { get; set; }
        private void InitVendor()
        {
            Container pack = BuyPack;
            Container resale = ResellPack;
            LastRestock = DateTime.UtcNow;
        }

        private void InitHireable()
        {
            SetStr(91, 91);
            SetDex(91, 91);
            SetInt(50, 50);
            SetDamage(7, 14);

            SetSkill(SkillName.Tactics, 36, 67);
            SetSkill(SkillName.Magery, 22, 22);
            SetSkill(SkillName.Swords, 64, 100);
            SetSkill(SkillName.Parry, 60, 82);
            SetSkill(SkillName.Macing, 36, 67);
            SetSkill(SkillName.Focus, 36, 67);
            SetSkill(SkillName.Wrestling, 25, 47);

            PackGold(25, 100);
        }

        private async Task<bool> CheckAndSetPatreonAsync(CustomPlayer pm)
        {
            if ((DateTime.UtcNow - pm.LastPatreonCheck).TotalHours >= 24)
            {
                bool isSubscriber = await PatreonChecker.HasAccessAsync(pm.Account?.Username ?? pm.Name);
                pm.HasPremium = isSubscriber;
                pm.LastPatreonCheck = DateTime.UtcNow;
            }
            return pm.HasPremium;
        }

        public class PayTimer : Timer
        {
            private readonly HashSet<IHireable> _hires = new();
            public static PayTimer Instance { get; set; }

            public PayTimer() : base(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1))
            {
            }

            public static TimeSpan GetInterval() => TimeSpan.FromMinutes(30.0);

            protected override void OnTick()
            {
                using var queue = PooledRefQueue<Mobile>.Create();
                foreach (var hire in _hires)
                {
                    if (hire.NextHirePay > Core.Now)
                    {
                        continue;
                    }

                    hire.NextHirePay = Core.Now + GetInterval();

                    int pay = hire.Pay;

                    if (hire.HoldGold <= pay)
                    {
                        queue.Enqueue(hire as Mobile);
                    }
                    else
                    {
                        hire.HoldGold -= pay;
                    }
                }

                while (queue.Count > 0)
                {
                    var hire = queue.Dequeue();

                    if (hire is BaseAICreature aiCreature)
                    {
                        aiCreature.GetOwner(); // Sets owner to null
                        aiCreature.Say(503235); // I regret nothing!
                        aiCreature.Delete();
                    }
                }
            }

            public static void RegisterTimer(IHireable hire)
            {
                Instance ??= new PayTimer();

                if (!Instance.Running)
                {
                    Instance.Start();
                }

                Instance._hires.Add(hire);
            }

            public static void RemoveTimer(IHireable hire)
            {
                if (Instance?._hires.Remove(hire) == true && Instance._hires.Count == 0)
                {
                    Instance.Stop();
                }
            }
        }
    }
}