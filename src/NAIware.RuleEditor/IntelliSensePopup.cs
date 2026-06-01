using NAIware.RuleIntelligence;

namespace NAIware.RuleEditor;

/// <summary>
/// Floating, non-activating IntelliSense popup built on <see cref="ToolStripDropDown"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ToolStripDropDown"/> is the WinForms primitive purpose-built for floating popups
/// (it backs every menu and combo dropdown). It is a top-level window that:
/// </para>
/// <list type="bullet">
///   <item><description>does not steal focus from the owner control,</description></item>
///   <item><description>does not raise <see cref="Form.Deactivate"/> on the owning form,</description></item>
///   <item><description>auto-closes when the user clicks outside,</description></item>
///   <item><description>renders above every sibling control regardless of clip bounds.</description></item>
/// </list>
/// <para>
/// While a <see cref="ToolStripDropDown"/> is visible the ToolStrip message filter intercepts
/// navigation and commit keys before they reach the focused control. We therefore override
/// <see cref="ProcessCmdKey"/> here so the popup itself owns Tab/Enter/Esc/Arrow handling,
/// mirroring how Visual Studio's completion controller works.
/// </para>
/// </remarks>
internal sealed class IntelliSensePopup : ToolStripDropDown
{
    private readonly ToolStripControlHost _host;

    /// <summary>The list box that displays completion items.</summary>
    public ListBox ListBox { get; }

    /// <summary>Raised when the user commits the current selection (Tab, Enter, or double-click).</summary>
    public event EventHandler? CommitRequested;

    /// <summary>Raised when the user dismisses the popup (Escape).</summary>
    public event EventHandler? CancelRequested;

    public IntelliSensePopup()
    {
        AutoSize = false;
        Margin = Padding.Empty;
        Padding = new Padding(1);
        DropShadowEnabled = true;
        AutoClose = true;
        TabStop = false;
        Size = new Size(360, 200);

        ListBox = new ListBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Cascadia Mono, Consolas", 9.25f),
            IntegralHeight = false,
            DisplayMember = nameof(RuleCompletionItem.Label),
            Dock = DockStyle.Fill,
            TabStop = false
        };
        ListBox.DoubleClick += (_, _) => CommitRequested?.Invoke(this, EventArgs.Empty);

        _host = new ToolStripControlHost(ListBox)
        {
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Size = new Size(Size.Width - 2, Size.Height - 2)
        };

        Items.Add(_host);
    }

    /// <summary>
    /// Shows the popup anchored at the supplied screen point. Focus stays on <paramref name="focusOwner"/>.
    /// </summary>
    public void ShowAt(Point screenLocation, Control focusOwner)
    {
        ArgumentNullException.ThrowIfNull(focusOwner);

        Rectangle workingArea = Screen.FromPoint(screenLocation).WorkingArea;
        int x = Math.Min(screenLocation.X, workingArea.Right - Width);
        int y = screenLocation.Y;
        if (y + Height > workingArea.Bottom) y = Math.Max(workingArea.Top, screenLocation.Y - Height - 4);

        Point clamped = new(Math.Max(workingArea.Left, x), y);

        if (Visible)
        {
            Location = clamped;
        }
        else
        {
            Show(clamped);
        }
    }

    /// <summary>
    /// Handles navigation and commit keys while the popup is open. Returning <c>true</c>
    /// suppresses delivery to the focused control so a Tab/Enter does not insert whitespace
    /// or a newline into the underlying text box.
    /// </summary>
    protected override bool ProcessCmdKey(ref Message m, Keys keyData)
    {
        if (!Visible) return base.ProcessCmdKey(ref m, keyData);

        // Strip modifier bits so Shift+Tab still commits like Tab (matches VS behavior).
        Keys key = keyData & Keys.KeyCode;

        switch (key)
        {
            case Keys.Down:
                MoveSelection(+1);
                return true;
            case Keys.Up:
                MoveSelection(-1);
                return true;
            case Keys.PageDown:
                MoveSelection(+VisibleItemCount());
                return true;
            case Keys.PageUp:
                MoveSelection(-VisibleItemCount());
                return true;
            case Keys.Home:
                if (ListBox.Items.Count > 0) ListBox.SelectedIndex = 0;
                return true;
            case Keys.End:
                if (ListBox.Items.Count > 0) ListBox.SelectedIndex = ListBox.Items.Count - 1;
                return true;
            case Keys.Tab:
            case Keys.Enter:
                CommitRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Escape:
                CancelRequested?.Invoke(this, EventArgs.Empty);
                return true;
        }

        return base.ProcessCmdKey(ref m, keyData);
    }

    private void MoveSelection(int delta)
    {
        int count = ListBox.Items.Count;
        if (count == 0) return;
        int next = Math.Clamp(ListBox.SelectedIndex + delta, 0, count - 1);
        if (next != ListBox.SelectedIndex) ListBox.SelectedIndex = next;
    }

    private int VisibleItemCount()
    {
        int itemHeight = Math.Max(1, ListBox.ItemHeight);
        return Math.Max(1, ListBox.ClientSize.Height / itemHeight);
    }
}

