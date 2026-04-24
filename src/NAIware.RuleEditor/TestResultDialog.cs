using NAIware.Rules.Runtime;

namespace NAIware.RuleEditor;

/// <summary>
/// Modal dialog that displays the outcome of a rule test run, including matches,
/// mismatches, and mismatch diagnostics.
/// </summary>
public sealed class TestResultDialog : Form
{
    private readonly ListView _summaryList = new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        FullRowSelect = true,
        GridLines = true,
        MultiSelect = false,
        Font = new Font("Segoe UI", 9.25f)
    };

    private readonly TextBox _detailBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font("Cascadia Mono, Consolas", 9.5f),
        BackColor = Color.White
    };

    /// <summary>Creates the result dialog.</summary>
    public TestResultDialog(RuleEvaluationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        Text = $"Test Results - {result.ContextName}";
        Width = 960;
        Height = 620;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(640, 400);

        var summaryHeader = new Label
        {
            Dock = DockStyle.Top,
            Height = 42,
            Padding = new Padding(12, 10, 12, 0),
            Font = new Font("Segoe UI Semibold", 10f),
            Text = $"Evaluated {result.TotalEvaluated} rule(s) · " +
                   $"{result.Matches.Count} match(es) · {result.Mismatches.Count} mismatch(es) · " +
                   $"{result.EvaluatedUtc:u}"
        };

        _summaryList.Columns.Add("Outcome", 90);
        _summaryList.Columns.Add("Rule", 240);
        _summaryList.Columns.Add("Code", 100);
        _summaryList.Columns.Add("Severity", 90);
        _summaryList.Columns.Add("Message / Explanation", 420);

        foreach (RuleExpressionResult match in result.Matches)
        {
            var item = new ListViewItem("Match")
            {
                UseItemStyleForSubItems = false,
                ForeColor = Color.DarkGreen
            };
            item.SubItems.Add(match.ExpressionName);
            item.SubItems.Add(match.Result?.Code ?? string.Empty);
            item.SubItems.Add(match.Result?.Severity ?? string.Empty);
            item.SubItems.Add(match.Result?.Message ?? string.Empty);
            item.Tag = match;
            _summaryList.Items.Add(item);
        }

        foreach (RuleExpressionResult mismatch in result.Mismatches)
        {
            var item = new ListViewItem("Mismatch")
            {
                UseItemStyleForSubItems = false,
                ForeColor = Color.DarkSlateGray
            };
            item.SubItems.Add(mismatch.ExpressionName);
            item.SubItems.Add(string.Empty);
            item.SubItems.Add(string.Empty);
            item.SubItems.Add(mismatch.Diagnostic?.Explanation ?? "(no diagnostic)");
            item.Tag = mismatch;
            _summaryList.Items.Add(item);
        }

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal
        };
        split.Panel1.Controls.Add(_summaryList);
        split.Panel2.Controls.Add(_detailBox);

        var closeButton = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            Width = 100,
            Height = 30
        };
        var buttonBar = new Panel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(12) };
        closeButton.Location = new Point(buttonBar.Width - 112, 10);
        buttonBar.Resize += (_, _) => closeButton.Location = new Point(buttonBar.Width - 112, 10);
        buttonBar.Controls.Add(closeButton);

        Controls.Add(split);
        Controls.Add(summaryHeader);
        Controls.Add(buttonBar);

        AcceptButton = closeButton;
        CancelButton = closeButton;

        _summaryList.SelectedIndexChanged += (_, _) => ShowDetailForSelection();
        _detailBox.Text = RuleTestService.FormatReport(result);

        Shown += (_, _) =>
        {
            split.SplitterDistance = (int)(Height * 0.5);
            if (_summaryList.Items.Count > 0) _summaryList.Items[0].Selected = true;
        };
    }

    private void ShowDetailForSelection()
    {
        if (_summaryList.SelectedItems.Count == 0) return;
        object? tag = _summaryList.SelectedItems[0].Tag;

        if (tag is RuleExpressionResult result)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"Rule:        {result.ExpressionName}");
            builder.AppendLine($"Identity:    {result.ExpressionIdentity:N}");
            builder.AppendLine($"Matched:     {result.Matched}");

            if (result.Result is not null)
            {
                builder.AppendLine();
                builder.AppendLine($"Code:        {result.Result.Code}");
                builder.AppendLine($"Severity:    {result.Result.Severity ?? "(none)"}");
                builder.AppendLine($"Message:     {result.Result.Message}");
            }

            if (result.Diagnostic is not null)
            {
                builder.AppendLine();
                builder.AppendLine("Diagnostic:");
                builder.AppendLine($"  Expression:  {result.Diagnostic.Expression}");
                if (!string.IsNullOrWhiteSpace(result.Diagnostic.Explanation))
                    builder.AppendLine($"  Explanation: {result.Diagnostic.Explanation}");

                if (result.Diagnostic.EvaluatedParameters.Count > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine("  Evaluated parameters:");
                    foreach (var kvp in result.Diagnostic.EvaluatedParameters.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                        builder.AppendLine($"    {kvp.Key} = {kvp.Value ?? "(null)"}");
                }
            }

            _detailBox.Text = builder.ToString();
        }
    }
}
