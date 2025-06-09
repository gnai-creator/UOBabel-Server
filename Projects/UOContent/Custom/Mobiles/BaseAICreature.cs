using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Collections;
using Server.ContextMenus;
using Server.Services.AI;
using System.Threading.Tasks;
using Server.Custom.AI;
using Server.Custom.Items;
using UOContent.Custom.Patreon;
using Server.Custom.Mobiles;
using Server.Custom.Features;

namespace Server.Mobiles
{
    public enum NpcRole
    {

        Vendor,
        Hireable
    }

    [SerializationGenerator(0)]
    public partial class BaseAIVendor : BaseVendor, IHireable
    {
        public NpcRole CurrentRole { get; private set; } = NpcRole.Vendor;
        private NpcRole OriginalRole { get; set; } = NpcRole.Vendor;
        private readonly List<SBInfo> m_SBInfos = new();

        private DateTime _nextAllowedSpeech = DateTime.MinValue;



        protected string DetectarEmocao(string texto)
        {
            texto = texto.ToLower();

            if (texto.Contains("medo") || texto.Contains("fugiu") || texto.Contains("assustado"))
                return "medo";

            if (texto.Contains("morreu") || texto.Contains("ataque") || texto.Contains("sangue"))
                return "raiva";

            if (texto.Contains("amor") || texto.Contains("carinho") || texto.Contains("abraço"))
                return "afeto";

            if (texto.Contains("dinheiro") || texto.Contains("ouro") || texto.Contains("riqueza"))
                return "ganancia";

            if (texto.Contains("ajuda") || texto.Contains("salvou") || texto.Contains("herói"))
                return "esperanca";

            return "neutra";
        }

        [DeltaDateTime]
        [SerializableField(0)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private DateTime _nextHirePay;

        [SerializableField(1)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private bool _isHired;

        [SerializableField(2)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _holdGold;

        DateTime IHireable.NextHirePay
        {
            get => _nextHirePay;
            set
            {
                _nextHirePay = value;
                Delta(MobileDelta.Noto);
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        bool IHireable.IsHired
        {
            get => _isHired;
            set
            {
                _isHired = value;
                Delta(MobileDelta.Noto);
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        int IHireable.HoldGold
        {
            get => _holdGold;
            set
            {
                _holdGold = value;
                Delta(MobileDelta.Noto);
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Pay => PerDayCost();

        public int GoldOnDeath { get; set; }

        public override bool IsBondable => false;
        public override bool KeepsItemsOnDeath => true;
        public override bool CanDrop => false;

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            IHireable hireable = this;
            if (hireable.IsHired)
            {
                PayTimer.RegisterTimer(this);
            }
        }

        public override bool OnBeforeDeath()
        {
            GoldOnDeath = Backpack?.GetAmount(typeof(Gold)) ?? 0;
            return base.OnBeforeDeath();
        }

        public override void Delete()
        {
            base.Delete();

            PayTimer.RemoveTimer(this);
        }

        public override void OnDeath(Container c)
        {
            if (GoldOnDeath > 0)
            {
                c.DropItem(new Gold(GoldOnDeath));
            }

            base.OnDeath(c);
        }

        public virtual Mobile GetOwner()
        {
            if (!Controlled)
            {
                return null;
            }

            var owner = ControlMaster;
            IHireable hireable = this;
            hireable.IsHired = true;

            if (owner == null)
            {
                return null;
            }

            if (owner.Deleted)
            {
                Say(1005653); // Hmmm. I seem to have lost my master.
                SetControlMaster(null);
                return null;
            }

            return owner;
        }

        public virtual bool RemoveHire(Mobile m)
        {
            Mobile owner = GetOwner();
            if (owner != null)
            {
                m.SendLocalizedMessage(1043284, owner.Name); // I am no longer following ~1_NAME~.
                SetControlMaster(null);
                IHireable hireable = this;
                hireable.IsHired = false;
                return false;
            }
            return true;
        }

        public virtual bool AddHire(Mobile m)
        {
            Mobile owner = GetOwner();

            if (owner != null)
            {
                m.SendLocalizedMessage(1043283, owner.Name); // I am following ~1_NAME~.
                return false;
            }

            if (SetControlMaster(m))
            {
                IHireable hireable = this;
                hireable.IsHired = true;

                return true;
            }

            return false;
        }

        public int PerDayCost() =>
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

        public override bool OnDragDrop(Mobile from, Item item) => OnHireDragDrop(from, item) || base.OnDragDrop(from, item);

        public void SayHireCost()
        {
            // I am available for hire for ~1_AMOUNT~ gold coins a day. If thou dost give me gold, I will work for thee.
            Say(1043256, $"{Pay}");
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

                    if (hire is BaseAIVendor aiCreature)
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



        public BaseAIVendor(string title = null) : base(title)
        {
            AI = AIType.AI_Vendor;
            FightMode = FightMode.None;
            Title = title;
            SetSpeed(0.5, 2.0);
            InitSBInfo();
            ControlSlots = 2;
            IHireable hireable = this;
            hireable.HoldGold = 8;
        }

        public BaseAIVendor(Serial serial) : base(serial)
        {
            OriginalRole = CurrentRole;
            ControlSlots = 2;

        }

        public override bool CanTeach => CurrentRole == NpcRole.Vendor;
        public override bool BardImmune => true;
        public override bool PlayerRangeSensitive => true;
        public override bool ShowFameTitle => false;
        public override bool IsInvulnerable => false;

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

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBButcher());
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

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

        public override async void OnSpeech(SpeechEventArgs e)
        {
            try
            {
                if (DateTime.UtcNow < _nextAllowedSpeech)
                    return;

                _nextAllowedSpeech = DateTime.UtcNow.AddSeconds(3);

                var speechType = SpeechType;
                MemoryFeature memFeature = null;
                if (this is CustomCreature cc &&
                    cc.CreatureManager.Features.TryGetValue("memory", out var feat))
                {
                    memFeature = (MemoryFeature)feat;
                    memFeature.MemoryId = $"{Serial}-{e.Mobile.Serial}";
                    memFeature.Load();
                }

                if (speechType?.OnSpeech(this, e.Mobile, e.Speech) == true)
                {
                    e.Handled = true;
                    return;
                }
                else if (!e.Handled && AIObject != null && e.Mobile.InRange(this, RangePerception))
                {
                    AIObject.OnSpeech(e);
                }

                if (e.Mobile is CustomPlayer pm)
                {
                    bool hasPremium = await CheckAndSetPatreonAsync(pm);
                    if (!hasPremium)
                    {
                        this.SendMessage("Você não tem acesso completo para interagir comigo.");
                        return;
                    }

                    if (e.Speech.Contains(Name, StringComparison.OrdinalIgnoreCase))
                    {
                        // === MEMÓRIA DIRETA MULTILÍNGUE ===
                        string speechLower = e.Speech.ToLower();
                        string[] triggers = { "lembra de ", "remember ", "do you remember ", "recuerda ", "tu te souviens de ", "помнишь " };

                        foreach (string trigger in triggers)
                        {
                            if (speechLower.Contains(trigger))
                            {
                                string termo = speechLower.Substring(speechLower.IndexOf(trigger) + trigger.Length).Trim();
                                string playerLang = GetPlayerLanguage(e.Mobile);
                                try
                                {
                                    string resultado = memFeature?.SearchMemory(termo) ?? string.Empty;
                                    string emocao = DetectarEmocao(resultado);
                                    FalarComEmocao(resultado, emocao, playerLang);
                                }
                                catch (Exception ex)
                                {
                                    this.Say("Algo deu errado com minhas memórias.");
                                    Console.WriteLine($"[MemoryFeature] ERRO ao buscar por '{termo}': {ex.Message}");
                                }

                                e.Handled = true;
                                return;
                            }
                        }

                        // === INTEGRAÇÃO COM IA ===
                        if (!e.Handled && e.Mobile.InRange(this, 3))
                        {
                            var nearbyNpcs = GetMobilesInRange(5);
                            var nearbyNpcsList = new List<AIService.NearbyNPC>();
                            foreach (var npc in nearbyNpcs)
                            {
                                if (npc == this) continue;
                                nearbyNpcsList.Add(new AIService.NearbyNPC
                                {
                                    id = npc.Serial.ToString(),
                                    name = npc.Name,
                                    role = npc.Title ?? "npc",
                                    mood = "neutro"
                                });
                            }

                            var fullState = new AIService.FullNPCState
                            {
                                npc_id = Serial.ToString(),
                                name = Name,
                                role = Title ?? "npc",
                                background = GetBackground() ?? "",
                                location = Location.ToString() ?? "",
                                mood = "neutro",
                                item_amount = Backpack.GetAmount(typeof(Gold)).ToString() ?? "0",
                                item_name = "",
                                memory = memFeature?.GetRecentMemories() ?? new List<string>(),
                                nearby_npcs = nearbyNpcsList ?? new List<AIService.NearbyNPC>(),
                                player_input = e.Speech.ToLower(),
                                player_name = e.Mobile.Name
                            };

                            AIService.NpcAction actionType = AIService.NpcAction.NENHUMA;
                            try
                            {
                                var decision = await AIService.DecideNpcActionAsync(fullState);
                                string playerLang = GetPlayerLanguage(e.Mobile);

                                Console.WriteLine($"[IAService] Decisão: {decision.type}");

                                foreach (var enumValue in Enum.GetValues(typeof(AIService.NpcAction)))
                                {
                                    if (AIService.GetNpcActionString((AIService.NpcAction)enumValue) == decision.type)
                                    {
                                        actionType = (AIService.NpcAction)enumValue;
                                        break;
                                    }
                                }

                                memFeature?.AddMemory($"Interagiu com {e.Mobile.Name}, que disse: \"{e.Speech}\"");
                                if (!string.IsNullOrWhiteSpace(decision.say))
                                    memFeature?.AddMemory($"Respondeu: \"{decision.say}\"");

                                if (!string.IsNullOrWhiteSpace(decision.say))
                                {
                                    string resposta = decision.say;

                                    if (!string.Equals(playerLang, "pt", StringComparison.OrdinalIgnoreCase))
                                    {
                                        resposta = await AITranslator.TranslateAsync(resposta, "pt", playerLang);
                                    }

                                    string emocao = DetectarEmocao(resposta);
                                    FalarComEmocao(resposta, emocao, playerLang);
                                }
                                else
                                {
                                    this.FalarComEmocao("*permanece em silencio*", "neutra", playerLang);
                                }


                                Console.WriteLine($"[IAService] Ação: {actionType}");

                                if (actionType != AIService.NpcAction.NENHUMA)
                                {
                                    switch (actionType)
                                    {

                                        case AIService.NpcAction.SEGUIR:
                                            this.FalarComEmocao("*segue o jogador*", "afeto", playerLang);
                                            ChangeRole(NpcRole.Hireable);
                                            AddHire(e.Mobile);
                                            break;

                                        case AIService.NpcAction.MONTAR_CAVALO:
                                            // this.Say("*sobe rapidamente em seu cavalo*");
                                            break;
                                        case AIService.NpcAction.PEGAR_DINHEIRO:
                                            this.FalarComEmocao("*pega o dinheiro*", "afeto", playerLang);

                                            foreach (var item in e.Mobile.Backpack.Items)
                                            {
                                                if (decision.item_amount == "")
                                                {
                                                    decision.item_amount = "1";
                                                }
                                                if (item is Gold gold && decision.item_amount != "")
                                                {
                                                    if (gold.Amount >= int.Parse(decision.item_amount))
                                                    {
                                                        gold.Amount -= int.Parse(decision.item_amount);
                                                        this.AddToBackpack(new Gold(int.Parse(decision.item_amount)));
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        this.FalarComEmocao("*não tem dinheiro suficiente*", "raiva", playerLang);
                                                        break;
                                                    }
                                                }
                                            }
                                            break;

                                        case AIService.NpcAction.DAR_DINHEIRO:
                                            this.FalarComEmocao("*oferece uma pequena quantia*", "afeto", playerLang);
                                            foreach (var item in this.Backpack.Items)
                                            {
                                                if (decision.item_amount == "")
                                                {
                                                    decision.item_amount = "1";
                                                }
                                                if (item is Gold gold && decision.item_amount != "")
                                                {
                                                    if (gold.Amount >= int.Parse(decision.item_amount))
                                                    {
                                                        gold.Amount -= int.Parse(decision.item_amount);
                                                        e.Mobile.AddToBackpack(new Gold(int.Parse(decision.item_amount)));
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        this.FalarComEmocao("*não tem dinheiro suficiente*", "raiva", playerLang);
                                                        break;
                                                    }
                                                }
                                            }
                                            break;

                                        case AIService.NpcAction.PEGAR_ITEM:
                                            this.FalarComEmocao("*pega o item*", "afeto", playerLang);
                                            PegarItem(e.Mobile, decision, playerLang);
                                            break;

                                        case AIService.NpcAction.DAR_ITEM:
                                            this.FalarComEmocao("*oferece o item*", "afeto", playerLang);
                                            DarItem(e.Mobile, decision, playerLang);
                                            break;
                                        case AIService.NpcAction.ATACAR:
                                            this.FalarComEmocao("*rosna e se prepara para atacar*", "raiva", playerLang);
                                            Mobile target = null;
                                            if (!string.IsNullOrWhiteSpace(decision.target))
                                            {
                                                foreach (var mob in GetMobilesInRange(5))
                                                {
                                                    if (mob.Name.InsensitiveEquals(decision.target))
                                                    {
                                                        target = mob;
                                                        break;
                                                    }
                                                }
                                            }
                                            this.Combatant = target ?? e.Mobile;
                                            break;

                                        case AIService.NpcAction.ROTINA:
                                            this.FalarComEmocao("*retoma seu posto habitual*", "afeto", playerLang);
                                            ChangeRole(OriginalRole);
                                            RemoveHire(e.Mobile);
                                            break;

                                        default:
                                            break;
                                    }
                                }


                            }
                            catch (Exception ex)
                            {
                                string playerLang = GetPlayerLanguage(e.Mobile);
                                this.FalarComEmocao("Desculpe, não consegui processar isso.", "raiva", playerLang);
                                Console.WriteLine($"[IAService] Erro ao decidir ação: {ex}");
                            }
                        }
                    }
                    else
                    {
                        this.SendMessage("Você não tem acesso completo para interagir comigo.");
                    }
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                string playerLang = GetPlayerLanguage(e.Mobile);
                this.FalarComEmocao("Houve uma falha na minha memória", "raiva", playerLang);
                Console.WriteLine($"[OnSpeech ERROR] {ex}");
            }
        }


        protected void DarItem(Mobile from, AIService.NpcDecision decision, string playerLang)
        {
            // Console.WriteLine($"[BaseAICreature] DarItem: {decision.item_name} - {decision.item_amount}");
            if (decision.item_amount == "")
            {
                decision.item_amount = "1";
            }
            int amount = int.Parse(decision.item_amount);
            if (amount == 0)
            {
                amount = 1;
            }

            Item item = ItemParser.IdentifyItem(decision.item_name, amount);
            if (item != null)
            {
                from.AddToBackpack(item);
            }
            else
            {
                this.FalarComEmocao("*Não tem o item*", "raiva", playerLang);
            }
        }
        protected void PegarItem(Mobile from, AIService.NpcDecision decision, string playerLang)
        {
            // Console.WriteLine($"[BaseAICreature] PegarItem: {decision.item_name} - {decision.item_amount}");
            if (decision.item_amount == "")
            {
                decision.item_amount = "1";
            }
            int amount = int.Parse(decision.item_amount);
            if (amount == 0)
            {
                amount = 1;
            }
            Item item = ItemParser.IdentifyItem(decision.item_name, amount);
            foreach (var ite in from.Backpack.Items)
            {
                if (ite.GetType() == item.GetType())
                {
                    this.AddToBackpack(item);
                    ite.Delete();
                    this.FalarComEmocao("*pega o item*", "afeto", playerLang);
                    break;
                }
            }
        }

        public int GetHueFromEmotion(string emocao)
        {
            return emocao switch
            {
                "medo" => 1150,       // azul claro
                "raiva" => 33,        // vermelho
                "afeto" => 1001,      // rosa suave
                "ganancia" => 1272,   // dourado
                "esperanca" => 1401,  // verde claro
                _ => 0                // branco padrão
            };
        }


        public async Task FalarComEmocao(string texto, string emocao, string playerLang)
        {
            texto = await AITranslator.TranslateAsync(texto, "pt", playerLang);
            int hue = GetHueFromEmotion(emocao);

            switch (emocao)
            {
                case "medo":
                    texto = $"*fala tremendo* {texto}...";
                    break;
                case "raiva":
                    texto = texto.ToUpper() + "!";
                    break;
                case "afeto":
                    texto = $"{texto}";
                    break;
                case "ganancia":
                    texto = $"{texto}";
                    break;
                case "esperanca":
                    texto = $"{texto}";
                    break;
                case "neutra":
                default:
                    break;
            }

            this.PublicOverheadMessage(Server.MessageType.Regular, hue, false, texto);
        }



        public string GetPlayerLanguage(Mobile mob)
        {
            if (mob is CustomPlayer cp)
            {

                if (!string.IsNullOrWhiteSpace(cp.PreferredLanguage))
                    return cp.PreferredLanguage;

            }
            return "pt";
            // Se for um jogador (PlayerMobile), retorna o idioma salvo; senão, retorna "pt" como padrão
        }


        public virtual string GetBackground()
        {

            switch (this.AI)
            {
                case AIType.AI_Melee:
                    return "Um guerreiro experiente, mestre no combate corpo a corpo e defensor dos inocentes.";
                case AIType.AI_Archer:
                    return "Caçador habilidoso, sempre atento à movimentação das criaturas e dos viajantes.";
                case AIType.AI_Mage:
                    return "Um mago estudioso dos arcanos, profundo conhecedor de magias antigas e mistérios.";
                case AIType.AI_Predator:
                    return "Um predador indomavel, cheio de furia e desejo por mais vitimas.";
                case AIType.AI_Berserk:
                    return "Um louco Sem controle, cheio de furia e imparável.";
                // case AIType.AI_Necro:
                //     return "Necromante sombrio, dedicado às artes proibidas e aos segredos da morte.";
                // case AIType.AI_Bard:
                //     return "Bardo alegre e viajante, adora contar histórias e cantar para animar tavernas.";
                case AIType.AI_Healer:
                    return "Sacerdote compassivo, sempre disposto a ajudar e curar os necessitados.";
                case AIType.AI_Vendor:
                    return "Comerciante de Sosaria, um pouco egoista, porque precisa de dinheiro para sobreviver.";
                case AIType.AI_Animal:
                    return "Criatura selvagem dos campos de Sosaria, guiada pelos instintos naturais.";
                // case AIType.AI_Banker:
                //     return "Banqueiro respeitado, cuida dos bens dos cidadãos com extrema cautela e seriedade.";
                // case AIType.AI_Tamer:
                //     return "Domador de animais, conhece o coração de toda criatura selvagem.";
                case AIType.AI_Thief:
                    return "Ladino astuto e sorrateiro, vive nas sombras em busca de novas oportunidades.";
                // case AIType.AI_Paladin:
                //     return "Paladino devoto, luta pela justiça e proteção dos indefesos.";
                // case AIType.AI_Spawner:
                //     return "Ser misterioso, responsável por trazer criaturas ao mundo.";
                // // Adicione outros tipos conforme suas necessidades!
                default:
                    return "Um habitante típico de Sosaria, vivendo suas próprias aventuras.";
            }
        }

        public DateTime LastRestock { get; set; }

        private async Task<bool> CheckAndSetPatreonAsync(CustomPlayer pm)
        {
            if (!pm.HasPremium)
            {
                bool isSubscriber = await PatreonChecker.HasAccessAsync(pm.Account?.Username ?? pm.Name);
                pm.HasPremium = isSubscriber;
                pm.HasPremium = true;
            }
            return pm.HasPremium;
        }

    }
}