using System;

public class MemoryEntry
{
    public DateTime Timestamp { get; set; }
    public string Content { get; set; }

    // Novo campo para emoção associada
    public string Emocao { get; set; } = "neutra"; 
}
