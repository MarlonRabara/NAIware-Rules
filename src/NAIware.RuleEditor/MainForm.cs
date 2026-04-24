using System.ComponentModel;
using NAIware.Rules.Runtime;

namespace NAIware.RuleEditor;

/// <summary>
/// The main rule editor window. Provides a three-panel developer experience:
/// library tree on the left, tabbed editor on the right, and a Visual Studio
/// style error list along the bottom.
/// </summary>
public sealed class MainForm : Form
{
    // Services
    private readonly AssemblyTypeDiscoveryService _typeDiscovery = new();
    private readonly IntelliSenseService _intellisense;
    private readonly RuleValidationService _validator;
    private readonly RuleTestService _testService = new();

    // State
    private RuleLibraryDocument _library = new();
    private string? _currentFile;
    private bool _dirty;
    private bool _suspendBinding;

    // Tree & toolbar
    private readonly TreeView _tree = new()
    {
        Dock = DockStyle.Fill,
        HideSelection = false,
        ShowNodeToolTips = true,
        Font = new Font("Segoe UI", 9.5f)
    };

    private readonly ImageList _treeImages = new() { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
    private readonly ImageList _errorImages = new() { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };

    // Editor controls
    private readonly TabControl _editorTabs = new() { Dock = DockStyle.Fill };
    private readonly Label _headerLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 36,
        Padding = new Padding(12, 10, 12, 6),
        Font = new Font("Segoe UI Semibold", 10.5f),
        Text = "Select an item in the library tree."
    };

    // Rule metadata
    private readonly TextBox _nameTextBox = new() { Dock = DockStyle.Top };
    private readonly TextBox _descriptionTextBox = new() { Dock = DockStyle.Top, Height = 60, Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly CheckBox _activeCheckBox = new() { Dock = DockStyle.Top, Text = "Enabled", Height = 28 };
    private readonly NumericUpDown _priorityNumeric = new() { Dock = DockStyle.Top, Minimum = 0, Maximum = 9999 };
    private readonly TextBox _tagsTextBox = new() { Dock = DockStyle.Top, PlaceholderText = "Comma-separated tags" };

    // Expression editor
    private readonly TextBox _expressionTextBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ScrollBars = ScrollBars.Both,
        AcceptsTab = true,
        Font = new Font("Cascadia Mono, Consolas", 10.5f),
        WordWrap = false
    };

    private readonly ListBox _intellisenseListBox = new()
    {
        Visible = false,
        Width = 280,
        Height = 180,
        Font = new Font("Cascadia Mono, Consolas", 9.5f),
        IntegralHeight = false
    };

    // Result definition
    private readonly TextBox _resultCodeTextBox = new() { Dock = DockStyle.Top };
    private readonly TextBox _resultMessageTextBox = new() { Dock = DockStyle.Top, Height = 60, Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly ComboBox _severityComboBox = new() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _optionalValueTextBox = new() { Dock = DockStyle.Top };

    // Context editor
    private readonly TextBox _contextNameTextBox = new() { Dock = DockStyle.Top };
    private readonly TextBox _contextTypeTextBox = new() { Dock = DockStyle.Top, ReadOnly = true };
    private readonly TextBox _contextDescriptionTextBox = new() { Dock = DockStyle.Top, Height = 60, Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox _contextAssemblyTextBox = new() { Dock = DockStyle.Top, ReadOnly = true };

    // Category editor
    private readonly TextBox _categoryNameTextBox = new() { Dock = DockStyle.Top };
    private readonly TextBox _categoryDescriptionTextBox = new() { Dock = DockStyle.Top, Height = 60, Multiline = true, ScrollBars = ScrollBars.Vertical };

    // Library editor
    private readonly TextBox _libraryNameTextBox = new() { Dock = DockStyle.Top };
    private readonly TextBox _libraryDescriptionTextBox = new() { Dock = DockStyle.Top, Height = 60, Multiline = true, ScrollBars = ScrollBars.Vertical };

    // Error list
    private readonly ListView _errorList = new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        FullRowSelect = true,
        GridLines = false,
        MultiSelect = false,
        Font = new Font("Segoe UI", 9.25f)
    };

    private readonly StatusStrip _statusStrip = new();
    private readonly ToolStripStatusLabel _statusLabel = new() { Text = "Ready", Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _countsLabel = new() { Text = "0 errors · 0 warnings" };

    // Editor tab page references (so we can toggle visibility)
    private TabPage? _expressionTab;
    private TabPage? _metadataTab;
    private TabPage? _resultTab;
    private TabPage? _contextTab;
    private TabPage? _categoryTab;
    private TabPage? _libraryTab;

    /// <summary>Creates the main form and wires up all services.</summary>
    public MainForm()
    {
        _intellisense = new IntelliSenseService(_typeDiscovery);
        _validator = new RuleValidationService(_intellisense);

        Text = "NAIware Rule Editor";
        Width = 1360;
        Height = 860;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(960, 640);
        KeyPreview = true;

        BuildIcons();
        _tree.ImageList = _treeImages;

        _severityComboBox.Items.AddRange(["Info", "Warning", "Error"]);
        _severityComboBox.SelectedIndex = 1;

        Controls.Add(BuildMainSplit());
        Controls.Add(BuildToolbar());
        Controls.Add(BuildMenu());
        Controls.Add(BuildStatusBar());

        _tree.AfterSelect += OnTreeSelectionChanged;
        _tree.NodeMouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                _tree.SelectedNode = e.Node;
                ShowTreeContextMenu(e.Location);
            }
        };

        WireUpFieldEvents();
        WireUpIntelliSense();

        _expressionTextBox.KeyDown += OnExpressionKeyDown;
        _errorList.MouseDoubleClick += (_, _) => NavigateToSelectedError();

        FormClosing += OnFormClosing;

        CreateSampleLibrary();
        RefreshTree();
        UpdateWindowTitle();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Layout construction
    // ─────────────────────────────────────────────────────────────────────────

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip { Dock = DockStyle.Top, RenderMode = ToolStripRenderMode.System };

        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add(MenuItem("&New Library", Keys.Control | Keys.N, (_, _) => NewLibrary()));
        fileMenu.DropDownItems.Add(MenuItem("&Open Library...", Keys.Control | Keys.O, (_, _) => OpenLibrary()));
        fileMenu.DropDownItems.Add(MenuItem("&Save Library", Keys.Control | Keys.S, (_, _) => SaveLibrary()));
        fileMenu.DropDownItems.Add(MenuItem("Save Library &As...", Keys.Control | Keys.Shift | Keys.S, (_, _) => SaveLibraryAs()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(MenuItem("E&xit", Keys.Alt | Keys.F4, (_, _) => Close()));

        var libraryMenu = new ToolStripMenuItem("&Library");
        libraryMenu.DropDownItems.Add(MenuItem("Add Context From &DLL...", Keys.Control | Keys.D, (_, _) => AddContextFromDll()));
        libraryMenu.DropDownItems.Add(MenuItem("Add &Category", Keys.None, (_, _) => AddCategory()));
        libraryMenu.DropDownItems.Add(MenuItem("Add &Subcategory", Keys.None, (_, _) => AddSubcategory()));
        libraryMenu.DropDownItems.Add(MenuItem("Add &Rule", Keys.Control | Keys.R, (_, _) => AddRule()));
        libraryMenu.DropDownItems.Add(new ToolStripSeparator());
        libraryMenu.DropDownItems.Add(MenuItem("Delete Selected", Keys.Delete, (_, _) => DeleteSelected()));

        var testMenu = new ToolStripMenuItem("&Test");
        testMenu.DropDownItems.Add(MenuItem("&Validate Library", Keys.F6, (_, _) => ValidateLibrary()));
        testMenu.DropDownItems.Add(MenuItem("Run &Test Rules...", Keys.F5, (_, _) => TestRules()));

        var helpMenu = new ToolStripMenuItem("&Help");
        helpMenu.DropDownItems.Add(MenuItem("&About...", Keys.None, (_, _) => ShowAbout()));

        menu.Items.AddRange([fileMenu, libraryMenu, testMenu, helpMenu]);
        return menu;
    }

    private ToolStrip BuildToolbar()
    {
        var strip = new ToolStrip
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden,
            RenderMode = ToolStripRenderMode.System,
            ImageScalingSize = new Size(16, 16)
        };

        strip.Items.Add(ToolbarButton("New", "Create a new library", (_, _) => NewLibrary()));
        strip.Items.Add(ToolbarButton("Open", "Open a library from JSON", (_, _) => OpenLibrary()));
        strip.Items.Add(ToolbarButton("Save", "Save the current library", (_, _) => SaveLibrary()));
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(ToolbarButton("Add Context From DLL", "Select a DLL and pick a context type", (_, _) => AddContextFromDll()));
        strip.Items.Add(ToolbarButton("Add Category", "Add a category under the selected context", (_, _) => AddCategory()));
        strip.Items.Add(ToolbarButton("Add Subcategory", "Add a subcategory under the selected category", (_, _) => AddSubcategory()));
        strip.Items.Add(ToolbarButton("Add Rule", "Add a rule expression under the selected node", (_, _) => AddRule()));
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(ToolbarButton("Validate Library", "Run full library validation (F6)", (_, _) => ValidateLibrary()));
        strip.Items.Add(ToolbarButton("Test Rules", "Load a JSON/XML object and run rules against it (F5)", (_, _) => TestRules()));
        return strip;
    }

    private Control BuildMainSplit()
    {
        _errorList.Columns.Add("", 24);            // icon column
        _errorList.Columns.Add("Severity", 90);
        _errorList.Columns.Add("Description", 540);
        _errorList.Columns.Add("Context", 220);
        _errorList.Columns.Add("Category", 160);
        _errorList.Columns.Add("Rule", 180);
        _errorList.Columns.Add("Expression Id", 110);
        _errorList.SmallImageList = _errorImages;

        var errorPanel = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = "Error List",
            Padding = new Padding(4)
        };
        errorPanel.Controls.Add(_errorList);

        BuildEditorTabs();

        var treeGroup = new GroupBox { Dock = DockStyle.Fill, Text = "Rule Library", Padding = new Padding(4) };
        treeGroup.Controls.Add(_tree);

        var editorHost = new Panel { Dock = DockStyle.Fill };
        editorHost.Controls.Add(_editorTabs);
        editorHost.Controls.Add(_headerLabel);

        var upperSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            Panel1MinSize = 260
        };
        upperSplit.Panel1.Controls.Add(treeGroup);
        upperSplit.Panel2.Controls.Add(editorHost);
        upperSplit.HandleCreated += (_, _) => upperSplit.SplitterDistance = 380;

        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            Panel2MinSize = 140
        };
        mainSplit.Panel1.Controls.Add(upperSplit);
        mainSplit.Panel2.Controls.Add(errorPanel);
        mainSplit.HandleCreated += (_, _) => mainSplit.SplitterDistance = 560;

        return mainSplit;
    }

    private void BuildEditorTabs()
    {
        _expressionTab = new TabPage("Expression") { Padding = new Padding(12) };
        _metadataTab = new TabPage("Metadata") { Padding = new Padding(12) };
        _resultTab = new TabPage("Result Definition") { Padding = new Padding(12) };
        _contextTab = new TabPage("Context") { Padding = new Padding(12) };
        _categoryTab = new TabPage("Category") { Padding = new Padding(12) };
        _libraryTab = new TabPage("Library") { Padding = new Padding(12) };

        _expressionTab.Controls.Add(BuildExpressionEditor());
        _metadataTab.Controls.Add(BuildMetadataEditor());
        _resultTab.Controls.Add(BuildResultEditor());
        _contextTab.Controls.Add(BuildContextEditor());
        _categoryTab.Controls.Add(BuildCategoryEditor());
        _libraryTab.Controls.Add(BuildLibraryEditor());
    }

    private Control BuildExpressionEditor()
    {
        var host = new Panel { Dock = DockStyle.Fill };
        host.Controls.Add(_expressionTextBox);
        host.Controls.Add(_intellisenseListBox);

        var hint = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            ForeColor = SystemColors.GrayText,
            Padding = new Padding(2, 6, 2, 0),
            Text = "Supports AND / OR, parentheses, dot notation (Foo.Bar.Baz), indexed collections (Items.0), and quoted strings."
        };

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.Controls.Add(host, 0, 0);
        panel.Controls.Add(hint, 0, 1);

        return panel;
    }

    private Control BuildMetadataEditor() =>
        StackPanel(
            ("Priority", _priorityNumeric),
            ("Tags", _tagsTextBox),
            ("Enabled", _activeCheckBox),
            ("Description", _descriptionTextBox),
            ("Name", _nameTextBox));

    private Control BuildResultEditor() =>
        StackPanel(
            ("Optional Value", _optionalValueTextBox),
            ("Severity", _severityComboBox),
            ("Message", _resultMessageTextBox),
            ("Code", _resultCodeTextBox));

    private Control BuildContextEditor() =>
        StackPanel(
            ("Source Assembly", _contextAssemblyTextBox),
            ("Description", _contextDescriptionTextBox),
            ("Qualified Type Name", _contextTypeTextBox),
            ("Context Name", _contextNameTextBox));

    private Control BuildCategoryEditor() =>
        StackPanel(
            ("Description", _categoryDescriptionTextBox),
            ("Category Name", _categoryNameTextBox));

    private Control BuildLibraryEditor() =>
        StackPanel(
            ("Description", _libraryDescriptionTextBox),
            ("Library Name", _libraryNameTextBox));

    private StatusStrip BuildStatusBar()
    {
        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.Items.Add(_countsLabel);
        return _statusStrip;
    }

    private static Control StackPanel(params (string Label, Control Control)[] rowsTopToBottom)
    {
        // WinForms Dock=Top stacks in reverse order; we accept the list top-to-bottom
        // visually and iterate the array as given, which puts the first entry at the bottom.
        // We reverse here so the first entry in the argument list appears at the top.
        var panel = new Panel { Dock = DockStyle.Fill };

        foreach ((string labelText, Control control) in rowsTopToBottom)
        {
            var label = new Label
            {
                Text = labelText,
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = SystemColors.ControlText,
                Padding = new Padding(0, 4, 0, 0)
            };
            control.Dock = control.Dock == DockStyle.None ? DockStyle.Top : control.Dock;
            control.Margin = new Padding(0, 0, 0, 8);

            panel.Controls.Add(control);
            panel.Controls.Add(label);
        }

        return panel;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Icons (drawn programmatically so no binary assets are required)
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildIcons()
    {
        _treeImages.Images.Add("library", RenderIcon("L", Color.SteelBlue));
        _treeImages.Images.Add("context", RenderIcon("C", Color.MediumSeaGreen));
        _treeImages.Images.Add("category", RenderIcon("G", Color.DarkOrange));
        _treeImages.Images.Add("rule", RenderIcon("R", Color.MediumPurple));

        _errorImages.Images.Add("Error", RenderIcon("!", Color.Firebrick));
        _errorImages.Images.Add("Warning", RenderIcon("!", Color.Goldenrod));
        _errorImages.Images.Add("Info", RenderIcon("i", Color.SteelBlue));
    }

    private static Image RenderIcon(string glyph, Color color)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 0, 0, 15, 15);
        using var textBrush = new SolidBrush(Color.White);
        using var font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);
        var size = g.MeasureString(glyph, font);
        g.DrawString(glyph, font, textBrush, (16 - size.Width) / 2, (16 - size.Height) / 2);
        return bmp;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // File operations
    // ─────────────────────────────────────────────────────────────────────────

    private bool ConfirmDiscardChanges()
    {
        if (!_dirty) return true;

        DialogResult choice = MessageBox.Show(this,
            "The current library has unsaved changes. Save before continuing?",
            "Unsaved Changes",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        return choice switch
        {
            DialogResult.Yes => SaveLibrary(),
            DialogResult.No => true,
            _ => false
        };
    }

    private void NewLibrary()
    {
        if (!ConfirmDiscardChanges()) return;
        _library = new RuleLibraryDocument();
        _currentFile = null;
        _dirty = false;
        _intellisense.Invalidate();
        RefreshTree();
        ClearEditors();
        _errorList.Items.Clear();
        UpdateStatus("New library created.");
        UpdateWindowTitle();
    }

    private void OpenLibrary()
    {
        if (!ConfirmDiscardChanges()) return;

        using var dialog = new OpenFileDialog
        {
            Filter = "Rule Library JSON (*.json)|*.json|All files (*.*)|*.*",
            Title = "Open Rule Library"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _library = RuleLibrarySerializer.Load(dialog.FileName);
            _currentFile = dialog.FileName;
            _dirty = false;
            _intellisense.Invalidate();
            RefreshTree();
            ClearEditors();
            _errorList.Items.Clear();
            UpdateStatus($"Opened {Path.GetFileName(_currentFile)}.");
            UpdateWindowTitle();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Unable to open library", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private bool SaveLibrary()
    {
        if (string.IsNullOrEmpty(_currentFile))
            return SaveLibraryAs();

        return SaveTo(_currentFile);
    }

    private bool SaveLibraryAs()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Rule Library JSON (*.json)|*.json",
            FileName = string.IsNullOrWhiteSpace(_library.Name) ? "rule-library.json" : $"{SafeFileName(_library.Name)}.json",
            Title = "Save Rule Library As"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return false;
        return SaveTo(dialog.FileName);
    }

    private bool SaveTo(string path)
    {
        try
        {
            FlushEditorsToModel();
            RuleLibrarySerializer.Save(path, _library);
            _currentFile = path;
            _dirty = false;
            UpdateStatus($"Saved {Path.GetFileName(path)}.");
            UpdateWindowTitle();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Unable to save library", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    private static string SafeFileName(string raw)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        return new string(raw.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tree editing
    // ─────────────────────────────────────────────────────────────────────────

    private void AddContextFromDll()
    {
        using var openDll = new OpenFileDialog
        {
            Filter = ".NET Assemblies (*.dll)|*.dll|All files (*.*)|*.*",
            Title = "Select Assembly"
        };

        if (openDll.ShowDialog(this) != DialogResult.OK) return;

        IReadOnlyList<ReflectedTypeInfo> types;
        try
        {
            types = _typeDiscovery.DiscoverTypes(openDll.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Unable to load assembly", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (types.Count == 0)
        {
            MessageBox.Show(this, "The selected assembly does not contain any public concrete classes.",
                "No Types Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var picker = new ContextTypePickerDialog(types);
        if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedType is null) return;

        _library.Contexts.Add(new RuleContextDocument
        {
            Name = picker.SelectedType.DisplayName,
            QualifiedTypeName = picker.SelectedType.FullName,
            AssemblyPath = picker.SelectedType.AssemblyPath
        });

        _intellisense.Invalidate();
        MarkDirty();
        RefreshTree();
        UpdateStatus($"Added context '{picker.SelectedType.DisplayName}'.");
    }

    private void AddCategory()
    {
        RuleContextDocument? context = FindSelectedContext();
        if (context is null)
        {
            MessageBox.Show(this, "Select a context before adding a category.", "Add Category",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        context.Categories.Add(new RuleCategoryDocument { Name = "New Category" });
        MarkDirty();
        RefreshTree();
    }

    private void AddSubcategory()
    {
        if (_tree.SelectedNode?.Tag is not RuleCategoryDocument parent)
        {
            MessageBox.Show(this, "Select a category before adding a subcategory.", "Add Subcategory",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        parent.Categories.Add(new RuleCategoryDocument { Name = "New Subcategory" });
        MarkDirty();
        RefreshTree();
    }

    private void AddRule()
    {
        RuleContextDocument? context = FindSelectedContext();
        if (context is null)
        {
            MessageBox.Show(this, "Select a context, category, or rule before adding a new rule.",
                "Add Rule", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var rule = new RuleExpressionDocument
        {
            Name = "NewRule",
            Expression = $"{context.Name}.Amount > 0"
        };

        context.Expressions.Add(rule);

        if (_tree.SelectedNode?.Tag is RuleCategoryDocument category)
            category.ExpressionIds.Add(rule.Id);

        MarkDirty();
        RefreshTree();
        SelectNodeForRule(rule.Id);
    }

    private void DeleteSelected()
    {
        TreeNode? node = _tree.SelectedNode;
        if (node?.Tag is null) return;

        string target = node.Tag switch
        {
            RuleContextDocument ctx => $"context '{ctx.Name}' and all of its categories and rules",
            RuleCategoryDocument cat => $"category '{cat.Name}'",
            RuleExpressionDocument rule => $"rule '{rule.Name}'",
            _ => "the selected item"
        };

        if (MessageBox.Show(this, $"Delete {target}? This cannot be undone.", "Delete",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
            return;

        switch (node.Tag)
        {
            case RuleContextDocument ctx:
                _library.Contexts.Remove(ctx);
                break;
            case RuleCategoryDocument cat:
                RemoveCategoryRecursive(_library, cat);
                break;
            case RuleExpressionDocument rule:
                RemoveRule(_library, rule);
                break;
        }

        MarkDirty();
        RefreshTree();
        ClearEditors();
    }

    private static void RemoveCategoryRecursive(RuleLibraryDocument library, RuleCategoryDocument target)
    {
        foreach (RuleContextDocument context in library.Contexts)
        {
            if (context.Categories.Remove(target)) return;
            foreach (RuleCategoryDocument category in context.Categories)
                if (RemoveFromCategoryTree(category, target)) return;
        }
    }

    private static bool RemoveFromCategoryTree(RuleCategoryDocument parent, RuleCategoryDocument target)
    {
        if (parent.Categories.Remove(target)) return true;
        foreach (RuleCategoryDocument child in parent.Categories)
            if (RemoveFromCategoryTree(child, target)) return true;
        return false;
    }

    private static void RemoveRule(RuleLibraryDocument library, RuleExpressionDocument rule)
    {
        foreach (RuleContextDocument context in library.Contexts)
        {
            if (context.Expressions.Remove(rule))
            {
                RemoveRuleIdFromCategories(context.Categories, rule.Id);
                return;
            }
        }
    }

    private static void RemoveRuleIdFromCategories(List<RuleCategoryDocument> categories, Guid id)
    {
        foreach (RuleCategoryDocument category in categories)
        {
            category.ExpressionIds.Remove(id);
            RemoveRuleIdFromCategories(category.Categories, id);
        }
    }

    private void ShowTreeContextMenu(Point location)
    {
        var menu = new ContextMenuStrip();

        if (_tree.SelectedNode?.Tag is RuleContextDocument)
        {
            menu.Items.Add("Add Category", null, (_, _) => AddCategory());
            menu.Items.Add("Add Rule", null, (_, _) => AddRule());
        }
        else if (_tree.SelectedNode?.Tag is RuleCategoryDocument)
        {
            menu.Items.Add("Add Subcategory", null, (_, _) => AddSubcategory());
            menu.Items.Add("Add Rule", null, (_, _) => AddRule());
        }

        if (menu.Items.Count > 0) menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Delete", null, (_, _) => DeleteSelected());
        menu.Show(_tree, location);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tree refresh & selection binding
    // ─────────────────────────────────────────────────────────────────────────

    private void RefreshTree()
    {
        Guid? previouslySelectedRule = (_tree.SelectedNode?.Tag as RuleExpressionDocument)?.Id;

        _tree.BeginUpdate();
        _tree.Nodes.Clear();

        var root = _tree.Nodes.Add(_library.Name);
        root.Tag = _library;
        root.ImageKey = root.SelectedImageKey = "library";
        root.ToolTipText = _library.Description;

        foreach (RuleContextDocument context in _library.Contexts)
        {
            TreeNode contextNode = root.Nodes.Add(context.Name);
            contextNode.Tag = context;
            contextNode.ImageKey = contextNode.SelectedImageKey = "context";
            contextNode.ToolTipText = context.QualifiedTypeName;

            foreach (RuleCategoryDocument category in context.Categories)
                AddCategoryNode(contextNode, category, context);

            List<RuleExpressionDocument> unassigned = context.Expressions
                .Where(e => !IsExpressionInAnyCategory(context.Categories, e.Id))
                .ToList();

            foreach (RuleExpressionDocument rule in unassigned)
                AddRuleNode(contextNode, rule);
        }

        _tree.EndUpdate();
        root.ExpandAll();

        if (previouslySelectedRule is Guid id) SelectNodeForRule(id);
    }

    private static void AddCategoryNode(TreeNode parent, RuleCategoryDocument category, RuleContextDocument context)
    {
        TreeNode node = parent.Nodes.Add(category.Name);
        node.Tag = category;
        node.ImageKey = node.SelectedImageKey = "category";
        node.ToolTipText = category.Description;

        foreach (RuleCategoryDocument child in category.Categories)
            AddCategoryNode(node, child, context);

        foreach (Guid id in category.ExpressionIds)
        {
            RuleExpressionDocument? rule = context.Expressions.FirstOrDefault(e => e.Id == id);
            if (rule is not null) AddRuleNode(node, rule);
        }
    }

    private static void AddRuleNode(TreeNode parent, RuleExpressionDocument rule)
    {
        TreeNode node = parent.Nodes.Add(rule.Name);
        node.Tag = rule;
        node.ImageKey = node.SelectedImageKey = "rule";
        node.ToolTipText = rule.Description;
    }

    private static bool IsExpressionInAnyCategory(IEnumerable<RuleCategoryDocument> categories, Guid id) =>
        categories.Any(c => c.ExpressionIds.Contains(id) || IsExpressionInAnyCategory(c.Categories, id));

    private void SelectNodeForRule(Guid id)
    {
        TreeNode? found = FindNode(_tree.Nodes, id);
        if (found is not null) _tree.SelectedNode = found;
    }

    private static TreeNode? FindNode(TreeNodeCollection nodes, Guid id)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag is RuleExpressionDocument rule && rule.Id == id) return node;
            TreeNode? nested = FindNode(node.Nodes, id);
            if (nested is not null) return nested;
        }
        return null;
    }

    private void OnTreeSelectionChanged(object? sender, TreeViewEventArgs e)
    {
        FlushEditorsToModel();
        BindSelectedNode();
    }

    private void BindSelectedNode()
    {
        _suspendBinding = true;
        try
        {
            object? tag = _tree.SelectedNode?.Tag;

            _editorTabs.TabPages.Clear();

            switch (tag)
            {
                case RuleExpressionDocument rule:
                    _headerLabel.Text = $"Rule · {rule.Name}";
                    _nameTextBox.Text = rule.Name;
                    _descriptionTextBox.Text = rule.Description;
                    _activeCheckBox.Checked = rule.IsActive;
                    _priorityNumeric.Value = Math.Clamp(rule.Priority, 0, 9999);
                    _tagsTextBox.Text = string.Join(", ", rule.Tags);
                    _expressionTextBox.Text = rule.Expression;
                    _resultCodeTextBox.Text = rule.ResultCode ?? string.Empty;
                    _resultMessageTextBox.Text = rule.ResultMessage ?? string.Empty;
                    _severityComboBox.SelectedItem = rule.Severity ?? "Warning";
                    _optionalValueTextBox.Text = rule.OptionalValue ?? string.Empty;

                    _editorTabs.TabPages.Add(_expressionTab!);
                    _editorTabs.TabPages.Add(_metadataTab!);
                    _editorTabs.TabPages.Add(_resultTab!);
                    _editorTabs.SelectedTab = _expressionTab;
                    break;

                case RuleContextDocument context:
                    _headerLabel.Text = $"Context · {context.Name}";
                    _contextNameTextBox.Text = context.Name;
                    _contextTypeTextBox.Text = context.QualifiedTypeName;
                    _contextDescriptionTextBox.Text = context.Description;
                    _contextAssemblyTextBox.Text = context.AssemblyPath ?? string.Empty;

                    _editorTabs.TabPages.Add(_contextTab!);
                    _editorTabs.SelectedTab = _contextTab;
                    break;

                case RuleCategoryDocument category:
                    _headerLabel.Text = $"Category · {category.Name}";
                    _categoryNameTextBox.Text = category.Name;
                    _categoryDescriptionTextBox.Text = category.Description;

                    _editorTabs.TabPages.Add(_categoryTab!);
                    _editorTabs.SelectedTab = _categoryTab;
                    break;

                case RuleLibraryDocument library:
                    _headerLabel.Text = $"Library · {library.Name}";
                    _libraryNameTextBox.Text = library.Name;
                    _libraryDescriptionTextBox.Text = library.Description;

                    _editorTabs.TabPages.Add(_libraryTab!);
                    _editorTabs.SelectedTab = _libraryTab;
                    break;

                default:
                    _headerLabel.Text = "Select an item in the library tree.";
                    break;
            }
        }
        finally
        {
            _suspendBinding = false;
        }
    }

    private void ClearEditors()
    {
        _editorTabs.TabPages.Clear();
        _headerLabel.Text = "Select an item in the library tree.";
    }

    private void FlushEditorsToModel()
    {
        if (_suspendBinding || _tree.SelectedNode?.Tag is null) return;

        switch (_tree.SelectedNode.Tag)
        {
            case RuleExpressionDocument rule:
                rule.Name = _nameTextBox.Text;
                rule.Description = _descriptionTextBox.Text;
                rule.IsActive = _activeCheckBox.Checked;
                rule.Priority = (int)_priorityNumeric.Value;
                rule.Tags = ParseTags(_tagsTextBox.Text);
                rule.Expression = _expressionTextBox.Text;
                rule.ResultCode = NullIfEmpty(_resultCodeTextBox.Text);
                rule.ResultMessage = NullIfEmpty(_resultMessageTextBox.Text);
                rule.Severity = _severityComboBox.SelectedItem?.ToString();
                rule.OptionalValue = NullIfEmpty(_optionalValueTextBox.Text);
                _tree.SelectedNode.Text = rule.Name;
                break;

            case RuleContextDocument context:
                context.Name = _contextNameTextBox.Text;
                context.Description = _contextDescriptionTextBox.Text;
                _tree.SelectedNode.Text = context.Name;
                break;

            case RuleCategoryDocument category:
                category.Name = _categoryNameTextBox.Text;
                category.Description = _categoryDescriptionTextBox.Text;
                _tree.SelectedNode.Text = category.Name;
                break;

            case RuleLibraryDocument library:
                library.Name = _libraryNameTextBox.Text;
                library.Description = _libraryDescriptionTextBox.Text;
                _tree.SelectedNode.Text = library.Name;
                UpdateWindowTitle();
                break;
        }
    }

    private static List<string> ParseTags(string raw) =>
        [.. raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    // ─────────────────────────────────────────────────────────────────────────
    // Validation & error list
    // ─────────────────────────────────────────────────────────────────────────

    private void ValidateLibrary()
    {
        FlushEditorsToModel();
        _intellisense.Invalidate();
        List<ValidationIssue> issues = _validator.Validate(_library);
        RenderErrorList(issues);

        int errors = issues.Count(i => string.Equals(i.Severity, "Error", StringComparison.OrdinalIgnoreCase));
        int warnings = issues.Count(i => string.Equals(i.Severity, "Warning", StringComparison.OrdinalIgnoreCase));
        UpdateStatus(issues.Count == 0 ? "Validation succeeded. No issues found." : $"Validation completed with {issues.Count} issue(s).");
        _countsLabel.Text = $"{errors} error(s) · {warnings} warning(s)";
    }

    private void RenderErrorList(IReadOnlyList<ValidationIssue> issues)
    {
        _errorList.BeginUpdate();
        _errorList.Items.Clear();

        foreach (ValidationIssue issue in issues
                     .OrderBy(i => SeverityOrder(i.Severity))
                     .ThenBy(i => i.Context)
                     .ThenBy(i => i.Rule))
        {
            var item = new ListViewItem(string.Empty)
            {
                ImageKey = issue.Severity,
                UseItemStyleForSubItems = false,
                Tag = issue
            };
            item.SubItems.Add(issue.Severity);
            item.SubItems.Add(issue.Message);
            item.SubItems.Add(issue.Context);
            item.SubItems.Add(issue.Category);
            item.SubItems.Add(issue.Rule);
            item.SubItems.Add(issue.ExpressionId);

            if (string.Equals(issue.Severity, "Error", StringComparison.OrdinalIgnoreCase))
                item.SubItems[1].ForeColor = Color.Firebrick;
            else if (string.Equals(issue.Severity, "Warning", StringComparison.OrdinalIgnoreCase))
                item.SubItems[1].ForeColor = Color.DarkGoldenrod;
            else
                item.SubItems[1].ForeColor = Color.SteelBlue;

            _errorList.Items.Add(item);
        }

        _errorList.EndUpdate();
    }

    private static int SeverityOrder(string severity) => severity?.ToLowerInvariant() switch
    {
        "error" => 0,
        "warning" => 1,
        _ => 2
    };

    private void NavigateToSelectedError()
    {
        if (_errorList.SelectedItems.Count == 0) return;
        if (_errorList.SelectedItems[0].Tag is not ValidationIssue issue) return;
        if (issue.RuleId is null) return;

        SelectNodeForRule(issue.RuleId.Value);
        _editorTabs.SelectedTab = _expressionTab!;
        _expressionTextBox.Focus();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test runner
    // ─────────────────────────────────────────────────────────────────────────

    private void TestRules()
    {
        FlushEditorsToModel();
        RuleContextDocument? context = FindSelectedContext();
        if (context is null)
        {
            MessageBox.Show(this, "Select a context or a rule under a context to run tests.",
                "Test Rules", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Type? contextType = _typeDiscovery.ResolveContextType(context);
        if (contextType is null)
        {
            MessageBox.Show(this,
                $"The context type '{context.QualifiedTypeName}' could not be resolved. " +
                "Ensure the source assembly is accessible.",
                "Test Rules", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var loader = new TestDataDialog(contextType);
        if (loader.ShowDialog(this) != DialogResult.OK || loader.LoadedObject is null) return;

        try
        {
            RuleEvaluationResult result = _testService.Run(_library, context, loader.LoadedObject, includeDiagnostics: true);
            using var dialog = new TestResultDialog(result);
            dialog.ShowDialog(this);
            UpdateStatus($"Test run: {result.Matches.Count} match(es), {result.Mismatches.Count} mismatch(es).");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Test run failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IntelliSense
    // ─────────────────────────────────────────────────────────────────────────

    private void WireUpIntelliSense()
    {
        _intellisenseListBox.Click += (_, _) => AcceptIntelliSenseSelection();
        _intellisenseListBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
            {
                AcceptIntelliSenseSelection();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _intellisenseListBox.Visible = false;
                _expressionTextBox.Focus();
                e.SuppressKeyPress = true;
            }
        };

        _expressionTextBox.TextChanged += (_, _) =>
        {
            if (_suspendBinding) return;
            MarkDirty();
            UpdateIntelliSense();
        };

        _expressionTextBox.LostFocus += (_, _) =>
        {
            // Only hide if focus didn't move to the intellisense list itself.
            if (!_intellisenseListBox.Focused) _intellisenseListBox.Visible = false;
        };
    }

    private void OnExpressionKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_intellisenseListBox.Visible) return;

        if (e.KeyCode == Keys.Down)
        {
            _intellisenseListBox.Focus();
            if (_intellisenseListBox.Items.Count > 0) _intellisenseListBox.SelectedIndex = 0;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            _intellisenseListBox.Visible = false;
            e.SuppressKeyPress = true;
        }
    }

    private void UpdateIntelliSense()
    {
        RuleContextDocument? context = FindSelectedContext();
        if (context is null)
        {
            _intellisenseListBox.Visible = false;
            return;
        }

        string prefix = ExtractCurrentToken();
        if (prefix.Length < 2)
        {
            _intellisenseListBox.Visible = false;
            return;
        }

        IReadOnlyList<string> suggestions = _intellisense.GetSuggestions(context, prefix);
        if (suggestions.Count == 0)
        {
            _intellisenseListBox.Visible = false;
            return;
        }

        _intellisenseListBox.BeginUpdate();
        _intellisenseListBox.Items.Clear();
        foreach (string suggestion in suggestions) _intellisenseListBox.Items.Add(suggestion);
        _intellisenseListBox.EndUpdate();

        PositionIntelliSense();
        _intellisenseListBox.Visible = true;
        _intellisenseListBox.BringToFront();
    }

    private string ExtractCurrentToken()
    {
        int caret = _expressionTextBox.SelectionStart;
        string text = _expressionTextBox.Text;
        int start = caret;
        while (start > 0)
        {
            char c = text[start - 1];
            if (!(char.IsLetterOrDigit(c) || c == '.' || c == '_')) break;
            start--;
        }
        return text[start..caret];
    }

    private void PositionIntelliSense()
    {
        int caret = _expressionTextBox.SelectionStart;
        Point caretClient = _expressionTextBox.GetPositionFromCharIndex(caret);
        Point caretScreen = _expressionTextBox.PointToScreen(caretClient);
        Point listParent = _expressionTextBox.Parent!.PointToClient(caretScreen);
        listParent.Offset(0, 18);
        _intellisenseListBox.Location = listParent;
    }

    private void AcceptIntelliSenseSelection()
    {
        if (_intellisenseListBox.SelectedItem is not string selection) return;

        string prefix = ExtractCurrentToken();
        int caret = _expressionTextBox.SelectionStart;
        int start = caret - prefix.Length;

        _expressionTextBox.Text = _expressionTextBox.Text.Remove(start, prefix.Length).Insert(start, selection);
        _expressionTextBox.SelectionStart = start + selection.Length;
        _expressionTextBox.SelectionLength = 0;

        _intellisenseListBox.Visible = false;
        _expressionTextBox.Focus();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Field-change wiring (dirty tracking)
    // ─────────────────────────────────────────────────────────────────────────

    private void WireUpFieldEvents()
    {
        foreach (Control tb in new Control[]
                 {
                     _nameTextBox, _descriptionTextBox, _tagsTextBox,
                     _resultCodeTextBox, _resultMessageTextBox, _optionalValueTextBox,
                     _contextNameTextBox, _contextDescriptionTextBox,
                     _categoryNameTextBox, _categoryDescriptionTextBox,
                     _libraryNameTextBox, _libraryDescriptionTextBox
                 })
        {
            tb.TextChanged += (_, _) => { if (!_suspendBinding) MarkDirty(); };
        }

        _activeCheckBox.CheckedChanged += (_, _) => { if (!_suspendBinding) MarkDirty(); };
        _priorityNumeric.ValueChanged += (_, _) => { if (!_suspendBinding) MarkDirty(); };
        _severityComboBox.SelectedIndexChanged += (_, _) => { if (!_suspendBinding) MarkDirty(); };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private RuleContextDocument? FindSelectedContext()
    {
        TreeNode? node = _tree.SelectedNode;
        while (node is not null)
        {
            if (node.Tag is RuleContextDocument context) return context;
            node = node.Parent;
        }
        return null;
    }

    private void MarkDirty()
    {
        if (_dirty) return;
        _dirty = true;
        UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        string file = _currentFile is null ? "(untitled)" : Path.GetFileName(_currentFile);
        string marker = _dirty ? " *" : string.Empty;
        Text = $"NAIware Rule Editor — {file}{marker}";
    }

    private void UpdateStatus(string message) => _statusLabel.Text = message;

    private static ToolStripButton ToolbarButton(string text, string tooltip, EventHandler click)
    {
        var button = new ToolStripButton(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ToolTipText = tooltip,
            Margin = new Padding(3, 2, 3, 2),
            Padding = new Padding(4)
        };
        button.Click += click;
        return button;
    }

    private static ToolStripMenuItem MenuItem(string text, Keys shortcut, EventHandler click)
    {
        var item = new ToolStripMenuItem(text);
        if (shortcut != Keys.None) item.ShortcutKeys = shortcut;
        item.Click += click;
        return item;
    }

    private void ShowAbout()
    {
        MessageBox.Show(this,
            "NAIware Rule Editor\n\n" +
            "A developer-focused Windows Forms editor for authoring, validating, and testing " +
            "rule libraries for the NAIware deterministic rules engine.\n\n" +
            ".NET 10 · Windows Forms",
            "About NAIware Rule Editor",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void OnFormClosing(object? sender, CancelEventArgs e)
    {
        if (!ConfirmDiscardChanges()) e.Cancel = true;
    }

    private void CreateSampleLibrary()
    {
        _library = new RuleLibraryDocument
        {
            Name = "Sample Mortgage Library",
            Description = "Demonstration rule library shipped with the editor."
        };

        var context = new RuleContextDocument
        {
            Name = "LoanApplication",
            QualifiedTypeName = "Mortgage.Models.LoanApplication",
            Description = "Mortgage loan application context (sample — resolve an actual DLL to enable validation)."
        };

        var category = new RuleCategoryDocument
        {
            Name = "Eligibility",
            Description = "Rules that determine loan eligibility."
        };

        var rule = new RuleExpressionDocument
        {
            Name = "MinimumLoanAmount",
            Description = "Ensures the loan amount meets the minimum threshold.",
            Expression = "LoanApplication.Amount > 1000",
            ResultCode = "AMT-001",
            ResultMessage = "Loan amount must be greater than 1000.",
            Severity = "Error",
            Priority = 100,
            Tags = ["eligibility", "amount"]
        };

        context.Expressions.Add(rule);
        category.ExpressionIds.Add(rule.Id);
        context.Categories.Add(category);
        _library.Contexts.Add(context);
    }
}
