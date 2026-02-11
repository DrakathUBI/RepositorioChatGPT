using System.Text.Json.Serialization;

namespace MacroStudio;

public class MacroFile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "macro";

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("events")]
    public List<MacroEvent> Events { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class MacroEvent
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("timestamp_ms")]
    public long TimestampMs { get; set; }

    [JsonPropertyName("data")]
    public Dictionary<string, string> Data { get; set; } = new();
}
