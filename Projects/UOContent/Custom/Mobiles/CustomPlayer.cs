using Server;
using Server.Items;
using Server.Mobiles;
using System;
using System.Threading.Tasks;
using UOContent.Custom.Patreon;
using UOContent.Custom.Enums;
using Server.Collections;
using Server.ContextMenus;
using UOContent.Custom.ContextMenus;
using ModernUO.CodeGeneratedEvents;
using Server.Custom.Gumps;
using Server.Gumps;
using Server.Custom.Features;

namespace Server.Custom.Mobiles
{
    public class CustomPlayer : PlayerMobile
    {
        [OnEvent(nameof(PlayerLoginEvent))]
        public static void OnLogin(PlayerMobile from)
        {
            if (from is CustomPlayer cp)
                cp.OnPlayerLogin();
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string PreferredLanguage { get; set; } = "";

        public string PatreonTier { get; set; } = "";
        public string PatreonStatus { get; set; } = "";
        public bool HasPremium { get; set; } = false;
        public DateTime LastPatreonCheck { get; set; } = DateTime.MinValue;

        public CombatMode CombatMode { get; set; } = CombatMode.PvM;

        public DateTime NextCombatModeChange { get; set; } = DateTime.MinValue;

        public PlayerManager Manager { get; private set; }

        public CustomPlayer() : base()
        {
            Manager = new PlayerManager(this);
            Console.WriteLine($"[CustomPlayer] Criado: {this.Name}");
        }

        public CustomPlayer(Serial serial) : base(serial)
        {
            Manager = new PlayerManager(this);
        }

        public override void OnDeath(Container c)
        {
            Manager?.OnDeath();

            var lastKiller = this.LastKiller;
            if (lastKiller is CustomPlayer lastKillerPlayer && lastKillerPlayer != this)
            {
                if (lastKillerPlayer.Manager?.Features.TryGetValue("ironman", out var ironmanKiller) == true)
                    (ironmanKiller as IronmanFeature)?.OnKill(this, this);
                if (this.Manager?.Features.TryGetValue("ironman", out var ironmanVictim) == true)
                    (ironmanVictim as IronmanFeature)?.OnKill(this, lastKillerPlayer);
            }
            base.OnDeath(c);
        }

        public override int Luck
        {
            get
            {
                int baseLuck = AosAttributes.GetValue(this, AosAttribute.Luck);
                if (CombatMode == CombatMode.PvM)
                {
                    return (int)(baseLuck * 0.5); // -50%
                }
                return baseLuck;
            }
        }

        public void OnPlayerLogin()
        {
            if (string.IsNullOrEmpty(this.PreferredLanguage))
            {
                this.SendGump(new LanguageSelectGump());
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    string loginUO = this.Account?.Username;
                    if (!string.IsNullOrEmpty(loginUO))
                    {
                        var patreonInfo = await PatreonChecker.GetTierInfoAsync(loginUO);
                        if (patreonInfo != null)
                        {
                            this.PatreonTier = patreonInfo.tier;
                            this.HasPremium = !string.IsNullOrWhiteSpace(this.PatreonTier);
                            this.PatreonStatus = patreonInfo.patronStatus;
                            this.SendMessage($"[Patreon] Acesso confirmado: Status {this.PatreonStatus}");
                            this.SendMessage("Bem-vindo, patrono! Você tem acesso completo.");
                        }
                        else
                        {
                            this.PatreonTier = "";
                            this.HasPremium = false;
                            this.PatreonStatus = "";
                            this.SendMessage("[Patreon] Acesso não encontrado.");
                            this.SendMessage("Bem-vindo, visitante! Você não tem acesso completo.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PatreonChecker] Erro: {ex.Message}");
                }
                this.AtualizarPatreonAsync();
            });
            Manager?.OnLogin();
        }

        public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, ref list);
            if (from == this && from is CustomPlayer player)
            {
                list.Add(new ToggleCombatModeEntry(player));
            }
        }

        public override void Damage(int amount, Mobile from, bool informMount = true, bool ignoreEvilOmen = false)
        {
            if (from is PlayerMobile attacker && this is PlayerMobile defender)
            {
                if (attacker != defender)
                {
                    if (attacker is CustomPlayer atk && defender is CustomPlayer def)
                    {
                        if (atk.CombatMode == CombatMode.PvP && def.CombatMode == CombatMode.PvP)
                        {
                            base.Damage(amount, from);
                            return;
                        }
                        atk.SendMessage(33, "Você não pode atacar um jogador em modo PvM.");
                        def.SendMessage(33, $"{atk.Name} tentou atacar você, mas você está protegido por estar em PvM.");
                        return;
                    }
                }
            }
            base.Damage(amount, from, informMount, ignoreEvilOmen);
        }

        public async Task AtualizarPatreonAsync(bool force = false)
        {
            if (!force && (DateTime.UtcNow - LastPatreonCheck).TotalHours < 24)
                return;
            try
            {
                string loginUO = Account?.Username;
                if (string.IsNullOrEmpty(loginUO))
                    return;
                var patreonInfo = await PatreonChecker.GetTierInfoAsync(loginUO);
                if (patreonInfo != null)
                {
                    PatreonTier = patreonInfo.tier;
                    HasPremium = !string.IsNullOrWhiteSpace(PatreonTier);
                    LastPatreonCheck = DateTime.UtcNow;
                    SendMessage($"[Patreon] Atualizado: Tier {PatreonTier}");
                }
                else
                {
                    PatreonTier = "";
                    HasPremium = false;
                    LastPatreonCheck = DateTime.UtcNow;
                    SendMessage("[Patreon] Nenhuma assinatura ativa.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PatreonChecker] Erro: {ex.Message}");
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            Manager?.Serialize(writer);
            base.Serialize(writer);
            writer.Write(1);
            writer.Write(PreferredLanguage);
            writer.Write((int)CombatMode);
            writer.Write(NextCombatModeChange);
            writer.Write(PatreonTier);
            writer.Write(PatreonStatus);
            writer.Write(HasPremium);
            writer.Write(LastPatreonCheck);
        }

        public override void Deserialize(IGenericReader reader)
        {
            Manager = new PlayerManager(this);
            Manager?.Deserialize(reader);
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    PreferredLanguage = reader.ReadString();
                    goto case 0;
                case 0:
                    CombatMode = (CombatMode)reader.ReadInt();
                    NextCombatModeChange = reader.ReadDateTime();
                    PatreonTier = reader.ReadString();
                    PatreonStatus = reader.ReadString();
                    HasPremium = reader.ReadBool();
                    LastPatreonCheck = reader.ReadDateTime();
                    break;
            }
        }
    }
}
