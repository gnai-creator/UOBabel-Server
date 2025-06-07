using System;
using Server;
using Server.Network;
using Server.Mobiles;
using Server.ContextMenus;
using UOContent.Custom.Enums;
using Server.Custom.Mobiles;

namespace UOContent.Custom.ContextMenus
{
    public class ToggleCombatModeEntry : ContextMenuEntry
    {
        private CustomPlayer m_Player;

        public ToggleCombatModeEntry(CustomPlayer player)
            : base(199, 10) // 2050 é um texto genérico, podemos customizar depois
        {
            m_Player = player;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from.Deleted || from.NetState == null)
                return;

            if (from is CustomPlayer player)
            {
                m_Player = player;

                if (DateTime.UtcNow < m_Player.NextCombatModeChange)
                {
                    TimeSpan restante = m_Player.NextCombatModeChange - DateTime.UtcNow;
                    from.SendMessage(33, $"Você só poderá mudar de modo novamente em {restante.Minutes:D2}:{restante.Seconds:D2}.");
                    return;
                }

                // Alternar modo
                m_Player.CombatMode = m_Player.CombatMode == CombatMode.PvP
                    ? CombatMode.PvM
                    : CombatMode.PvP;
                    
                if (m_Player.CombatMode == CombatMode.PvM)
                {
                    from.SendMessage(38, "Você entrou no modo PvM. Sua sorte foi reduzida em 50%.");
                }

                // Definir próximo tempo de troca
                m_Player.NextCombatModeChange = DateTime.UtcNow.AddMinutes(15);

                string modo = m_Player.CombatMode == CombatMode.PvP ? "PvP" : "PvM";
                from.SendMessage(1272, $"Modo de combate alterado para: {modo}");
            }
        }

    }

}