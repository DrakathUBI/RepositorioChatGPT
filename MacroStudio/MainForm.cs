using System.Text;

namespace MacroStudio;

public class MainForm : Form
{
    private readonly TextBox _macroPath = new() { Width = 720 };
    private readonly TextBox _params = new() { Width = 520, Text = "" };
    private readonly TextBox _speed = new() { Width = 80, Text = "1.0" };
    private readonly TextBox _stopKey = new() { Width = 80, Text = "F8" };
    private readonly TextBox _model = new() { Width = 180, Text = "gemini-2.0-flash" };
    private readonly TextBox _waitThreshold = new() { Width = 70, Text = "250" };
    private readonly TextBox _loopCount = new() { Width = 60, Text = "1" };
    private readonly TextBox _startDelayMs = new() { Width = 80, Text = "0" };
    private readonly TextBox _jitterMs = new() { Width = 80, Text = "0" };
    private readonly ComboBox _speedPreset = new() { Width = 126, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _variableName = new() { Width = 100, Text = "periodo" };
    private readonly TextBox _variableValues = new() { Width = 520, Text = "" };

    private readonly TextBox _log = new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        Width = 1140,
        Height = 110,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        BackColor = Color.White
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
    private readonly CheckBox _editMode = new() { Text = "Modo edição (ações reais)", Checked = false, AutoSize = true };

    private readonly ComboBox _designerActionType = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _designerValue = new() { Width = 700, Text = "" };
    private readonly TextBox _designerLabel = new() { Width = 160, Text = "" };
    private readonly TextBox _designerComment = new() { Width = 340, Text = "" };

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
        BorderStyle = BorderStyle.None,
        GridColor = Color.FromArgb(229, 231, 235)
    };

    private readonly MacroRecorderService _recorder = new();
    private readonly MacroPlayerService _player = new();
    private readonly GeminiCoachService _coach = new();
    private CancellationTokenSource? _playCts;
    private MacroFile? _currentMacro;
    private bool _hasUnsavedChanges;
    private Label? _statusLabel;
    private MacroFile? _lastReplayMacro;
    private Dictionary<string, string> _lastReplayParameters = new();
    private double _lastReplaySpeed = 1.0;
    private PlaybackFilterOptions _lastReplayOptions = new();

    public MainForm()
    {
        Text = "Macro Studio Professional";
        Width = 1220;
        Height = 900;
        MinimumSize = new Size(1220, 900);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        DoubleBuffered = true;
        BackColor = Color.FromArgb(239, 242, 247);
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        BuildUi();
    }

    private void BuildUi()
    {
        Controls.Clear();

        var hero = new Panel
        {
            Left = 16,
            Top = 14,
            Width = 1144,
            Height = 82,
            BackColor = Color.FromArgb(15, 23, 42)
        };

        var title = new Label
        {
            Left = 18,
            Top = 14,
            Width = 520,
            Height = 30,
            Text = "Macro Studio • Pro Console",
            Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold),
            ForeColor = Color.White
        };

        var subtitle = new Label
        {
            Left = 20,
            Top = 46,
            Width = 820,
            Height = 20,
            Text = "Visual avançado com timeline inteligente, filtros e execução controlada por teclado.",
            ForeColor = Color.FromArgb(191, 219, 254)
        };

        _statusLabel = new Label
        {
            Left = 860,
            Top = 28,
            Width = 260,
            Height = 30,
            Text = "Status: pronto",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(30, 64, 175)
        };

        hero.Controls.Add(title);
        hero.Controls.Add(subtitle);
        hero.Controls.Add(_statusLabel);
        Controls.Add(hero);

        var tabs = new TabControl
        {
            Left = 16,
            Top = hero.Bottom + 10,
            Width = 1144,
            Height = 760,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        var playbackPage = new TabPage("Gravação e Replay") { BackColor = Color.FromArgb(239, 242, 247), AutoScroll = true };
        var inspectorPage = new TabPage("Inspeção e Edição") { BackColor = Color.FromArgb(239, 242, 247), AutoScroll = true };
        var designerPage = new TabPage("Designer de Ações") { BackColor = Color.FromArgb(239, 242, 247), AutoScroll = true };

        tabs.TabPages.Add(playbackPage);
        tabs.TabPages.Add(inspectorPage);
        tabs.TabPages.Add(designerPage);
        Controls.Add(tabs);

        var yPlayback = 12;
        BuildMacroFileSection(playbackPage, ref yPlayback);
        BuildActionSection(playbackPage, ref yPlayback);
        BuildShortcutSection(playbackPage, ref yPlayback);
        BuildLogSection(playbackPage, ref yPlayback);

        var yInspector = 12;
        BuildFilterSection(inspectorPage, ref yInspector);
        BuildGridSection(inspectorPage, ref yInspector);

        var yDesigner = 12;
        BuildMacroDesignerSection(designerPage, ref yDesigner);
    }

    private Panel CreateCard(Control host, string title, int y, int height)
    {
        var card = new Panel
        {
            Left = 16,
            Top = y,
            Width = 1144,
            Height = height,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var accent = new Panel
        {
            Left = 0,
            Top = 0,
            Width = 6,
            Height = card.Height,
            BackColor = Color.FromArgb(37, 99, 235)
        };

        var header = new Label
        {
            Left = 16,
            Top = 10,
            Width = 720,
            Height = 20,
            Text = title,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39)
        };

        card.Controls.Add(accent);
        card.Controls.Add(header);
        host.Controls.Add(card);
        return card;
    }

    private static void StyleInput(Control c)
    {
        if (c is TextBox tb)
        {
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.BackColor = Color.FromArgb(249, 250, 251);
            tb.ForeColor = Color.FromArgb(17, 24, 39);
        }

        c.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    }

    private static Button CreatePrimaryButton(string text, int left, int top, int width)
    {
        var btn = new Button
        {
            Left = left,
            Top = top,
            Width = width,
            Height = 34,
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        btn.FlatAppearance.BorderSize = 0;
        btn.MouseEnter += (_, _) => btn.BackColor = Color.FromArgb(29, 78, 216);
        btn.MouseLeave += (_, _) => btn.BackColor = Color.FromArgb(37, 99, 235);
        return btn;
    }

    private static Button CreateGhostButton(string text, int left, int top, int width)
    {
        var btn = new Button
        {
            Left = left,
            Top = top,
            Width = width,
            Height = 34,
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(243, 244, 246),
            ForeColor = Color.FromArgb(31, 41, 55),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            Cursor = Cursors.Hand
        };

        btn.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
        btn.FlatAppearance.BorderSize = 1;
        btn.MouseEnter += (_, _) => btn.BackColor = Color.FromArgb(229, 231, 235);
        btn.MouseLeave += (_, _) => btn.BackColor = Color.FromArgb(243, 244, 246);
        return btn;
    }

    private static void StyleToggle(CheckBox check)
    {
        check.ForeColor = Color.FromArgb(55, 65, 81);
        check.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        check.BackColor = Color.Transparent;
        check.FlatStyle = FlatStyle.Flat;
    }

    private void BuildMacroFileSection(Control host, ref int y)
    {
        var card = CreateCard(host, "Arquivo da macro", y, 92);

        card.Controls.Add(new Label { Left = 14, Top = 44, Width = 96, Text = "Macro (.json)", ForeColor = Color.FromArgb(75, 85, 99) });

        _macroPath.Left = 106;
        _macroPath.Top = 40;
        StyleInput(_macroPath);
        card.Controls.Add(_macroPath);

        var btnBrowse = CreateGhostButton("Abrir", 835, 36, 90);
        var btnReload = CreateGhostButton("Inspecionar", 932, 36, 100);
        var btnAi = CreatePrimaryButton("Analisar IA", 1038, 36, 95);

        btnBrowse.Click += (_, _) => BrowseMacro();
        btnReload.Click += async (_, _) => await LoadAndRenderMacroAsync();
        btnAi.Click += async (_, _) => await AnalyzeAsync();

        card.Controls.Add(btnBrowse);
        card.Controls.Add(btnReload);
        card.Controls.Add(btnAi);

        y += card.Height + 10;
    }

    private void BuildActionSection(Control host, ref int y)
    {
        var card = CreateCard(host, "Record and Playback", y, 202);

        card.Controls.Add(new Label { Left = 14, Top = 44, Width = 86, Text = "Parâmetros", ForeColor = Color.FromArgb(75, 85, 99) });
        _params.Left = 105;
        _params.Top = 40;
        StyleInput(_params);
        card.Controls.Add(_params);

        card.Controls.Add(new Label { Left = 634, Top = 44, Width = 50, Text = "Speed", ForeColor = Color.FromArgb(75, 85, 99) });
        _speed.Left = 680;
        _speed.Top = 40;
        StyleInput(_speed);
        card.Controls.Add(_speed);

        card.Controls.Add(new Label { Left = 770, Top = 44, Width = 56, Text = "Stop key", ForeColor = Color.FromArgb(75, 85, 99) });
        _stopKey.Left = 830;
        _stopKey.Top = 40;
        StyleInput(_stopKey);
        card.Controls.Add(_stopKey);

        card.Controls.Add(new Label { Left = 922, Top = 44, Width = 50, Text = "Modelo", ForeColor = Color.FromArgb(75, 85, 99) });
        _model.Left = 980;
        _model.Top = 40;
        StyleInput(_model);
        card.Controls.Add(_model);
        card.Controls.Add(new Label { Left = 14, Top = 82, Width = 74, Text = "Preset", ForeColor = Color.FromArgb(75, 85, 99) });
        _speedPreset.Left = 88;
        _speedPreset.Top = 78;
        _speedPreset.Items.Clear();
        _speedPreset.Items.AddRange(new object[] { "Custom", "Lento (0.5x)", "Normal (1x)", "Rápido (2x)", "Muito rápido (4x)" });
        _speedPreset.SelectedIndex = 2;
        _speedPreset.SelectedIndexChanged += (_, _) => ApplySpeedPreset();
        card.Controls.Add(_speedPreset);

        card.Controls.Add(new Label { Left = 230, Top = 82, Width = 70, Text = "Loops", ForeColor = Color.FromArgb(75, 85, 99) });
        _loopCount.Left = 280;
        _loopCount.Top = 78;
        StyleInput(_loopCount);
        card.Controls.Add(_loopCount);

        card.Controls.Add(new Label { Left = 350, Top = 82, Width = 95, Text = "Delay inicial", ForeColor = Color.FromArgb(75, 85, 99) });
        _startDelayMs.Left = 436;
        _startDelayMs.Top = 78;
        StyleInput(_startDelayMs);
        card.Controls.Add(_startDelayMs);

        card.Controls.Add(new Label { Left = 525, Top = 82, Width = 90, Text = "Jitter (ms)", ForeColor = Color.FromArgb(75, 85, 99) });
        _jitterMs.Left = 606;
        _jitterMs.Top = 78;
        StyleInput(_jitterMs);
        card.Controls.Add(_jitterMs);

        card.Controls.Add(new Label { Left = 14, Top = 118, Width = 106, Text = "Nome variável", ForeColor = Color.FromArgb(75, 85, 99) });
        _variableName.Left = 120;
        _variableName.Top = 114;
        StyleInput(_variableName);
        card.Controls.Add(_variableName);

        card.Controls.Add(new Label { Left = 232, Top = 118, Width = 174, Text = "Valores CSV (loop automático)", ForeColor = Color.FromArgb(75, 85, 99) });
        _variableValues.Left = 406;
        _variableValues.Top = 114;
        StyleInput(_variableValues);
        card.Controls.Add(_variableValues);

        var btnRecord = CreatePrimaryButton("● Gravar", 700, 114, 110);
        var btnStopRecord = CreateGhostButton("■ Parar Gravação", 816, 114, 136);
        var btnPlay = CreatePrimaryButton("▶ Reproduzir", 958, 114, 90);
        var btnStopPlay = CreateGhostButton("■ Parar", 1052, 114, 80);

        btnRecord.Click += async (_, _) => await StartRecordAsync();
        btnStopRecord.Click += async (_, _) => await StopRecordAsync();
        btnPlay.Click += async (_, _) => await PlayAsync();
        btnStopPlay.Click += (_, _) => StopPlay();

        card.Controls.Add(btnRecord);
        card.Controls.Add(btnStopRecord);
        card.Controls.Add(btnPlay);
        card.Controls.Add(btnStopPlay);

        _playMouseMoves.Left = 700;
        _playMouseMoves.Top = 120;
        _playMouseClicks.Left = 796;
        _playMouseClicks.Top = 120;
        _playKeyPresses.Left = 896;
        _playKeyPresses.Top = 120;
        _playWaitTimes.Left = 1002;
        _playWaitTimes.Top = 120;

        StyleToggle(_playMouseMoves);
        StyleToggle(_playMouseClicks);
        StyleToggle(_playKeyPresses);
        StyleToggle(_playWaitTimes);

        card.Controls.Add(_playMouseMoves);
        card.Controls.Add(_playMouseClicks);
        card.Controls.Add(_playKeyPresses);
        card.Controls.Add(_playWaitTimes);

        y += card.Height + 10;
    }

    private void BuildShortcutSection(Control host, ref int y)
    {
        var card = CreateCard(host, "Painel de atalhos", y, 134);

        var hint = new Label
        {
            Left = 14,
            Top = 40,
            Width = 1110,
            Height = 78,
            ForeColor = Color.FromArgb(55, 65, 81),
            Text = "Ctrl+S/Ctrl+P: reproduzir   |   Ctrl+Shift+S: salvar macro   |   Ctrl+O: abrir arquivo\n"
                 + "Ctrl+I: inspecionar macro   |   Ctrl+R: iniciar gravação   |   Ctrl+Shift+R: parar gravação\n"
                 + "Ctrl+Shift+P ou ESC: parar replay   |   Ctrl+Alt+P: repetir último replay"
        };

        card.Controls.Add(hint);
        y += card.Height + 10;
    }

    private void BuildMacroDesignerSection(Control host, ref int y)
    {
        var card = CreateCard(host, "Construtor visual de ações", y, 212);

        card.Controls.Add(new Label { Left = 14, Top = 44, Width = 90, Text = "Tipo ação", ForeColor = Color.FromArgb(75, 85, 99) });
        _designerActionType.Left = 102;
        _designerActionType.Top = 40;
        _designerActionType.Items.Clear();
        _designerActionType.Items.AddRange(new object[]
        {
            "Text input",
            "Wait",
            "Key down",
            "Key up",
            "Mouse left click"
        });
        _designerActionType.SelectedIndex = 0;
        card.Controls.Add(_designerActionType);

        card.Controls.Add(new Label { Left = 336, Top = 44, Width = 48, Text = "Valor", ForeColor = Color.FromArgb(75, 85, 99) });
        _designerValue.Left = 384;
        _designerValue.Top = 40;
        StyleInput(_designerValue);
        card.Controls.Add(_designerValue);

        card.Controls.Add(new Label { Left = 14, Top = 84, Width = 90, Text = "Label", ForeColor = Color.FromArgb(75, 85, 99) });
        _designerLabel.Left = 102;
        _designerLabel.Top = 80;
        StyleInput(_designerLabel);
        card.Controls.Add(_designerLabel);

        card.Controls.Add(new Label { Left = 276, Top = 84, Width = 74, Text = "Comentário", ForeColor = Color.FromArgb(75, 85, 99) });
        _designerComment.Left = 350;
        _designerComment.Top = 80;
        StyleInput(_designerComment);
        card.Controls.Add(_designerComment);

        var btnInsert = CreatePrimaryButton("Inserir ação", 14, 124, 126);
        var btnDuplicate = CreateGhostButton("Duplicar selecionada", 146, 124, 154);
        var btnMoveUp = CreateGhostButton("Mover acima", 306, 124, 110);
        var btnMoveDown = CreateGhostButton("Mover abaixo", 422, 124, 112);
        var btnDelete = CreateGhostButton("Excluir selecionada", 540, 124, 140);

        btnInsert.Click += (_, _) => InsertDesignedAction();
        btnDuplicate.Click += (_, _) => DuplicateSelectedAction();
        btnMoveUp.Click += (_, _) => MoveSelectedAction(-1);
        btnMoveDown.Click += (_, _) => MoveSelectedAction(1);
        btnDelete.Click += (_, _) => DeleteSelectedAction();

        card.Controls.Add(btnInsert);
        card.Controls.Add(btnDuplicate);
        card.Controls.Add(btnMoveUp);
        card.Controls.Add(btnMoveDown);
        card.Controls.Add(btnDelete);

        card.Controls.Add(new Label
        {
            Left = 14,
            Top = 168,
            Width = 1110,
            Height = 34,
            ForeColor = Color.FromArgb(75, 85, 99),
            Text = "Dica: para variável, use no texto: {{periodo}}. Para loop por lista, preencha Nome variável e Valores CSV na aba Gravação e Replay."
        });

        y += card.Height + 10;
    }

    private void BuildFilterSection(Control host, ref int y)
    {
        var card = CreateCard(host, "Inspeção inteligente", y, 124);

        card.Controls.Add(new Label { Left = 14, Top = 44, Width = 50, Text = "Filtro", ForeColor = Color.FromArgb(75, 85, 99) });
        _inspectFilter.Left = 64;
        _inspectFilter.Top = 40;
        _inspectFilter.Items.AddRange(new object[] { "Tudo", "Cliques", "Movimento mouse", "Teclado", "Espera" });
        _inspectFilter.SelectedIndex = 0;
        _inspectFilter.SelectedIndexChanged += (_, _) => RefreshGrid();
        card.Controls.Add(_inspectFilter);

        _compactView.Left = 245;
        _compactView.Top = 43;
        _compactView.CheckedChanged += (_, _) => RefreshGrid();
        StyleToggle(_compactView);
        card.Controls.Add(_compactView);

        _naturalView.Left = 434;
        _naturalView.Top = 43;
        _naturalView.CheckedChanged += (_, _) => RefreshGrid();
        StyleToggle(_naturalView);
        card.Controls.Add(_naturalView);

        _editMode.Left = 638;
        _editMode.Top = 43;
        _editMode.CheckedChanged += (_, _) => RefreshGrid();
        StyleToggle(_editMode);
        card.Controls.Add(_editMode);

        card.Controls.Add(new Label { Left = 845, Top = 44, Width = 116, Text = "Wait mínimo (ms)", ForeColor = Color.FromArgb(75, 85, 99) });
        _waitThreshold.Left = 952;
        _waitThreshold.Top = 40;
        _waitThreshold.TextChanged += (_, _) => RefreshGrid();
        StyleInput(_waitThreshold);
        card.Controls.Add(_waitThreshold);

        var btnRefresh = CreateGhostButton("Atualizar", 1030, 36, 102);
        btnRefresh.Click += (_, _) => RefreshGrid();
        card.Controls.Add(btnRefresh);

        var btnDeleteSelected = CreateGhostButton("Excluir selecionada", 14, 78, 150);
        var btnDeleteFiltered = CreateGhostButton("Excluir filtradas", 170, 78, 130);
        var btnSaveEdits = CreatePrimaryButton("Salvar alterações", 306, 78, 150);

        btnDeleteSelected.Click += (_, _) => DeleteSelectedAction();
        btnDeleteFiltered.Click += (_, _) => DeleteFilteredActions();
        btnSaveEdits.Click += async (_, _) => await SaveCurrentMacroAsync();

        card.Controls.Add(btnDeleteSelected);
        card.Controls.Add(btnDeleteFiltered);
        card.Controls.Add(btnSaveEdits);

        y += card.Height + 10;
    }

    private void BuildGridSection(Control host, ref int y)
    {
        var card = CreateCard(host, "Timeline da macro", y, 430);
        SetupGridColumns();

        _eventsGrid.Left = 2;
        _eventsGrid.Top = 34;
        card.Controls.Add(_eventsGrid);

        y += card.Height + 10;
    }

    private void BuildLogSection(Control host, ref int y)
    {
        var card = CreateCard(host, "Log", y, 154);
        _log.Left = 2;
        _log.Top = 34;
        card.Controls.Add(_log);
    }

    private void SetupGridColumns()
    {
        _eventsGrid.EnableHeadersVisualStyles = false;
        _eventsGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        _eventsGrid.ColumnHeadersHeight = 34;
        _eventsGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
        _eventsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _eventsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        _eventsGrid.DefaultCellStyle.BackColor = Color.White;
        _eventsGrid.DefaultCellStyle.ForeColor = Color.FromArgb(31, 41, 55);
        _eventsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        _eventsGrid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 58, 138);
        _eventsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        _eventsGrid.RowTemplate.Height = 32;

        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Index", HeaderText = "#", DataPropertyName = nameof(EventRow.Index), Width = 44, ReadOnly = true });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "Action", DataPropertyName = nameof(EventRow.Action), Width = 240, ReadOnly = true });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "Value", DataPropertyName = nameof(EventRow.Value), Width = 356 });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TimestampMs", HeaderText = "Timestamp (ms)", DataPropertyName = nameof(EventRow.TimestampMs), Width = 120, ReadOnly = true });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "WaitMs", HeaderText = "Wait (ms)", DataPropertyName = nameof(EventRow.WaitMs), Width = 100 });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawIdx", HeaderText = "RawIdx", DataPropertyName = nameof(EventRow.SourceEventIndex), Width = 76, ReadOnly = true });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Kind", HeaderText = "Kind", DataPropertyName = nameof(EventRow.Kind), Width = 180, ReadOnly = true });

        _eventsGrid.CellEndEdit += (_, e) => ApplyGridEdit(e.RowIndex, e.ColumnIndex);
    }

    private void Log(string message)
    {
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        if (_statusLabel is not null)
        {
            _statusLabel.Text = $"Status: {message}";
        }
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
        _hasUnsavedChanges = false;
        RefreshGrid();
        Log($"Macro salva em {_macroPath.Text} ({macro.Events.Count} eventos). Edição direta habilitada.");
    }

    private async Task PlayAsync()
    {
        if (_playCts is not null && !_playCts.IsCancellationRequested)
        {
            Log("Replay já está em execução.");
            return;
        }

        var macro = await ResolveMacroForPlaybackAsync();
        if (macro is null)
        {
            MessageBox.Show("Selecione ou carregue uma macro válida.");
            return;
        }

        var parameters = ParseParameters(_params.Text);
        var speed = double.TryParse(_speed.Text, out var s) ? s : 1.0;

        _playCts = new CancellationTokenSource();
        var playbackOptions = new PlaybackFilterOptions
        {
            PlayMouseMoves = _playMouseMoves.Checked,
            PlayMouseClicks = _playMouseClicks.Checked,
            PlayKeyPresses = _playKeyPresses.Checked,
            RespectWaitTimes = _playWaitTimes.Checked,
            LoopCount = ParsePositiveInt(_loopCount.Text, 1),
            StartDelayMs = ParseNonNegativeInt(_startDelayMs.Text, 0),
            DelayJitterMs = ParseNonNegativeInt(_jitterMs.Text, 0)
        };

        var variableName = _variableName.Text.Trim();
        var variableValues = ParseCsvValues(_variableValues.Text);

        var selectedRowStart = _eventsGrid.SelectedRows.Count > 0
            ? _eventsGrid.SelectedRows[0].DataBoundItem as EventRow
            : null;

        if (selectedRowStart?.SourceEventIndex is int sourceStart && sourceStart > 0)
        {
            macro = new MacroFile
            {
                Name = macro.Name,
                CreatedAt = macro.CreatedAt,
                Metadata = new Dictionary<string, string>(macro.Metadata),
                Events = macro.Events.Skip(sourceStart).ToList()
            };
            Log($"Replay parcial a partir da linha selecionada (RawIdx={sourceStart}).");
        }

        Log($"Reproduzindo {macro.Name} com {macro.Events.Count} eventos{(_hasUnsavedChanges ? " (edições locais)" : "")}...");

        try
        {
            _lastReplayMacro = new MacroFile
            {
                Name = macro.Name,
                CreatedAt = macro.CreatedAt,
                Metadata = new Dictionary<string, string>(macro.Metadata),
                Events = macro.Events.Select(ev => new MacroEvent
                {
                    Kind = ev.Kind,
                    TimestampMs = ev.TimestampMs,
                    Data = new Dictionary<string, string>(ev.Data)
                }).ToList()
            };
            _lastReplayParameters = new Dictionary<string, string>(parameters);
            _lastReplaySpeed = speed;
            _lastReplayOptions = playbackOptions;

            if (variableValues.Count > 0 && !string.IsNullOrWhiteSpace(variableName))
            {
                var sequenceOptions = new PlaybackFilterOptions
                {
                    PlayMouseMoves = playbackOptions.PlayMouseMoves,
                    PlayMouseClicks = playbackOptions.PlayMouseClicks,
                    PlayKeyPresses = playbackOptions.PlayKeyPresses,
                    RespectWaitTimes = playbackOptions.RespectWaitTimes,
                    LoopCount = 1,
                    StartDelayMs = playbackOptions.StartDelayMs,
                    DelayJitterMs = playbackOptions.DelayJitterMs
                };

                _lastReplayOptions = sequenceOptions;

                for (var i = 0; i < variableValues.Count; i++)
                {
                    _playCts.Token.ThrowIfCancellationRequested();
                    var loopParameters = new Dictionary<string, string>(parameters)
                    {
                        [variableName] = variableValues[i]
                    };

                    _lastReplayParameters = new Dictionary<string, string>(loopParameters);
                    Log($"Loop variável {i + 1}/{variableValues.Count}: {variableName}={variableValues[i]}");
                    await _player.PlayAsync(macro, loopParameters, speed, _playCts.Token, sequenceOptions);
                }

                Log("Replay concluído (todas variáveis processadas).");
            }
            else
            {
                await _player.PlayAsync(
                    macro,
                    parameters,
                    speed,
                    _playCts.Token,
                    playbackOptions
                );
                Log("Replay concluído.");
            }
        }
        catch (OperationCanceledException)
        {
            Log("Replay interrompido pelo usuário.");
        }
        finally
        {
            _playCts?.Dispose();
            _playCts = null;
        }
    }

    private void ApplySpeedPreset()
    {
        var value = _speedPreset.SelectedItem?.ToString() ?? "Custom";
        _speed.Text = value switch
        {
            "Lento (0.5x)" => "0.5",
            "Normal (1x)" => "1.0",
            "Rápido (2x)" => "2.0",
            "Muito rápido (4x)" => "4.0",
            _ => _speed.Text
        };
    }

    private static int ParsePositiveInt(string raw, int fallback)
    {
        return int.TryParse(raw, out var value) && value > 0 ? value : fallback;
    }

    private static int ParseNonNegativeInt(string raw, int fallback)
    {
        return int.TryParse(raw, out var value) && value >= 0 ? value : fallback;
    }

    private static List<string> ParseCsvValues(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new();
        }

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private async Task<MacroFile?> ResolveMacroForPlaybackAsync()
    {
        if (_currentMacro is not null)
        {
            return _currentMacro;
        }

        if (!File.Exists(_macroPath.Text))
        {
            return null;
        }

        _currentMacro = await MacroStorage.LoadAsync(_macroPath.Text);
        _hasUnsavedChanges = false;
        return _currentMacro;
    }

    private void StopPlay()
    {
        _playCts?.Cancel();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape && _playCts is not null && !_playCts.IsCancellationRequested)
        {
            StopPlay();
            Log("Replay interrompido via ESC.");
            return true;
        }

        if (keyData == (Keys.Control | Keys.S))
        {
            _ = InvokeShortcutAsync(PlayAsync);
            return true;
        }

        if (keyData == (Keys.Control | Keys.Shift | Keys.S))
        {
            _ = InvokeShortcutAsync(SaveCurrentMacroAsync);
            return true;
        }

        if (keyData == (Keys.Control | Keys.O))
        {
            BrowseMacro();
            return true;
        }

        if (keyData == (Keys.Control | Keys.I))
        {
            _ = InvokeShortcutAsync(LoadAndRenderMacroAsync);
            return true;
        }

        if (keyData == (Keys.Control | Keys.R))
        {
            _ = InvokeShortcutAsync(StartRecordAsync);
            return true;
        }

        if (keyData == (Keys.Control | Keys.Shift | Keys.R))
        {
            _ = InvokeShortcutAsync(StopRecordAsync);
            return true;
        }

        if (keyData == (Keys.Control | Keys.P))
        {
            _ = InvokeShortcutAsync(PlayAsync);
            return true;
        }

        if (keyData == (Keys.Control | Keys.Shift | Keys.P))
        {
            StopPlay();
            return true;
        }

        if (keyData == (Keys.Control | Keys.Alt | Keys.P))
        {
            _ = InvokeShortcutAsync(ReplayLastAsync);
            return true;
        }

        if (keyData == (Keys.Control | Keys.D))
        {
            DuplicateSelectedAction();
            return true;
        }

        if (keyData == (Keys.Alt | Keys.Up))
        {
            MoveSelectedAction(-1);
            return true;
        }

        if (keyData == (Keys.Alt | Keys.Down))
        {
            MoveSelectedAction(1);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private async Task InvokeShortcutAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Log($"Erro no atalho: {ex.Message}");
        }
    }

    private async Task ReplayLastAsync()
    {
        if (_lastReplayMacro is null)
        {
            Log("Ainda não existe último replay para repetir.");
            return;
        }

        if (_playCts is not null && !_playCts.IsCancellationRequested)
        {
            Log("Replay já está em execução.");
            return;
        }

        _playCts = new CancellationTokenSource();
        Log($"Repetindo último replay: {_lastReplayMacro.Name}...");

        try
        {
            await _player.PlayAsync(_lastReplayMacro, _lastReplayParameters, _lastReplaySpeed, _playCts.Token, _lastReplayOptions);
            Log("Repetição concluída.");
        }
        catch (OperationCanceledException)
        {
            Log("Repetição interrompida pelo usuário.");
        }
        finally
        {
            _playCts?.Dispose();
            _playCts = null;
        }
    }

    private async Task LoadAndRenderMacroAsync()
    {
        if (!File.Exists(_macroPath.Text))
        {
            MessageBox.Show("Selecione um arquivo de macro válido.");
            return;
        }

        _currentMacro = await MacroStorage.LoadAsync(_macroPath.Text);
        _hasUnsavedChanges = false;
        RefreshGrid();
        Log($"Inspeção carregada: {_currentMacro.Name}, {_currentMacro.Events.Count} eventos brutos.");
    }

    private async Task SaveCurrentMacroAsync()
    {
        if (_currentMacro is null)
        {
            MessageBox.Show("Nenhuma macro carregada em memória para salvar.");
            return;
        }

        var targetPath = _macroPath.Text;
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            using var sfd = new SaveFileDialog { Filter = "Macro JSON (*.json)|*.json", DefaultExt = "json" };
            if (sfd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            targetPath = sfd.FileName;
            _macroPath.Text = targetPath;
        }

        await MacroStorage.SaveAsync(targetPath, _currentMacro);
        _hasUnsavedChanges = false;
        Log($"Alterações salvas em {targetPath}.");
    }

    private void DeleteSelectedAction()
    {
        if (_currentMacro is null)
        {
            MessageBox.Show("Carregue uma macro primeiro.");
            return;
        }

        if (_eventsGrid.CurrentRow?.DataBoundItem is not EventRow row)
        {
            MessageBox.Show("Selecione uma linha para excluir.");
            return;
        }

        if (row.SourceEventIndex is null)
        {
            MessageBox.Show("No modo agregado, essa linha é derivada (wait/grupo). Ative 'Modo edição (ações reais)' para excluir ações reais.");
            return;
        }

        var idx = row.SourceEventIndex.Value;
        if (idx < 0 || idx >= _currentMacro.Events.Count)
        {
            MessageBox.Show("Índice de ação inválido.");
            return;
        }

        _currentMacro.Events.RemoveAt(idx);
        _hasUnsavedChanges = true;
        RefreshGrid();
        Log($"Ação #{idx} excluída da macro em memória. Reproduza sem salvar se quiser testar agora.");
    }

    private void DeleteFilteredActions()
    {
        if (_currentMacro is null)
        {
            MessageBox.Show("Carregue uma macro primeiro.");
            return;
        }

        var bound = _eventsGrid.DataSource as List<EventRow>;
        if (bound is null || bound.Count == 0)
        {
            MessageBox.Show("Não há linhas para excluir.");
            return;
        }

        var indexes = bound
            .Where(r => r.SourceEventIndex.HasValue)
            .Select(r => r.SourceEventIndex!.Value)
            .Distinct()
            .OrderByDescending(i => i)
            .ToList();

        if (indexes.Count == 0)
        {
            MessageBox.Show("Nenhuma ação real no filtro atual. Ative 'Modo edição (ações reais)'.");
            return;
        }

        foreach (var idx in indexes)
        {
            if (idx >= 0 && idx < _currentMacro.Events.Count)
            {
                _currentMacro.Events.RemoveAt(idx);
            }
        }

        _hasUnsavedChanges = true;
        RefreshGrid();
        Log($"{indexes.Count} ações removidas com base no filtro atual (edição em memória).");
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
            EditMode = _editMode.Checked,
            MinWaitMs = int.TryParse(_waitThreshold.Text, out var wait) ? Math.Max(wait, 0) : 250
        };

        var rows = BuildRows(_currentMacro, options);
        _eventsGrid.DataSource = rows;

        var editable = options.EditMode;
        _eventsGrid.ReadOnly = !editable;
        if (_eventsGrid.Columns["Value"] is not null) _eventsGrid.Columns["Value"].ReadOnly = !editable;
        if (_eventsGrid.Columns["WaitMs"] is not null) _eventsGrid.Columns["WaitMs"].ReadOnly = !editable;
    }

    private static List<EventRow> BuildRows(MacroFile macro, InspectRenderOptions options)
    {
        if (options.EditMode)
        {
            return BuildEditableRows(macro, options.Filter);
        }

        return BuildSmartRows(macro, options);
    }

    private static List<EventRow> BuildEditableRows(MacroFile macro, string filter)
    {
        var rows = new List<EventRow>();
        long prevTimestamp = 0;
        int? lastX = null;
        int? lastY = null;

        for (var i = 0; i < macro.Events.Count; i++)
        {
            var ev = macro.Events[i];
            var waitMs = Math.Max(ev.TimestampMs - prevTimestamp, 0);
            var row = new EventRow
            {
                Action = PrettyAction(ev),
                Value = PrettyValue(ev, ref lastX, ref lastY),
                TimestampMs = ev.TimestampMs,
                WaitMs = waitMs,
                Kind = ev.Kind,
                SourceEventIndex = i
            };

            if (MatchInspectFilter(filter, row.Kind))
            {
                rows.Add(row);
            }

            prevTimestamp = ev.TimestampMs;
        }

        ReindexRows(rows);
        return rows;
    }

    private static List<EventRow> BuildSmartRows(MacroFile macro, InspectRenderOptions options)
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
                Kind = ev.Kind,
                SourceEventIndex = i
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
            Kind = "wait",
            SourceEventIndex = null
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
            Kind = "mouse_move_group",
            SourceEventIndex = null
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
            "Teclado" => kind is "key_down" or "key_up" or "text_input",
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
            "text_input" => "Text input",
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

        if (ev.Kind == "text_input")
        {
            return ev.Data.GetValueOrDefault("text", "");
        }

        return string.Join(", ", ev.Data.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private void InsertDesignedAction()
    {
        if (_currentMacro is null)
        {
            MessageBox.Show("Carregue uma macro antes de inserir ações.");
            return;
        }

        var kind = _designerActionType.SelectedItem?.ToString() ?? "Text input";
        var rawValue = _designerValue.Text;
        var newEvent = new MacroEvent
        {
            Kind = kind switch
            {
                "Text input" => "text_input",
                "Wait" => "wait",
                "Key down" => "key_down",
                "Key up" => "key_up",
                "Mouse left click" => "mouse_down",
                _ => "text_input"
            },
            TimestampMs = _currentMacro.Events.LastOrDefault()?.TimestampMs ?? 0,
            Data = new Dictionary<string, string>()
        };

        switch (newEvent.Kind)
        {
            case "text_input":
                newEvent.Data["text"] = rawValue;
                break;
            case "wait":
                if (!long.TryParse(rawValue, out var waitMs) || waitMs < 0)
                {
                    MessageBox.Show("Para Wait, informe milissegundos válidos.");
                    return;
                }
                newEvent.TimestampMs += waitMs;
                break;
            case "key_down":
            case "key_up":
                newEvent.Data["key"] = rawValue;
                break;
            case "mouse_down":
                var pos = Cursor.Position;
                newEvent.Data["x"] = pos.X.ToString();
                newEvent.Data["y"] = pos.Y.ToString();
                newEvent.Data["button"] = "Left";
                break;
        }

        if (!string.IsNullOrWhiteSpace(_designerLabel.Text))
        {
            newEvent.Data["label"] = _designerLabel.Text.Trim();
        }

        if (!string.IsNullOrWhiteSpace(_designerComment.Text))
        {
            newEvent.Data["comment"] = _designerComment.Text.Trim();
        }

        _currentMacro.Events.Add(newEvent);
        _hasUnsavedChanges = true;
        RefreshGrid();
        Log($"Ação '{newEvent.Kind}' inserida no final da macro.");
    }

    private void DuplicateSelectedAction()
    {
        if (_currentMacro is null || _eventsGrid.SelectedRows.Count == 0)
        {
            return;
        }

        if (_eventsGrid.SelectedRows[0].DataBoundItem is not EventRow row || row.SourceEventIndex is not int idx || idx < 0 || idx >= _currentMacro.Events.Count)
        {
            return;
        }

        var source = _currentMacro.Events[idx];
        var copy = new MacroEvent
        {
            Kind = source.Kind,
            TimestampMs = source.TimestampMs,
            Data = new Dictionary<string, string>(source.Data)
        };

        _currentMacro.Events.Insert(idx + 1, copy);
        _hasUnsavedChanges = true;
        RefreshGrid();
        Log("Ação duplicada.");
    }

    private void MoveSelectedAction(int direction)
    {
        if (_currentMacro is null || _eventsGrid.SelectedRows.Count == 0)
        {
            return;
        }

        if (_eventsGrid.SelectedRows[0].DataBoundItem is not EventRow row || row.SourceEventIndex is not int idx)
        {
            return;
        }

        var target = idx + direction;
        if (idx < 0 || idx >= _currentMacro.Events.Count || target < 0 || target >= _currentMacro.Events.Count)
        {
            return;
        }

        (_currentMacro.Events[idx], _currentMacro.Events[target]) = (_currentMacro.Events[target], _currentMacro.Events[idx]);
        _hasUnsavedChanges = true;
        RefreshGrid();
        Log(direction < 0 ? "Ação movida para cima." : "Ação movida para baixo.");
    }

    private void ApplyGridEdit(int rowIndex, int columnIndex)
    {
        if (_currentMacro is null || !_editMode.Checked || rowIndex < 0 || columnIndex < 0)
        {
            return;
        }

        if (_eventsGrid.Rows[rowIndex].DataBoundItem is not EventRow row || row.SourceEventIndex is not int sourceIndex)
        {
            return;
        }

        var colName = _eventsGrid.Columns[columnIndex].Name;
        var input = _eventsGrid.Rows[rowIndex].Cells[columnIndex].Value?.ToString() ?? string.Empty;

        var ok = colName switch
        {
            "Value" => TryApplyValueEdit(sourceIndex, row.Kind, input),
            "WaitMs" => TryApplyWaitEdit(sourceIndex, input),
            _ => false
        };

        if (!ok)
        {
            Log("Edição inválida para esta linha. Use formato esperado.");
            RefreshGrid();
            return;
        }

        _hasUnsavedChanges = true;
        RefreshGrid();
    }

    private bool TryApplyValueEdit(int sourceIndex, string kind, string input)
    {
        if (_currentMacro is null || sourceIndex < 0 || sourceIndex >= _currentMacro.Events.Count)
        {
            return false;
        }

        var ev = _currentMacro.Events[sourceIndex];
        input = input.Trim();

        if (kind is "key_down" or "key_up")
        {
            ev.Data["key"] = input;
            return !string.IsNullOrWhiteSpace(input);
        }

        if (kind == "text_input")
        {
            ev.Data["text"] = input;
            return true;
        }

        if (kind == "mouse_wheel")
        {
            var raw = input.Replace("delta=", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (!int.TryParse(raw, out var delta)) return false;
            ev.Data["delta"] = delta.ToString();
            return true;
        }

        if (kind == "mouse_move")
        {
            var target = input.Contains("->", StringComparison.Ordinal)
                ? input.Split("->", 2, StringSplitOptions.TrimEntries)[1]
                : input;

            if (!TryParsePoint(target, out var x, out var y)) return false;
            ev.Data["x"] = x.ToString();
            ev.Data["y"] = y.ToString();
            return true;
        }

        if (kind is "mouse_down" or "mouse_up")
        {
            if (!TryParsePoint(input, out var x, out var y)) return false;
            ev.Data["x"] = x.ToString();
            ev.Data["y"] = y.ToString();
            return true;
        }

        return false;
    }

    private bool TryApplyWaitEdit(int sourceIndex, string input)
    {
        if (_currentMacro is null || sourceIndex < 0 || sourceIndex >= _currentMacro.Events.Count)
        {
            return false;
        }

        var raw = input.Replace("ms", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (!long.TryParse(raw, out var waitMs) || waitMs < 0)
        {
            return false;
        }

        var prevTs = sourceIndex > 0 ? _currentMacro.Events[sourceIndex - 1].TimestampMs : 0;
        var oldTs = _currentMacro.Events[sourceIndex].TimestampMs;
        var newTs = prevTs + waitMs;
        var delta = newTs - oldTs;

        for (var i = sourceIndex; i < _currentMacro.Events.Count; i++)
        {
            _currentMacro.Events[i].TimestampMs += delta;
        }

        return true;
    }

    private static bool TryParsePoint(string raw, out int x, out int y)
    {
        x = 0;
        y = 0;
        var parts = raw.Split(',', StringSplitOptions.TrimEntries);
        return parts.Length == 2 && int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y);
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
        using var form = new Form
        {
            Text = title,
            ClientSize = new System.Drawing.Size(390, 120),
            StartPosition = FormStartPosition.CenterParent,
            Font = new Font("Segoe UI", 9.5F)
        };

        var textLabel = new Label { Left = 10, Top = 20, Text = title, Width = 360 };
        var textBox = new TextBox { Left = 10, Top = 45, Width = 360, Text = defaultValue };
        var confirmation = CreatePrimaryButton("OK", 290, 78, 80);
        confirmation.DialogResult = DialogResult.OK;

        form.Controls.AddRange(new Control[] { textLabel, textBox, confirmation });
        form.AcceptButton = confirmation;

        return form.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
    }

    private sealed class InspectRenderOptions
    {
        public string Filter { get; init; } = "Tudo";
        public bool CompactMoves { get; init; }
        public bool NaturalView { get; init; }
        public bool EditMode { get; init; }
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
        public int? SourceEventIndex { get; set; }
        public string Kind { get; set; } = string.Empty;
    }
}
