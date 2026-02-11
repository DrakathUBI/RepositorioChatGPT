using Gma.System.MouseKeyHook;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MacroStudio;

public class MacroRecorderService
{
    private IKeyboardMouseEvents? _hook;
    private readonly Stopwatch _stopwatch = new();
    private readonly List<MacroEvent> _events = new();

    public bool IsRecording { get; private set; }

    public void Start(string stopKey)
    {
        if (IsRecording)
        {
            return;
        }

        _events.Clear();
        _stopwatch.Restart();
        IsRecording = true;

        _hook = Hook.GlobalEvents();
        _hook.KeyDown += (_, e) =>
        {
            AddEvent("key_down", new Dictionary<string, string> { ["key"] = e.KeyCode.ToString() });
            if (string.Equals(e.KeyCode.ToString(), stopKey, StringComparison.OrdinalIgnoreCase))
            {
                Stop();
            }
        };

        _hook.KeyUp += (_, e) =>
        {
            AddEvent("key_up", new Dictionary<string, string> { ["key"] = e.KeyCode.ToString() });
        };

        _hook.MouseMove += (_, e) =>
        {
            AddEvent("mouse_move", new Dictionary<string, string>
            {
                ["x"] = e.X.ToString(),
                ["y"] = e.Y.ToString()
            });
        };

        _hook.MouseDownExt += (_, e) =>
        {
            AddEvent("mouse_down", new Dictionary<string, string>
            {
                ["x"] = e.X.ToString(),
                ["y"] = e.Y.ToString(),
                ["button"] = e.Button.ToString()
            });
        };

        _hook.MouseUpExt += (_, e) =>
        {
            AddEvent("mouse_up", new Dictionary<string, string>
            {
                ["x"] = e.X.ToString(),
                ["y"] = e.Y.ToString(),
                ["button"] = e.Button.ToString()
            });
        };

        _hook.MouseWheelExt += (_, e) =>
        {
            AddEvent("mouse_wheel", new Dictionary<string, string>
            {
                ["x"] = e.X.ToString(),
                ["y"] = e.Y.ToString(),
                ["delta"] = e.Delta.ToString()
            });
        };
    }

    public MacroFile StopAndBuild(string name, string stopKey)
    {
        Stop();
        return new MacroFile
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            Events = _events.ToList(),
            Metadata = new Dictionary<string, string> { ["stop_key"] = stopKey }
        };
    }

    public void Stop()
    {
        if (!IsRecording)
        {
            return;
        }

        _hook?.Dispose();
        _hook = null;
        _stopwatch.Stop();
        IsRecording = false;
    }

    private void AddEvent(string kind, Dictionary<string, string> data)
    {
        _events.Add(new MacroEvent
        {
            Kind = kind,
            TimestampMs = _stopwatch.ElapsedMilliseconds,
            Data = data
        });
    }
}

internal static class NativeInput
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    public const uint MOUSEEVENTF_WHEEL = 0x0800;

    public const uint KEYEVENTF_KEYUP = 0x0002;
}

public class MacroPlayerService
{

    public async Task PlayAsync(MacroFile macro, Dictionary<string, string> parameters, double speed, CancellationToken ct)
    {
        long previous = 0;
        foreach (var ev in macro.Events)
        {
            ct.ThrowIfCancellationRequested();
            var delay = Math.Max((ev.TimestampMs - previous) / Math.Max(speed, 0.1), 0);
            await Task.Delay((int)delay, ct);

            var data = ev.Data.ToDictionary(kvp => kvp.Key, kvp => ApplyParameters(kvp.Value, parameters));
            Execute(ev.Kind, data);
            previous = ev.TimestampMs;
        }
    }

    private void Execute(string kind, Dictionary<string, string> data)
    {
        if (kind == "key_down" && data.TryGetValue("key", out var keyDown) && TryParseKey(keyDown, out var kd))
        {
            NativeInput.keybd_event((byte)kd, 0, 0, UIntPtr.Zero);
            return;
        }

        if (kind == "key_up" && data.TryGetValue("key", out var keyUp) && TryParseKey(keyUp, out var ku))
        {
            NativeInput.keybd_event((byte)ku, 0, NativeInput.KEYEVENTF_KEYUP, UIntPtr.Zero);
            return;
        }

        if (kind.StartsWith("mouse_", StringComparison.Ordinal))
        {
            MouseEventHandler(kind, data);
        }
    }

    private void MouseEventHandler(string kind, Dictionary<string, string> data)
    {
        if (data.TryGetValue("x", out var xRaw) && data.TryGetValue("y", out var yRaw) && int.TryParse(xRaw, out var x) && int.TryParse(yRaw, out var y))
        {
            Cursor.Position = new System.Drawing.Point(x, y);
        }

        if (kind == "mouse_down" || kind == "mouse_up")
        {
            var button = data.GetValueOrDefault("button", "Left");
            if (button.Equals("Left", StringComparison.OrdinalIgnoreCase))
            {
                if (kind == "mouse_down") NativeInput.mouse_event(NativeInput.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                else NativeInput.mouse_event(NativeInput.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            }
            if (button.Equals("Right", StringComparison.OrdinalIgnoreCase))
            {
                if (kind == "mouse_down") NativeInput.mouse_event(NativeInput.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                else NativeInput.mouse_event(NativeInput.MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
            }
        }

        if (kind == "mouse_wheel" && data.TryGetValue("delta", out var deltaRaw) && int.TryParse(deltaRaw, out var delta))
        {
            NativeInput.mouse_event(NativeInput.MOUSEEVENTF_WHEEL, 0, 0, unchecked((uint)delta), UIntPtr.Zero);
        }
    }


    private static bool TryParseKey(string keyName, out Keys key)
    {
        key = Keys.None;
        if (Enum.TryParse(keyName, true, out Keys parsed))
        {
            key = parsed;
            return true;
        }

        return false;
    }
    private static string ApplyParameters(string value, Dictionary<string, string> parameters)
    {
        var output = value;
        foreach (var pair in parameters)
        {
            output = output.Replace("{{" + pair.Key + "}}", pair.Value, StringComparison.Ordinal);
        }

        return output;
    }
}

public static class MacroStorage
{
    public static async Task SaveAsync(string path, MacroFile macro)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(macro, options);
        await File.WriteAllTextAsync(path, json, Encoding.UTF8);
    }

    public static async Task<MacroFile> LoadAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path, Encoding.UTF8);
        var macro = JsonSerializer.Deserialize<MacroFile>(json);
        return macro ?? throw new InvalidDataException("Arquivo de macro inválido.");
    }
}

public class GeminiCoachService
{
    private readonly HttpClient _httpClient = new();

    public async Task<string> AnalyzeAsync(MacroFile macro, Dictionary<string, string> parameters, string model)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "Defina GEMINI_API_KEY para usar a IA.";
        }

        var prompt = BuildPrompt(macro, parameters);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            }
        };

        var response = await _httpClient.PostAsync(
            url,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        );

        if (!response.IsSuccessStatusCode)
        {
            return $"Erro Gemini: {(int)response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
        }

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? "A IA não retornou conteúdo.";
    }

    private static string BuildPrompt(MacroFile macro, Dictionary<string, string> parameters)
    {
        return $@"Você é um assistente para melhoria de automações.

Macro em JSON:
{JsonSerializer.Serialize(macro, new JsonSerializerOptions {{ WriteIndented = true }})}

Parâmetros:
{JsonSerializer.Serialize(parameters, new JsonSerializerOptions {{ WriteIndented = true }})}

Responda:
1) O que a macro faz.
2) Melhorias de robustez.
3) Novos parâmetros recomendados.
4) Riscos e limites.";
    }
}
