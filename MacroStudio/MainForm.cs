using System.Text;

namespace MacroStudio;

public class MainForm : Form
{
    private readonly TextBox _macroPath = new() { Width = 760 };
    private readonly TextBox _params = new() { Width = 760, Text = "" };
    private readonly TextBox _speed = new() { Width = 80, Text = "1.0" };
    private readonly TextBox _stopKey = new() { Width = 80, Text = "F8" };
    private readonly TextBox _model = new() { Width = 180, Text = "gemini-2.0-flash" };
    private readonly TextBox _log = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Width = 1060, Height = 120 };

    private readonly ComboBox _inspectFilter = new()
    {
        Width = 220,
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    private readonly DataGridView _eventsGrid = new()
    {
        Width = 1060,
        Height = 330,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoGenerateColumns = false,
        RowHeadersVisible = false
    };

    private readonly CheckBox _playMouseMoves = new() { Text = "Mouse moves", Checked = true, AutoSize = true };
    private readonly CheckBox _playMouseClicks = new() { Text = "Mouse clicks", Checked = true, AutoSize = true };
    private readonly CheckBox _playKeyPresses = new() { Text = "Key presses", Checked = true, AutoSize = true };
    private readonly CheckBox _playWaitTimes = new() { Text = "Wait times", Checked = true, AutoSize = true };

    private readonly MacroRecorderService _recorder = new();
    private readonly MacroPlayerService _player = new();
    private readonly GeminiCoachService _coach = new();
    private CancellationTokenSource? _playCts;
    private MacroFile? _currentMacro;

    public MainForm()
    {
        Text = "Macro Studio (.exe)";
        Width = 1100;
        Height = 760;

        var y = 12;
        Controls.Add(new Label { Left = 12, Top = y, Width = 200, Text = "Arquivo macro (.json)" });
        y += 20;
        _macroPath.Left = 12;
        _macroPath.Top = y;
        Controls.Add(_macroPath);

        var btnBrowse = new Button { Left = 780, Top = y - 1, Width = 90, Text = "Abrir" };
        var btnReload = new Button { Left = 876, Top = y - 1, Width = 90, Text = "Recarregar" };
        btnBrowse.Click += (_, _) => BrowseMacro();
        btnReload.Click += async (_, _) => await LoadAndRenderMacroAsync();
        Controls.Add(btnBrowse);
        Controls.Add(btnReload);

        y += 34;
        Controls.Add(new Label { Left = 12, Top = y, Width = 300, Text = "Parâmetros (a=1,b=2 ou JSON)" });
        y += 20;
        _params.Left = 12;
        _params.Top = y;
        Controls.Add(_params);

        y += 34;
        Controls.Add(new Label { Left = 12, Top = y, Width = 90, Text = "Velocidade" });
        _speed.Left = 86;
        _speed.Top = y - 3;
        Controls.Add(_speed);

        Controls.Add(new Label { Left = 186, Top = y, Width = 90, Text = "Tecla stop" });
        _stopKey.Left = 250;
        _stopKey.Top = y - 3;
        Controls.Add(_stopKey);

        Controls.Add(new Label { Left = 350, Top = y, Width = 70, Text = "Modelo" });
        _model.Left = 405;
        _model.Top = y - 3;
        Controls.Add(_model);

        y += 34;
        var btnRecord = new Button { Left = 12, Top = y, Width = 120, Text = "Gravar" };
        var btnStopRecord = new Button { Left = 138, Top = y, Width = 120, Text = "Parar Gravação" };
        var btnPlay = new Button { Left = 264, Top = y, Width = 120, Text = "Reproduzir" };
        var btnStopPlay = new Button { Left = 390, Top = y, Width = 120, Text = "Parar Replay" };
        var btnInspect = new Button { Left = 516, Top = y, Width = 120, Text = "Inspecionar" };
        var btnAi = new Button { Left = 642, Top = y, Width = 120, Text = "Analisar IA" };

        btnRecord.Click += async (_, _) => await StartRecordAsync();
        btnStopRecord.Click += async (_, _) => await StopRecordAsync();
        btnPlay.Click += async (_, _) => await PlayAsync();
        btnStopPlay.Click += (_, _) => StopPlay();
        btnInspect.Click += async (_, _) => await LoadAndRenderMacroAsync();
        btnAi.Click += async (_, _) => await AnalyzeAsync();

        Controls.Add(btnRecord);
        Controls.Add(btnStopRecord);
        Controls.Add(btnPlay);
        Controls.Add(btnStopPlay);
        Controls.Add(btnInspect);
        Controls.Add(btnAi);

        y += 40;
        Controls.Add(new Label { Left = 12, Top = y + 4, Width = 130, Text = "Filtro inspeção" });
        _inspectFilter.Left = 110;
        _inspectFilter.Top = y;
        _inspectFilter.Items.AddRange(new object[] { "Tudo", "Cliques", "Movimento mouse", "Teclado", "Espera" });
        _inspectFilter.SelectedIndex = 0;
        _inspectFilter.SelectedIndexChanged += (_, _) => RefreshGrid();
        Controls.Add(_inspectFilter);

        Controls.Add(new Label { Left = 350, Top = y + 4, Width = 130, Text = "Playback filter" });
        _playMouseMoves.Left = 450;
        _playMouseMoves.Top = y + 3;
        _playMouseClicks.Left = 560;
        _playMouseClicks.Top = y + 3;
        _playKeyPresses.Left = 670;
        _playKeyPresses.Top = y + 3;
        _playWaitTimes.Left = 770;
        _playWaitTimes.Top = y + 3;
        Controls.Add(_playMouseMoves);
        Controls.Add(_playMouseClicks);
        Controls.Add(_playKeyPresses);
        Controls.Add(_playWaitTimes);

        y += 36;
        SetupGridColumns();
        _eventsGrid.Left = 12;
        _eventsGrid.Top = y;
        Controls.Add(_eventsGrid);

        y += 340;
        _log.Left = 12;
        _log.Top = y;
        Controls.Add(_log);
    }

    private void SetupGridColumns()
    {
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", DataPropertyName = nameof(EventRow.Index), Width = 48 });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Action", DataPropertyName = nameof(EventRow.Action), Width = 210 });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value", DataPropertyName = nameof(EventRow.Value), Width = 360 });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Timestamp (ms)", DataPropertyName = nameof(EventRow.TimestampMs), Width = 120 });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Wait (ms)", DataPropertyName = nameof(EventRow.WaitMs), Width = 100 });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kind", DataPropertyName = nameof(EventRow.Kind), Width = 190 });
    }

    private void Log(string message)
    {
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void BrowseMacro()
    {
        using var ofd = new OpenFileDialog { Filter = "Macro JSON (*.json)|*.json|Todos (*.*)|*.*" };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            _macroPath.Text = ofd.FileName;
        }
    }

    private async Task StartRecordAsync()
    {
        var name = Prompt("Nome da macro", "Nova Macro");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var stopKey = _stopKey.Text.Trim();
        _recorder.Start(stopKey);
        Log($"Gravando '{name}'. Aperte {stopKey} para parar automaticamente, ou clique em Parar Gravação.");
        await Task.CompletedTask;
    }

    private async Task StopRecordAsync()
    {
        if (!_recorder.IsRecording)
        {
            Log("Nenhuma gravação em andamento.");
            return;
        }

        using var sfd = new SaveFileDialog { Filter = "Macro JSON (*.json)|*.json", DefaultExt = "json" };
        if (sfd.ShowDialog() != DialogResult.OK)
        {
            Log("Gravação cancelada: arquivo não selecionado.");
            _recorder.Stop();
            return;
        }

        var name = Path.GetFileNameWithoutExtension(sfd.FileName);
        var macro = _recorder.StopAndBuild(name, _stopKey.Text.Trim());
        await MacroStorage.SaveAsync(sfd.FileName, macro);
        _macroPath.Text = sfd.FileName;
        _currentMacro = macro;
        RefreshGrid();
        Log($"Macro salva em {_macroPath.Text} ({macro.Events.Count} eventos). Use o filtro para ver só cliques/teclas/espera.");
    }

    private async Task PlayAsync()
    {
        if (!File.Exists(_macroPath.Text))
        {
            MessageBox.Show("Selecione um arquivo de macro válido.");
            return;
        }

        var macro = await MacroStorage.LoadAsync(_macroPath.Text);
        var parameters = ParseParameters(_params.Text);
        var speed = double.TryParse(_speed.Text, out var s) ? s : 1.0;

        _playCts = new CancellationTokenSource();
        Log($"Reproduzindo {macro.Name} com {macro.Events.Count} eventos...");

        try
        {
            await _player.PlayAsync(
                macro,
                parameters,
                speed,
                _playCts.Token,
                new PlaybackFilterOptions
                {
                    PlayMouseMoves = _playMouseMoves.Checked,
                    PlayMouseClicks = _playMouseClicks.Checked,
                    PlayKeyPresses = _playKeyPresses.Checked,
                    RespectWaitTimes = _playWaitTimes.Checked
                }
            );
            Log("Replay concluído.");
        }
        catch (OperationCanceledException)
        {
            Log("Replay interrompido pelo usuário.");
        }
    }

    private void StopPlay()
    {
        _playCts?.Cancel();
    }

    private async Task LoadAndRenderMacroAsync()
    {
        if (!File.Exists(_macroPath.Text))
        {
            MessageBox.Show("Selecione um arquivo de macro válido.");
            return;
        }

        _currentMacro = await MacroStorage.LoadAsync(_macroPath.Text);
        RefreshGrid();
        Log($"Inspeção carregada: {_currentMacro.Name}, {_currentMacro.Events.Count} eventos.");
    }

    private void RefreshGrid()
    {
        if (_currentMacro is null)
        {
            _eventsGrid.DataSource = null;
            return;
        }

        var rows = BuildRows(_currentMacro, _inspectFilter.SelectedItem?.ToString() ?? "Tudo");
        _eventsGrid.DataSource = rows;
    }

    private static List<EventRow> BuildRows(MacroFile macro, string filter)
    {
        var rows = new List<EventRow>();
        long prevTimestamp = 0;
        int? lastX = null;
        int? lastY = null;

        for (var i = 0; i < macro.Events.Count; i++)
        {
            var ev = macro.Events[i];
            var waitMs = Math.Max(ev.TimestampMs - prevTimestamp, 0);

            if (waitMs > 0)
            {
                var waitRow = new EventRow
                {
                    Index = rows.Count + 1,
                    Action = "Wait",
                    Value = $"{waitMs} ms",
                    TimestampMs = ev.TimestampMs,
                    WaitMs = waitMs,
                    Kind = "wait"
                };

                if (MatchInspectFilter(filter, waitRow.Kind))
                {
                    rows.Add(waitRow);
                }
            }

            var action = PrettyAction(ev);
            var value = PrettyValue(ev, ref lastX, ref lastY);
            var row = new EventRow
            {
                Index = rows.Count + 1,
                Action = action,
                Value = value,
                TimestampMs = ev.TimestampMs,
                WaitMs = waitMs,
                Kind = ev.Kind
            };

            if (MatchInspectFilter(filter, row.Kind))
            {
                rows.Add(row);
            }

            prevTimestamp = ev.TimestampMs;
        }

        return rows;
    }

    private static bool MatchInspectFilter(string filter, string kind)
    {
        return filter switch
        {
            "Cliques" => kind is "mouse_down" or "mouse_up",
            "Movimento mouse" => kind == "mouse_move",
            "Teclado" => kind is "key_down" or "key_up",
            "Espera" => kind == "wait",
            _ => true
        };
    }

    private static string PrettyAction(MacroEvent ev)
    {
        return ev.Kind switch
        {
            "mouse_move" => "Mouse move",
            "mouse_down" => $"Mouse {ev.Data.GetValueOrDefault("button", "left").ToLowerInvariant()} down",
            "mouse_up" => $"Mouse {ev.Data.GetValueOrDefault("button", "left").ToLowerInvariant()} up",
            "mouse_wheel" => "Mouse wheel",
            "key_down" => "Key down",
            "key_up" => "Key up",
            _ => ev.Kind
        };
    }

    private static string PrettyValue(MacroEvent ev, ref int? lastX, ref int? lastY)
    {
        if (ev.Kind.StartsWith("mouse_", StringComparison.Ordinal))
        {
            var hasX = int.TryParse(ev.Data.GetValueOrDefault("x"), out var x);
            var hasY = int.TryParse(ev.Data.GetValueOrDefault("y"), out var y);

            if (ev.Kind == "mouse_move" && hasX && hasY)
            {
                var fromX = lastX ?? x;
                var fromY = lastY ?? y;
                lastX = x;
                lastY = y;
                return $"{fromX}, {fromY} -> {x}, {y}";
            }

            if ((ev.Kind == "mouse_down" || ev.Kind == "mouse_up") && hasX && hasY)
            {
                lastX = x;
                lastY = y;
                return $"{x}, {y}";
            }

            if (ev.Kind == "mouse_wheel")
            {
                return $"delta={ev.Data.GetValueOrDefault("delta", "0")}";
            }
        }

        if (ev.Kind is "key_down" or "key_up")
        {
            return ev.Data.GetValueOrDefault("key", "");
        }

        return string.Join(", ", ev.Data.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private async Task AnalyzeAsync()
    {
        if (!File.Exists(_macroPath.Text))
        {
            MessageBox.Show("Selecione um arquivo de macro válido.");
            return;
        }

        var macro = await MacroStorage.LoadAsync(_macroPath.Text);
        var parameters = ParseParameters(_params.Text);
        Log("Consultando Gemini...");
        var analysis = await _coach.AnalyzeAsync(macro, parameters, _model.Text.Trim());
        Log(analysis);
    }

    private static Dictionary<string, string> ParseParameters(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new();
        }

        raw = raw.Trim();
        if (raw.StartsWith("{"))
        {
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
            return dict ?? new();
        }

        var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2)
            {
                output[kv[0].Trim()] = kv[1].Trim();
            }
        }

        return output;
    }

    private static string Prompt(string title, string defaultValue)
    {
        using var form = new Form();
        var textLabel = new Label { Left = 10, Top = 20, Text = title, Width = 360 };
        var textBox = new TextBox { Left = 10, Top = 45, Width = 360, Text = defaultValue };
        var confirmation = new Button { Text = "OK", Left = 290, Width = 80, Top = 80, DialogResult = DialogResult.OK };
        form.Text = title;
        form.ClientSize = new System.Drawing.Size(390, 120);
        form.Controls.AddRange(new Control[] { textLabel, textBox, confirmation });
        form.AcceptButton = confirmation;

        return form.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
    }

    private sealed class EventRow
    {
        public int Index { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public long TimestampMs { get; set; }
        public long WaitMs { get; set; }
        public string Kind { get; set; } = string.Empty;
    }
}
