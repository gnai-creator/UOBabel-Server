using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace UOContent.Custom.Patreon
{
    public class PatreonChecker
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<PatreonResponse?> GetTierInfoAsync(string loginUO)
        {
            try
            {
                var url = $"https://www.uobabel.com/api/uobabel/subscriber-status?loginUO={loginUO}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<PatreonResponse>(content);

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar assinatura do Patreon: {ex.Message}");
                return null;
            }
        }

        public static async Task<bool> HasAccessAsync(string loginUO)
        {
            var result = await GetTierInfoAsync(loginUO);
            return result != null && !string.IsNullOrWhiteSpace(result.tier);
        }
    }

    public class PatreonResponse
    {
        public string loginUO { get; set; }
        public string tier { get; set; }
        public string patronStatus { get; set; }
    }
}
