using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Serviço utilitário para tradução automática usando o microserviço ModernUO Translator Service.
/// </summary>
/// 
namespace Server.Custom.AI
{
    public static class AITranslator
    {
        private static readonly HttpClient httpClient;

        static AITranslator()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5); // Protege o servidor de travar se a API demorar
        }

        /// <summary>
        /// Traduz um texto entre dois idiomas, usando o microserviço de tradução.
        /// </summary>
        /// <param name="text">Texto original</param>
        /// <param name="srcLang">Idioma de origem (ex: \"pt\", \"en\", \"ru\")</param>
        /// <param name="tgtLang">Idioma de destino (ex: \"pt\", \"en\", \"ru\")</param>
        /// <returns>Texto traduzido, ou original em caso de erro</returns>
        public static async Task<string> TranslateAsync(string text, string srcLang, string tgtLang)
        {
            var payload = new
            {
                text,
                src_lang = srcLang,
                tgt_lang = tgtLang
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("http://localhost:10100/translate", content);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("TRADUTOR: " + responseString);
                using var doc = JsonDocument.Parse(responseString);

                // Espera: {"original":"...","translated":"...","src":"pt","tgt":"en"}
                if (doc.RootElement.TryGetProperty("translated", out var txt) && txt.GetString() is string translated)
                    return translated;
                else
                    return text; // Fallback: retorna original
            }
            catch (TaskCanceledException)
            {
                // Timeout!

                return text;
            }
            catch (Exception ex)
            {
                // Qualquer outro erro: retorna texto original (pode logar, se quiser)
                Console.WriteLine("TRADUTOR ERRO: " + ex.ToString());
                return text;
            }
        }

        public static async Task<string> TranslateTo(Mobile m, string tgtLang, string text)
        {
            var payload = new
            {
                text,
                src_lang = "pt",
                tgt_lang = tgtLang
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("http://localhost:10100/translate", content);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("TRADUTOR: " + responseString);
                using var doc = JsonDocument.Parse(responseString);

                if (doc.RootElement.TryGetProperty("translated", out var txt) && txt.GetString() is string translated)
                    return translated;
                else
                    return text;
            }
            catch (TaskCanceledException)
            {
                return text;
            }
            catch (Exception ex)
            {
                Console.WriteLine("TRADUTOR ERRO: " + ex.ToString());
                return text;
            }
        }
    }
}

