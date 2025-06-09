using System.Threading.Tasks;
using Server.Mobiles;
using Server.Services.AI;

namespace Server.Mobiles
{
    public class CustomBaseAI : BaseAI
    {
        private bool _forceFlee;

        public CustomBaseAI(BaseCreature m) : base(m)
        {
        }

        public override bool DoActionFlee()
        {
            var from = m_Mobile.FocusMob ?? m_Mobile.Combatant;
            if (from == null || from.Deleted || from.Map != m_Mobile.Map)
            {
                Action = ActionType.Flee;
                return true;
            }

            WalkMobileRange(from, 1, true, m_Mobile.RangePerception * 2, m_Mobile.RangePerception * 3);
            Action = ActionType.Flee;
            _ = SendFleeFeedback();
            return true;
        }

        private async Task SendFleeFeedback()
        {
            var state = new AIService.FullNPCState
            {
                npc_id = m_Mobile.Serial.ToString(),
                name = m_Mobile.Name,
                role = m_Mobile.Title ?? "npc",
                background = string.Empty,
                location = m_Mobile.Location.ToString(),
                mood = "neutro",
                item_amount = m_Mobile.Backpack?.GetAmount(typeof(Gold)).ToString() ?? "0",
                item_name = string.Empty,
                memory = new(),
                nearby_npcs = new(),
                player_input = string.Empty,
                player_name = string.Empty
            };

            var decision = await AIService.DecideNpcActionNewAsync(state);
            if (decision != null && decision.type != AIService.GetNpcActionString(AIService.NpcAction.FUGIR))
            {
                _forceFlee = false;
                Action = ActionType.Guard;
            }
        }
    }
}

