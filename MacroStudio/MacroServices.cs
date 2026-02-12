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
    private long _lastMouseMoveTs = -1;
    private int? _lastMouseMoveX;
    private int? _lastMouseMoveY;
    private PendingMouseMove? _pendingMouseMove;
    private PendingTextInput? _pendingTextInput;
    private const int MinMouseMoveIntervalMs = 80;
    private const int MinMouseMoveDistancePx = 6;
    private const int TextInputMergeWindowMs = 2000;

    public bool IsRecording { get; private set; }

    public void Start(string stopKey)
    {
        if (IsRecording)
        {
            return;
        }

        _events.Clear();
        _lastMouseMoveTs = -1;
        _lastMouseMoveX = null;
        _lastMouseMoveY = null;
        _pendingMouseMove = null;
        _pendingTextInput = null;
        _stopwatch.Restart();
        IsRecording = true;

        _hook = Hook.GlobalEvents();
        _hook.KeyDown += (_, e) =>
        {
            if (ShouldRecordKeyEvent(e))
            {
                AddEvent("key_down", new Dictionary<string, string> { ["key"] = e.KeyCode.ToString() });
            }

            if (string.Equals(e.KeyCode.ToString(), stopKey, StringComparison.OrdinalIgnoreCase))
            {
                Stop();
            }
        };

        _hook.KeyUp += (_, e) =>
        {
            if (ShouldRecordKeyEvent(e))
            {
                AddEvent("key_up", new Dictionary<string, string> { ["key"] = e.KeyCode.ToString() });
            }
        };

        _hook.KeyPress += (_, e) =>
        {
            AddTextInputChar(e.KeyChar);
        };

        _hook.MouseMove += (_, e) =>
        {
            var currentTs = _stopwatch.ElapsedMilliseconds;
            if (ShouldRecordMouseMove(e.X, e.Y, currentTs))
            {
                _pendingMouseMove = new PendingMouseMove(e.X, e.Y, currentTs);
                _lastMouseMoveTs = currentTs;
                _lastMouseMoveX = e.X;
                _lastMouseMoveY = e.Y;
            }
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

        FlushPendingBufferedEvents();
        _hook?.Dispose();
        _hook = null;
        _stopwatch.Stop();
        IsRecording = false;
    }


    private static bool ShouldRecordKeyEvent(KeyEventArgs e)
    {
        if (e.Control || e.Alt)
        {
            return true;
        }

        return e.KeyCode switch
        {
            Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
            or Keys.Menu or Keys.LMenu or Keys.RMenu
            or Keys.Enter or Keys.Back or Keys.Tab or Keys.Escape
            or Keys.Delete or Keys.Insert
            or Keys.Up or Keys.Down or Keys.Left or Keys.Right
            or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown
            or Keys.F1 or Keys.F2 or Keys.F3 or Keys.F4 or Keys.F5 or Keys.F6
            or Keys.F7 or Keys.F8 or Keys.F9 or Keys.F10 or Keys.F11 or Keys.F12 => true,
            _ => false
        };
    }


    private bool ShouldRecordMouseMove(int x, int y, long ts)
    {
        if (_lastMouseMoveTs < 0 || _lastMouseMoveX is null || _lastMouseMoveY is null)
        {
            return true;
        }

        var elapsed = ts - _lastMouseMoveTs;
        var dx = Math.Abs(x - _lastMouseMoveX.Value);
        var dy = Math.Abs(y - _lastMouseMoveY.Value);

        return elapsed >= MinMouseMoveIntervalMs || dx >= MinMouseMoveDistancePx || dy >= MinMouseMoveDistancePx;
    }

    private void AddEvent(string kind, Dictionary<string, string> data)
    {
        if (kind != "mouse_move" && kind != "text_input")
        {
            FlushPendingBufferedEvents();
        }

        _events.Add(new MacroEvent
        {
            Kind = kind,
            TimestampMs = _stopwatch.ElapsedMilliseconds,
            Data = data
        });
    }

    private void AddTextInputChar(char keyChar)
    {
        var now = _stopwatch.ElapsedMilliseconds;
        if (_pendingTextInput is null)
        {
            _pendingTextInput = new PendingTextInput(keyChar.ToString(), now);
            return;
        }

        if (now - _pendingTextInput.LastTimestampMs <= TextInputMergeWindowMs)
        {
            _pendingTextInput.Text += keyChar;
            _pendingTextInput.LastTimestampMs = now;
            return;
        }

        FlushPendingTextInput();
        _pendingTextInput = new PendingTextInput(keyChar.ToString(), now);
    }

    private void FlushPendingMouseMove()
    {
        if (_pendingMouseMove is null)
        {
            return;
        }

        _events.Add(new MacroEvent
        {
            Kind = "mouse_move",
            TimestampMs = _pendingMouseMove.TimestampMs,
            Data = new Dictionary<string, string>
            {
                ["x"] = _pendingMouseMove.X.ToString(),
                ["y"] = _pendingMouseMove.Y.ToString()
            }
        });

        _pendingMouseMove = null;
    }

    private void FlushPendingTextInput()
    {
        if (_pendingTextInput is null || string.IsNullOrEmpty(_pendingTextInput.Text))
        {
            _pendingTextInput = null;
            return;
        }

        _events.Add(new MacroEvent
        {
            Kind = "text_input",
            TimestampMs = _pendingTextInput.LastTimestampMs,
            Data = new Dictionary<string, string>
            {
                ["text"] = _pendingTextInput.Text
            }
        });

        _pendingTextInput = null;
    }

    private void FlushPendingBufferedEvents()
    {
        while (_pendingMouseMove is not null || _pendingTextInput is not null)
        {
            if (_pendingMouseMove is not null && _pendingTextInput is not null)
            {
                if (_pendingMouseMove.TimestampMs <= _pendingTextInput.LastTimestampMs)
                {
                    FlushPendingMouseMove();
                }
                else
                {
                    FlushPendingTextInput();
                }

                continue;
            }

            if (_pendingMouseMove is not null)
            {
                FlushPendingMouseMove();
            }
            else
            {
                FlushPendingTextInput();
            }
        }
    }

    private sealed record PendingMouseMove(int X, int Y, long TimestampMs);

    private sealed class PendingTextInput
    {
        public PendingTextInput(string text, long lastTimestampMs)
        {
            Text = text;
            LastTimestampMs = lastTimestampMs;
        }

        public string Text { get; set; }
        public long LastTimestampMs { get; set; }
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

public sealed class PlaybackFilterOptions
{
    public bool PlayMouseMoves { get; init; } = true;
    public bool PlayMouseClicks { get; init; } = true;
    public bool PlayKeyPresses { get; init; } = true;
    public bool RespectWaitTimes { get; init; } = true;
    public int LoopCount { get; init; } = 1;
    public int StartDelayMs { get; init; } = 0;
    public int DelayJitterMs { get; init; } = 0;
}

public class MacroPlayerService
{

    public async Task PlayAsync(MacroFile macro, Dictionary<string, string> parameters, double speed, CancellationToken ct, PlaybackFilterOptions? options = null)
    {
        options ??= new PlaybackFilterOptions();
        var playbackEvents = BuildPlaybackEvents(macro.Events, options);
        if (playbackEvents.Count == 0)
        {
            return;
        }

        if (options.StartDelayMs > 0)
        {
            await Task.Delay(options.StartDelayMs, ct);
        }

        var loopCount = Math.Max(options.LoopCount, 1);
        var jitter = Math.Max(options.DelayJitterMs, 0);
        var random = jitter > 0 ? new Random() : null;

        for (var loop = 0; loop < loopCount; loop++)
        {
            long previous = 0;
            foreach (var ev in playbackEvents)
            {
                ct.ThrowIfCancellationRequested();
                var delay = Math.Max((ev.TimestampMs - previous) / Math.Max(speed, 0.1), 0);
                if (options.RespectWaitTimes && delay > 0)
                {
                    if (random is not null)
                    {
                        delay = Math.Max(delay + random.Next(-jitter, jitter + 1), 0);
                    }

                    await Task.Delay((int)delay, ct);
                }

                var data = ev.Data.ToDictionary(kvp => kvp.Key, kvp => ApplyParameters(kvp.Value, parameters));
                Execute(ev.Kind, data);

                previous = ev.TimestampMs;
            }
        }
    }

    private static List<MacroEvent> BuildPlaybackEvents(IEnumerable<MacroEvent> events, PlaybackFilterOptions options)
    {
        var filtered = events
            .Where(ev => ShouldPlayEvent(ev.Kind, options))
            .Select(ev => new MacroEvent
            {
                Kind = ev.Kind,
                TimestampMs = ev.TimestampMs,
                Data = new Dictionary<string, string>(ev.Data)
            })
            .ToList();

        return CompactMouseMoves(filtered);
    }

    private static List<MacroEvent> CompactMouseMoves(List<MacroEvent> events)
    {
        if (events.Count == 0)
        {
            return events;
        }

        var compacted = new List<MacroEvent>(events.Count);
        MacroEvent? pendingMove = null;

        foreach (var ev in events)
        {
            if (ev.Kind == "mouse_move")
            {
                pendingMove = ev;
                continue;
            }

            if (pendingMove is not null)
            {
                compacted.Add(pendingMove);
                pendingMove = null;
            }

            compacted.Add(ev);
        }

        if (pendingMove is not null)
        {
            compacted.Add(pendingMove);
        }

        return compacted;
    }

    private static bool ShouldPlayEvent(string kind, PlaybackFilterOptions options)
    {
        return kind switch
        {
            "mouse_move" => options.PlayMouseMoves,
            "mouse_down" or "mouse_up" or "mouse_wheel" => options.PlayMouseClicks,
            "key_down" or "key_up" or "text_input" => options.PlayKeyPresses,
            _ => true
        };
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

        if (kind == "text_input" && data.TryGetValue("text", out var text) && !string.IsNullOrEmpty(text))
        {
            SendKeys.SendWait(EscapeSendKeysText(text));
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


    private static string EscapeSendKeysText(string text)
    {
        return text
            .Replace("{", "{{}", StringComparison.Ordinal)
            .Replace("}", "{}}", StringComparison.Ordinal)
            .Replace("+", "{+}", StringComparison.Ordinal)
            .Replace("^", "{^}", StringComparison.Ordinal)
            .Replace("%", "{%}", StringComparison.Ordinal)
            .Replace("~", "{~}", StringComparison.Ordinal)
            .Replace("(", "{(}", StringComparison.Ordinal)
            .Replace(")", "{)}", StringComparison.Ordinal)
            .Replace("[", "{[}", StringComparison.Ordinal)
            .Replace("]", "{]}", StringComparison.Ordinal);
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
{JsonSerializer.Serialize(macro, new JsonSerializerOptions { WriteIndented = true })}

Parâmetros:
{JsonSerializer.Serialize(parameters, new JsonSerializerOptions { WriteIndented = true })}

Responda:
1) O que a macro faz.
2) Melhorias de robustez.
3) Novos parâmetros recomendados.
4) Riscos e limites.";
    }
}
