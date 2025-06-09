using System;
using System.Threading.Tasks;
using Server.Mobiles;
using Server.Services.AI;
using Server.Items;

namespace Server.Mobiles
{
    public class CustomBaseAI : BaseAI
    {
        private bool _forceFlee;
        private Task? _decisionTask;

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
                background = $"Você está com {m_Mobile.Hits/m_Mobile.HitsMax*100}% de vida e fugindo de {m_Mobile.Combatant?.Name ?? "um inimigo"}.",
                location = m_Mobile.Location.ToString(),
                mood = "neutro",
                item_amount = m_Mobile.Backpack?.GetAmount(typeof(Gold)).ToString() ?? "0",
                item_name = string.Empty,
                memory = new(),
                nearby_npcs = new(),
                player_input = string.Empty,
                player_name = string.Empty
            };

            var decision = await AIService.DecideNpcActionAsync(state);
            if (decision != null && decision.type != AIService.GetNpcActionString(AIService.NpcAction.FUGIR))
            {
                _forceFlee = false;
                Action = ActionType.Guard;
            }
        }

        public override bool Think()
        {
            if (_forceFlee && Action != ActionType.Flee)
            {
                Action = ActionType.Flee;
            }

            if (_decisionTask == null || _decisionTask.IsCompleted)
            {
                _decisionTask = DecideActionAsync();
            }

            return base.Think();
        }

        public override bool CheckFlee()
        {
            if (_forceFlee)
            {
                Action = ActionType.Flee;
                return true;
            }

            return base.CheckFlee();
        }

        private async Task DecideActionAsync()
        {
            var state = new AIService.FullNPCState
            {
                npc_id = m_Mobile.Serial.ToString(),
                name = m_Mobile.Name,
                role = m_Mobile.Title ?? "npc",
                background = $"Vida: {m_Mobile.Hits}/{m_Mobile.HitsMax}",
                location = m_Mobile.Location.ToString(),
                mood = "neutro",
                item_amount = m_Mobile.Backpack?.GetAmount(typeof(Gold)).ToString() ?? "0",
                item_name = string.Empty,
                memory = new(),
                nearby_npcs = new(),
                player_input = string.Empty,
                player_name = string.Empty
            };

            var decision = await AIService.DecideNpcActionAsync(state);
            if (decision == null)
            {
                return;
            }

            AIService.NpcAction actionType = AIService.NpcAction.NENHUMA;
            foreach (AIService.NpcAction enumValue in Enum.GetValues(typeof(AIService.NpcAction)))
            {
                if (AIService.GetNpcActionString(enumValue) == decision.type)
                {
                    actionType = enumValue;
                    break;
                }
            }

            switch (actionType)
            {
                case AIService.NpcAction.ATACAR:
                    Action = ActionType.Combat;
                    if (m_Mobile.Combatant == null)
                    {
                        m_Mobile.Combatant = m_Mobile.FocusMob;
                    }
                    break;
                case AIService.NpcAction.FUGIR:
                    _forceFlee = true;
                    Action = ActionType.Flee;
                    break;
                case AIService.NpcAction.ROTINA:
                    _forceFlee = false;
                    Action = ActionType.Guard;
                    break;
            }
        }
    }
}

