using Server.Mobiles;
using UOContent.Custom.Enums;
using Server;
using Server.Custom.Mobiles;

namespace UOContent.Custom.Utilities
{
    public static class PvPCombatRules
    {
        public static bool CanAttack(Mobile attacker, Mobile defender)
        {
            if (attacker is CustomPlayer atk && defender is CustomPlayer def)
            {
                if (atk.CombatMode == CombatMode.PvM || def.CombatMode == CombatMode.PvM)
                    return false;
            }

            return true;
        }
    }

}