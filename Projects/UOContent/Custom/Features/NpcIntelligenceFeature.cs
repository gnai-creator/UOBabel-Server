using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Server;
using Server.Mobiles;
using Server.Services.AI;
using Server.Custom.AI;
using Server.Custom.Mobiles;
using Server.Items;
using Server.Custom.Items;
using Server.Misc;
using Server.Gumps;
using Server.Custom.Gumps;

namespace Server.Custom.Features
{
    public class NpcIntelligenceFeature : CreatureFeatureBase
    {
        private MemoryFeature memory;
        private DateTime _nextAllowedSpeech;

        public virtual InhumanSpeech SpeechType => null;

        public NpcIntelligenceFeature() { }

        public override void Initialize()
        {
            if (Owner is CustomCreature cc)
            {
                if (cc.CreatureManager.Features.TryGetValue("memory", out var feat))
                {
                    memory = (MemoryFeature)feat;
                }
                else if (Owner is BaseCreature bc)
                {
                    memory = new MemoryFeature(bc);
                    memory.Initialize();
                    cc.CreatureManager.Features["memory"] = memory;
                }
            }
        }

        public override void OnThink() { }

        public override void OnDeath()
        {
            memory?.Save();
        }

        public override void OnCombat(Mobile combatant)
        {
            // Handle combat events here
        }

        public override async void OnSpeech(SpeechEventArgs e)
        {
            if (Owner is not BaseCreature creature)
                return;

            try
            {
                if (DateTime.UtcNow < _nextAllowedSpeech)
                    return;

                _nextAllowedSpeech = DateTime.UtcNow.AddSeconds(3);

                var speechType = SpeechType;
                if (memory != null)
                {
                    memory.MemoryId = $"{creature.Serial}-{e.Mobile.Serial}";
                    memory.Load();
                }

                if (speechType?.OnSpeech(creature, e.Mobile, e.Speech) == true)
                {
                    e.Handled = true;
                    return;
                }
                else if (!e.Handled && creature.AIObject != null && e.Mobile.InRange(creature, creature.RangePerception))
                {
                    creature.AIObject.OnSpeech(e);
                }
                if (e.Mobile is CustomPlayer pm && pm.HasPremium)
                {
                    if (e.Speech.Contains(creature.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        // === MEMÓRIA DIRETA MULTILÍNGUE ===
                        string speechLower = e.Speech.ToLower();
                        string[] triggers = { "lembra de ", "remember ", "do you remember ", "recuerda ", "tu te souviens de ", "помнишь " };
                        string playerLang = GetPlayerLanguage(e.Mobile);
                        foreach (string trigger in triggers)
                        {
                            if (speechLower.Contains(trigger))
                            {
                                string termo = speechLower.Substring(speechLower.IndexOf(trigger) + trigger.Length).Trim();

                                try
                                {
                                    string resultado = memory?.SearchMemory(termo) ??
                                        "Nao consigo me lembrar disso.";
                                    string emocao = DetectarEmocao(resultado);
                                    await FalarComEmocao(resultado, emocao, playerLang);
                                }
                                catch (Exception ex)
                                {
                                    creature.Say("Algo deu errado com minhas memórias.");
                                    Console.WriteLine($"[MemoryFeature] ERRO ao buscar por '{termo}': {ex.Message}");
                                }

                                e.Handled = true;
                                return;
                            }
                        }

                        // === INTEGRAÇÃO COM IA ===
                        if (!e.Handled && e.Mobile.InRange(creature, 3))
                        {
                            var nearbyNpcs = creature.GetMobilesInRange(5);
                            var nearbyNpcsList = new List<AIService.NearbyNPC>();
                            foreach (var npc in nearbyNpcs)
                            {
                                if (npc == creature) continue;
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
                                npc_id = creature.Serial.ToString(),
                                name = creature.Name,
                                role = creature.Title ?? "npc",
                                background = GetBackground() ?? "",
                                location = creature.Location.ToString() ?? "",
                                mood = "neutro",
                                item_amount = creature.Backpack?.GetAmount(typeof(Gold)).ToString() ?? "0",
                                item_name = "",
                                memory = memory?.GetRecentMemories() ?? new List<string>(),
                                nearby_npcs = nearbyNpcsList ?? new List<AIService.NearbyNPC>(),
                                player_input = e.Speech.ToLower(),
                                player_name = e.Mobile.Name
                            };

                            AIService.NpcAction actionType = AIService.NpcAction.NENHUMA;
                            try
                            {
                                var decision = await AIService.DecideNpcActionAsync(fullState);

                                Console.WriteLine($"[IAService] Decisão: {decision.type}");

                                foreach (var enumValue in Enum.GetValues(typeof(AIService.NpcAction)))
                                {
                                    if (AIService.GetNpcActionString((AIService.NpcAction)enumValue) == decision.type)
                                    {
                                        actionType = (AIService.NpcAction)enumValue;
                                        break;
                                    }
                                }

                                memory?.AddMemory($"Interagiu com {e.Mobile.Name}, que disse: \"{e.Speech}\"");
                                if (!string.IsNullOrWhiteSpace(decision.say))
                                    memory?.AddMemory($"Respondeu: \"{decision.say}\"");

                                if (!string.IsNullOrWhiteSpace(decision.say))
                                {
                                    string resposta = decision.say;

                                    if (!string.Equals(playerLang, "pt", StringComparison.OrdinalIgnoreCase))
                                    {
                                        resposta = await AITranslator.TranslateAsync(resposta, "pt", playerLang);
                                    }

                                    string emocao = DetectarEmocao(resposta);
                                    await FalarComEmocao(resposta, emocao, playerLang);
                                }
                                else
                                {
                                    await FalarComEmocao("*permanece em silencio*", "neutra", playerLang);
                                }

                                Console.WriteLine($"[IAService] Ação: {actionType}");

                                if (actionType != AIService.NpcAction.NENHUMA)
                                {
                                    switch (actionType)
                                    {
                                        case AIService.NpcAction.SEGUIR:
                                            await FalarComEmocao("*segue o jogador*", "afeto", playerLang);
                                            break;
                                        case AIService.NpcAction.MONTAR_CAVALO:
                                            break;
                                        case AIService.NpcAction.DESMONTAR_CAVALO:
                                            break;
                                        case AIService.NpcAction.MOVER_PARA:
                                            break;
                                        case AIService.NpcAction.MOVER_PARA_CAVALO:
                                            break;
                                        case AIService.NpcAction.MOVER_PARA_AUTOR:
                                            break;
                                        case AIService.NpcAction.FUGIR:
                                            await FalarComEmocao("*fugiu*", "medo", playerLang);
                                            FugaPorSpeech(e.Mobile, decision, playerLang);
                                            break;
                                        case AIService.NpcAction.PEGAR_DINHEIRO:
                                            goto case AIService.NpcAction.PEGAR_ITEM;
                                        case AIService.NpcAction.DAR_DINHEIRO:
                                            goto case AIService.NpcAction.DAR_ITEM;
                                        case AIService.NpcAction.PEGAR_ITEM:
                                            await FalarComEmocao("*pega o item*", "afeto", playerLang);
                                            PegarItem(e.Mobile, decision, playerLang);
                                            break;
                                        case AIService.NpcAction.DAR_ITEM:
                                            await FalarComEmocao("*oferece o item*", "afeto", playerLang);
                                            DarItem(e.Mobile, decision, playerLang);
                                            break;
                                        case AIService.NpcAction.ATACAR:
                                            await FalarComEmocao("*rosna e se prepara para atacar*", "raiva", playerLang);
                                            AtacarPorSpeech(e.Mobile, decision, playerLang);
                                            break;
                                        case AIService.NpcAction.ROTINA:
                                            await FalarComEmocao("*retoma seu posto habitual*", "afeto", playerLang);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                await FalarComEmocao("Desculpe, não consegui processar isso.", "raiva", playerLang);
                                Console.WriteLine($"[IAService] Erro ao decidir ação: {ex}");
                            }
                        }
                    }
                    else
                    {
                        creature.SendMessage("Você não tem acesso completo para interagir comigo.");
                    }
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                string playerLang = GetPlayerLanguage(e.Mobile);
                await FalarComEmocao("Houve uma falha na minha memória", "raiva", playerLang);
                Console.WriteLine($"[OnSpeech ERROR] {ex}");
            }
        }

        protected void FugaPorSpeech(Mobile from, AIService.NpcDecision decision, string playerLang)
        {
            if (Owner.AIObject != null)
            {
                Owner.AIObject.Action = ActionType.Flee;
            }
        }

        protected void AtacarPorSpeech(Mobile from, AIService.NpcDecision decision, string playerLang)
        {
            Mobile target = null;
            if (!string.IsNullOrWhiteSpace(decision.target))
            {
                foreach (var mob in from.GetMobilesInRange(10))
                {
                    if (mob.Name.InsensitiveEquals(decision.target))
                    {
                        target = mob;
                        break;
                    }
                }
            }
            if (target != null)
            {
                Owner.Combatant = target;
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
                FalarComEmocao("*Não tem o item*", "raiva", playerLang);
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
                    Owner.AddToBackpack(item);
                    ite.Delete();
                    FalarComEmocao("*pega o item*", "afeto", playerLang);
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

            Owner.PublicOverheadMessage(Server.MessageType.Regular, hue, false, texto);
        }



        public string GetPlayerLanguage(Mobile mob)
        {
            if (mob is CustomPlayer player)
            {
                if (!string.IsNullOrWhiteSpace(player.PreferredLanguage))
                    return player.PreferredLanguage;
                else
                {
                    mob.SendGump(new LanguageSelectGump());
                    return player.PreferredLanguage;
                }
            }
            return mob.Language ?? "pt";
        }


        public virtual string GetBackground()
        {

            switch (Owner.AI)
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
        public override void Serialize(IGenericWriter writer)
        {
            memory?.Save();
        }

        public override void Deserialize(IGenericReader reader)
        {
            if (Owner is BaseCreature)
            {
                // memory will be loaded lazily on first interaction
                memory = null;
            }
        }
    }
}
