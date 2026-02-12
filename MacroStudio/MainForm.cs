using System.Text;

namespace MacroStudio;

public class MainForm : Form
{
    private readonly TextBox _macroPath = new() { Width = 720 };
    private readonly TextBox _params = new() { Width = 520, Text = "" };
    private readonly TextBox _speed = new() { Width = 80, Text = "1.0" };
    private readonly TextBox _stopKey = new() { Width = 80, Text = "F8" };
    private readonly TextBox _model = new() { Width = 180, Text = "gemini-2.0-flash" };
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


    private readonly ComboBox _designerActionType = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _designerValue = new() { Width = 700, Text = "" };
    private readonly TextBox _designerLabel = new() { Width = 160, Text = "" };
    private readonly TextBox _designerComment = new() { Width = 340, Text = "" };

    private readonly TextBox _hkStartRecord = new() { Width = 120, Text = "Ctrl+R" };
    private readonly TextBox _hkStopRecord = new() { Width = 120, Text = "Ctrl+Shift+R" };
    private readonly TextBox _hkPlay = new() { Width = 120, Text = "Ctrl+P" };
    private readonly TextBox _hkStopPlay = new() { Width = 120, Text = "Ctrl+Shift+P" };

    private readonly DataGridView _eventsGrid = new()
    {
        Width = 1140,
        Height = 390,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = true,
        AutoGenerateColumns = false,
        RowHeadersVisible = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None,
        GridColor = Color.FromArgb(229, 231, 235),
        AllowDrop = true
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
    private readonly Stack<MacroFile> _undoStack = new();
    private readonly Stack<MacroFile> _redoStack = new();
    private int _dragRowStartIndex = -1;
    private Point _dragStartPoint = Point.Empty;
    private bool _gridRefreshQueued;


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
            Width = ClientSize.Width - 32,
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
            Width = ClientSize.Width - 32,
            Height = ClientSize.Height - (hero.Bottom + 26),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        var playbackPage = new TabPage("Gravação e Replay") { BackColor = Color.FromArgb(239, 242, 247), AutoScroll = true };
        var inspectorPage = new TabPage("Inspeção e Edição") { BackColor = Color.FromArgb(239, 242, 247), AutoScroll = true };
        var hotkeysPage = new TabPage("Configurações de Hotkeys") { BackColor = Color.FromArgb(239, 242, 247), AutoScroll = true };

        tabs.TabPages.Add(playbackPage);
        tabs.TabPages.Add(inspectorPage);
        tabs.TabPages.Add(hotkeysPage);
        Controls.Add(tabs);

        var yPlayback = 12;
        BuildMacroFileSection(playbackPage, ref yPlayback);
        BuildActionSection(playbackPage, ref yPlayback);
        BuildLogSection(playbackPage, ref yPlayback);

        var yInspector = 12;
        BuildFilterSection(inspectorPage, ref yInspector);
        BuildMacroDesignerSection(inspectorPage, ref yInspector);
        BuildGridSection(inspectorPage, ref yInspector);

        var yHotkeys = 12;
        BuildHotkeysConfigSection(hotkeysPage, ref yHotkeys);
    }

    private Panel CreateCard(Control host, string title, int y, int height)
    {
        var card = new Panel
        {
            Left = 16,
            Top = y,
            Width = Math.Max(host.ClientSize.Width - 34, 900),
            Height = height,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
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
        var card = CreateCard(host, "Record and Playback", y, 208);

        card.Controls.Add(new Label { Left = 14, Top = 44, Width = 86, Text = "Parâmetros", ForeColor = Color.FromArgb(75, 85, 99) });
        _params.Left = 105;
        _params.Top = 40;
        _params.Width = 470;
        StyleInput(_params);
        card.Controls.Add(_params);

        card.Controls.Add(new Label { Left = 588, Top = 44, Width = 50, Text = "Speed", ForeColor = Color.FromArgb(75, 85, 99) });
        _speed.Left = 634;
        _speed.Top = 40;
        StyleInput(_speed);
        card.Controls.Add(_speed);

        card.Controls.Add(new Label { Left = 724, Top = 44, Width = 56, Text = "Stop key", ForeColor = Color.FromArgb(75, 85, 99) });
        _stopKey.Left = 784;
        _stopKey.Top = 40;
        StyleInput(_stopKey);
        card.Controls.Add(_stopKey);

        card.Controls.Add(new Label { Left = 876, Top = 44, Width = 50, Text = "Modelo", ForeColor = Color.FromArgb(75, 85, 99) });
        _model.Left = 934;
        _model.Top = 40;
        _model.Width = 188;
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

        var btnRecord = CreatePrimaryButton("● Gravar", 14, 132, 110);
        var btnStopRecord = CreateGhostButton("■ Parar Gravação", 130, 132, 140);
        var btnPlay = CreatePrimaryButton("▶ Reproduzir", 276, 132, 120);
        var btnStopPlay = CreateGhostButton("■ Parar", 402, 132, 96);

        btnRecord.Click += async (_, _) => await StartRecordAsync();
        btnStopRecord.Click += async (_, _) => await StopRecordAsync();
        btnPlay.Click += async (_, _) => await PlayAsync();
        btnStopPlay.Click += (_, _) => StopPlay();

        card.Controls.Add(btnRecord);
        card.Controls.Add(btnStopRecord);
        card.Controls.Add(btnPlay);
        card.Controls.Add(btnStopPlay);

        y += card.Height + 10;
    }

    private void EnsureHotkeyDefaults()
    {
        if (string.IsNullOrWhiteSpace(_hkStartRecord.Text)) _hkStartRecord.Text = "Ctrl+R";
        if (string.IsNullOrWhiteSpace(_hkStopRecord.Text)) _hkStopRecord.Text = "Ctrl+Shift+R";
        if (string.IsNullOrWhiteSpace(_hkPlay.Text)) _hkPlay.Text = "Ctrl+P";
        if (string.IsNullOrWhiteSpace(_hkStopPlay.Text)) _hkStopPlay.Text = "Ctrl+Shift+P";
    }

    private void BuildHotkeysConfigSection(Control host, ref int y)
    {
        EnsureHotkeyDefaults();
        NormalizeHotkeyTextBoxes();
        var card = CreateCard(host, "Configuração de atalhos", y, 236);

        card.Controls.Add(new Label { Left = 14, Top = 44, Width = 300, Text = "Iniciar gravação", ForeColor = Color.FromArgb(75, 85, 99) });
        _hkStartRecord.Left = 168;
        _hkStartRecord.Top = 40;
        _hkStartRecord.Width = 220;
        StyleInput(_hkStartRecord);
        card.Controls.Add(_hkStartRecord);
        BindHotkeyInput(_hkStartRecord);
        StyleHotkeyInput(_hkStartRecord);

        card.Controls.Add(new Label { Left = 320, Top = 44, Width = 300, Text = "Parar gravação", ForeColor = Color.FromArgb(75, 85, 99) });
        _hkStopRecord.Left = 486;
        _hkStopRecord.Top = 40;
        _hkStopRecord.Width = 220;
        StyleInput(_hkStopRecord);
        card.Controls.Add(_hkStopRecord);
        BindHotkeyInput(_hkStopRecord);
        StyleHotkeyInput(_hkStopRecord);

        card.Controls.Add(new Label { Left = 14, Top = 96, Width = 300, Text = "Reproduzir gravação", ForeColor = Color.FromArgb(75, 85, 99) });
        _hkPlay.Left = 168;
        _hkPlay.Top = 92;
        _hkPlay.Width = 220;
        StyleInput(_hkPlay);
        card.Controls.Add(_hkPlay);
        BindHotkeyInput(_hkPlay);
        StyleHotkeyInput(_hkPlay);

        card.Controls.Add(new Label { Left = 320, Top = 96, Width = 300, Text = "Parar replay", ForeColor = Color.FromArgb(75, 85, 99) });
        _hkStopPlay.Left = 486;
        _hkStopPlay.Top = 92;
        _hkStopPlay.Width = 220;
        StyleInput(_hkStopPlay);
        card.Controls.Add(_hkStopPlay);
        BindHotkeyInput(_hkStopPlay);
        StyleHotkeyInput(_hkStopPlay);

        var btnDefaults = CreateGhostButton("Restaurar padrão", 14, 154, 170);
        btnDefaults.Click += (_, _) =>
        {
            _hkStartRecord.Text = "Ctrl+R";
            _hkStopRecord.Text = "Ctrl+Shift+R";
            _hkPlay.Text = "Ctrl+P";
            _hkStopPlay.Text = "Ctrl+Shift+P";
            NormalizeHotkeyTextBoxes();
            Log("Hotkeys restauradas para o padrão.");
        };

        card.Controls.Add(btnDefaults);
        card.Controls.Add(new Label
        {
            Left = 176,
            Top = 156,
            Width = 920,
            Height = 64,
            ForeColor = Color.FromArgb(75, 85, 99),
            Text = "Clique no campo e pressione a combinação desejada. Ex.: Home, Ctrl+S, Alt Gr+Q, Ctrl+Shift+P. ESC continua como parada rápida de replay."
        });

        y += card.Height + 10;
    }

    private static void StyleHotkeyInput(TextBox target)
    {
        target.AutoSize = false;
        target.Height = 34;
        target.BorderStyle = BorderStyle.FixedSingle;
        target.TextAlign = HorizontalAlignment.Center;
        target.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
        target.BackColor = Color.White;
        target.ForeColor = Color.FromArgb(17, 24, 39);
    }

    private void BindHotkeyInput(TextBox target)
    {
        target.ReadOnly = true;
        target.ShortcutsEnabled = false;
        target.KeyDown -= HotkeyTextBox_KeyDown;
        target.KeyDown += HotkeyTextBox_KeyDown;
        target.GotFocus -= HotkeyTextBox_GotFocus;
        target.GotFocus += HotkeyTextBox_GotFocus;
        target.KeyPress -= HotkeyTextBox_KeyPress;
        target.KeyPress += HotkeyTextBox_KeyPress;
    }

    private void HotkeyTextBox_GotFocus(object? sender, EventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.SelectAll();
        }
    }

    private void HotkeyTextBox_KeyPress(object? sender, KeyPressEventArgs e)
    {
        e.Handled = true;
    }

    private void NormalizeHotkeyTextBoxes()
    {
        _hkStartRecord.Text = NormalizeHotkeyText(_hkStartRecord.Text, "Ctrl+R");
        _hkStopRecord.Text = NormalizeHotkeyText(_hkStopRecord.Text, "Ctrl+Shift+R");
        _hkPlay.Text = NormalizeHotkeyText(_hkPlay.Text, "Ctrl+P");
        _hkStopPlay.Text = NormalizeHotkeyText(_hkStopPlay.Text, "Ctrl+Shift+P");
    }

    private static string NormalizeHotkeyText(string raw, string fallback)
    {
        if (TryParseHotkey(raw, out var parsed))
        {
            return FormatHotkey(parsed);
        }

        if (TryParseHotkey(fallback, out var fallbackParsed))
        {
            return FormatHotkey(fallbackParsed);
        }

        return fallback;
    }

    private void HotkeyTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb)
        {
            return;
        }

        e.SuppressKeyPress = true;
        e.Handled = true;

        if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
        {
            tb.Clear();
            return;
        }

        var keyData = e.KeyData;
        var keyCode = e.KeyCode;
        if (keyCode == Keys.ControlKey || keyCode == Keys.ShiftKey || keyCode == Keys.Menu)
        {
            return;
        }

        tb.Text = FormatHotkey(keyData);
    }

    private static string FormatHotkey(Keys hotkey)
    {
        var parts = new List<string>();
        var modifiers = hotkey & Keys.Modifiers;
        var keyCode = hotkey & Keys.KeyCode;

        var hasCtrl = (modifiers & Keys.Control) == Keys.Control;
        var hasAlt = (modifiers & Keys.Alt) == Keys.Alt;
        var hasShift = (modifiers & Keys.Shift) == Keys.Shift;

        if (hasCtrl && hasAlt)
        {
            parts.Add("Alt Gr");
        }
        else
        {
            if (hasCtrl) parts.Add("Ctrl");
            if (hasAlt) parts.Add("Alt");
        }

        if (hasShift)
        {
            parts.Add("Shift");
        }

        if (keyCode != Keys.None)
        {
            parts.Add(keyCode.ToString());
        }

        return string.Join("+", parts);
    }

    private bool IsEditingHotkeyField()
    {
        return ActiveControl == _hkStartRecord
            || ActiveControl == _hkStopRecord
            || ActiveControl == _hkPlay
            || ActiveControl == _hkStopPlay;
    }

    private void BuildMacroDesignerSection(Control host, ref int y)
    {
        var card = CreateCard(host, "Ações e variáveis", y, 252);

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

        card.Controls.Add(new Label { Left = 14, Top = 120, Width = 106, Text = "Nome variável", ForeColor = Color.FromArgb(75, 85, 99) });
        _variableName.Left = 120;
        _variableName.Top = 116;
        StyleInput(_variableName);
        card.Controls.Add(_variableName);

        card.Controls.Add(new Label { Left = 232, Top = 120, Width = 174, Text = "Valores CSV (loop automático)", ForeColor = Color.FromArgb(75, 85, 99) });
        _variableValues.Left = 406;
        _variableValues.Top = 116;
        _variableValues.Width = 716;
        StyleInput(_variableValues);
        card.Controls.Add(_variableValues);

        var btnInsert = CreatePrimaryButton("Inserir ação", 14, 158, 126);
        var btnUpdate = CreateGhostButton("Atualizar selecionada", 146, 158, 166);
        var btnDuplicate = CreateGhostButton("Duplicar selecionada", 318, 158, 154);
        var btnMoveUp = CreateGhostButton("Mover acima", 478, 158, 110);
        var btnMoveDown = CreateGhostButton("Mover abaixo", 594, 158, 112);
        var btnDelete = CreateGhostButton("Excluir selecionada", 712, 158, 140);

        btnInsert.Click += (_, _) => InsertDesignedAction();
        btnUpdate.Click += (_, _) => UpdateSelectedActionFromDesigner();
        btnDuplicate.Click += (_, _) => DuplicateSelectedAction();
        btnMoveUp.Click += (_, _) => MoveSelectedAction(-1);
        btnMoveDown.Click += (_, _) => MoveSelectedAction(1);
        btnDelete.Click += (_, _) => DeleteSelectedAction();

        card.Controls.Add(btnInsert);
        card.Controls.Add(btnUpdate);
        card.Controls.Add(btnDuplicate);
        card.Controls.Add(btnMoveUp);
        card.Controls.Add(btnMoveDown);
        card.Controls.Add(btnDelete);

        card.Controls.Add(new Label
        {
            Left = 14,
            Top = 204,
            Width = 1110,
            Height = 34,
            ForeColor = Color.FromArgb(75, 85, 99),
            Text = "Dica: selecione uma linha real na timeline para preencher e editar. Para variável no texto, use {{periodo}}."
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


        _playMouseMoves.Left = 14;
        _playMouseMoves.Top = 80;
        _playMouseClicks.Left = 128;
        _playMouseClicks.Top = 80;
        _playKeyPresses.Left = 246;
        _playKeyPresses.Top = 80;
        _playWaitTimes.Left = 358;
        _playWaitTimes.Top = 80;
        _playMouseMoves.CheckedChanged += (_, _) => { RefreshGrid(); Log("Filtro de replay atualizado."); };
        _playMouseClicks.CheckedChanged += (_, _) => { RefreshGrid(); Log("Filtro de replay atualizado."); };
        _playKeyPresses.CheckedChanged += (_, _) => { RefreshGrid(); Log("Filtro de replay atualizado."); };
        _playWaitTimes.CheckedChanged += (_, _) => { RefreshGrid(); Log("Filtro de replay atualizado."); };
        StyleToggle(_playMouseMoves);
        StyleToggle(_playMouseClicks);
        StyleToggle(_playKeyPresses);
        StyleToggle(_playWaitTimes);
        card.Controls.Add(_playMouseMoves);
        card.Controls.Add(_playMouseClicks);
        card.Controls.Add(_playKeyPresses);
        card.Controls.Add(_playWaitTimes);

        var btnDeleteSelected = CreateGhostButton("Excluir selecionada", 500, 74, 150);
        var btnDeleteFiltered = CreateGhostButton("Excluir filtradas", 656, 74, 130);
        var btnSaveEdits = CreatePrimaryButton("Salvar alterações", 792, 74, 150);

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
        _eventsGrid.SelectionChanged += (_, _) => SyncDesignerWithSelection();
        _eventsGrid.MouseDown += EventsGrid_MouseDown;
        _eventsGrid.MouseMove += EventsGrid_MouseMove;
        _eventsGrid.DragOver += EventsGrid_DragOver;
        _eventsGrid.DragDrop += EventsGrid_DragDrop;
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

        var tempName = $"Macro temporária {DateTime.Now:HHmmss}";
        var macro = _recorder.StopAndBuild(tempName, _stopKey.Text.Trim());

        using var sfd = new SaveFileDialog { Filter = "Macro JSON (*.json)|*.json", DefaultExt = "json" };
        if (sfd.ShowDialog() == DialogResult.OK)
        {
            macro.Name = Path.GetFileNameWithoutExtension(sfd.FileName);
            await MacroStorage.SaveAsync(sfd.FileName, macro);
            _macroPath.Text = sfd.FileName;
            _hasUnsavedChanges = false;
            Log($"Macro salva em {_macroPath.Text} ({macro.Events.Count} eventos). Edição direta habilitada.");
        }
        else
        {
            _macroPath.Text = string.Empty;
            _hasUnsavedChanges = true;
            Log($"Gravação mantida em memória ({macro.Events.Count} eventos). Salve somente se quiser reutilizar depois.");
        }

        _currentMacro = macro;
        ClearHistory();
        RefreshGrid();
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

            var variableSets = ParseVariableLoopSets(variableName, _variableValues.Text);
            if (variableSets.Count > 0)
            {
            var sequenceOptions = new PlaybackFilterOptions
            {
                PlayMouseMoves = playbackOptions.PlayMouseMoves,
                PlayMouseClicks = playbackOptions.PlayMouseClicks,
                PlayKeyPresses = playbackOptions.PlayKeyPresses,
                RespectWaitTimes = playbackOptions.RespectWaitTimes,
                LoopCount = playbackOptions.LoopCount,
                StartDelayMs = playbackOptions.StartDelayMs,
                DelayJitterMs = playbackOptions.DelayJitterMs
            };

                _lastReplayOptions = sequenceOptions;

                var maxIterations = variableSets.Max(v => v.Values.Count);
                var variableOrder = variableSets.Select(v => v.Name).ToList();

                for (var i = 0; i < maxIterations; i++)
                {
                    _playCts.Token.ThrowIfCancellationRequested();

                    var loopValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var variable in variableSets)
                    {
                        var currentValue = i < variable.Values.Count
                            ? variable.Values[i]
                            : variable.Values[^1];
                        loopValues[variable.Name] = currentValue;
                    }

                    var loopParameters = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);
                    foreach (var pair in loopValues)
                    {
                        loopParameters[pair.Key] = pair.Value;
                    }

                    if (variableSets.Count > 0)
                    {
                        var primaryValue = loopValues[variableSets[0].Name];
                        loopParameters["value"] = primaryValue;
                        loopParameters["item"] = primaryValue;
                    }

                    _lastReplayParameters = new Dictionary<string, string>(loopParameters);
                    var macroForIteration = BuildMacroForVariableIteration(macro, loopValues, variableOrder);
                    Log($"Loop variável {i + 1}/{maxIterations}: " + string.Join(", ", loopValues.Select(kv => $"{kv.Key}={kv.Value}")));
                    await _player.PlayAsync(macroForIteration, loopParameters, speed, _playCts.Token, sequenceOptions);
                }

                Log("Replay concluído (todas variáveis processadas). Loop encerrado ao fim da lista de variáveis.");
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

    private static List<string> ParseDelimitedValues(string raw, bool includeSemicolon)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new();
        }

        var output = new List<string>();
        var token = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < raw.Length; i++)
        {
            var ch = raw[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < raw.Length && raw[i + 1] == '"')
                {
                    token.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            var isLineBreak = ch == (char)10 || ch == (char)13;
            var isSeparator = ch == ',' || (includeSemicolon && ch == ';') || isLineBreak;

            if (!inQuotes && isSeparator)
            {
                var value = token.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    output.Add(value);
                }

                token.Clear();
                continue;
            }

            token.Append(ch);
        }

        var finalValue = token.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(finalValue))
        {
            output.Add(finalValue);
        }

        return output;
    }

    private static List<VariableSeries> ParseVariableLoopSets(string variableNameRaw, string variableValuesRaw)
    {
        var output = new List<VariableSeries>();

        var valueLines = variableValuesRaw
            .Split(new[] { (char)10, (char)13 }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        foreach (var line in valueLines)
        {
            var separator = line.IndexOf(':');
            if (separator <= 0 || separator >= line.Length - 1)
            {
                continue;
            }

            var name = line[..separator].Trim();
            var values = ParseDelimitedValues(line[(separator + 1)..], includeSemicolon: true);
            if (string.IsNullOrWhiteSpace(name) || values.Count == 0)
            {
                continue;
            }

            output.Add(new VariableSeries(name, values));
        }

        if (output.Count > 0)
        {
            return output;
        }

        var normalizedName = variableNameRaw.Trim();
        var normalizedValues = ParseDelimitedValues(variableValuesRaw, includeSemicolon: true);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return output;
        }

        if (normalizedValues.Count == 0)
        {
            var inlineSeparator = normalizedName.IndexOf(':');
            if (inlineSeparator > 0 && inlineSeparator < normalizedName.Length - 1)
            {
                var inlineName = normalizedName[..inlineSeparator].Trim();
                var inlineValues = ParseDelimitedValues(normalizedName[(inlineSeparator + 1)..], includeSemicolon: true);
                if (!string.IsNullOrWhiteSpace(inlineName) && inlineValues.Count > 0)
                {
                    output.Add(new VariableSeries(inlineName, inlineValues));
                    return output;
                }
            }

            return output;
        }

        var names = ParseDelimitedValues(normalizedName, includeSemicolon: false);
        if (names.Count == 0)
        {
            names.Add("value");
        }

        if (names.Count == 1)
        {
            output.Add(new VariableSeries(names[0], normalizedValues));
            return output;
        }

        foreach (var name in names)
        {
            output.Add(new VariableSeries(name, normalizedValues));
        }

        return output;
    }

    private static MacroFile BuildMacroForVariableIteration(MacroFile macro, Dictionary<string, string> iterationValues, IReadOnlyList<string> variableOrder)
    {
        var clone = CloneMacroFile(macro);
        var hasInlineTokens = false;

        foreach (var ev in clone.Events)
        {
            var keys = ev.Data.Keys.ToList();
            foreach (var key in keys)
            {
                var value = ev.Data[key];
                var replaced = value;

                foreach (var pair in iterationValues)
                {
                    replaced = replaced
                        .Replace("{{" + pair.Key + "}}", pair.Value, StringComparison.OrdinalIgnoreCase)
                        .Replace("{" + pair.Key + "}", pair.Value, StringComparison.OrdinalIgnoreCase);
                }

                if (variableOrder.Count > 0 && iterationValues.TryGetValue(variableOrder[0], out var primaryValue))
                {
                    replaced = replaced
                        .Replace("{{value}}", primaryValue, StringComparison.OrdinalIgnoreCase)
                        .Replace("{value}", primaryValue, StringComparison.OrdinalIgnoreCase)
                        .Replace("{{item}}", primaryValue, StringComparison.OrdinalIgnoreCase)
                        .Replace("{item}", primaryValue, StringComparison.OrdinalIgnoreCase);
                }

                if (!string.Equals(value, replaced, StringComparison.Ordinal))
                {
                    hasInlineTokens = true;
                }

                ev.Data[key] = replaced;
            }
        }

        var textInputs = clone.Events.Where(ev => ev.Kind == "text_input").ToList();
        for (var i = 0; i < variableOrder.Count && i < textInputs.Count; i++)
        {
            var variableKey = variableOrder[i];
            if (!iterationValues.TryGetValue(variableKey, out var currentValue))
            {
                continue;
            }

            var existingText = textInputs[i].Data.GetValueOrDefault("text", string.Empty);
            if (!hasInlineTokens || string.IsNullOrWhiteSpace(existingText))
            {
                textInputs[i].Data["text"] = currentValue;
            }
        }

        if (textInputs.Count == 1 && variableOrder.Count >= 1)
        {
            var key = variableOrder[0];
            if (iterationValues.TryGetValue(key, out var currentValue))
            {
                var existingText = textInputs[0].Data.GetValueOrDefault("text", string.Empty);
                if (!hasInlineTokens || string.IsNullOrWhiteSpace(existingText))
                {
                    textInputs[0].Data["text"] = currentValue;
                }
            }
        }

        return clone;
    }

    private static MacroFile CloneMacroFile(MacroFile source)
    {
        return new MacroFile
        {
            Name = source.Name,
            CreatedAt = source.CreatedAt,
            Metadata = new Dictionary<string, string>(source.Metadata),
            Events = source.Events.Select(ev => new MacroEvent
            {
                Kind = ev.Kind,
                TimestampMs = ev.TimestampMs,
                Data = new Dictionary<string, string>(ev.Data)
            }).ToList()
        };
    }

    private sealed record VariableSeries(string Name, List<string> Values);

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

        if (TryCaptureHotkeyFromEditor(keyData))
        {
            return true;
        }

        if (IsHotkeyPressed(keyData, _hkStartRecord.Text, Keys.Control | Keys.R))
        {
            _ = InvokeShortcutAsync(StartRecordAsync);
            return true;
        }

        if (IsHotkeyPressed(keyData, _hkStopRecord.Text, Keys.Control | Keys.Shift | Keys.R))
        {
            _ = InvokeShortcutAsync(StopRecordAsync);
            return true;
        }

        if (IsHotkeyPressed(keyData, _hkPlay.Text, Keys.Control | Keys.P))
        {
            _ = InvokeShortcutAsync(PlayAsync);
            return true;
        }

        if (IsHotkeyPressed(keyData, _hkStopPlay.Text, Keys.Control | Keys.Shift | Keys.P))
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

        if (keyData == Keys.Delete && _eventsGrid.ContainsFocus && !_eventsGrid.IsCurrentCellInEditMode)
        {
            DeleteSelectedAction();
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

        if (keyData == (Keys.Control | Keys.Z))
        {
            Undo();
            return true;
        }

        if (keyData == (Keys.Control | Keys.Y))
        {
            Redo();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private bool TryCaptureHotkeyFromEditor(Keys keyData)
    {
        if (!IsEditingHotkeyField() || ActiveControl is not TextBox tb)
        {
            return false;
        }

        var keyCode = keyData & Keys.KeyCode;
        if (keyCode == Keys.Back || keyCode == Keys.Delete)
        {
            tb.Clear();
            return true;
        }

        if (keyCode == Keys.ControlKey || keyCode == Keys.ShiftKey || keyCode == Keys.Menu)
        {
            return true;
        }

        tb.Text = FormatHotkey(keyData);
        return true;
    }

    private static bool IsHotkeyPressed(Keys pressed, string configured, Keys fallback)
    {
        if (TryParseHotkey(configured, out var parsed))
        {
            return pressed == parsed;
        }

        return pressed == fallback;
    }

    private static bool TryParseHotkey(string raw, out Keys parsed)
    {
        parsed = Keys.None;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var tokens = raw.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return false;
        }

        Keys result = Keys.None;
        foreach (var tokenRaw in tokens)
        {
            var token = tokenRaw.Trim();
            var normalized = token.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
            if (normalized is "CTRL" or "CONTROL")
            {
                result |= Keys.Control;
                continue;
            }

            if (normalized == "SHIFT")
            {
                result |= Keys.Shift;
                continue;
            }

            if (normalized == "ALT")
            {
                result |= Keys.Alt;
                continue;
            }

            if (normalized is "ALTGR" or "ALTRIGHT")
            {
                result |= Keys.Control | Keys.Alt;
                continue;
            }

            if (!Enum.TryParse<Keys>(token, true, out var keyPart))
            {
                return false;
            }

            result |= keyPart;
        }

        parsed = result;
        var keyCode = parsed & Keys.KeyCode;
        return parsed != Keys.None && keyCode != Keys.None;
    }

    private void PushUndoState()
    {
        if (_currentMacro is null)
        {
            return;
        }

        _undoStack.Push(CloneMacro(_currentMacro));
        _redoStack.Clear();
    }

    private void Undo()
    {
        if (_undoStack.Count == 0 || _currentMacro is null)
        {
            Log("Nada para desfazer.");
            return;
        }

        _redoStack.Push(CloneMacro(_currentMacro));
        _currentMacro = _undoStack.Pop();
        _hasUnsavedChanges = true;
        RefreshGrid();
        Log("Desfeito (Ctrl+Z).");
    }

    private void Redo()
    {
        if (_redoStack.Count == 0 || _currentMacro is null)
        {
            Log("Nada para refazer.");
            return;
        }

        _undoStack.Push(CloneMacro(_currentMacro));
        _currentMacro = _redoStack.Pop();
        _hasUnsavedChanges = true;
        RefreshGrid();
        Log("Refeito (Ctrl+Y).");
    }

    private void UndoInternal()
    {
        if (_undoStack.Count == 0 || _currentMacro is null)
        {
            return;
        }

        _currentMacro = _undoStack.Pop();
    }

    private void ClearHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    private static MacroFile CloneMacro(MacroFile source)
    {
        return new MacroFile
        {
            Name = source.Name,
            CreatedAt = source.CreatedAt,
            Metadata = new Dictionary<string, string>(source.Metadata),
            Events = source.Events.Select(ev => new MacroEvent
            {
                Kind = ev.Kind,
                TimestampMs = ev.TimestampMs,
                Data = new Dictionary<string, string>(ev.Data)
            }).ToList()
        };
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
        ClearHistory();
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

        if (_eventsGrid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Selecione uma ou mais linhas para excluir.");
            return;
        }

        var selectedRows = _eventsGrid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => r.DataBoundItem as EventRow)
            .Where(r => r is not null)
            .Cast<EventRow>()
            .ToList();

        var anchorDisplayIndex = _eventsGrid.CurrentCell?.RowIndex ?? Math.Max(selectedRows.Min(r => r.Index) - 1, 0);
        var topIndex = _eventsGrid.FirstDisplayedScrollingRowIndex;

        var removableIndexes = CollectRemovableIndexes(selectedRows);

        var waitRows = selectedRows
            .Where(r => r.Kind == "wait" && r.WaitMs > 0)
            .OrderByDescending(r => r.TimestampMs)
            .ToList();

        if (removableIndexes.Count == 0 && waitRows.Count == 0)
        {
            MessageBox.Show("As linhas selecionadas não apontam para ações reais editáveis.");
            return;
        }

        PushUndoState();

        foreach (var waitRow in waitRows)
        {
            if (waitRow.SourceEventIndex is not int waitSource || waitSource < 0 || waitSource >= _currentMacro.Events.Count)
            {
                continue;
            }

            var shift = waitRow.WaitMs;
            if (shift <= 0)
            {
                continue;
            }

            for (var i = waitSource; i < _currentMacro.Events.Count; i++)
            {
                _currentMacro.Events[i].TimestampMs = Math.Max(_currentMacro.Events[i].TimestampMs - shift, 0);
            }
        }

        foreach (var idx in removableIndexes)
        {
            if (idx >= 0 && idx < _currentMacro.Events.Count)
            {
                _currentMacro.Events.RemoveAt(idx);
            }
        }

        _hasUnsavedChanges = true;
        RefreshGrid();
        RestoreGridPosition(anchorDisplayIndex, topIndex);
        Log($"{removableIndexes.Count} ação(ões) excluída(s) e {waitRows.Count} espera(s) removida(s) da macro em memória.");
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

        var indexes = CollectRemovableIndexes(bound);

        var waitRows = bound
            .Where(r => r.Kind == "wait" && r.WaitMs > 0)
            .OrderByDescending(r => r.TimestampMs)
            .ToList();

        if (indexes.Count == 0 && waitRows.Count == 0)
        {
            MessageBox.Show("Nenhuma ação real no filtro atual.");
            return;
        }

        PushUndoState();

        foreach (var waitRow in waitRows)
        {
            if (waitRow.SourceEventIndex is not int waitSource || waitSource < 0 || waitSource >= _currentMacro.Events.Count)
            {
                continue;
            }

            var shift = waitRow.WaitMs;
            for (var i = waitSource; i < _currentMacro.Events.Count; i++)
            {
                _currentMacro.Events[i].TimestampMs = Math.Max(_currentMacro.Events[i].TimestampMs - shift, 0);
            }
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
        RestoreGridPosition(0, 0);
        Log($"{indexes.Count} ações removidas e {waitRows.Count} esperas removidas com base no filtro atual.");
    }

    private static List<int> CollectRemovableIndexes(IEnumerable<EventRow> rows)
    {
        var indexes = new HashSet<int>();
        foreach (var row in rows.Where(r => r.Kind != "wait"))
        {
            if (row.Kind == "mouse_move_group"
                && row.SourceEventIndex is int firstMoveIndex
                && row.SecondarySourceEventIndex is int lastMoveIndex)
            {
                var start = Math.Min(firstMoveIndex, lastMoveIndex);
                var end = Math.Max(firstMoveIndex, lastMoveIndex);
                for (var i = start; i <= end; i++)
                {
                    indexes.Add(i);
                }

                continue;
            }

            if (row.SourceEventIndex is int sourceIndex)
            {
                indexes.Add(sourceIndex);
            }

            if (row.SecondarySourceEventIndex is int secondaryIndex)
            {
                indexes.Add(secondaryIndex);
            }
        }

        return indexes.OrderByDescending(i => i).ToList();
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
            PlayMouseMoves = _playMouseMoves.Checked,
            PlayMouseClicks = _playMouseClicks.Checked,
            PlayKeyPresses = _playKeyPresses.Checked,
            PlayWaitTimes = _playWaitTimes.Checked,
            CompactMoves = true,
            NaturalView = false,
            MinWaitMs = 0
        };

        var rows = BuildRows(_currentMacro, options);
        _eventsGrid.DataSource = rows;

        _eventsGrid.ReadOnly = false;
        if (_eventsGrid.Columns["Value"] is not null) _eventsGrid.Columns["Value"].ReadOnly = false;
        if (_eventsGrid.Columns["WaitMs"] is not null) _eventsGrid.Columns["WaitMs"].ReadOnly = false;
    }

    private static List<EventRow> BuildRows(MacroFile macro, InspectRenderOptions options)
    {
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
                        FirstSourceEventIndex = i,
                        StartX = startX,
                        StartY = startY,
                        EndX = moveX,
                        EndY = moveY,
                        TotalWaitMs = waitMs,
                        LastTimestampMs = ev.TimestampMs,
                        Count = 1,
                        Distance = Math.Abs(moveX - startX) + Math.Abs(moveY - startY),
                        LastSourceEventIndex = i
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
                    moveAggregation.LastSourceEventIndex = i;
                }

                lastX = moveX;
                lastY = moveY;
                prevTimestamp = ev.TimestampMs;
                continue;
            }

            FlushMoveAggregation(rows, options, moveAggregation);
            moveAggregation = null;

            if (TryBuildClickRow(macro.Events, i, prevTimestamp, ref lastX, ref lastY, out var clickRow, out var skipTo, out var clickTimestamp))
            {
                AddWaitRowIfRelevant(rows, options, clickTimestamp, clickRow.WaitMs, clickRow.SourceEventIndex);
                if (MatchInspectFilter(options.Filter, clickRow.Kind, options))
                {
                    rows.Add(clickRow);
                }

                i = skipTo;
                prevTimestamp = clickTimestamp;
                continue;
            }

            AddWaitRowIfRelevant(rows, options, ev.TimestampMs, waitMs, i);

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

            if (MatchInspectFilter(options.Filter, row.Kind, options))
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

    private static bool TryBuildClickRow(
        IReadOnlyList<MacroEvent> events,
        int index,
        long prevTimestamp,
        ref int? lastX,
        ref int? lastY,
        out EventRow row,
        out int skipTo,
        out long timestampMs)
    {
        row = new EventRow();
        skipTo = index;
        timestampMs = prevTimestamp;

        var down = events[index];
        if (down.Kind != "mouse_down" || index + 1 >= events.Count)
        {
            return false;
        }

        var up = events[index + 1];
        if (up.Kind != "mouse_up")
        {
            return false;
        }

        var downButton = down.Data.GetValueOrDefault("button", "Left");
        var upButton = up.Data.GetValueOrDefault("button", "Left");
        if (!string.Equals(downButton, upButton, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(up.Data.GetValueOrDefault("x"), out var x) || !int.TryParse(up.Data.GetValueOrDefault("y"), out var y))
        {
            return false;
        }

        timestampMs = up.TimestampMs;
        var waitMs = Math.Max(timestampMs - prevTimestamp, 0);
        lastX = x;
        lastY = y;

        row = new EventRow
        {
            Action = $"Mouse {upButton.ToLowerInvariant()} click",
            Value = $"{x}, {y}",
            TimestampMs = timestampMs,
            WaitMs = waitMs,
            Kind = "mouse_click",
            SourceEventIndex = index,
            SecondarySourceEventIndex = index + 1
        };
        skipTo = index + 1;
        return true;
    }

    private static void AddWaitRowIfRelevant(List<EventRow> rows, InspectRenderOptions options, long timestampMs, long waitMs, int? sourceEventIndex = null)
    {
        if (waitMs <= 0)
        {
            return;
        }

        if (options.NaturalView && waitMs < options.MinWaitMs)
        {
            return;
        }

        if (!options.PlayWaitTimes)
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
            SourceEventIndex = sourceEventIndex
        };

        if (MatchInspectFilter(options.Filter, waitRow.Kind, options))
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
            AddWaitRowIfRelevant(rows, options, aggregation.LastTimestampMs, aggregation.TotalWaitMs, aggregation.LastSourceEventIndex);
        }

        var moveRow = new EventRow
        {
            Action = aggregation.Count > 1 ? $"Mouse move (x{aggregation.Count})" : "Mouse move",
            Value = $"{aggregation.StartX}, {aggregation.StartY} -> {aggregation.EndX}, {aggregation.EndY}",
            TimestampMs = aggregation.LastTimestampMs,
            WaitMs = aggregation.TotalWaitMs,
            Kind = "mouse_move_group",
            SourceEventIndex = aggregation.FirstSourceEventIndex,
            SecondarySourceEventIndex = aggregation.LastSourceEventIndex
        };

        if (MatchInspectFilter(options.Filter, moveRow.Kind, options))
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

    private static bool MatchInspectFilter(string filter, string kind, InspectRenderOptions? options = null)
    {
        if (options is not null)
        {
            var allowedByPlayback = kind switch
            {
                "mouse_move" or "mouse_move_group" => options.PlayMouseMoves,
                "mouse_down" or "mouse_up" or "mouse_click" or "mouse_wheel" => options.PlayMouseClicks,
                "key_down" or "key_up" or "text_input" => options.PlayKeyPresses,
                "wait" => options.PlayWaitTimes,
                _ => true
            };

            if (!allowedByPlayback)
            {
                return false;
            }
        }

        return filter switch
        {
            "Cliques" => kind is "mouse_down" or "mouse_up" or "mouse_click",
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
            "mouse_click" => $"Mouse {ev.Data.GetValueOrDefault("button", "left").ToLowerInvariant()} click",
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

        var insertAfterIndex = ResolveSelectedInsertAnchorIndex();
        var baseTimestamp = insertAfterIndex >= 0 && insertAfterIndex < _currentMacro.Events.Count
            ? _currentMacro.Events[insertAfterIndex].TimestampMs
            : _currentMacro.Events.LastOrDefault()?.TimestampMs ?? 0;

        if (!TryBuildEventFromDesigner(baseTimestamp, out var newEvent))
        {
            return;
        }

        PushUndoState();
        var targetInsertIndex = insertAfterIndex + 1;
        if (targetInsertIndex >= 0 && targetInsertIndex <= _currentMacro.Events.Count)
        {
            _currentMacro.Events.Insert(targetInsertIndex, newEvent);
        }
        else
        {
            _currentMacro.Events.Add(newEvent);
        }

        _hasUnsavedChanges = true;
        RefreshGrid();
        Log($"Ação '{newEvent.Kind}' inserida abaixo da linha selecionada.");
    }

    private int ResolveSelectedInsertAnchorIndex()
    {
        if (_currentMacro is null || _eventsGrid.SelectedRows.Count == 0)
        {
            return _currentMacro?.Events.Count - 1 ?? -1;
        }

        if (_eventsGrid.SelectedRows[0].DataBoundItem is not EventRow row)
        {
            return _currentMacro.Events.Count - 1;
        }

        if (row.SecondarySourceEventIndex is int secondary && secondary >= 0 && secondary < _currentMacro.Events.Count)
        {
            return secondary;
        }

        if (row.SourceEventIndex is int source && source >= 0 && source < _currentMacro.Events.Count)
        {
            return source;
        }

        var fallback = _currentMacro.Events
            .Select((ev, idx) => new { ev, idx })
            .Where(x => x.ev.TimestampMs <= row.TimestampMs)
            .Select(x => x.idx)
            .DefaultIfEmpty(_currentMacro.Events.Count - 1)
            .Max();

        return Math.Clamp(fallback, -1, _currentMacro.Events.Count - 1);
    }

    private void UpdateSelectedActionFromDesigner()
    {
        if (_currentMacro is null || _eventsGrid.SelectedRows.Count == 0)
        {
            return;
        }

        if (_eventsGrid.SelectedRows[0].DataBoundItem is not EventRow row || row.SourceEventIndex is not int idx || idx < 0 || idx >= _currentMacro.Events.Count)
        {
            MessageBox.Show("Selecione uma ação real na timeline para atualizar.");
            return;
        }

        var currentTs = _currentMacro.Events[idx].TimestampMs;
        if (!TryBuildEventFromDesigner(currentTs, out var updated))
        {
            return;
        }

        PushUndoState();
        _currentMacro.Events[idx] = updated;
        _hasUnsavedChanges = true;
        RefreshGrid();
        Log($"Ação na linha selecionada atualizada ({updated.Kind}).");
    }

    private bool TryBuildEventFromDesigner(long baseTimestamp, out MacroEvent result)
    {
        var kind = _designerActionType.SelectedItem?.ToString() ?? "Text input";
        var rawValue = _designerValue.Text;
        result = new MacroEvent
        {
            Kind = kind switch
            {
                "Text input" => "text_input",
                "Wait" => "wait",
                "Key down" => "key_down",
                "Key up" => "key_up",
                "Mouse left click" => "mouse_click",
                _ => "text_input"
            },
            TimestampMs = baseTimestamp,
            Data = new Dictionary<string, string>()
        };

        switch (result.Kind)
        {
            case "text_input":
                result.Data["text"] = rawValue;
                break;
            case "wait":
                if (!long.TryParse(rawValue, out var waitMs) || waitMs < 0)
                {
                    MessageBox.Show("Para Wait, informe milissegundos válidos.");
                    return false;
                }
                result.TimestampMs += waitMs;
                break;
            case "key_down":
            case "key_up":
                result.Data["key"] = rawValue;
                break;
            case "mouse_click":
                if (!string.IsNullOrWhiteSpace(rawValue) && TryParsePoint(rawValue, out var x, out var y))
                {
                    result.Data["x"] = x.ToString();
                    result.Data["y"] = y.ToString();
                }
                else
                {
                    var pos = Cursor.Position;
                    result.Data["x"] = pos.X.ToString();
                    result.Data["y"] = pos.Y.ToString();
                }

                result.Data["button"] = "Left";
                break;
        }

        if (!string.IsNullOrWhiteSpace(_designerLabel.Text))
        {
            result.Data["label"] = _designerLabel.Text.Trim();
        }

        if (!string.IsNullOrWhiteSpace(_designerComment.Text))
        {
            result.Data["comment"] = _designerComment.Text.Trim();
        }

        return true;
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

        PushUndoState();
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

        if (_eventsGrid.SelectedRows[0].DataBoundItem is not EventRow row || row.SourceEventIndex is not int sourceIndex)
        {
            return;
        }

        var targetSourceIndex = sourceIndex + direction;
        var selectedDisplayRow = _eventsGrid.SelectedRows[0].Index;
        var topIndex = _eventsGrid.FirstDisplayedScrollingRowIndex;

        if (!MoveEventToIndex(sourceIndex, targetSourceIndex))
        {
            return;
        }

        RefreshGrid();
        RestoreGridPosition(selectedDisplayRow + direction, topIndex);
        Log(direction < 0 ? "Ação movida para cima." : "Ação movida para baixo.");
    }

    private bool MoveEventToIndex(int sourceIndex, int targetIndex)
    {
        if (_currentMacro is null)
        {
            return false;
        }

        if (sourceIndex < 0 || sourceIndex >= _currentMacro.Events.Count || targetIndex < 0 || targetIndex >= _currentMacro.Events.Count || sourceIndex == targetIndex)
        {
            return false;
        }

        PushUndoState();
        var moving = _currentMacro.Events[sourceIndex];
        _currentMacro.Events.RemoveAt(sourceIndex);

        _currentMacro.Events.Insert(targetIndex, moving);
        _hasUnsavedChanges = true;
        return true;
    }

    private void EventsGrid_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            _dragRowStartIndex = -1;
            _dragStartPoint = Point.Empty;
            return;
        }

        var hit = _eventsGrid.HitTest(e.X, e.Y);
        _dragRowStartIndex = hit.RowIndex;
        _dragStartPoint = e.Location;
    }

    private void EventsGrid_MouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _dragRowStartIndex < 0 || _dragRowStartIndex >= _eventsGrid.Rows.Count)
        {
            return;
        }

        var dragSize = SystemInformation.DragSize;
        if (Math.Abs(e.X - _dragStartPoint.X) < dragSize.Width / 2 && Math.Abs(e.Y - _dragStartPoint.Y) < dragSize.Height / 2)
        {
            return;
        }

        if (_eventsGrid.Rows[_dragRowStartIndex].DataBoundItem is not EventRow row || row.SourceEventIndex is not int sourceIndex)
        {
            _dragRowStartIndex = -1;
            _dragStartPoint = Point.Empty;
            return;
        }

        _eventsGrid.DoDragDrop(sourceIndex, DragDropEffects.Move);
        _dragRowStartIndex = -1;
        _dragStartPoint = Point.Empty;
    }

    private void EventsGrid_DragOver(object? sender, DragEventArgs e)
    {
        var dragData = e.Data;
        if (dragData is null || !dragData.GetDataPresent(typeof(int)))
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        e.Effect = DragDropEffects.Move;
    }

    private void EventsGrid_DragDrop(object? sender, DragEventArgs e)
    {
        var dragData = e.Data;
        if (_currentMacro is null || dragData is null || !dragData.GetDataPresent(typeof(int)))
        {
            return;
        }

        if (dragData.GetData(typeof(int)) is not int sourceIndex)
        {
            return;
        }

        var clientPoint = _eventsGrid.PointToClient(new Point(e.X, e.Y));
        var hit = _eventsGrid.HitTest(clientPoint.X, clientPoint.Y);
        if (hit.RowIndex < 0 || hit.RowIndex >= _eventsGrid.Rows.Count)
        {
            return;
        }

        if (_eventsGrid.Rows[hit.RowIndex].DataBoundItem is not EventRow targetRow || targetRow.SourceEventIndex is not int targetSourceIndex)
        {
            return;
        }

        var topIndex = _eventsGrid.FirstDisplayedScrollingRowIndex;
        if (!MoveEventToIndex(sourceIndex, targetSourceIndex))
        {
            return;
        }

        RefreshGrid();

        var rows = _eventsGrid.DataSource as List<EventRow>;
        var displayRow = rows?.FindIndex(r => r.SourceEventIndex == targetSourceIndex) ?? hit.RowIndex;
        RestoreGridPosition(displayRow, topIndex);
        Log("Ação movida com arrastar e soltar.");
    }

    private void ApplyGridEdit(int rowIndex, int columnIndex)
    {
        if (_currentMacro is null || rowIndex < 0 || columnIndex < 0)
        {
            return;
        }

        if (_eventsGrid.Rows[rowIndex].DataBoundItem is not EventRow row || row.SourceEventIndex is not int sourceIndex)
        {
            return;
        }

        var colName = _eventsGrid.Columns[columnIndex].Name;
        var input = _eventsGrid.Rows[rowIndex].Cells[columnIndex].Value?.ToString() ?? string.Empty;

        PushUndoState();

        var ok = colName switch
        {
            "Value" => TryApplyValueEdit(row, input),
            "WaitMs" => TryApplyWaitEdit(sourceIndex, input),
            _ => false
        };

        if (!ok)
        {
            UndoInternal();
            Log("Edição inválida para esta linha. Use formato esperado.");
            RequestGridRefresh();
            return;
        }

        _hasUnsavedChanges = true;
        var topIndex = _eventsGrid.FirstDisplayedScrollingRowIndex;
        RequestGridRefresh(rowIndex, topIndex);
    }

    private void RequestGridRefresh(int? desiredRowIndex = null, int? desiredTopRow = null)
    {
        if (_gridRefreshQueued || IsDisposed)
        {
            return;
        }

        _gridRefreshQueued = true;
        BeginInvoke(new Action(() =>
        {
            try
            {
                RefreshGrid();
                if (desiredRowIndex.HasValue || desiredTopRow.HasValue)
                {
                    RestoreGridPosition(desiredRowIndex ?? 0, desiredTopRow ?? _eventsGrid.FirstDisplayedScrollingRowIndex);
                }
            }
            finally
            {
                _gridRefreshQueued = false;
            }
        }));
    }

    private void RestoreGridPosition(int desiredRowIndex, int desiredTopRow)
    {
        if (_eventsGrid.Rows.Count == 0)
        {
            return;
        }

        var rowIndex = Math.Clamp(desiredRowIndex, 0, _eventsGrid.Rows.Count - 1);
        _eventsGrid.ClearSelection();
        _eventsGrid.Rows[rowIndex].Selected = true;

        if (_eventsGrid.Columns.Count > 0)
        {
            _eventsGrid.CurrentCell = _eventsGrid.Rows[rowIndex].Cells[0];
        }

        if (desiredTopRow >= 0)
        {
            _eventsGrid.FirstDisplayedScrollingRowIndex = Math.Clamp(desiredTopRow, 0, _eventsGrid.Rows.Count - 1);
        }
    }

    private void SyncDesignerWithSelection()
    {
        if (_currentMacro is null || _eventsGrid.SelectedRows.Count == 0)
        {
            return;
        }

        if (_eventsGrid.SelectedRows[0].DataBoundItem is not EventRow row || row.SourceEventIndex is not int idx || idx < 0 || idx >= _currentMacro.Events.Count)
        {
            return;
        }

        var ev = _currentMacro.Events[idx];
        _designerActionType.SelectedItem = ev.Kind switch
        {
            "text_input" => "Text input",
            "wait" => "Wait",
            "key_down" => "Key down",
            "key_up" => "Key up",
            "mouse_down" => "Mouse left click",
            "mouse_click" => "Mouse left click",
            _ => _designerActionType.SelectedItem
        };

        _designerValue.Text = ev.Kind switch
        {
            "text_input" => ev.Data.GetValueOrDefault("text", string.Empty),
            "wait" => (idx > 0 ? Math.Max(ev.TimestampMs - _currentMacro.Events[idx - 1].TimestampMs, 0) : ev.TimestampMs).ToString(),
            "key_down" or "key_up" => ev.Data.GetValueOrDefault("key", string.Empty),
            "mouse_down" => $"{ev.Data.GetValueOrDefault("x", "0")}, {ev.Data.GetValueOrDefault("y", "0")}",
            "mouse_click" => $"{ev.Data.GetValueOrDefault("x", "0")}, {ev.Data.GetValueOrDefault("y", "0")}",
            _ => string.Empty
        };

        _designerLabel.Text = ev.Data.GetValueOrDefault("label", string.Empty);
        _designerComment.Text = ev.Data.GetValueOrDefault("comment", string.Empty);
    }

    private bool TryApplyValueEdit(EventRow row, string input)
    {
        var sourceIndex = row.SourceEventIndex;
        var kind = row.Kind;
        if (_currentMacro is null || sourceIndex is not int idx || idx < 0 || idx >= _currentMacro.Events.Count)
        {
            return false;
        }

        var ev = _currentMacro.Events[idx];
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

        if (kind == "mouse_click")
        {
            if (!TryParsePoint(input, out var x, out var y)) return false;
            ev.Data["x"] = x.ToString();
            ev.Data["y"] = y.ToString();

            if (row.SecondarySourceEventIndex is int upIdx && upIdx >= 0 && upIdx < _currentMacro.Events.Count)
            {
                _currentMacro.Events[upIdx].Data["x"] = x.ToString();
                _currentMacro.Events[upIdx].Data["y"] = y.ToString();
            }

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
        public int MinWaitMs { get; init; }
        public bool PlayMouseMoves { get; init; } = true;
        public bool PlayMouseClicks { get; init; } = true;
        public bool PlayKeyPresses { get; init; } = true;
        public bool PlayWaitTimes { get; init; } = true;
    }

    private sealed class MoveAggregation
    {
        public int? FirstSourceEventIndex { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
        public long TotalWaitMs { get; set; }
        public long LastTimestampMs { get; set; }
        public int Count { get; set; }
        public int Distance { get; set; }
        public int? LastSourceEventIndex { get; set; }
    }

    private sealed class EventRow
    {
        public int Index { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public long TimestampMs { get; set; }
        public long WaitMs { get; set; }
        public int? SourceEventIndex { get; set; }
        public int? SecondarySourceEventIndex { get; set; }
        public string Kind { get; set; } = string.Empty;
    }
}
