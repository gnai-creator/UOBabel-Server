using System;
using System.Threading.Tasks;
using Server.Mobiles;
using Server.Services.AI;
using Server.Items;
using Server.Custom.Mobiles;

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
                background = $"Você está com {m_Mobile.Hits / m_Mobile.HitsMax * 100}% de vida e fugindo de {m_Mobile.Combatant?.Name ?? "um inimigo"}.",
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

                if (Action == ActionType.Follow)
                {
                    if (m_Mobile.ControlMaster is CustomPlayer player)
                    {
                        if (player.HasPremium)
                        {
                            _decisionTask = DecideActionAsync($"você está seguindo {m_Mobile.ControlMaster.Name}. Decida sua ação.");
                        }
                    }
                }
                else if (Action == ActionType.Wander)
                {
                    if (m_Mobile.ControlMaster is CustomPlayer player)
                    {
                        if (player.HasPremium)
                        {
                            _decisionTask = DecideActionAsync($"você está vagando por {m_Mobile.Region.Name}. Decida sua ação.");
                        }
                    }
                }
                else if (Action == ActionType.Combat)
                {
                    if (m_Mobile.ControlMaster is CustomPlayer player)
                    {
                        if (player.HasPremium)
                        {
                            _decisionTask = DecideActionAsync($"você está lutando com {m_Mobile.Combatant?.Name ?? "um inimigo"}. Decida sua ação.");
                        }
                    }
                }
                else if (Action == ActionType.Flee)
                {
                    if (m_Mobile.ControlMaster is CustomPlayer player)
                    {
                        if (player.HasPremium)
                        {
                            _decisionTask = DecideActionAsync($"você está fugindo de {m_Mobile.Combatant?.Name ?? "um inimigo"}. Decida sua ação.");
                        }
                    }
                }
                else if (Action == ActionType.Guard)
                {
                    if (m_Mobile.ControlMaster is CustomPlayer player)
                    {
                        if (player.HasPremium)
                        {
                            _decisionTask = DecideActionAsync($"você está guardando {m_Mobile.Combatant?.Name ?? "um inimigo"}. Decida sua ação.");
                        }
                    }
                }
                else if (Action == ActionType.Interact)
                {
                    if (m_Mobile.ControlMaster is CustomPlayer player)
                    {
                        if (player.HasPremium)
                        {
                            _decisionTask = DecideActionAsync($"você está interagindo com {m_Mobile.FocusMob?.Name ?? "um objeto"}. Decida sua ação.");
                        }
                    }
                }
                else if (Action == ActionType.Backoff)
                {
                    if (m_Mobile.ControlMaster is CustomPlayer player)
                    {
                        if (player.HasPremium)
                        {
                            _decisionTask = DecideActionAsync($"você está se afastando de {m_Mobile.Combatant?.Name ?? "um inimigo"}. Decida sua ação.");
                        }
                    }
                }

                else
                {
                    _decisionTask = DecideActionAsync($"Decida sua ação.");
                }
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

        private async Task DecideActionAsync(string background)
        {
            var state = new AIService.FullNPCState
            {
                npc_id = m_Mobile.Serial.ToString(),
                name = m_Mobile.Name,
                role = m_Mobile.Title ?? "npc",
                background = $"Com {m_Mobile.Hits}/{m_Mobile.HitsMax * 100}% de vida, {background}",
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
                case AIService.NpcAction.MOVER_PARA:
                    Action = ActionType.Wander;
                    break;
                case AIService.NpcAction.MOVER_PARA_CAVALO:
                    Action = ActionType.Wander;
                    break;
                case AIService.NpcAction.MOVER_PARA_AUTOR:
                    Action = ActionType.Follow;
                    break;
                case AIService.NpcAction.DIZER:
                    Action = ActionType.Interact;
                    break;
                case AIService.NpcAction.SEGUIR:
                    Action = ActionType.Follow;
                    break;
                default:
                    Action = ActionType.Unknown;
                    break;
            }
        }
    }
}

