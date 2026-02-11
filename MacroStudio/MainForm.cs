using System.Text;

namespace MacroStudio;

public class MainForm : Form
{
    private readonly TextBox _macroPath = new() { Width = 690 };
    private readonly TextBox _params = new() { Width = 460, Text = "" };
    private readonly TextBox _speed = new() { Width = 70, Text = "1.0" };
    private readonly TextBox _stopKey = new() { Width = 70, Text = "F8" };
    private readonly TextBox _model = new() { Width = 170, Text = "gemini-2.0-flash" };
    private readonly TextBox _waitThreshold = new() { Width = 60, Text = "250" };

    private readonly TextBox _log = new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        Width = 1140,
        Height = 110,
        ReadOnly = true
    };

    private readonly ComboBox _inspectFilter = new()
    {
        Width = 170,
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    private readonly CheckBox _playMouseMoves = new() { Text = "Mouse moves", Checked = true, AutoSize = true };
    private readonly CheckBox _playMouseClicks = new() { Text = "Mouse clicks", Checked = true, AutoSize = true };
    private readonly CheckBox _playKeyPresses = new() { Text = "Key presses", Checked = true, AutoSize = true };
    private readonly CheckBox _playWaitTimes = new() { Text = "Wait times", Checked = true, AutoSize = true };

    private readonly CheckBox _compactView = new() { Text = "Compactar mouse moves", Checked = true, AutoSize = true };
    private readonly CheckBox _naturalView = new() { Text = "Visão natural (menos spam)", Checked = true, AutoSize = true };

    private readonly DataGridView _eventsGrid = new()
    {
        Width = 1140,
        Height = 390,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoGenerateColumns = false,
        RowHeadersVisible = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
    };

    private readonly MacroRecorderService _recorder = new();
    private readonly MacroPlayerService _player = new();
    private readonly GeminiCoachService _coach = new();
    private CancellationTokenSource? _playCts;
    private MacroFile? _currentMacro;

    public MainForm()
    {
        Text = "Macro Studio Professional";
        Width = 1180;
        Height = 860;
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
    }

    private void BuildUi()
    {
        var y = 10;

        var header = new Label
        {
            Left = 12,
            Top = y,
            Width = 800,
            Height = 24,
            Text = "Macro Studio • Record and Edit / Playback",
            Font = new Font(Font, FontStyle.Bold)
        };
        Controls.Add(header);
        y += 28;

        BuildMacroFileSection(ref y);
        BuildActionSection(ref y);
        BuildFilterSection(ref y);
        BuildGridSection(ref y);
        BuildLogSection(ref y);
    }

    private void BuildMacroFileSection(ref int y)
    {
        var group = new GroupBox { Left = 12, Top = y, Width = 1140, Height = 82, Text = "Arquivo da macro" };

        group.Controls.Add(new Label { Left = 12, Top = 26, Width = 140, Text = "Macro (.json)" });
        _macroPath.Left = 92;
        _macroPath.Top = 22;
        group.Controls.Add(_macroPath);

        var btnBrowse = new Button { Left = 790, Top = 20, Width = 95, Text = "Abrir" };
        var btnReload = new Button { Left = 892, Top = 20, Width = 95, Text = "Inspecionar" };
        var btnAi = new Button { Left = 994, Top = 20, Width = 130, Text = "Analisar IA" };

        btnBrowse.Click += (_, _) => BrowseMacro();
        btnReload.Click += async (_, _) => await LoadAndRenderMacroAsync();
        btnAi.Click += async (_, _) => await AnalyzeAsync();

        group.Controls.Add(btnBrowse);
        group.Controls.Add(btnReload);
        group.Controls.Add(btnAi);

        Controls.Add(group);
        y += group.Height + 8;
    }

    private void BuildActionSection(ref int y)
    {
        var group = new GroupBox { Left = 12, Top = y, Width = 1140, Height = 108, Text = "Record and Playback" };

        group.Controls.Add(new Label { Left = 12, Top = 27, Width = 90, Text = "Parâmetros" });
        _params.Left = 92;
        _params.Top = 23;
        group.Controls.Add(_params);

        group.Controls.Add(new Label { Left = 565, Top = 27, Width = 70, Text = "Speed" });
        _speed.Left = 614;
        _speed.Top = 23;
        group.Controls.Add(_speed);

        group.Controls.Add(new Label { Left = 695, Top = 27, Width = 70, Text = "Stop key" });
        _stopKey.Left = 754;
        _stopKey.Top = 23;
        group.Controls.Add(_stopKey);

        group.Controls.Add(new Label { Left = 835, Top = 27, Width = 60, Text = "Modelo" });
        _model.Left = 892;
        _model.Top = 23;
        group.Controls.Add(_model);

        var btnRecord = new Button { Left = 12, Top = 60, Width = 120, Text = "● Gravar" };
        var btnStopRecord = new Button { Left = 138, Top = 60, Width = 130, Text = "■ Parar Gravação" };
        var btnPlay = new Button { Left = 274, Top = 60, Width = 120, Text = "▶ Reproduzir" };
        var btnStopPlay = new Button { Left = 400, Top = 60, Width = 120, Text = "■ Parar Replay" };

        btnRecord.Click += async (_, _) => await StartRecordAsync();
        btnStopRecord.Click += async (_, _) => await StopRecordAsync();
        btnPlay.Click += async (_, _) => await PlayAsync();
        btnStopPlay.Click += (_, _) => StopPlay();

        group.Controls.Add(btnRecord);
        group.Controls.Add(btnStopRecord);
        group.Controls.Add(btnPlay);
        group.Controls.Add(btnStopPlay);

        _playMouseMoves.Left = 540;
        _playMouseMoves.Top = 64;
        _playMouseClicks.Left = 650;
        _playMouseClicks.Top = 64;
        _playKeyPresses.Left = 760;
        _playKeyPresses.Top = 64;
        _playWaitTimes.Left = 865;
        _playWaitTimes.Top = 64;

        group.Controls.Add(_playMouseMoves);
        group.Controls.Add(_playMouseClicks);
        group.Controls.Add(_playKeyPresses);
        group.Controls.Add(_playWaitTimes);

        Controls.Add(group);
        y += group.Height + 8;
    }

    private void BuildFilterSection(ref int y)
    {
        var group = new GroupBox { Left = 12, Top = y, Width = 1140, Height = 70, Text = "Inspeção inteligente" };

        group.Controls.Add(new Label { Left = 12, Top = 31, Width = 75, Text = "Filtro" });
        _inspectFilter.Left = 58;
        _inspectFilter.Top = 27;
        _inspectFilter.Items.AddRange(new object[] { "Tudo", "Cliques", "Movimento mouse", "Teclado", "Espera" });
        _inspectFilter.SelectedIndex = 0;
        _inspectFilter.SelectedIndexChanged += (_, _) => RefreshGrid();
        group.Controls.Add(_inspectFilter);

        _compactView.Left = 245;
        _compactView.Top = 29;
        _compactView.CheckedChanged += (_, _) => RefreshGrid();
        group.Controls.Add(_compactView);

        _naturalView.Left = 430;
        _naturalView.Top = 29;
        _naturalView.CheckedChanged += (_, _) => RefreshGrid();
        group.Controls.Add(_naturalView);

        group.Controls.Add(new Label { Left = 640, Top = 31, Width = 130, Text = "Wait mínimo (ms)" });
        _waitThreshold.Left = 736;
        _waitThreshold.Top = 27;
        _waitThreshold.TextChanged += (_, _) => RefreshGrid();
        group.Controls.Add(_waitThreshold);

        var btnRefresh = new Button { Left = 810, Top = 25, Width = 130, Text = "Atualizar tabela" };
        btnRefresh.Click += (_, _) => RefreshGrid();
        group.Controls.Add(btnRefresh);

        Controls.Add(group);
        y += group.Height + 8;
    }

    private void BuildGridSection(ref int y)
    {
        SetupGridColumns();
        _eventsGrid.Left = 12;
        _eventsGrid.Top = y;
        Controls.Add(_eventsGrid);
        y += _eventsGrid.Height + 8;
    }

    private void BuildLogSection(ref int y)
    {
        _log.Left = 12;
        _log.Top = y;
        Controls.Add(_log);
    }

    private void SetupGridColumns()
    {
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", DataPropertyName = nameof(EventRow.Index), Width = 46 });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Action", DataPropertyName = nameof(EventRow.Action), Width = 225 });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value", DataPropertyName = nameof(EventRow.Value), Width = 400 });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Timestamp (ms)", DataPropertyName = nameof(EventRow.TimestampMs), Width = 120 });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Wait (ms)", DataPropertyName = nameof(EventRow.WaitMs), Width = 105 });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kind", DataPropertyName = nameof(EventRow.Kind), Width = 220 });
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
        Log($"Macro salva em {_macroPath.Text} ({macro.Events.Count} eventos). O modo natural já reduz spam visual.");
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
        Log($"Inspeção carregada: {_currentMacro.Name}, {_currentMacro.Events.Count} eventos brutos.");
    }

    private void RefreshGrid()
    {
        if (_currentMacro is null)
        {
            _eventsGrid.DataSource = null;
            return;
        }

        var options = new InspectRenderOptions
        {
            Filter = _inspectFilter.SelectedItem?.ToString() ?? "Tudo",
            CompactMoves = _compactView.Checked,
            NaturalView = _naturalView.Checked,
            MinWaitMs = int.TryParse(_waitThreshold.Text, out var wait) ? Math.Max(wait, 0) : 250
        };

        var rows = BuildRows(_currentMacro, options);
        _eventsGrid.DataSource = rows;
    }

    private static List<EventRow> BuildRows(MacroFile macro, InspectRenderOptions options)
    {
        var rows = new List<EventRow>();
        long prevTimestamp = 0;
        int? lastX = null;
        int? lastY = null;

        MoveAggregation? moveAggregation = null;

        for (var i = 0; i < macro.Events.Count; i++)
        {
            var ev = macro.Events[i];
            var waitMs = Math.Max(ev.TimestampMs - prevTimestamp, 0);

            if (options.CompactMoves && IsMouseMove(ev, out var moveX, out var moveY))
            {
                var startX = lastX ?? moveX;
                var startY = lastY ?? moveY;

                if (moveAggregation is null)
                {
                    moveAggregation = new MoveAggregation
                    {
                        StartX = startX,
                        StartY = startY,
                        EndX = moveX,
                        EndY = moveY,
                        TotalWaitMs = waitMs,
                        LastTimestampMs = ev.TimestampMs,
                        Count = 1,
                        Distance = Math.Abs(moveX - startX) + Math.Abs(moveY - startY)
                    };
                }
                else
                {
                    moveAggregation.Distance += Math.Abs(moveX - moveAggregation.EndX) + Math.Abs(moveY - moveAggregation.EndY);
                    moveAggregation.EndX = moveX;
                    moveAggregation.EndY = moveY;
                    moveAggregation.TotalWaitMs += waitMs;
                    moveAggregation.LastTimestampMs = ev.TimestampMs;
                    moveAggregation.Count += 1;
                }

                lastX = moveX;
                lastY = moveY;
                prevTimestamp = ev.TimestampMs;
                continue;
            }

            FlushMoveAggregation(rows, options, moveAggregation);
            moveAggregation = null;

            AddWaitRowIfRelevant(rows, options, ev.TimestampMs, waitMs);

            var action = PrettyAction(ev);
            var value = PrettyValue(ev, ref lastX, ref lastY);
            var row = new EventRow
            {
                Action = action,
                Value = value,
                TimestampMs = ev.TimestampMs,
                WaitMs = waitMs,
                Kind = ev.Kind
            };

            if (MatchInspectFilter(options.Filter, row.Kind))
            {
                rows.Add(row);
            }

            prevTimestamp = ev.TimestampMs;
        }

        FlushMoveAggregation(rows, options, moveAggregation);
        ReindexRows(rows);
        return rows;
    }

    private static bool IsMouseMove(MacroEvent ev, out int x, out int y)
    {
        x = 0;
        y = 0;
        return ev.Kind == "mouse_move"
               && int.TryParse(ev.Data.GetValueOrDefault("x"), out x)
               && int.TryParse(ev.Data.GetValueOrDefault("y"), out y);
    }

    private static void AddWaitRowIfRelevant(List<EventRow> rows, InspectRenderOptions options, long timestampMs, long waitMs)
    {
        if (waitMs <= 0)
        {
            return;
        }

        if (options.NaturalView && waitMs < options.MinWaitMs)
        {
            return;
        }

        if (rows.Count > 0 && rows[^1].Kind == "wait")
        {
            rows[^1].WaitMs += waitMs;
            rows[^1].Value = $"{rows[^1].WaitMs} ms";
            rows[^1].TimestampMs = timestampMs;
            return;
        }

        var waitRow = new EventRow
        {
            Action = "Wait",
            Value = $"{waitMs} ms",
            TimestampMs = timestampMs,
            WaitMs = waitMs,
            Kind = "wait"
        };

        if (MatchInspectFilter(options.Filter, waitRow.Kind))
        {
            rows.Add(waitRow);
        }
    }

    private static void FlushMoveAggregation(List<EventRow> rows, InspectRenderOptions options, MoveAggregation? aggregation)
    {
        if (aggregation is null)
        {
            return;
        }

        var isRelevantMove = !options.NaturalView || aggregation.Count >= 3 || aggregation.Distance >= 20;
        if (!isRelevantMove)
        {
            return;
        }

        if (aggregation.TotalWaitMs > 0)
        {
            AddWaitRowIfRelevant(rows, options, aggregation.LastTimestampMs, aggregation.TotalWaitMs);
        }

        var moveRow = new EventRow
        {
            Action = aggregation.Count > 1 ? $"Mouse move (x{aggregation.Count})" : "Mouse move",
            Value = $"{aggregation.StartX}, {aggregation.StartY} -> {aggregation.EndX}, {aggregation.EndY}",
            TimestampMs = aggregation.LastTimestampMs,
            WaitMs = aggregation.TotalWaitMs,
            Kind = "mouse_move_group"
        };

        if (MatchInspectFilter(options.Filter, moveRow.Kind))
        {
            rows.Add(moveRow);
        }
    }

    private static void ReindexRows(List<EventRow> rows)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Index = i + 1;
        }
    }

    private static bool MatchInspectFilter(string filter, string kind)
    {
        return filter switch
        {
            "Cliques" => kind is "mouse_down" or "mouse_up",
            "Movimento mouse" => kind is "mouse_move" or "mouse_move_group",
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

    private sealed class InspectRenderOptions
    {
        public string Filter { get; init; } = "Tudo";
        public bool CompactMoves { get; init; }
        public bool NaturalView { get; init; }
        public int MinWaitMs { get; init; }
    }

    private sealed class MoveAggregation
    {
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
        public long TotalWaitMs { get; set; }
        public long LastTimestampMs { get; set; }
        public int Count { get; set; }
        public int Distance { get; set; }
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
