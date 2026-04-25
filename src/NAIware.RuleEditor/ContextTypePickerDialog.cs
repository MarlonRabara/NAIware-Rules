namespace NAIware.RuleEditor;

/// <summary>
/// Modal dialog that lets the user pick a .NET type from a DLL as the context
/// for a rule library. Supports case-insensitive filtering and keyboard navigation.
/// </summary>
public sealed class ContextTypePickerDialog : Form
{
    private readonly TextBox _filterTextBox = new()
    {
        Dock = DockStyle.Top,
        PlaceholderText = "Filter types (e.g., LoanApplication)...",
        Margin = new Padding(8)
    };

    private readonly ListBox _typeListBox = new()
    {
        Dock = DockStyle.Fill,
        IntegralHeight = false,
        Font = new Font("Segoe UI", 9.75f)
    };

    private readonly Button _okButton = new()
    {
        Text = "OK",
        DialogResult = DialogResult.OK,
        Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
        Width = 100,
        Height = 30,
        Enabled = false
    };

    private readonly Button _cancelButton = new()
    {
        Text = "Cancel",
        DialogResult = DialogResult.Cancel,
        Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
        Width = 100,
        Height = 30
    };

    private readonly IReadOnlyList<ReflectedTypeInfo> _types;

    /// <summary>Gets the type selected by the user, or null if none was selected.</summary>
    public ReflectedTypeInfo? SelectedType => _typeListBox.SelectedItem as ReflectedTypeInfo;

    /// <summary>Initializes the dialog with the candidate types.</summary>
    public ContextTypePickerDialog(
        IReadOnlyList<ReflectedTypeInfo> types,
        string title = "Select Context Type",
        string? headerText = null,
        string filterPlaceholder = "Filter types (e.g., LoanApplication)...")
    {
        ArgumentNullException.ThrowIfNull(types);
        _types = types;

        Text = title;
        Width = 820;
        Height = 540;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(520, 360);
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        _filterTextBox.PlaceholderText = filterPlaceholder;
        _typeListBox.DisplayMember = nameof(ReflectedTypeInfo.DisplayName);

        var header = new Label
        {
            Text = headerText ?? $"{types.Count} public concrete type(s) discovered. Select one to use as the rule context.",
            Dock = DockStyle.Top,
            Height = 32,
            Padding = new Padding(10, 8, 10, 0),
            ForeColor = SystemColors.GrayText
        };

        var listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 0) };
        listPanel.Controls.Add(_typeListBox);

        var filterPanel = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(10, 4, 10, 4) };
        filterPanel.Controls.Add(_filterTextBox);

        var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(10) };
        _okButton.Location = new Point(buttonPanel.Width - 220, 10);
        _cancelButton.Location = new Point(buttonPanel.Width - 110, 10);
        buttonPanel.Resize += (_, _) =>
        {
            _okButton.Location = new Point(buttonPanel.Width - 220, 10);
            _cancelButton.Location = new Point(buttonPanel.Width - 110, 10);
        };
        buttonPanel.Controls.Add(_okButton);
        buttonPanel.Controls.Add(_cancelButton);

        Controls.Add(listPanel);
        Controls.Add(filterPanel);
        Controls.Add(header);
        Controls.Add(buttonPanel);

        _filterTextBox.TextChanged += (_, _) => ApplyFilter();
        _typeListBox.SelectedIndexChanged += (_, _) => _okButton.Enabled = _typeListBox.SelectedItem is not null;
        _typeListBox.DoubleClick += (_, _) =>
        {
            if (_typeListBox.SelectedItem is not null)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        Load += (_, _) => ApplyFilter();
    }

    private void ApplyFilter()
    {
        string filter = _filterTextBox.Text.Trim();
        _typeListBox.BeginUpdate();
        try
        {
            _typeListBox.Items.Clear();
            foreach (ReflectedTypeInfo type in _types.Where(t =>
                         string.IsNullOrWhiteSpace(filter)
                         || t.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            {
                _typeListBox.Items.Add(type);
            }

            if (_typeListBox.Items.Count > 0) _typeListBox.SelectedIndex = 0;
        }
        finally
        {
            _typeListBox.EndUpdate();
        }
    }
}
