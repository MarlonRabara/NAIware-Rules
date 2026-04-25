using System.Text.Json;
using System.Xml.Serialization;

namespace NAIware.RuleEditor;

/// <summary>
/// Prompts the user to select a JSON or XML file and hydrates its contents into
/// an instance of the selected context type. The hydrated object is then evaluated
/// against the active library by <see cref="RuleTestService"/>.
/// </summary>
public sealed class TestDataDialog : Form
{
    private readonly TextBox _fileTextBox = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        Font = new Font("Segoe UI", 9.75f)
    };

    private readonly Button _browseButton = new()
    {
        Text = "Browse...",
        Dock = DockStyle.Right,
        Width = 100,
        Height = 28
    };

    private readonly Button _runButton = new()
    {
        Text = "Load && Run",
        DialogResult = DialogResult.None, // Controlled manually so we can trap load failures.
        Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
        Width = 120,
        Height = 30
    };

    private readonly Button _cancelButton = new()
    {
        Text = "Cancel",
        DialogResult = DialogResult.Cancel,
        Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
        Width = 100,
        Height = 30
    };

    private readonly Type _contextType;

    /// <summary>Gets the hydrated object if loading succeeded; otherwise null.</summary>
    public object? LoadedObject { get; private set; }

    /// <summary>Gets the full path of the selected test data file.</summary>
    public string? SelectedFile => _fileTextBox.Text;

    /// <summary>Creates the test data dialog for the supplied context type.</summary>
    public TestDataDialog(Type contextType)
    {
        ArgumentNullException.ThrowIfNull(contextType);
        _contextType = contextType;

        Text = $"Load Test Data - {contextType.FullName}";
        Width = 680;
        Height = 220;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        AcceptButton = _runButton;
        CancelButton = _cancelButton;

        var instructions = new Label
        {
            Text = $"Select a JSON or XML file that can be deserialized into:\n    {contextType.FullName}",
            Dock = DockStyle.Top,
            Height = 54,
            Padding = new Padding(12, 12, 12, 0),
            ForeColor = SystemColors.ControlText
        };

        var filePanel = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(12, 4, 12, 4) };
        filePanel.Controls.Add(_fileTextBox);
        filePanel.Controls.Add(_browseButton);

        var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(12) };
        _runButton.Location = new Point(buttonPanel.Width - 240, 12);
        _cancelButton.Location = new Point(buttonPanel.Width - 112, 12);
        buttonPanel.Resize += (_, _) =>
        {
            _runButton.Location = new Point(buttonPanel.Width - 240, 12);
            _cancelButton.Location = new Point(buttonPanel.Width - 112, 12);
        };
        buttonPanel.Controls.Add(_runButton);
        buttonPanel.Controls.Add(_cancelButton);

        Controls.Add(filePanel);
        Controls.Add(instructions);
        Controls.Add(buttonPanel);

        _browseButton.Click += (_, _) => BrowseForFile();
        _runButton.Click += (_, _) => AttemptLoad();
    }

    private void BrowseForFile()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "JSON and XML files (*.json;*.xml)|*.json;*.xml|JSON (*.json)|*.json|XML (*.xml)|*.xml|All files (*.*)|*.*",
            Title = "Select Test Data File"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _fileTextBox.Text = dialog.FileName;
        }
    }

    private void AttemptLoad()
    {
        try
        {
            LoadedObject = LoadObjectFromFile(_fileTextBox.Text, _contextType);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Unable to load test data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    public static object LoadObjectFromFile(string path, Type type)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new FileNotFoundException("Please select a valid test data file.", path);

        string extension = Path.GetExtension(path).ToLowerInvariant();
        using FileStream stream = File.OpenRead(path);

        return extension switch
        {
            ".json" => JsonSerializer.Deserialize(stream, type, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("JSON deserialized to null."),
            ".xml" => new XmlSerializer(type).Deserialize(stream)
                ?? throw new InvalidOperationException("XML deserialized to null."),
            _ => throw new InvalidOperationException("Unsupported test file format. Use JSON or XML.")
        };
    }
}
