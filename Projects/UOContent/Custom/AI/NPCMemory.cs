using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Server.Mobiles;

public class NpcMemory
{
    private const int MaxMemoryEntries = 100;
    public string NpcId { get; set; }
    public List<MemoryEntry> MemoryEntries { get; set; } = new();

    public static readonly string SavePath = "Data/NpcMemory/";
    private static readonly object _fileLock = new();

    public NpcMemory() { }

    public NpcMemory(string npcId)
    {
        NpcId = npcId;
    }

    private string GetFilePath()
    {
        Directory.CreateDirectory(SavePath);
        return Path.Combine(SavePath, $"{NpcId}.json");
    }

    public void AddMemory(string content, string emocao = "neutra")
    {
        MemoryEntries.Add(new MemoryEntry
        {
            Timestamp = DateTime.UtcNow,
            Content = content,
            Emocao = emocao
        });

        // Mantém apenas as 100 mais recentes
        if (MemoryEntries.Count > MaxMemoryEntries)
        {
            MemoryEntries = MemoryEntries
                .Skip(MemoryEntries.Count - MaxMemoryEntries)
                .ToList();
        }

        Save();
    }


    private readonly Queue<string> ultimasFalas = new();
    private const int MaxFalasRecentes = 10;

    private readonly Dictionary<string, DateTime> falasRecentes = new();
    private readonly TimeSpan intervaloRepeticao = TimeSpan.FromMinutes(5);


    public bool DeveFalar(string conteudo)
    {
        var agora = DateTime.UtcNow;

        if (falasRecentes.TryGetValue(conteudo, out var ultimaFala))
        {
            if (agora - ultimaFala < intervaloRepeticao)
                return false; // Ainda dentro do tempo de espera
        }

        falasRecentes[conteudo] = agora;

        // Limita tamanho para evitar crescimento infinito
        if (falasRecentes.Count > 50)
        {
            var maisAntigas = falasRecentes
                .OrderBy(kv => kv.Value)
                .Take(10)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var chave in maisAntigas)
                falasRecentes.Remove(chave);
        }

        return true;
    }

    private string DetectarEmocao(string texto)
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



    private int GetHueFromEmotion(string emocao)
    {
        return emocao switch
        {
            "medo" => 0x005A,
            "raiva" => 0x0021,
            "afeto" => 0x0030,
            "ganancia" => 0x0035,
            "esperanca" => 0x59,
            _ => 0x0481,
        };
    }



    public void CompartilharMemoriasCom(NpcMemory outroNpc, BaseCreature outroCreature, int maxMemorias = 5)
    {
        var memoriasParaCompartilhar = this.MemoryEntries
            .OrderByDescending(m => m.Timestamp)
            .Take(maxMemorias)
            .ToList();

        foreach (var mem in memoriasParaCompartilhar)
        {
            string fala = $"Ouvi dizer: {mem.Content}";
            string emocao = DetectarEmocao(fala);

            outroNpc.AddMemory(fala, emocao);

            if (outroNpc.DeveFalar(fala))
            {
                int hue = GetHueFromEmotion(emocao);
                outroCreature.PublicOverheadMessage(Server.MessageType.Regular, hue, false, fala);
            }
        }
    }







    public List<string> GetRecentMemories(int count = 20)
    {
        return MemoryEntries
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .Select(m => $"[{m.Timestamp:HH:mm:ss}] {m.Content}")
            .ToList();
    }

    public string SearchMemory(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "Você precisa me dizer o que deseja que eu lembre.";

        foreach (var entry in MemoryEntries.OrderByDescending(m => m.Timestamp))
        {
            if (!string.IsNullOrWhiteSpace(entry?.Content) &&
                entry.Content.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"[{entry.Timestamp:HH:mm:ss}] {entry.Content}";
            }
        }

        return $"Desculpe, não me lembro de nada relacionado a \"{query}\".";
    }

    public void Save()
    {
        lock (_fileLock)
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(GetFilePath(), json);
        }
    }

    public void Load()
    {
        lock (_fileLock)
        {
            string path = GetFilePath();

            if (File.Exists(path))
            {
                try
                {
                    Console.WriteLine($"[NpcMemory] Carregando {path}...");
                    var json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<NpcMemory>(json);

                    if (data?.MemoryEntries != null)
                    {
                        this.MemoryEntries = data.MemoryEntries;
                        Console.WriteLine($"[NpcMemory] Memórias carregadas: {MemoryEntries.Count}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NpcMemory] ERRO ao carregar memória do NPC {NpcId}: {ex.Message}");
                    this.MemoryEntries = new();
                }
            }
        }
    }

}
