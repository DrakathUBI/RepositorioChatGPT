using System.Text;

namespace MacroStudio;

public class MainForm : Form
{
    private readonly TextBox _macroPath = new() { Width = 620 };
    private readonly TextBox _params = new() { Width = 620, Text = "" };
    private readonly TextBox _speed = new() { Width = 80, Text = "1.0" };
    private readonly TextBox _stopKey = new() { Width = 80, Text = "F8" };
    private readonly TextBox _model = new() { Width = 180, Text = "gemini-2.0-flash" };
    private readonly TextBox _log = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Width = 760, Height = 260 };

    private readonly MacroRecorderService _recorder = new();
    private readonly MacroPlayerService _player = new();
    private readonly GeminiCoachService _coach = new();
    private CancellationTokenSource? _playCts;

    public MainForm()
    {
        Text = "Macro Studio (.exe)";
        Width = 820;
        Height = 520;

        var y = 12;
        Controls.Add(new Label { Left = 12, Top = y, Width = 200, Text = "Arquivo macro (.json)" });
        y += 20;
        _macroPath.Left = 12;
        _macroPath.Top = y;
        Controls.Add(_macroPath);
        var btnBrowse = new Button { Left = 640, Top = y - 1, Width = 120, Text = "Abrir" };
        btnBrowse.Click += (_, _) => BrowseMacro();
        Controls.Add(btnBrowse);

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
        btnInspect.Click += async (_, _) => await InspectAsync();
        btnAi.Click += async (_, _) => await AnalyzeAsync();

        Controls.Add(btnRecord);
        Controls.Add(btnStopRecord);
        Controls.Add(btnPlay);
        Controls.Add(btnStopPlay);
        Controls.Add(btnInspect);
        Controls.Add(btnAi);

        y += 40;
        _log.Left = 12;
        _log.Top = y;
        Controls.Add(_log);
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
        Log($"Macro salva em {_macroPath.Text} ({macro.Events.Count} eventos).");
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
            await _player.PlayAsync(macro, parameters, speed, _playCts.Token);
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

    private async Task InspectAsync()
    {
        if (!File.Exists(_macroPath.Text))
        {
            MessageBox.Show("Selecione um arquivo de macro válido.");
            return;
        }

        var macro = await MacroStorage.LoadAsync(_macroPath.Text);
        Log($"Nome: {macro.Name}");
        Log($"Criada em: {macro.CreatedAt:u}");
        Log($"Eventos: {macro.Events.Count}");
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
}
