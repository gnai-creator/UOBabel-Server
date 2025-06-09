using Server.ContextMenus;
using Server.Custom.Companions;
using Server.Custom.Features.CompanionFeatures.Gumps;
using Server.Mobiles;

namespace Server.Custom.Features.CompanionFeatures
{
    public class CompanionStatusEntry : ContextMenuEntry
    {
        private readonly CustomCreature _companion;

        public CompanionStatusEntry(CustomCreature companion) : base(3006125)
        {
            _companion = companion;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from is PlayerMobile player)
            {
                if (_companion.CreatureManager?.Features.TryGetValue("companion", out var feat) == true
                    && feat is CompanionFeature feature)
                {
                    player.CloseGump<CompanionStatusGump>();
                    player.SendGump(new CompanionStatusGump(player, _companion, feature));
                }
            }
        }
    }
}
