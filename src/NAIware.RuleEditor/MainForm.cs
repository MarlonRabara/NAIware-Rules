using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using NAIware.Rules.Runtime;

namespace NAIware.RuleEditor;

/// <summary>
/// Main Windows Forms UI for the NAIware Rule Editor. The layout intentionally
/// follows the mockup in docs/images/naiware-rule-editor-mockup.png: menu and
/// command ribbon at the top, rule library tree on the left, expression editor
/// in the center, properties on the right, and a Visual Studio-style error list
/// at the bottom.
/// </summary>
public sealed partial class MainForm : Form
{
    private readonly AssemblyTypeDiscoveryService _typeDiscovery = new();
    private readonly IntelliSenseService _intelliSense;
    private readonly RuleValidationService _validator;
    private readonly RuleTestService _testService = new();
    private SplitContainer? _editorAndPropsSplit;
    private Panel? _propertiesViewHost;

    private RulesLibrary _library = new();
    private string? _currentFile;
    private bool _dirty;
    private bool _binding;

    private readonly TreeView _ruleTree = new()
    {
        Dock = DockStyle.Fill,
        HideSelection = false,
        ShowLines = true,
        Font = new Font("Segoe UI", 9.25f)
    };

    private readonly TextBox _treeSearchTextBox = new()
    {
        Dock = DockStyle.Top,
        PlaceholderText = "Search...",
        BorderStyle = BorderStyle.FixedSingle
    };

    private readonly ImageList _treeImages = new() { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
    private readonly ImageList _issueImages = new() { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };

    private readonly Label _documentTabLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 36,
        Padding = new Padding(12, 8, 8, 0),
        Text = "  01 - Age Rule      ×",
        Font = new Font("Segoe UI Semibold", 9.25f),
        BackColor = Color.White
    };

    private readonly TextBox _expressionTextBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ScrollBars = ScrollBars.Both,
        AcceptsTab = true,
        WordWrap = false,
        Font = new Font("Cascadia Mono, Consolas", 10.25f),
        BorderStyle = BorderStyle.None
    };

    private readonly ListBox _intelliSenseListBox = new()
    {
        Visible = false,
        Width = 320,
        Height = 180,
        Font = new Font("Cascadia Mono, Consolas", 9.25f),
        IntegralHeight = false
    };

    private readonly TextBox _resultCodeTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _resultMessageTextBox = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _severityComboBox = new() { Dock = DockStyle.Left, Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _optionalValueTextBox = new() { Dock = DockStyle.Fill };

    private readonly Label _propertiesTitleLabel = new()
    {
        Text = "Rule Properties                                      ×",
        Dock = DockStyle.Top,
        Height = 32,
        Font = new Font("Segoe UI Semibold", 10f),
        Padding = new Padding(0, 6, 0, 0)
    };

    private readonly Panel _rulePropertiesView = new() { Dock = DockStyle.Fill };
    private readonly Panel _libraryPropertiesView = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly Panel _contextPropertiesView = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly Panel _categoryPropertiesView = new() { Dock = DockStyle.Fill, Visible = false };

    private readonly TextBox _ruleNameTextBox = new() { Dock = DockStyle.Top };
    private readonly TextBox _ruleDescriptionTextBox = new() { Dock = DockStyle.Top, Multiline = true, Height = 52, ScrollBars = ScrollBars.Vertical };
    private readonly CheckBox _ruleEnabledCheckBox = new() { Dock = DockStyle.Top, Text = "Enabled", Height = 25 };
    private readonly TextBox _tagsTextBox = new() { Dock = DockStyle.Top, PlaceholderText = "age, loan, eligibility" };
    private readonly NumericUpDown _priorityNumeric = new() { Dock = DockStyle.Top, Minimum = 0, Maximum = 9999 };

    private readonly Label _createdByLabel = new() { Dock = DockStyle.Top, Height = 24, Text = "Created By:    jdoe" };
    private readonly Label _createdOnLabel = new() { Dock = DockStyle.Top, Height = 24, Text = "Created On:    5/13/2025 9:15 AM" };
    private readonly Label _modifiedByLabel = new() { Dock = DockStyle.Top, Height = 24, Text = "Modified By:   jdoe" };
    private readonly Label _modifiedOnLabel = new() { Dock = DockStyle.Top, Height = 24, Text = "Modified On:   5/13/2025 9:18 AM" };
    private readonly Label _contextNameLabel = new() { Dock = DockStyle.Top, Height = 24, Text = "Context Name:" };
    private readonly Label _contextTypeLabel = new() { Dock = DockStyle.Top, Height = 44, Text = "Context Type:" };

    private readonly Label _libraryViewStatusLabel = new() { Dock = DockStyle.Top, Height = 28, Text = "Status:" };
    private readonly Label _libraryViewSavedOnLabel = new() { Dock = DockStyle.Top, Height = 28, Text = "Saved On (UTC):" };
    private readonly Label _libraryViewFileLabel = new() { Dock = DockStyle.Top, Height = 44, Text = "File:" };
    private readonly Label _libraryViewSummaryLabel = new() { Dock = DockStyle.Top, Height = 28, Text = "Summary:" };

    private readonly Label _contextDllPathLabel = new() { Height = 18, Text = "DLL Path" };
    private readonly Label _contextClassLabel = new() { Height = 18, Text = "Qualified Type Name" };
    private readonly Label _contextInstanceLabel = new() { Height = 18, Text = "Instance Name" };
    private readonly TextBox _contextAssemblyPathTextBox = new() { ReadOnly = true, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _contextQualifiedTypeTextBox = new() { ReadOnly = true, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _contextInstanceNameTextBox = new() { BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _contextSerializedFileTextBox = new() { ReadOnly = true, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _contextSerializerAssemblyTextBox = new() { ReadOnly = true, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _contextSerializerTypeTextBox = new() { ReadOnly = true, BorderStyle = BorderStyle.FixedSingle };
    private readonly Button _contextSelectTypeButton = new() { Height = 28, Text = "Browse..." };
    private readonly Button _contextBrowseSerializedButton = new() { Height = 28, Text = "Browse..." };
    private readonly Button _contextSelectSerializerButton = new() { Height = 28, Text = "Browse..." };
    private readonly Label _contextSerializedFileLabel = new() { Height = 18, Text = "Serialized File (JSON/XML)" };
    private readonly Label _contextSerializerAssemblyLabel = new() { Height = 18, Text = "Serializer DLL" };
    private readonly Label _contextSerializerTypeLabel = new() { Height = 18, Text = "Serializer Type" };
    private readonly Label _contextObjectGraphLabel = new() { Dock = DockStyle.Top, Height = 24, Text = "Select a JSON or XML file to preview the hydrated object graph.", ForeColor = SystemColors.GrayText };
    private readonly TreeView _contextObjectGraphTreeView = new()
    {
        Dock = DockStyle.Fill,
        HideSelection = false,
        ShowLines = true,
        Font = new Font("Segoe UI", 9.0f)
    };

    private readonly Label _categoryNameLabel = new() { Dock = DockStyle.Top, Height = 28, Text = "Category Name:" };
    private readonly Label _categoryParentLabel = new() { Dock = DockStyle.Top, Height = 28, Text = "Parent:" };
    private readonly TextBox _categoryNameTextBox = new() { Dock = DockStyle.Top };
    private readonly TextBox _categoryDescriptionTextBox = new() { Dock = DockStyle.Top, Multiline = true, Height = 72, ScrollBars = ScrollBars.Vertical };

    private readonly ListView _errorList = new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        FullRowSelect = true,
        MultiSelect = false,
        GridLines = true,
        Font = new Font("Segoe UI", 9.25f)
    };

    private readonly TextBox _errorSearchTextBox = new()
    {
        Dock = DockStyle.Right,
        Width = 260,
        PlaceholderText = "Search Error List...",
        BorderStyle = BorderStyle.FixedSingle
    };

    private readonly Label _libraryCountLabel = new() { Dock = DockStyle.Bottom, Height = 26, Padding = new Padding(12, 6, 0, 0), Text = "1 context(s)      11 rule(s)" };
    private readonly StatusStrip _statusStrip = new();
    private readonly ToolStripStatusLabel _readyStatusLabel = new() { Text = "Ready", Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _libraryStatusLabel = new() { Text = "Library: LoanEligibilityRules" };
    private readonly ToolStripStatusLabel _validationStatusLabel = new() { Text = "Validation not run" };

    /// <summary>Creates the main form.</summary>
    public MainForm()
    {
        _intelliSense = new IntelliSenseService(_typeDiscovery);
        _validator = new RuleValidationService(_intelliSense);

        InitializeComponent();
        BuildFormLayout();

        _ruleTree.AfterSelect += (_, _) => BindSelection();
        _ruleTree.NodeMouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                _ruleTree.SelectedNode = e.Node;
                ShowTreeMenu(e.Location);
            }
        };
        _errorList.MouseDoubleClick += (_, _) => NavigateToSelectedIssue();

        WireEditorEvents();
        WireIntelliSenseEvents();
        WireContextViewEvents();

        FormClosing += (_, e) =>
        {
            if (!ConfirmDiscardChanges()) e.Cancel = true;
        };

        CreateMockupSampleLibrary();
        RefreshTree();
        UpdateCountsAndStatus("Ready");
    }

    private void BuildFormLayout()
    {
        BuildImages();
        _ruleTree.ImageList = _treeImages;
        _errorList.SmallImageList = _issueImages;
        _severityComboBox.Items.AddRange(["Info", "Warning", "Error"]);
        _severityComboBox.SelectedItem = "Error";

        Controls.Add(BuildShell());
        Controls.Add(BuildCommandStrip());
        Controls.Add(BuildMenu());
        Controls.Add(BuildStatusBar());
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip
        {
            Dock = DockStyle.Top,
            RenderMode = ToolStripRenderMode.System,
            BackColor = Color.White
        };

        ToolStripMenuItem file = new("&File");
        file.DropDownItems.Add(MenuItem("&New Library", Keys.Control | Keys.N, (_, _) => NewLibrary()));
        file.DropDownItems.Add(MenuItem("&Open Library...", Keys.Control | Keys.O, (_, _) => OpenLibrary()));
        file.DropDownItems.Add(MenuItem("&Save", Keys.Control | Keys.S, (_, _) => SaveLibrary()));
        file.DropDownItems.Add(MenuItem("Save &As...", Keys.Control | Keys.Shift | Keys.S, (_, _) => SaveLibraryAs()));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(MenuItem("E&xit", Keys.Alt | Keys.F4, (_, _) => Close()));

        ToolStripMenuItem library = new("&Library");
        library.DropDownItems.Add(MenuItem("Add &Context From DLL...", Keys.Control | Keys.D, (_, _) => AddContextFromDll()));
        library.DropDownItems.Add(MenuItem("Manage &Contexts...", Keys.None, (_, _) => MessageBox.Show(this, "Manage Contexts is planned for a future revision.", "Manage Contexts")));
        library.DropDownItems.Add(new ToolStripSeparator());
        library.DropDownItems.Add(MenuItem("&Validate Library", Keys.F6, (_, _) => ValidateLibrary()));

        ToolStripMenuItem context = new("&Context");
        context.DropDownItems.Add(MenuItem("Add Context From &DLL...", Keys.Control | Keys.D, (_, _) => AddContextFromDll()));
        context.DropDownItems.Add(MenuItem("Remove Selected Context", Keys.None, (_, _) => DeleteSelected()));

        ToolStripMenuItem category = new("&Category");
        category.DropDownItems.Add(MenuItem("Add &Category", Keys.None, (_, _) => AddCategory()));
        category.DropDownItems.Add(MenuItem("Add &Subcategory", Keys.None, (_, _) => AddSubcategory()));
        category.DropDownItems.Add(MenuItem("Delete Category", Keys.None, (_, _) => DeleteSelected()));

        ToolStripMenuItem rule = new("&Rule");
        rule.DropDownItems.Add(MenuItem("Add &Rule", Keys.Control | Keys.R, (_, _) => AddRule()));
        rule.DropDownItems.Add(MenuItem("Duplicate Rule", Keys.Control | Keys.Shift | Keys.D, (_, _) => DuplicateRule()));
        rule.DropDownItems.Add(MenuItem("Delete Rule", Keys.Delete, (_, _) => DeleteSelected()));
        rule.DropDownItems.Add(new ToolStripSeparator());
        rule.DropDownItems.Add(MenuItem("Move Up", Keys.Control | Keys.Up, (_, _) => MoveSelected(-1)));
        rule.DropDownItems.Add(MenuItem("Move Down", Keys.Control | Keys.Down, (_, _) => MoveSelected(1)));

        ToolStripMenuItem test = new("&Test");
        test.DropDownItems.Add(MenuItem("&Run Tests...", Keys.F5, (_, _) => TestRules()));
        test.DropDownItems.Add(MenuItem("Test &Settings...", Keys.None, (_, _) => MessageBox.Show(this, "Test Settings is planned for a future revision.", "Test Settings")));
        test.DropDownItems.Add(new ToolStripSeparator());
        test.DropDownItems.Add(MenuItem("Validate &Library", Keys.F6, (_, _) => ValidateLibrary()));

        ToolStripMenuItem view = new("&View");
        view.DropDownItems.Add(MenuItem("Expand All", Keys.None, (_, _) => _ruleTree.ExpandAll()));
        view.DropDownItems.Add(MenuItem("Collapse All", Keys.None, (_, _) => _ruleTree.CollapseAll()));
        view.DropDownItems.Add(MenuItem("Error List", Keys.Control | Keys.E, (_, _) => _errorList.Focus()));

        ToolStripMenuItem tools = new("&Tools");
        tools.DropDownItems.Add(MenuItem("Options...", Keys.None, (_, _) => MessageBox.Show(this, "Options is planned for a future revision.", "Tools")));

        ToolStripMenuItem help = new("&Help");
        help.DropDownItems.Add(MenuItem("View Documentation", Keys.F1, (_, _) => MessageBox.Show(this, "See docs/windows-ui.md.", "Documentation")));
        help.DropDownItems.Add(MenuItem("About NAIware Rule Editor", Keys.None, (_, _) => ShowAbout()));

        menu.Items.AddRange([file, library, context, category, rule, test, view, tools, help]);
        return menu;
    }

    private ToolStrip BuildCommandStrip()
    {
        var strip = new ToolStrip
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden,
            RenderMode = ToolStripRenderMode.System,
            ImageScalingSize = new Size(28, 28),
            AutoSize = false,
            Height = 128,
            Padding = new Padding(8, 6, 8, 0),
            BackColor = Color.White
        };

        strip.Items.Add(ToolButton("New\nLibrary", "File", MakeIcon(IconKind.Document, Color.SeaGreen), (_, _) => NewLibrary()));
        strip.Items.Add(ToolButton("Open\nLibrary", "File", MakeIcon(IconKind.Folder, Color.Goldenrod), (_, _) => OpenLibrary()));
        strip.Items.Add(ToolButton("Save", "File", MakeIcon(IconKind.Disk, Color.RoyalBlue), (_, _) => SaveLibrary()));
        strip.Items.Add(ToolButton("Save\nAs", "File", MakeIcon(IconKind.DiskPlus, Color.SteelBlue), (_, _) => SaveLibraryAs()));
        strip.Items.Add(new ToolStripSeparator());

        strip.Items.Add(ToolButton("Add\nContext", "Library", MakeIcon(IconKind.Database, Color.DimGray), (_, _) => AddContextFromDll()));
        strip.Items.Add(ToolButton("Manage\nContexts", "Library", MakeIcon(IconKind.Cube, Color.RoyalBlue), (_, _) => MessageBox.Show(this, "Manage Contexts is planned for a future revision.", "Manage Contexts")));
        strip.Items.Add(new ToolStripSeparator());

        strip.Items.Add(ToolButton("Add\nCategory", "Category", MakeIcon(IconKind.FolderPlus, Color.DarkOrange), (_, _) => AddCategory()));
        strip.Items.Add(ToolButton("Add\nSubcategory", "Category", MakeIcon(IconKind.FolderBranch, Color.Peru), (_, _) => AddSubcategory()));
        strip.Items.Add(ToolButton("Delete\nCategory", "Category", MakeIcon(IconKind.FolderMinus, Color.IndianRed), (_, _) => DeleteSelected()));
        strip.Items.Add(ToolButton("Reorder", "Category", MakeIcon(IconKind.Sort, Color.SteelBlue), (_, _) => MessageBox.Show(this, "Use Move Up / Move Down for the selected rule.", "Reorder")));
        strip.Items.Add(new ToolStripSeparator());

        strip.Items.Add(ToolButton("Add\nRule", "Rule", MakeIcon(IconKind.DocumentPlus, Color.SeaGreen), (_, _) => AddRule()));
        strip.Items.Add(ToolButton("Duplicate\nRule", "Rule", MakeIcon(IconKind.Copy, Color.DimGray), (_, _) => DuplicateRule()));
        strip.Items.Add(ToolButton("Delete\nRule", "Rule", MakeIcon(IconKind.Trash, Color.Firebrick), (_, _) => DeleteSelected()));
        strip.Items.Add(ToolButton("Move\nUp", "Rule", MakeIcon(IconKind.Up, Color.RoyalBlue), (_, _) => MoveSelected(-1)));
        strip.Items.Add(ToolButton("Move\nDown", "Rule", MakeIcon(IconKind.Down, Color.RoyalBlue), (_, _) => MoveSelected(1)));
        strip.Items.Add(new ToolStripSeparator());

        strip.Items.Add(ToolButton("Run\nTests", "Test", MakeIcon(IconKind.Play, Color.RoyalBlue), (_, _) => TestRules()));
        strip.Items.Add(ToolButton("Test\nSettings", "Test", MakeIcon(IconKind.Gear, Color.DimGray), (_, _) => MessageBox.Show(this, "Test Settings is planned for a future revision.", "Test Settings")));
        strip.Items.Add(new ToolStripSeparator());

        strip.Items.Add(ToolButton("Validate\nLibrary", "Validation", MakeIcon(IconKind.ClipboardCheck, Color.SeaGreen), (_, _) => ValidateLibrary()));
        return strip;
    }

    private Control BuildShell()
    {
        var root = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 5
        };

        var top = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 4
        };

        var editorAndProps = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 4
        };

        _editorAndPropsSplit = editorAndProps;

        top.Panel1.Controls.Add(BuildLibraryPanel());
        editorAndProps.Panel1.Controls.Add(BuildEditorPanel());
        editorAndProps.Panel2.Controls.Add(BuildPropertiesPanel());
        top.Panel2.Controls.Add(editorAndProps);

        root.Panel1.Controls.Add(top);
        root.Panel2.Controls.Add(BuildErrorPanel());

        ApplySplitLayout();

        void OnLayoutChanged(object? _, EventArgs __) => ApplySplitLayout();
        root.HandleCreated += OnLayoutChanged;
        root.SizeChanged += OnLayoutChanged;
        top.SizeChanged += OnLayoutChanged;
        editorAndProps.SizeChanged += OnLayoutChanged;

        void ApplySplitLayout()
        {
            ConfigureSplit(root, panel1MinSize: 0, panel2MinSize: 165, preferredDistance: root.Height - 360);
            ConfigureSplit(top, panel1MinSize: 260, panel2MinSize: 500, preferredDistance: 410);
            ConfigureSplit(editorAndProps, panel1MinSize: 420, panel2MinSize: 250, preferredDistance: editorAndProps.Width - 360);
        }

        static void ConfigureSplit(SplitContainer split, int panel1MinSize, int panel2MinSize, int preferredDistance)
        {
            int length = split.Orientation == Orientation.Vertical ? split.Width : split.Height;
            if (length <= 0) return;

            int maxPanelSpace = Math.Max(0, length - split.SplitterWidth);

            int p1 = Math.Max(0, panel1MinSize);
            int p2 = Math.Max(0, panel2MinSize);
            if (p1 + p2 > maxPanelSpace)
            {
                if (p2 >= maxPanelSpace)
                {
                    p2 = maxPanelSpace;
                    p1 = 0;
                }
                else
                {
                    p1 = maxPanelSpace - p2;
                }
            }

            split.Panel1MinSize = p1;
            split.Panel2MinSize = p2;

            int minDistance = split.Panel1MinSize;
            int maxDistance = Math.Max(minDistance, length - split.Panel2MinSize);
            split.SplitterDistance = Math.Clamp(preferredDistance, minDistance, maxDistance);
        }

        return root;
    }

    private void UpdateEditorVisibility()
    {
        if (_editorAndPropsSplit is null) return;
        _editorAndPropsSplit.Panel1Collapsed = _ruleTree.SelectedNode?.Tag is not RuleExpression;
    }

    private void ShowPropertiesView(string title, Control view)
    {
        _propertiesTitleLabel.Text = title;
        _rulePropertiesView.Visible = ReferenceEquals(view, _rulePropertiesView);
        _libraryPropertiesView.Visible = ReferenceEquals(view, _libraryPropertiesView);
        _contextPropertiesView.Visible = ReferenceEquals(view, _contextPropertiesView);
        _categoryPropertiesView.Visible = ReferenceEquals(view, _categoryPropertiesView);
    }

    private Control BuildLibraryPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8), BackColor = Color.White };
        var title = new Label
        {
            Text = "Rule Library",
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI Semibold", 10f),
            Padding = new Padding(0, 4, 0, 0)
        };

        var searchHost = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(0, 4, 0, 4) };
        searchHost.Controls.Add(_treeSearchTextBox);
        panel.Controls.Add(_ruleTree);
        panel.Controls.Add(_libraryCountLabel);
        panel.Controls.Add(searchHost);
        panel.Controls.Add(title);
        return panel;
    }

    private Control BuildEditorPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = Color.White };
        panel.Controls.Add(BuildResultDefinitionPanel());
        panel.Controls.Add(BuildExpressionPanel());
        panel.Controls.Add(_documentTabLabel);
        return panel;
    }

    private Control BuildExpressionPanel()
    {
        var group = new GroupBox { Text = "Expression", Dock = DockStyle.Fill, Padding = new Padding(12) };
        var topBar = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(0, 0, 0, 6) };
        var fx = new Label
        {
            Text = "ƒx",
            Dock = DockStyle.Right,
            Width = 38,
            Font = new Font("Segoe UI", 16f, FontStyle.Italic),
            TextAlign = ContentAlignment.MiddleCenter
        };
        var validate = new Button { Text = "✓ Validate", Dock = DockStyle.Right, Width = 110 };
        var insert = new ComboBox { Dock = DockStyle.Right, Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
        insert.Items.AddRange(["Insert", "Property", "Operator", "Keyword"]);
        insert.SelectedIndex = 0;
        insert.SelectedIndexChanged += (_, _) => InsertSelectedSuggestion(insert);
        validate.Click += (_, _) => ValidateLibrary();
        topBar.Controls.Add(validate);
        topBar.Controls.Add(insert);
        topBar.Controls.Add(fx);

        var editorHost = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
        var lineNumbers = new Label
        {
            Dock = DockStyle.Left,
            Width = 58,
            Text = "1\r\n2\r\n3\r\n4\r\n5",
            Font = _expressionTextBox.Font,
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.TopRight,
            Padding = new Padding(0, 8, 12, 0),
            BackColor = Color.FromArgb(248, 248, 248)
        };
        _expressionTextBox.Padding = new Padding(8);
        editorHost.Controls.Add(_expressionTextBox);
        editorHost.Controls.Add(_intelliSenseListBox);
        editorHost.Controls.Add(lineNumbers);

        group.Controls.Add(editorHost);
        group.Controls.Add(topBar);
        return group;
    }

    private Control BuildResultDefinitionPanel()
    {
        var group = new GroupBox { Text = "Result Definition", Dock = DockStyle.Bottom, Height = 212, Padding = new Padding(12) };
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(0)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 4; i++) table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        AddRow(table, 0, "Code:", _resultCodeTextBox);
        AddRow(table, 1, "Message:", _resultMessageTextBox);
        AddRow(table, 2, "Severity:", _severityComboBox);
        AddRow(table, 3, "Value:", _optionalValueTextBox);
        group.Controls.Add(table);
        return group;
    }

    private Control BuildPropertiesPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 8), BackColor = Color.White };
        _propertiesViewHost = new Panel { Dock = DockStyle.Fill };
        _rulePropertiesView.Controls.Add(BuildRulePropertiesView());
        _libraryPropertiesView.Controls.Add(BuildLibraryPropertiesView());
        _contextPropertiesView.Controls.Add(BuildContextPropertiesView());
        _categoryPropertiesView.Controls.Add(BuildCategoryPropertiesView());

        _propertiesViewHost.Controls.Add(_rulePropertiesView);
        _propertiesViewHost.Controls.Add(_libraryPropertiesView);
        _propertiesViewHost.Controls.Add(_contextPropertiesView);
        _propertiesViewHost.Controls.Add(_categoryPropertiesView);

        panel.Controls.Add(_propertiesViewHost);
        panel.Controls.Add(_propertiesTitleLabel);
        return panel;
    }

    private Control BuildRulePropertiesView()
    {
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var content = new Panel { Dock = DockStyle.Top, Height = 520 };

        int y = 6;
        AddSection(content, "▣  General", ref y);
        AddPropertyLabelAndControl(content, "Name:", _ruleNameTextBox, ref y, 26);
        AddPropertyLabelAndControl(content, "Description:", _ruleDescriptionTextBox, ref y, 56);
        AddPropertyLabelAndControl(content, "Enabled:", _ruleEnabledCheckBox, ref y, 26);
        AddPropertyLabelAndControl(content, "Tags:", _tagsTextBox, ref y, 26);
        AddPropertyLabelAndControl(content, "Priority:", _priorityNumeric, ref y, 26);
        y += 12;
        AddSection(content, "▣  Metadata", ref y);
        AddInfoLabel(content, _createdByLabel, ref y);
        AddInfoLabel(content, _createdOnLabel, ref y);
        AddInfoLabel(content, _modifiedByLabel, ref y);
        AddInfoLabel(content, _modifiedOnLabel, ref y);
        y += 12;
        AddSection(content, "▣  Context", ref y);
        AddInfoLabel(content, _contextNameLabel, ref y);
        AddInfoLabel(content, _contextTypeLabel, ref y);
        content.Height = y + 20;

        scroll.Controls.Add(content);
        return scroll;
    }

    private Control BuildLibraryPropertiesView()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };
        panel.Controls.Add(_libraryViewSummaryLabel);
        panel.Controls.Add(_libraryViewFileLabel);
        panel.Controls.Add(_libraryViewSavedOnLabel);
        panel.Controls.Add(_libraryViewStatusLabel);
        return panel;
    }

    private Control BuildContextPropertiesView()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };

        var graphGroup = new GroupBox { Text = "Object Graph", Dock = DockStyle.Fill, Padding = new Padding(10, 18, 10, 10) };
        _contextObjectGraphTreeView.BorderStyle = BorderStyle.FixedSingle;
        graphGroup.Controls.Add(_contextObjectGraphTreeView);
        graphGroup.Controls.Add(_contextObjectGraphLabel);

        var dataGroup = new GroupBox { Text = "Serialized Data", Dock = DockStyle.Top, Height = 160, Padding = new Padding(10, 18, 10, 10) };
        var dataTable = CreateContextFieldTable(rowCount: 3, includeButtonColumn: true);
        AddContextFieldRow(dataTable, 0, _contextSerializerAssemblyLabel, _contextSerializerAssemblyTextBox, _contextSelectSerializerButton);
        AddContextFieldRow(dataTable, 1, _contextSerializerTypeLabel, _contextSerializerTypeTextBox, null);
        AddContextFieldRow(dataTable, 2, _contextSerializedFileLabel, _contextSerializedFileTextBox, _contextBrowseSerializedButton);
        dataGroup.Controls.Add(dataTable);

        var identityGroup = new GroupBox { Text = "Context Identity", Dock = DockStyle.Top, Height = 128, Padding = new Padding(10, 18, 10, 10) };
        var identityTable = CreateContextFieldTable(rowCount: 2, includeButtonColumn: false);
        AddContextFieldRow(identityTable, 0, _contextInstanceLabel, _contextInstanceNameTextBox, null);
        AddContextFieldRow(identityTable, 1, _contextClassLabel, _contextQualifiedTypeTextBox, null);
        identityGroup.Controls.Add(identityTable);

        var assemblyGroup = new GroupBox { Text = "Assembly", Dock = DockStyle.Top, Height = 92, Padding = new Padding(10, 18, 10, 10) };
        var assemblyTable = CreateContextFieldTable(rowCount: 1, includeButtonColumn: true);
        AddContextFieldRow(assemblyTable, 0, _contextDllPathLabel, _contextAssemblyPathTextBox, _contextSelectTypeButton);
        assemblyGroup.Controls.Add(assemblyTable);

        panel.Controls.Add(graphGroup);
        panel.Controls.Add(dataGroup);
        panel.Controls.Add(identityGroup);
        panel.Controls.Add(assemblyGroup);
        return panel;
    }

    private static TableLayoutPanel CreateContextFieldTable(int rowCount, bool includeButtonColumn)
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = includeButtonColumn ? 3 : 2,
            RowCount = rowCount,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        if (includeButtonColumn) table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));

        for (int i = 0; i < rowCount; i++)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        }

        return table;
    }

    private static void AddContextFieldRow(TableLayoutPanel table, int row, Label label, Control editor, Button? button)
    {
        label.Dock = DockStyle.Fill;
        label.Margin = new Padding(0, 7, 8, 0);
        label.TextAlign = ContentAlignment.TopLeft;

        editor.Dock = DockStyle.Fill;
        editor.Margin = button is null ? new Padding(0, 3, 0, 3) : new Padding(0, 3, 8, 3);

        table.Controls.Add(label, 0, row);
        table.Controls.Add(editor, 1, row);

        if (button is not null)
        {
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(0, 3, 0, 3);
            table.Controls.Add(button, 2, row);
        }
    }

    private Control BuildCategoryPropertiesView()
    {
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var content = new Panel { Dock = DockStyle.Top, Height = 260 };

        int y = 6;
        AddSection(content, "▣  Category", ref y);
        AddPropertyLabelAndControl(content, "Name:", _categoryNameTextBox, ref y, 26);
        AddPropertyLabelAndControl(content, "Description:", _categoryDescriptionTextBox, ref y, 72);
        y += 12;
        AddSection(content, "▣  Hierarchy", ref y);
        AddInfoLabel(content, _categoryParentLabel, ref y);
        AddInfoLabel(content, _categoryNameLabel, ref y);
        content.Height = y + 20;

        scroll.Controls.Add(content);
        return scroll;
    }

    private Control BuildErrorPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        var bar = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(8, 7, 8, 5), BackColor = Color.WhiteSmoke };
        var label = new Label
        {
            Text = "Error List",
            Dock = DockStyle.Left,
            Width = 80,
            Font = new Font("Segoe UI Semibold", 9.5f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var all = new Button { Text = "All (0)", Dock = DockStyle.Left, Width = 76 };
        var errors = new Button { Text = "Errors (0)", Dock = DockStyle.Left, Width = 92 };
        var warnings = new Button { Text = "⚠ Warnings (0)", Dock = DockStyle.Left, Width = 118 };
        var info = new Button { Text = "ⓘ Info (0)", Dock = DockStyle.Left, Width = 90 };
        bar.Controls.Add(_errorSearchTextBox);
        bar.Controls.Add(info);
        bar.Controls.Add(warnings);
        bar.Controls.Add(errors);
        bar.Controls.Add(all);
        bar.Controls.Add(label);

        _errorList.Columns.Add("Severity", 120);
        _errorList.Columns.Add("Message", 690);
        _errorList.Columns.Add("Context", 140);
        _errorList.Columns.Add("Category", 150);
        _errorList.Columns.Add("Rule", 200);
        _errorList.Columns.Add("Line", 80);
        _errorList.Columns.Add("Column", 80);

        panel.Controls.Add(_errorList);
        panel.Controls.Add(bar);
        return panel;
    }

    private StatusStrip BuildStatusBar()
    {
        _statusStrip.Items.Add(_readyStatusLabel);
        _statusStrip.Items.Add(_libraryStatusLabel);
        _statusStrip.Items.Add(new ToolStripStatusLabel("   "));
        _statusStrip.Items.Add(_validationStatusLabel);
        return _statusStrip;
    }

    private static void AddRow(TableLayoutPanel table, int row, string labelText, Control control)
    {
        var label = new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        control.Margin = new Padding(0, 3, 0, 3);
        table.Controls.Add(label, 0, row);
        table.Controls.Add(control, 1, row);
    }

    private static void AddSection(Control parent, string text, ref int y)
    {
        var label = new Label
        {
            Text = text,
            Font = new Font("Segoe UI Semibold", 9.5f),
            Location = new Point(0, y),
            Size = new Size(parent.Width - 12, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        parent.Controls.Add(label);
        y += 32;
    }

    private static void AddPropertyLabelAndControl(Control parent, string labelText, Control control, ref int y, int height)
    {
        var label = new Label { Text = labelText, Location = new Point(8, y + 4), Size = new Size(100, height), TextAlign = ContentAlignment.TopLeft };
        control.Location = new Point(118, y);
        control.Size = new Size(parent.Width - 130, height);
        control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        parent.Controls.Add(label);
        parent.Controls.Add(control);
        y += height + 10;
    }

    private static void AddInfoLabel(Control parent, Label label, ref int y)
    {
        label.Location = new Point(8, y);
        label.Size = new Size(parent.Width - 16, label.Height);
        label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        parent.Controls.Add(label);
        y += label.Height + 2;
    }

    private void WireEditorEvents()
    {
        foreach (Control control in new Control[]
        {
            _expressionTextBox, _resultCodeTextBox, _resultMessageTextBox, _optionalValueTextBox,
            _ruleNameTextBox, _ruleDescriptionTextBox, _tagsTextBox,
            _categoryNameTextBox, _categoryDescriptionTextBox
        })
        {
            if (control is TextBox textBox) textBox.TextChanged += (_, _) => OnEditorChanged();
        }

        _ruleEnabledCheckBox.CheckedChanged += (_, _) => OnEditorChanged();
        _priorityNumeric.ValueChanged += (_, _) => OnEditorChanged();
        _severityComboBox.SelectedIndexChanged += (_, _) => OnEditorChanged();
    }

    private void WireContextViewEvents()
    {
        _contextSelectTypeButton.Click += (_, _) => SelectContextTypeForCurrentSelection();
        _contextSelectSerializerButton.Click += (_, _) => SelectSerializerForCurrentSelection();
        _contextBrowseSerializedButton.Click += (_, _) => SelectSerializedFileForCurrentSelection();
        _contextInstanceNameTextBox.TextChanged += (_, _) =>
        {
            if (_binding || _ruleTree.SelectedNode?.Tag is not RuleContext context) return;
            context.Name = _contextInstanceNameTextBox.Text.Trim();
            _ruleTree.SelectedNode.Text = GetContextNodeText(context);
            _dirty = true;
            UpdateContextLabels();
            UpdateWindowTitle();
            UpdateCountsAndStatus("Ready");
        };
    }

    private void WireIntelliSenseEvents()
    {
        _expressionTextBox.KeyUp += (_, e) =>
        {
            if (e.KeyCode is Keys.Up or Keys.Down or Keys.Enter or Keys.Escape) return;
            ShowIntelliSense();
        };
        _expressionTextBox.KeyDown += (_, e) =>
        {
            if (!_intelliSenseListBox.Visible) return;

            if (e.KeyCode == Keys.Down)
            {
                if (_intelliSenseListBox.SelectedIndex < _intelliSenseListBox.Items.Count - 1)
                    _intelliSenseListBox.SelectedIndex++;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (_intelliSenseListBox.SelectedIndex > 0) _intelliSenseListBox.SelectedIndex--;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode is Keys.Enter or Keys.Tab)
            {
                InsertIntelliSenseSelection();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _intelliSenseListBox.Visible = false;
                e.SuppressKeyPress = true;
            }
        };
        _intelliSenseListBox.DoubleClick += (_, _) => InsertIntelliSenseSelection();
    }

    private void OnEditorChanged()
    {
        if (_binding) return;
        FlushSelectionToModel();
        _dirty = true;
        UpdateWindowTitle();
    }

    private void NewLibrary()
    {
        if (!ConfirmDiscardChanges()) return;
        _library = new RulesLibrary { Name = "New Rule Library" };
        _currentFile = null;
        _dirty = false;
        _errorList.Items.Clear();
        RefreshTree();
        UpdateCountsAndStatus("New library created.");
        UpdateWindowTitle();
    }

    private void OpenLibrary()
    {
        if (!ConfirmDiscardChanges()) return;
        using var dialog = new OpenFileDialog { Filter = "Rule Library JSON (*.json)|*.json|All files (*.*)|*.*", Title = "Open Rule Library" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _library = RuleLibrarySerializer.Load(dialog.FileName);
            _currentFile = dialog.FileName;
            _dirty = false;
            _intelliSense.Invalidate();
            _errorList.Items.Clear();
            RefreshTree();
            UpdateCountsAndStatus($"Opened {Path.GetFileName(dialog.FileName)}.");
            UpdateWindowTitle();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Open Library", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SelectContextTypeForCurrentSelection()
    {
        if (_ruleTree.SelectedNode?.Tag is not RuleContext context)
        {
            MessageBox.Show(this, "Select a context node before changing its type.", "Context Type", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new OpenFileDialog { Filter = ".NET Assemblies (*.dll)|*.dll|All files (*.*)|*.*", Title = "Select Context Assembly" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            IReadOnlyList<ReflectedTypeInfo> types = _typeDiscovery.DiscoverTypes(dialog.FileName);
            using var picker = new ContextTypePickerDialog(types);
            if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedType is null) return;

            ReflectedTypeInfo selected = picker.SelectedType;
            context.AssemblyPath = selected.AssemblyPath;
            context.QualifiedTypeName = selected.Type?.AssemblyQualifiedName ?? selected.FullName;
            if (string.IsNullOrWhiteSpace(context.Name))
                context.Name = selected.Type?.Name ?? selected.DisplayName;

            _intelliSense.Invalidate();
            _dirty = true;
            _ruleTree.SelectedNode.Text = GetContextNodeText(context);
            UpdateSelectionInfoPanel();
            UpdateContextLabels();
            UpdateWindowTitle();
            UpdateCountsAndStatus($"Updated context type to {selected.FullName}.");

            if (!string.IsNullOrWhiteSpace(context.SerializedDataPath))
            {
                TryHydrateAndShowObjectGraph(context, context.SerializedDataPath, setDirty: false, showErrors: false);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Select Context Type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SelectSerializedFileForCurrentSelection()
    {
        if (_ruleTree.SelectedNode?.Tag is not RuleContext context)
        {
            MessageBox.Show(this, "Select a context node before loading serialized data.", "Serialized Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Filter = "JSON and XML files (*.json;*.xml)|*.json;*.xml|JSON (*.json)|*.json|XML (*.xml)|*.xml|All files (*.*)|*.*",
            Title = "Select Serialized Data File"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        TryHydrateAndShowObjectGraph(context, dialog.FileName, setDirty: true, showErrors: true);
    }

    private void SelectSerializerForCurrentSelection()
    {
        if (_ruleTree.SelectedNode?.Tag is not RuleContext context)
        {
            MessageBox.Show(this, "Select a context node before selecting a serializer.", "Serializer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new OpenFileDialog { Filter = ".NET Assemblies (*.dll)|*.dll|All files (*.*)|*.*", Title = "Select Serializer Assembly" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            IReadOnlyList<ReflectedTypeInfo> serializerTypes = _typeDiscovery.DiscoverTypes(dialog.FileName);

            if (serializerTypes.Count == 0)
            {
                MessageBox.Show(this, "No public concrete classes were found in the selected serializer assembly.", "Serializer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var picker = new ContextTypePickerDialog(
                serializerTypes,
                title: "Select Serializer Type",
                headerText: $"{serializerTypes.Count} public concrete type(s) discovered. Select the serializer class that exposes Deserialize(string filePath).",
                filterPlaceholder: "Filter serializer types...");
            if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedType is null) return;

            ReflectedTypeInfo selected = picker.SelectedType;
            if (!SupportsFilePathDeserialize(selected))
            {
                MessageBox.Show(this, $"'{selected.FullName}' must expose a public Deserialize(string filePath) method.", "Serializer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            context.SerializerAssemblyPath = selected.AssemblyPath;
            context.SerializerQualifiedTypeName = selected.FullName;
            _contextSerializerAssemblyTextBox.Text = selected.AssemblyPath;
            _contextSerializerTypeTextBox.Text = selected.FullName;
            _dirty = true;
            UpdateWindowTitle();
            UpdateCountsAndStatus($"Selected serializer {selected.FullName}.");

            if (!string.IsNullOrWhiteSpace(context.SerializedDataPath))
            {
                TryHydrateAndShowObjectGraph(context, context.SerializedDataPath, setDirty: false, showErrors: true);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Serializer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static bool SupportsFilePathDeserialize(ReflectedTypeInfo typeInfo)
    {
        return typeInfo.Type?.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
            .Any(method => string.Equals(method.Name, "Deserialize", StringComparison.Ordinal)
                           && method.GetParameters() is [{ ParameterType: var parameterType }]
                           && parameterType == typeof(string)
                           && method.ReturnType != typeof(void)) == true;
    }

    private void TryHydrateAndShowObjectGraph(RuleContext context, string filePath, bool setDirty, bool showErrors)
    {
        Type? contextType = _typeDiscovery.ResolveContextType(context);
        if (contextType is null)
        {
            if (showErrors)
                MessageBox.Show(this, $"The context type '{context.QualifiedTypeName}' could not be resolved. Select a valid DLL and type first.", "Serialized Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            object hydrated = DeserializeContextData(context, filePath, contextType);
            context.SerializedDataPath = filePath;
            _contextSerializedFileTextBox.Text = filePath;
            PopulateObjectGraph(context.Name, hydrated);
            if (setDirty)
                _dirty = true;
            UpdateWindowTitle();
            UpdateCountsAndStatus($"Hydrated serialized data for {context.Name}.");
        }
        catch (Exception ex)
        {
            if (showErrors)
                MessageBox.Show(this, ex.Message, "Serialized Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private object DeserializeContextData(RuleContext context, string filePath, Type contextType)
    {
        if (string.IsNullOrWhiteSpace(context.SerializerAssemblyPath)
            || string.IsNullOrWhiteSpace(context.SerializerQualifiedTypeName))
        {
            return TestDataDialog.LoadObjectFromFile(filePath, contextType);
        }

        if (!File.Exists(context.SerializerAssemblyPath))
        {
            throw new FileNotFoundException("Serializer assembly not found.", context.SerializerAssemblyPath);
        }

        Assembly serializerAssembly = Assembly.LoadFrom(context.SerializerAssemblyPath);
        Type serializerType = ResolveTypeFromAssembly(serializerAssembly, context.SerializerQualifiedTypeName)
            ?? throw new InvalidOperationException($"Serializer type '{context.SerializerQualifiedTypeName}' could not be resolved.");

        MethodInfo deserializeMethod = serializerType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(method => string.Equals(method.Name, "Deserialize", StringComparison.Ordinal)
                                      && method.GetParameters() is [{ ParameterType: var parameterType }]
                                      && parameterType == typeof(string)
                                      && method.ReturnType != typeof(void))
            ?? throw new InvalidOperationException($"Serializer type '{context.SerializerQualifiedTypeName}' must expose Deserialize(string filePath).");

        object? serializer = deserializeMethod.IsStatic ? null : Activator.CreateInstance(serializerType);
        object? result = deserializeMethod.Invoke(serializer, [filePath]);
        if (result is null) throw new InvalidOperationException("Serializer deserialized to null.");

        if (!contextType.IsInstanceOfType(result))
        {
            throw new InvalidOperationException(
                $"Serializer returned '{result.GetType().FullName}', which is not assignable to context type '{contextType.FullName}'.");
        }

        return result;
    }

    private static Type? ResolveTypeFromAssembly(Assembly assembly, string typeName)
    {
        return assembly.GetType(typeName)
            ?? assembly.GetTypes().FirstOrDefault(t => string.Equals(t.AssemblyQualifiedName, typeName, StringComparison.Ordinal)
                || string.Equals(t.FullName, typeName, StringComparison.Ordinal));
    }

    private void PopulateObjectGraph(string rootName, object root)
    {
        _contextObjectGraphTreeView.BeginUpdate();
        try
        {
            _contextObjectGraphTreeView.Nodes.Clear();
            string displayRoot = string.IsNullOrWhiteSpace(rootName) ? "instance" : rootName;
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            TreeNode rootNode = new($"{displayRoot}: {FormatValue(root)}");
            _contextObjectGraphTreeView.Nodes.Add(rootNode);
            AddObjectGraphNodes(rootNode, root, visited, depth: 0, maxDepth: 12);
            rootNode.Expand();
        }
        finally
        {
            _contextObjectGraphTreeView.EndUpdate();
        }
    }

    private void AddObjectGraphNodes(TreeNode parentNode, object? value, HashSet<object> visited, int depth, int maxDepth)
    {
        if (value is null || depth >= maxDepth) return;

        Type type = value.GetType();
        if (IsLeafType(type)) return;

        if (!type.IsValueType)
        {
            if (!visited.Add(value)) return;
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            int index = 0;
            foreach (object? item in enumerable)
            {
                TreeNode child = parentNode.Nodes.Add($"[{index}] = {FormatValue(item)}");
                if (item is not null)
                    AddObjectGraphNodes(child, item, visited, depth + 1, maxDepth);
                index++;
            }
            return;
        }

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length > 0) continue;

            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(value);
            }
            catch
            {
                continue;
            }

            TreeNode child = parentNode.Nodes.Add($"{property.Name}: {FormatValue(propertyValue)}");
            if (propertyValue is not null)
                AddObjectGraphNodes(child, propertyValue, visited, depth + 1, maxDepth);
        }
    }

    private static string FormatValue(object? value)
    {
        if (value is null) return "null";
        Type type = value.GetType();
        if (IsLeafType(type)) return Convert.ToString(value) ?? type.Name;
        return type.Name;
    }

    private static bool IsLeafType(Type type)
    {
        Type t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive
               || t.IsEnum
               || t == typeof(string)
               || t == typeof(decimal)
               || t == typeof(DateTime)
               || t == typeof(DateTimeOffset)
               || t == typeof(TimeSpan)
               || t == typeof(Guid);
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }

    private bool SaveLibrary()
    {
        if (string.IsNullOrWhiteSpace(_currentFile)) return SaveLibraryAs();
        return SaveTo(_currentFile);
    }

    private bool SaveLibraryAs()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Rule Library JSON (*.json)|*.json",
            FileName = string.IsNullOrWhiteSpace(_library.Name) ? "rule-library.json" : SafeFileName(_library.Name) + ".json",
            Title = "Save Rule Library As"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return false;
        return SaveTo(dialog.FileName);
    }

    private bool SaveTo(string path)
    {
        try
        {
            FlushSelectionToModel();
            _library.SavedUtc = DateTimeOffset.UtcNow;
            RuleLibrarySerializer.Save(path, _library);
            _currentFile = path;
            _dirty = false;
            UpdateCountsAndStatus($"Saved {Path.GetFileName(path)}.");
            UpdateSelectionInfoPanel();
            UpdateWindowTitle();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save Library", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    private void AddContextFromDll()
    {
        using var dialog = new OpenFileDialog { Filter = ".NET Assemblies (*.dll)|*.dll|All files (*.*)|*.*", Title = "Select Context Assembly" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            IReadOnlyList<ReflectedTypeInfo> types = _typeDiscovery.DiscoverTypes(dialog.FileName);
            using var picker = new ContextTypePickerDialog(types);
            if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedType is null) return;

            ReflectedTypeInfo selected = picker.SelectedType;
            _library.Contexts.Add(new RuleContext
            {
                Name = selected.Type?.Name ?? selected.DisplayName,
                QualifiedTypeName = selected.FullName,
                AssemblyPath = selected.AssemblyPath,
                Description = $"Rules for {selected.FullName}."
            });
            _intelliSense.Invalidate();
            _dirty = true;
            RefreshTree();
            UpdateCountsAndStatus($"Added context {selected.FullName}.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Add Context From DLL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void AddCategory()
    {
        RuleContext? context = GetSelectedContext();
        if (context is null) return;
        context.Categories.Add(new RuleCategory { Name = NextNumberedName(context.Categories.Select(c => c.Name), "New Category") });
        _dirty = true;
        RefreshTree();
    }

    private void AddSubcategory()
    {
        RuleCategory? category = GetSelectedCategory();
        if (category is null)
        {
            AddCategory();
            return;
        }
        category.Categories.Add(new RuleCategory { Name = NextNumberedName(category.Categories.Select(c => c.Name), "New Subcategory") });
        _dirty = true;
        RefreshTree();
    }

    private void AddRule()
    {
        RuleContext? context = GetSelectedContext();
        if (context is null) return;

        var rule = new RuleExpression
        {
            Name = NextNumberedName(context.Expressions.Select(e => e.Name), "New Rule"),
            Expression = "Loan.Amount > 1000",
            ResultCode = "RULE-001",
            ResultMessage = "Rule matched.",
            Severity = "Warning",
            IsActive = true,
            Priority = 10
        };
        context.Expressions.Add(rule);

        RuleCategory? category = GetSelectedCategory();
        category?.ExpressionIds.Add(rule.Id);
        _dirty = true;
        RefreshTree(rule.Id);
    }

    private void DuplicateRule()
    {
        if (GetSelectedRule() is not { } selected || GetSelectedContext() is not { } context) return;
        var copy = new RuleExpression
        {
            Name = selected.Name + " Copy",
            Description = selected.Description,
            Expression = selected.Expression,
            IsActive = selected.IsActive,
            Priority = selected.Priority,
            Tags = [.. selected.Tags],
            ResultCode = selected.ResultCode,
            ResultMessage = selected.ResultMessage,
            Severity = selected.Severity,
            OptionalValue = selected.OptionalValue
        };
        context.Expressions.Add(copy);
        GetSelectedCategory()?.ExpressionIds.Add(copy.Id);
        _dirty = true;
        RefreshTree(copy.Id);
    }

    private void DeleteSelected()
    {
        if (_ruleTree.SelectedNode?.Tag is null) return;
        if (MessageBox.Show(this, "Delete the selected item?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        object tag = _ruleTree.SelectedNode.Tag;
        if (tag is RuleExpression rule && GetSelectedContext() is { } context)
        {
            context.Expressions.Remove(rule);
            RemoveRuleReferences(context.Categories, rule.Id);
        }
        else if (tag is RuleCategory category && GetSelectedContext() is { } selectedContext)
        {
            RemoveCategory(selectedContext.Categories, category);
        }
        else if (tag is RuleContext contextDoc)
        {
            _library.Contexts.Remove(contextDoc);
        }

        _dirty = true;
        RefreshTree();
    }

    private void MoveSelected(int direction)
    {
        if (GetSelectedRule() is not { } rule || GetSelectedContext() is not { } context) return;
        List<RuleExpression> list = context.Expressions;
        int index = list.IndexOf(rule);
        int newIndex = index + direction;
        if (index < 0 || newIndex < 0 || newIndex >= list.Count) return;
        list.RemoveAt(index);
        list.Insert(newIndex, rule);
        _dirty = true;
        RefreshTree(rule.Id);
    }

    private void ValidateLibrary()
    {
        FlushSelectionToModel();
        List<ValidationIssue> issues = _validator.Validate(_library);
        PopulateIssues(issues);
        int errors = issues.Count(i => string.Equals(i.Severity, "Error", StringComparison.OrdinalIgnoreCase));
        int warnings = issues.Count(i => string.Equals(i.Severity, "Warning", StringComparison.OrdinalIgnoreCase));
        _validationStatusLabel.Text = issues.Count == 0 ? "✓ Validation passed" : $"✓ Validation completed with {errors} error(s), {warnings} warning(s)";
        _readyStatusLabel.Text = "Ready";
    }

    private void TestRules()
    {
        FlushSelectionToModel();
        RuleContext? context = GetSelectedContext() ?? _library.Contexts.FirstOrDefault();
        if (context is null)
        {
            MessageBox.Show(this, "Add or select a context before running tests.", "Run Tests", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Type? contextType = _typeDiscovery.ResolveContextType(context);
        if (contextType is null)
        {
            MessageBox.Show(this, $"The context type '{context.QualifiedTypeName}' could not be resolved. Re-load the source DLL.", "Run Tests", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new TestDataDialog(contextType);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.LoadedObject is null) return;

        try
        {
            RuleEvaluationResult result = _testService.Run(_library, context, dialog.LoadedObject, includeDiagnostics: true);
            using var resultDialog = new TestResultDialog(result);
            resultDialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Run Tests", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void PopulateIssues(IReadOnlyList<ValidationIssue> issues)
    {
        _errorList.BeginUpdate();
        try
        {
            _errorList.Items.Clear();
            foreach (ValidationIssue issue in issues)
            {
                var item = new ListViewItem(issue.Severity) { ImageKey = issue.Severity, Tag = issue };
                item.SubItems.Add(issue.Message);
                item.SubItems.Add(issue.Context);
                item.SubItems.Add(issue.Category);
                item.SubItems.Add(issue.Rule);
                item.SubItems.Add(string.IsNullOrWhiteSpace(issue.Rule) ? "-" : "1");
                item.SubItems.Add("-");
                _errorList.Items.Add(item);
            }
        }
        finally
        {
            _errorList.EndUpdate();
        }
    }

    private void BindSelection()
    {
        _binding = true;
        try
        {
            if (_ruleTree.SelectedNode?.Tag is RuleExpression rule)
            {
                _documentTabLabel.Text = "  " + rule.Name + "      ×";
                _expressionTextBox.Text = rule.Expression;
                _resultCodeTextBox.Text = rule.ResultCode ?? string.Empty;
                _resultMessageTextBox.Text = rule.ResultMessage ?? string.Empty;
                _severityComboBox.SelectedItem = string.IsNullOrWhiteSpace(rule.Severity) ? "Warning" : rule.Severity;
                _optionalValueTextBox.Text = rule.OptionalValue ?? string.Empty;
                _ruleNameTextBox.Text = rule.Name;
                _ruleDescriptionTextBox.Text = rule.Description;
                _ruleEnabledCheckBox.Checked = rule.IsActive;
                _tagsTextBox.Text = string.Join(", ", rule.Tags);
                _priorityNumeric.Value = Math.Clamp(rule.Priority, (int)_priorityNumeric.Minimum, (int)_priorityNumeric.Maximum);
            }
            else if (_ruleTree.SelectedNode?.Tag is RuleContext context)
            {
                _documentTabLabel.Text = "  Context: " + context.Name + "      ×";
                _expressionTextBox.Text = context.QualifiedTypeName;
                _resultCodeTextBox.Clear();
                _resultMessageTextBox.Clear();
                _optionalValueTextBox.Clear();
                _ruleNameTextBox.Text = context.Name;
                _ruleDescriptionTextBox.Text = context.Description;
                _ruleEnabledCheckBox.Checked = true;
                _tagsTextBox.Clear();
                _priorityNumeric.Value = 0;
                _categoryNameTextBox.Clear();
                _categoryDescriptionTextBox.Clear();

                _contextAssemblyPathTextBox.Text = context.AssemblyPath ?? string.Empty;
                _contextQualifiedTypeTextBox.Text = context.QualifiedTypeName;
                _contextInstanceNameTextBox.Text = context.Name;
                _contextSerializerAssemblyTextBox.Text = context.SerializerAssemblyPath ?? string.Empty;
                _contextSerializerTypeTextBox.Text = context.SerializerQualifiedTypeName ?? string.Empty;
                _contextSerializedFileTextBox.Text = context.SerializedDataPath ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(context.SerializedDataPath))
                {
                    TryHydrateAndShowObjectGraph(context, context.SerializedDataPath, setDirty: false, showErrors: false);
                }
                else
                {
                    _contextObjectGraphTreeView.Nodes.Clear();
                }
            }
            else if (_ruleTree.SelectedNode?.Tag is RuleCategory category)
            {
                _documentTabLabel.Text = "  Category: " + category.Name + "      ×";
                _expressionTextBox.Text = category.Description;
                _resultCodeTextBox.Clear();
                _resultMessageTextBox.Clear();
                _optionalValueTextBox.Clear();
                _ruleNameTextBox.Text = category.Name;
                _ruleDescriptionTextBox.Text = category.Description;
                _ruleEnabledCheckBox.Checked = true;
                _tagsTextBox.Clear();
                _priorityNumeric.Value = 0;
                _categoryNameTextBox.Text = category.Name;
                _categoryDescriptionTextBox.Text = category.Description;
            }
            else if (_ruleTree.SelectedNode?.Tag is RulesLibrary)
            {
                _documentTabLabel.Text = "  Library: " + _library.Name + "      ×";
                _expressionTextBox.Text = _library.Description;
                _resultCodeTextBox.Clear();
                _resultMessageTextBox.Clear();
                _optionalValueTextBox.Clear();
                _ruleNameTextBox.Text = _library.Name;
                _ruleDescriptionTextBox.Text = _library.Description;
                _ruleEnabledCheckBox.Checked = true;
                _tagsTextBox.Clear();
                _priorityNumeric.Value = 0;
            }

            UpdateContextLabels();
            UpdateEditorVisibility();
            UpdateSelectionInfoPanel();
        }
        finally
        {
            _binding = false;
        }
    }

    private void FlushSelectionToModel()
    {
        if (_binding || _ruleTree.SelectedNode?.Tag is null) return;

        switch (_ruleTree.SelectedNode.Tag)
        {
            case RuleExpression rule:
                rule.Name = _ruleNameTextBox.Text.Trim();
                rule.Description = _ruleDescriptionTextBox.Text;
                rule.Expression = _expressionTextBox.Text;
                rule.ResultCode = NullIfWhiteSpace(_resultCodeTextBox.Text);
                rule.ResultMessage = NullIfWhiteSpace(_resultMessageTextBox.Text);
                rule.Severity = _severityComboBox.SelectedItem?.ToString();
                rule.OptionalValue = NullIfWhiteSpace(_optionalValueTextBox.Text);
                rule.IsActive = _ruleEnabledCheckBox.Checked;
                rule.Priority = (int)_priorityNumeric.Value;
                rule.Tags = [.. _tagsTextBox.Text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
                _ruleTree.SelectedNode.Text = rule.Name;
                _documentTabLabel.Text = "  " + rule.Name + "      ×";
                break;
            case RuleContext context:
                context.Name = _contextInstanceNameTextBox.Text.Trim();
                context.Description = _ruleDescriptionTextBox.Text;
                _contextQualifiedTypeTextBox.Text = context.QualifiedTypeName;
                _contextAssemblyPathTextBox.Text = context.AssemblyPath ?? string.Empty;
                _contextSerializerAssemblyTextBox.Text = context.SerializerAssemblyPath ?? string.Empty;
                _contextSerializerTypeTextBox.Text = context.SerializerQualifiedTypeName ?? string.Empty;
                _ruleTree.SelectedNode.Text = GetContextNodeText(context);
                break;
            case RuleCategory category:
                category.Name = _categoryNameTextBox.Text.Trim();
                category.Description = _categoryDescriptionTextBox.Text;
                _ruleTree.SelectedNode.Text = category.Name;
                _documentTabLabel.Text = "  Category: " + category.Name + "      ×";
                break;
            case RulesLibrary:
                _library.Name = _ruleNameTextBox.Text.Trim();
                _library.Description = _ruleDescriptionTextBox.Text;
                _ruleTree.SelectedNode.Text = _library.Name;
                break;
        }

        UpdateCountsAndStatus("Ready");
    }

    private void RefreshTree(Guid? selectRuleId = null)
    {
        _ruleTree.BeginUpdate();
        try
        {
            _ruleTree.Nodes.Clear();
            TreeNode root = _ruleTree.Nodes.Add(_library.Name);
            root.Tag = _library;
            root.ImageKey = root.SelectedImageKey = "library";

            foreach (RuleContext context in _library.Contexts)
            {
                TreeNode contextNode = root.Nodes.Add(GetContextNodeText(context));
                contextNode.Tag = context;
                contextNode.ImageKey = contextNode.SelectedImageKey = "context";

                foreach (RuleCategory category in context.Categories)
                {
                    AddCategoryNode(contextNode, category, context, selectRuleId);
                }

                foreach (RuleExpression rule in context.Expressions.Where(r => !ContainsRule(context.Categories, r.Id)))
                {
                    TreeNode ruleNode = contextNode.Nodes.Add(rule.Name);
                    ruleNode.Tag = rule;
                    ruleNode.ImageKey = ruleNode.SelectedImageKey = "rule";
                    if (rule.Id == selectRuleId) _ruleTree.SelectedNode = ruleNode;
                }
            }

            root.ExpandAll();
            if (_ruleTree.SelectedNode is null) _ruleTree.SelectedNode = root;
        }
        finally
        {
            _ruleTree.EndUpdate();
        }

        UpdateCountsAndStatus("Ready");
        UpdateWindowTitle();
    }

    private void AddCategoryNode(TreeNode parent, RuleCategory category, RuleContext context, Guid? selectRuleId)
    {
        TreeNode node = parent.Nodes.Add(category.Name);
        node.Tag = category;
        node.ImageKey = node.SelectedImageKey = "category";

        foreach (Guid id in category.ExpressionIds)
        {
            RuleExpression? rule = context.Expressions.FirstOrDefault(e => e.Id == id);
            if (rule is null) continue;
            TreeNode ruleNode = node.Nodes.Add(rule.Name);
            ruleNode.Tag = rule;
            ruleNode.ImageKey = ruleNode.SelectedImageKey = "rule";
            if (rule.Id == selectRuleId) _ruleTree.SelectedNode = ruleNode;
        }

        foreach (RuleCategory child in category.Categories)
        {
            AddCategoryNode(node, child, context, selectRuleId);
        }
    }

    private void NavigateToSelectedIssue()
    {
        if (_errorList.SelectedItems.Count == 0 || _errorList.SelectedItems[0].Tag is not ValidationIssue issue || issue.RuleId is null) return;
        SelectRuleNode(issue.RuleId.Value);
    }

    private void SelectRuleNode(Guid id)
    {
        foreach (TreeNode node in Flatten(_ruleTree.Nodes))
        {
            if (node.Tag is RuleExpression rule && rule.Id == id)
            {
                _ruleTree.SelectedNode = node;
                node.EnsureVisible();
                _expressionTextBox.Focus();
                return;
            }
        }
    }

    private IEnumerable<TreeNode> Flatten(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            yield return node;
            foreach (TreeNode child in Flatten(node.Nodes)) yield return child;
        }
    }

    private void ShowIntelliSense()
    {
        RuleContext? context = GetSelectedContext();
        if (context is null) return;

        string prefix = GetCurrentToken(_expressionTextBox.Text, _expressionTextBox.SelectionStart);
        if (prefix.Length < 1)
        {
            _intelliSenseListBox.Visible = false;
            return;
        }

        IReadOnlyList<string> suggestions = _intelliSense.GetSuggestions(context, prefix);
        if (suggestions.Count == 0)
        {
            _intelliSenseListBox.Visible = false;
            return;
        }

        _intelliSenseListBox.BeginUpdate();
        _intelliSenseListBox.Items.Clear();
        foreach (string suggestion in suggestions) _intelliSenseListBox.Items.Add(suggestion);
        _intelliSenseListBox.SelectedIndex = 0;
        _intelliSenseListBox.EndUpdate();
        _intelliSenseListBox.Location = new Point(70, 36);
        _intelliSenseListBox.BringToFront();
        _intelliSenseListBox.Visible = true;
    }

    private void InsertIntelliSenseSelection()
    {
        if (!_intelliSenseListBox.Visible || _intelliSenseListBox.SelectedItem is not string suggestion) return;
        int start = _expressionTextBox.SelectionStart;
        string token = GetCurrentToken(_expressionTextBox.Text, start);
        int replaceStart = Math.Max(0, start - token.Length);
        _expressionTextBox.Text = _expressionTextBox.Text.Remove(replaceStart, token.Length).Insert(replaceStart, suggestion);
        _expressionTextBox.SelectionStart = replaceStart + suggestion.Length;
        _intelliSenseListBox.Visible = false;
    }

    private void InsertSelectedSuggestion(ComboBox comboBox)
    {
        if (comboBox.SelectedItem is null || comboBox.SelectedIndex == 0) return;
        string insert = comboBox.SelectedItem.ToString() switch
        {
            "Operator" => " > ",
            "Keyword" => " and ",
            _ => string.Empty
        };
        if (!string.IsNullOrEmpty(insert))
        {
            int pos = _expressionTextBox.SelectionStart;
            _expressionTextBox.Text = _expressionTextBox.Text.Insert(pos, insert);
            _expressionTextBox.SelectionStart = pos + insert.Length;
        }
        comboBox.SelectedIndex = 0;
    }

    private static string GetCurrentToken(string text, int cursor)
    {
        int i = Math.Min(cursor, text.Length) - 1;
        while (i >= 0)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c) || c is '(' or ')' or '>' or '<' or '=' or '!' or ',' or '"') break;
            i--;
        }
        return text[(i + 1)..Math.Min(cursor, text.Length)];
    }

    private void UpdateContextLabels()
    {
        RuleContext? context = GetSelectedContext();
        if (context is null)
        {
            _contextNameLabel.Text = "Context Name:";
            _contextTypeLabel.Text = "Context Type:";
            return;
        }
        _contextNameLabel.Text = "Context Name:    " + context.Name;
        _contextTypeLabel.Text = "Context Type:    " + context.QualifiedTypeName;
    }

    private void UpdateSelectionInfoPanel()
    {
        object? selection = _ruleTree.SelectedNode?.Tag;

        if (selection is RulesLibrary)
        {
            ShowPropertiesView("Library View                                      ×", _libraryPropertiesView);
            _libraryViewStatusLabel.Text = "Status:    " + (_dirty ? "Not saved" : "Saved");
            _libraryViewSavedOnLabel.Text = "Saved On (UTC):    " + _library.SavedUtc.ToString("u");
            _libraryViewFileLabel.Text = "File:    " + (string.IsNullOrWhiteSpace(_currentFile) ? "(not saved to disk)" : _currentFile);
            _libraryViewSummaryLabel.Text = $"Summary:    v{_library.Version} | {_library.Contexts.Count} context(s) | {_library.Contexts.Sum(c => c.Expressions.Count)} rule(s)";
            return;
        }

        if (selection is RuleContext context)
        {
            ShowPropertiesView("Context View                                      ×", _contextPropertiesView);
            _contextDllPathLabel.Text = "DLL Path";
            _contextClassLabel.Text = "Qualified Type";
            _contextInstanceLabel.Text = "Instance Name";
            _contextAssemblyPathTextBox.Text = context.AssemblyPath ?? string.Empty;
            _contextQualifiedTypeTextBox.Text = context.QualifiedTypeName;
            _contextInstanceNameTextBox.Text = context.Name;
            _contextSerializerAssemblyTextBox.Text = context.SerializerAssemblyPath ?? string.Empty;
            _contextSerializerTypeTextBox.Text = context.SerializerQualifiedTypeName ?? string.Empty;
            _contextSerializedFileTextBox.Text = context.SerializedDataPath ?? string.Empty;
            return;
        }

        if (selection is RuleCategory category)
        {
            ShowPropertiesView("Category View                                      ×", _categoryPropertiesView);
            _categoryNameLabel.Text = "Category Name:    " + category.Name;
            _categoryParentLabel.Text = "Parent:    " + GetCategoryParentName(_ruleTree.SelectedNode);
            return;
        }

        if (selection is RuleExpression rule)
        {
            ShowPropertiesView("Rule Properties                                      ×", _rulePropertiesView);
            _createdByLabel.Text = "Rule Id:    " + rule.Id;
            _createdOnLabel.Text = "Priority:    " + rule.Priority;
            _modifiedByLabel.Text = "Enabled:    " + (rule.IsActive ? "Yes" : "No");
            _modifiedOnLabel.Text = "Severity:    " + (string.IsNullOrWhiteSpace(rule.Severity) ? "(not set)" : rule.Severity);
            return;
        }

        ShowPropertiesView("Properties                                      ×", _libraryPropertiesView);
        _createdByLabel.Text = "Created By:";
        _createdOnLabel.Text = "Created On:";
        _modifiedByLabel.Text = "Modified By:";
        _modifiedOnLabel.Text = "Modified On:";
    }

    private static string GetCategoryParentName(TreeNode? categoryNode)
    {
        if (categoryNode?.Parent is null) return "(none)";

        return categoryNode.Parent.Tag switch
        {
            RuleCategory parentCategory => parentCategory.Name,
            RuleContext parentContext => parentContext.Name,
            _ => categoryNode.Parent.Text
        };
    }

    private void UpdateCountsAndStatus(string status)
    {
        int contextCount = _library.Contexts.Count;
        int ruleCount = _library.Contexts.Sum(c => c.Expressions.Count);
        _libraryCountLabel.Text = $"{contextCount} context(s)      {ruleCount} rule(s)";
        _libraryStatusLabel.Text = "Library: " + _library.Name;
        _readyStatusLabel.Text = status;
    }

    private void UpdateWindowTitle()
    {
        Text = (_dirty ? "*" : string.Empty) + "NAIware Rule Editor" + (_currentFile is null ? string.Empty : $" - {Path.GetFileName(_currentFile)}");
    }

    private void ShowTreeMenu(Point location)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Add Category", null, (_, _) => AddCategory());
        menu.Items.Add("Add Subcategory", null, (_, _) => AddSubcategory());
        menu.Items.Add("Add Rule", null, (_, _) => AddRule());
        menu.Items.Add("Duplicate Rule", null, (_, _) => DuplicateRule());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Delete", null, (_, _) => DeleteSelected());
        menu.Show(_ruleTree, location);
    }

    private RuleContext? GetSelectedContext()
    {
        TreeNode? node = _ruleTree.SelectedNode;
        while (node is not null)
        {
            if (node.Tag is RuleContext context) return context;
            node = node.Parent;
        }
        return _library.Contexts.FirstOrDefault();
    }

    private RuleCategory? GetSelectedCategory()
    {
        TreeNode? node = _ruleTree.SelectedNode;
        while (node is not null)
        {
            if (node.Tag is RuleCategory category) return category;
            node = node.Parent;
        }
        return null;
    }

    private RuleExpression? GetSelectedRule() => _ruleTree.SelectedNode?.Tag as RuleExpression;

    private bool ConfirmDiscardChanges()
    {
        if (!_dirty) return true;
        DialogResult result = MessageBox.Show(this, "The current library has unsaved changes. Save before continuing?", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        return result switch
        {
            DialogResult.Yes => SaveLibrary(),
            DialogResult.No => true,
            _ => false
        };
    }

    private void ShowAbout() => MessageBox.Show(this,
        "NAIware Rule Editor\n\nDeveloper-focused WinForms editor for NAIware rule libraries.",
        "About", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void CreateMockupSampleLibrary()
    {
        _library = new RulesLibrary { Name = "LoanEligibilityRules", Description = "Sample loan eligibility rule library." };
        var loanContext = new RuleContext { Name = "LoanApplication", QualifiedTypeName = "Mortgage.Models.LoanApplication" };
        var coBorrower = new RuleContext { Name = "CoBorrower", QualifiedTypeName = "Mortgage.Models.CoBorrower" };
        var property = new RuleContext { Name = "Property", QualifiedTypeName = "Mortgage.Models.Property" };

        AddCategoryWithRules(loanContext, "01 - Borrower",
            ("01 - Age Rule", "Loan.LoanCalculation.YoungestNonBorrowerSpouseAge < 18\r\nand Loan.Amount > 50000", "AGE-001", "Applicant's youngest non-borrower spouse is under 18 and loan amount exceeds $50,000."),
            ("02 - Citizenship Rule", "Loan.Borrower.IsCitizen = true", "CIT-001", "Borrower citizenship requirement met."),
            ("03 - Credit Score Rule", "Loan.Borrower.CreditScore >= 620", "CREDIT-001", "Credit score meets minimum threshold."));
        AddCategoryWithRules(loanContext, "02 - Loan",
            ("01 - Loan Amount Rule", "Loan.DeletedProperty > 1000", "AMT-001", "Loan amount is valid."),
            ("02 - Loan Purpose Rule", "Loan.Purpose = \"Purchase\"", "PURPOSE-001", "Loan purpose is supported."));
        AddCategoryWithRules(loanContext, "03 - Property",
            ("01 - Property Type Rule", "UnknownMethod(Loan.Property.Type)", "PROP-001", "Property type is supported."),
            ("02 - Property Value Rule", "Loan.Property.Value > 100000", "VALUE-001", "Property value is acceptable."));
        AddCategoryWithRules(loanContext, "04 - Debt",
            ("01 - Debt To Income Rule", "Loan.DebtToIncomeRatio < 45", "DTI-001", "Debt ratio is acceptable."));

        loanContext.Expressions.Add(new RuleExpression
        {
            Name = "03 - Invalid Rule",
            Expression = "Loan.Amount > \"ABC\"",
            Severity = "Error",
            ResultCode = "BAD-001",
            ResultMessage = "Invalid sample."
        });
        loanContext.Expressions.Add(new RuleExpression
        {
            Name = "02 - Unmapped Rule",
            Expression = "Loan.Amount > 1000",
            Severity = "Warning"
        });

        _library.Contexts.Add(loanContext);
        _library.Contexts.Add(coBorrower);
        _library.Contexts.Add(property);
    }

    private static void AddCategoryWithRules(RuleContext context, string categoryName, params (string Name, string Expression, string Code, string Message)[] rules)
    {
        var category = new RuleCategory { Name = categoryName };
        foreach ((string name, string expression, string code, string message) in rules)
        {
            var rule = new RuleExpression
            {
                Name = name,
                Expression = expression,
                ResultCode = code,
                ResultMessage = message,
                Severity = name.Contains("Age", StringComparison.OrdinalIgnoreCase) ? "Error" : "Warning",
                Priority = 10,
                Tags = ["age", "loan", "eligibility"]
            };
            context.Expressions.Add(rule);
            category.ExpressionIds.Add(rule.Id);
        }
        context.Categories.Add(category);
    }

    private static string GetContextNodeText(RuleContext context) => string.IsNullOrWhiteSpace(context.QualifiedTypeName)
        ? context.Name
        : $"{context.Name}  ({context.QualifiedTypeName})";

    private static bool ContainsRule(IEnumerable<RuleCategory> categories, Guid id) =>
        categories.Any(c => c.ExpressionIds.Contains(id) || ContainsRule(c.Categories, id));

    private static void RemoveRuleReferences(IEnumerable<RuleCategory> categories, Guid id)
    {
        foreach (RuleCategory category in categories)
        {
            category.ExpressionIds.Remove(id);
            RemoveRuleReferences(category.Categories, id);
        }
    }

    private static bool RemoveCategory(List<RuleCategory> categories, RuleCategory target)
    {
        if (categories.Remove(target)) return true;
        foreach (RuleCategory category in categories)
        {
            if (RemoveCategory(category.Categories, target)) return true;
        }
        return false;
    }

    private static string SafeFileName(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
        return value;
    }

    private static string? NullIfWhiteSpace(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string NextNumberedName(IEnumerable<string> existing, string baseName)
    {
        var set = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!set.Contains(baseName)) return baseName;
        int i = 2;
        while (set.Contains($"{baseName} {i}")) i++;
        return $"{baseName} {i}";
    }

    private static ToolStripMenuItem MenuItem(string text, Keys shortcut, EventHandler click)
    {
        var item = new ToolStripMenuItem(text) { ShortcutKeys = shortcut };
        item.Click += click;
        return item;
    }

    private static ToolStripButton ToolButton(string text, string toolTip, Image image, EventHandler click)
    {
        var button = new ToolStripButton(text.Replace("\\n", Environment.NewLine), image)
        {
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            TextImageRelation = TextImageRelation.ImageAboveText,
            ToolTipText = toolTip,
            AutoSize = false,
            Width = 82,
            Height = 92,
            TextAlign = ContentAlignment.BottomCenter,
            ImageAlign = ContentAlignment.TopCenter
        };
        button.Click += click;
        return button;
    }

    private enum IconKind { Document, Folder, Disk, DiskPlus, Database, Cube, FolderPlus, FolderBranch, FolderMinus, Sort, DocumentPlus, Copy, Trash, Up, Down, Play, Gear, ClipboardCheck }

    private void BuildImages()
    {
        _treeImages.Images.Add("library", MakeIcon(IconKind.Cube, Color.SteelBlue, 16));
        _treeImages.Images.Add("context", MakeIcon(IconKind.Database, Color.RoyalBlue, 16));
        _treeImages.Images.Add("category", MakeIcon(IconKind.Folder, Color.DarkGoldenrod, 16));
        _treeImages.Images.Add("rule", MakeIcon(IconKind.Document, Color.SteelBlue, 16));

        _issueImages.Images.Add("Error", SeverityIcon("!", Color.FromArgb(210, 55, 48)));
        _issueImages.Images.Add("Warning", SeverityIcon("!", Color.Goldenrod));
        _issueImages.Images.Add("Info", SeverityIcon("i", Color.RoyalBlue));
    }

    private static Image SeverityIcon(string glyph, Color color)
    {
        var bmp = new Bitmap(16, 16);
        using Graphics g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var b = new SolidBrush(color);
        g.FillEllipse(b, 1, 1, 14, 14);
        using var tb = new SolidBrush(Color.White);
        using var f = new Font("Segoe UI", 8, FontStyle.Bold);
        SizeF s = g.MeasureString(glyph, f);
        g.DrawString(glyph, f, tb, (16 - s.Width) / 2, (16 - s.Height) / 2 - 1);
        return bmp;
    }

    private static Image MakeIcon(IconKind kind, Color color, int size = 32)
    {
        var bmp = new Bitmap(size, size);
        using Graphics g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var pen = new Pen(color, Math.Max(1.5f, size / 18f));
        using var brush = new SolidBrush(Color.FromArgb(30, color));
        float s = size;

        switch (kind)
        {
            case IconKind.Document:
            case IconKind.DocumentPlus:
                g.FillRectangle(brush, s * .25f, s * .12f, s * .48f, s * .72f);
                g.DrawRectangle(pen, s * .25f, s * .12f, s * .48f, s * .72f);
                if (kind == IconKind.DocumentPlus) DrawPlus(g, color, s * .68f, s * .72f, s * .18f);
                break;
            case IconKind.Folder:
            case IconKind.FolderPlus:
            case IconKind.FolderBranch:
            case IconKind.FolderMinus:
                g.FillRectangle(brush, s * .12f, s * .35f, s * .76f, s * .42f);
                g.DrawRectangle(pen, s * .12f, s * .35f, s * .76f, s * .42f);
                g.DrawLine(pen, s * .16f, s * .35f, s * .36f, s * .22f);
                g.DrawLine(pen, s * .36f, s * .22f, s * .54f, s * .35f);
                if (kind == IconKind.FolderPlus) DrawPlus(g, color, s * .78f, s * .68f, s * .16f);
                if (kind == IconKind.FolderMinus) g.DrawLine(pen, s * .68f, s * .68f, s * .86f, s * .68f);
                if (kind == IconKind.FolderBranch) g.DrawArc(pen, s * .62f, s * .55f, s * .22f, s * .22f, 0, 270);
                break;
            case IconKind.Disk:
            case IconKind.DiskPlus:
                g.FillRectangle(brush, s * .18f, s * .16f, s * .64f, s * .64f);
                g.DrawRectangle(pen, s * .18f, s * .16f, s * .64f, s * .64f);
                g.DrawRectangle(pen, s * .32f, s * .18f, s * .32f, s * .2f);
                if (kind == IconKind.DiskPlus) DrawPlus(g, color, s * .76f, s * .76f, s * .16f);
                break;
            case IconKind.Database:
                g.DrawEllipse(pen, s * .22f, s * .16f, s * .56f, s * .2f);
                g.DrawRectangle(pen, s * .22f, s * .26f, s * .56f, s * .5f);
                g.DrawArc(pen, s * .22f, s * .66f, s * .56f, s * .2f, 0, 180);
                break;
            case IconKind.Cube:
                PointF[] top = [new(s*.5f,s*.12f), new(s*.78f,s*.28f), new(s*.5f,s*.44f), new(s*.22f,s*.28f)];
                g.DrawPolygon(pen, top); g.DrawLine(pen, s*.22f,s*.28f,s*.22f,s*.62f); g.DrawLine(pen, s*.78f,s*.28f,s*.78f,s*.62f); g.DrawLine(pen, s*.5f,s*.44f,s*.5f,s*.82f); g.DrawLine(pen, s*.22f,s*.62f,s*.5f,s*.82f); g.DrawLine(pen, s*.78f,s*.62f,s*.5f,s*.82f);
                break;
            case IconKind.Copy:
                g.DrawRectangle(pen, s*.20f,s*.28f,s*.42f,s*.52f); g.DrawRectangle(pen, s*.36f,s*.14f,s*.42f,s*.52f);
                break;
            case IconKind.Trash:
                g.DrawRectangle(pen, s*.28f,s*.32f,s*.44f,s*.48f); g.DrawLine(pen,s*.22f,s*.28f,s*.78f,s*.28f); g.DrawLine(pen,s*.40f,s*.18f,s*.60f,s*.18f);
                break;
            case IconKind.Sort:
                g.DrawLine(pen,s*.36f,s*.18f,s*.36f,s*.78f); g.DrawLine(pen,s*.25f,s*.30f,s*.36f,s*.18f); g.DrawLine(pen,s*.47f,s*.30f,s*.36f,s*.18f); g.DrawLine(pen,s*.64f,s*.18f,s*.64f,s*.78f); g.DrawLine(pen,s*.53f,s*.66f,s*.64f,s*.78f); g.DrawLine(pen,s*.75f,s*.66f,s*.64f,s*.78f);
                break;
            case IconKind.Up:
                g.DrawLine(pen,s*.50f,s*.18f,s*.50f,s*.78f); g.DrawLine(pen,s*.32f,s*.36f,s*.50f,s*.18f); g.DrawLine(pen,s*.68f,s*.36f,s*.50f,s*.18f); break;
            case IconKind.Down:
                g.DrawLine(pen,s*.50f,s*.18f,s*.50f,s*.78f); g.DrawLine(pen,s*.32f,s*.60f,s*.50f,s*.78f); g.DrawLine(pen,s*.68f,s*.60f,s*.50f,s*.78f); break;
            case IconKind.Play:
                g.DrawPolygon(pen, [new PointF(s*.32f,s*.18f), new PointF(s*.32f,s*.82f), new PointF(s*.78f,s*.50f)]); break;
            case IconKind.Gear:
                g.DrawEllipse(pen,s*.24f,s*.24f,s*.52f,s*.52f); g.DrawEllipse(pen,s*.42f,s*.42f,s*.16f,s*.16f); break;
            case IconKind.ClipboardCheck:
                g.DrawRectangle(pen,s*.24f,s*.18f,s*.52f,s*.66f); g.DrawLine(pen,s*.36f,s*.56f,s*.46f,s*.68f); g.DrawLine(pen,s*.46f,s*.68f,s*.68f,s*.42f); break;
        }

        return bmp;
    }

    private static void DrawPlus(Graphics g, Color color, float x, float y, float len)
    {
        using var pen = new Pen(color, 2.2f);
        g.DrawLine(pen, x - len / 2, y, x + len / 2, y);
        g.DrawLine(pen, x, y - len / 2, x, y + len / 2);
    }
}
