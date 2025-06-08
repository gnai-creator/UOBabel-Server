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

namespace Server.Custom.Mobiles
{

    public class CustomPlayer : PlayerMobile
    {
        [OnEvent(nameof(PlayerLoginEvent))]
        public static void OnLogin(PlayerMobile from)
        {
            if (from is CustomPlayer cp)
            {
                cp.OnPlayerLogin(from);
            }
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
            Manager.InitializeDefaults();
            Console.WriteLine($"[CustomPlayer] Criado: {this.Name}");
        }

        public CustomPlayer(Serial serial) : base(serial)
        {
            Manager = new PlayerManager(this);
            Manager.InitializeDefaults();
        }

        public override void OnDeath(Container c)
        {
            Manager?.OnDeath();

            var lastKiller = this.LastKiller;
            if (lastKiller is CustomPlayer lastKillerPlayer && lastKillerPlayer != this)
            {
                lastKillerPlayer.Manager?.Features["ironman"]?.OnKill(this, this);
                this.Manager?.Features["ironman"]?.OnKill(this, lastKillerPlayer);
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

        public void OnPlayerLogin(PlayerMobile from)
        {

            if (from is CustomPlayer cp)
            {
                if (string.IsNullOrEmpty(cp.PreferredLanguage))
                {
                    cp.SendGump(new LanguageSelectGump());
                }

                _ = Task.Run(async () =>
               {
                   try
                   {
                       string loginUO = cp.Account?.Username;

                       if (!string.IsNullOrEmpty(loginUO))
                       {
                           var patreonInfo = await PatreonChecker.GetTierInfoAsync(loginUO);

                           if (patreonInfo != null)
                           {
                               cp.PatreonTier = patreonInfo.tier;
                               cp.HasPremium = !string.IsNullOrWhiteSpace(cp.PatreonTier);
                               cp.PatreonStatus = patreonInfo.patronStatus;
                               cp.SendMessage($"[Patreon] Acesso confirmado: Status {cp.PatreonStatus}");
                               cp.SendMessage("Bem-vindo, patrono! Você tem acesso completo.");

                           }
                           else
                           {
                               cp.PatreonTier = "";
                               cp.HasPremium = false;
                               cp.PatreonStatus = "";
                               cp.SendMessage("[Patreon] Acesso não encontrado.");
                               cp.SendMessage("Bem-vindo, visitante! Você não tem acesso completo.");
                           }
                       }
                   }
                   catch (Exception ex)
                   {
                       Console.WriteLine($"[PatreonChecker] Erro: {ex.Message}");
                   }
                   cp.AtualizarPatreonAsync();
               });
                Manager?.OnLogin();
            }
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
                if (attacker != defender) // ignora auto-dano
                {
                    if (attacker is CustomPlayer atk && defender is CustomPlayer def)
                    {
                        if (atk.CombatMode == CombatMode.PvP && def.CombatMode == CombatMode.PvP)
                        {
                            base.Damage(amount, from); // Permite PvP legítimo
                            return;
                        }

                        // Um dos dois não está em PvP
                        atk.SendMessage(33, "Você não pode atacar um jogador em modo PvM.");
                        def.SendMessage(33, $"{atk.Name} tentou atacar você, mas você está protegido por estar em PvM.");
                        return;
                    }
                }
            }

            base.Damage(amount, from, informMount, ignoreEvilOmen); // Dano normal para PvM e NPCs
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