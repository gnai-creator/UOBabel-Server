using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Serviço utilitário para integração entre ModernUO e o microserviço de IA (NPC roleplay).
/// </summary>
/// <remarks>
/// Este serviço é responsável por enviar o estado completo do NPC para a API e receber uma decisão (fala ou ação).
/// </remarks>
namespace Server.Services.AI
{
    public static class AIService
    {
        private static readonly HttpClient httpClient;

        static AIService()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public class NearbyNPC
        {
            public string id { get; set; }
            public string name { get; set; }
            public string role { get; set; }
            public string mood { get; set; } = "neutro";
        }

        public class FullNPCState
        {
            public string npc_id { get; set; }
            public string name { get; set; } = "";
            public string role { get; set; } = "";
            public string background { get; set; } = "";
            public string location { get; set; } = "";
            public string mood { get; set; } = "neutro";
            public string item_amount { get; set; } = "0";
            public string item_name { get; set; } = "";
            public List<string> memory { get; set; } = new();
            public List<NearbyNPC> nearby_npcs { get; set; } = new();
            public string player_input { get; set; } = "";
            public string player_name { get; set; } = "";
        }

        public class NpcDecision
        {
            public string type { get; set; }
            public string target { get; set; }
            public string say { get; set; }
            public string item_amount { get; set; }
            public string item_name { get; set; }
            public string details { get; set; }
        }

        public enum NpcAction
        {
            NENHUMA,
            PEGAR_DINHEIRO,
            DAR_DINHEIRO,
            PEGAR_ITEM,
            DAR_ITEM,
            ATACAR,
            ROTINA,
            DIZER,
            SEGUIR,
            MONTAR_CAVALO,
            DESMONTAR_CAVALO,
            MOVER_PARA,
            MOVER_PARA_CAVALO,
            MOVER_PARA_AUTOR,
            FUGIR,
        }

        public static string GetNpcActionString(NpcAction action) => action switch
        {
            NpcAction.NENHUMA => "nenhuma ação",
            NpcAction.PEGAR_DINHEIRO => "pegar dinheiro",
            NpcAction.DAR_DINHEIRO => "dar dinheiro",
            NpcAction.PEGAR_ITEM => "pegar item",
            NpcAction.DAR_ITEM => "dar item",
            NpcAction.ATACAR => "atacar",
            NpcAction.ROTINA => "voltar à rotina",
            NpcAction.DIZER => "Say",
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };

        public enum ItemType
        {
            ARCO,
            ESPADA,
            BARDICHE,
            ALABARDA,
            LANCA,
            MACHADO,
            ADAGA,
            MARRETA,
            PICARETA,
            FACA,
            BESTA,
            KATANA,
            KRYSS,
            FOICE,
            PORRETE,
            BORDAO,
            ESCUDO,
            BROQUEL,
            MADEIRA,
            SERRA,
            TAMBOR,
            TAMBORIM,
            HARPA,
            ALAUDE,
            SACOLA,
            BOLSA,
            MOCHILA,
            COURO,
            PAO,
            BOLO,
            COOKIES,
            PIZZA,
            BANDAGE,
            SETAS,
            FLECHA,
            TESOURA,
            NONE,
        }

        public static string GetItemTypeString(ItemType type) => type switch
        {
            ItemType.ARCO => "arco",
            ItemType.ESPADA => "espada",
            ItemType.BARDICHE => "bardiche",
            ItemType.ALABARDA => "alabarda",
            ItemType.LANCA => "lanca",
            ItemType.MACHADO => "machado",
            ItemType.ADAGA => "adaga",
            ItemType.MARRETA => "marreta",
            ItemType.PICARETA => "picareta",
            ItemType.FACA => "facão",
            ItemType.BESTA => "besta",
            ItemType.KATANA => "katana",
            ItemType.KRYSS => "kryss",
            ItemType.FOICE => "foice",
            ItemType.PORRETE => "porrete",
            ItemType.BORDAO => "bordão",
            ItemType.ESCUDO => "escudo",
            ItemType.BROQUEL => "broquel",
            ItemType.MADEIRA => "madeira",
            ItemType.SERRA => "serra",
            ItemType.TAMBOR => "tambor",
            ItemType.TAMBORIM => "tamborim",
            ItemType.HARPA => "harpa",
            ItemType.ALAUDE => "alaude",
            ItemType.SACOLA => "sacola",
            ItemType.BOLSA => "bolsa",
            ItemType.MOCHILA => "mochila",
            ItemType.COURO => "couro",
            ItemType.PAO => "pão",
            ItemType.BOLO => "bolo",
            ItemType.COOKIES => "cookies",
            ItemType.PIZZA => "pizza",
            ItemType.BANDAGE => "bandage",
            ItemType.SETAS => "setas",
            ItemType.FLECHA => "flecha",
            ItemType.TESOURA => "tesoura",
            ItemType.NONE => "none",
            _ => "none"
        };

        /// <summary>
        /// Envia um estado completo do NPC para a API e retorna uma decisão (fala ou ação).
        /// </summary>
        public static async Task<NpcDecision> DecideNpcActionAsync(FullNPCState state)
        {
            var json = JsonSerializer.Serialize(state);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("http://localhost:10000/npc/decide", content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<NpcDecision>(responseString);
            }
            catch (TaskCanceledException)
            {
                return new NpcDecision
                {
                    type = GetNpcActionString(NpcAction.NENHUMA),
                    target = "",
                    say = "Estou pensando demais no momento...",
                    item_amount = "0",
                    item_name = "",
                    details = "timeout"
                };
            }
            catch (Exception ex)
            {
                return new NpcDecision
                {
                    type = GetNpcActionString(NpcAction.NENHUMA),
                    target = "",
                    say = ex.Message, //"Não consigo decidir agora.",
                    item_amount = "0",
                    details = ex.Message
                };
            }
        }
        
    }
}


