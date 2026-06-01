namespace NAIware.RuleEditor;

partial class MainForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer? components;

    // --- Form shell controls (designer-managed) ---
    // These provide a visible design surface in Visual Studio. They are intentionally
    // left "empty" of dynamic content (menu items, toolstrip buttons, status labels,
    // and the rule library / editor / properties / error list internals) which are
    // populated at runtime from MainForm.cs. The designer should not attempt to
    // instantiate runtime-generated icons or business controls at design time.
    private MenuStrip _menuStrip = null!;
    private ToolStrip _commandStrip = null!;
    private StatusStrip _statusStrip = null!;

    private SplitContainer _shellSplit = null!;
    private SplitContainer _topSplit = null!;
    private SplitContainer _editorAndPropsSplit = null!;

    private Panel _libraryHost = null!;
    private Panel _editorHost = null!;
    private Panel _propertiesHost = null!;
    private Panel _errorHost = null!;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        _menuStrip = new MenuStrip();
        _commandStrip = new ToolStrip();
        _statusStrip = new StatusStrip();
        _shellSplit = new SplitContainer();
        _topSplit = new SplitContainer();
        _libraryHost = new Panel();
        _editorAndPropsSplit = new SplitContainer();
        _editorHost = new Panel();
        _propertiesHost = new Panel();
        _errorHost = new Panel();
        ((System.ComponentModel.ISupportInitialize)_shellSplit).BeginInit();
        _shellSplit.Panel1.SuspendLayout();
        _shellSplit.Panel2.SuspendLayout();
        _shellSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_topSplit).BeginInit();
        _topSplit.Panel1.SuspendLayout();
        _topSplit.Panel2.SuspendLayout();
        _topSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_editorAndPropsSplit).BeginInit();
        _editorAndPropsSplit.Panel1.SuspendLayout();
        _editorAndPropsSplit.Panel2.SuspendLayout();
        _editorAndPropsSplit.SuspendLayout();
        SuspendLayout();
        // 
        // _menuStrip
        // 
        _menuStrip.BackColor = Color.White;
        _menuStrip.Location = new Point(0, 0);
        _menuStrip.Name = "_menuStrip";
        _menuStrip.RenderMode = ToolStripRenderMode.System;
        _menuStrip.Size = new Size(1500, 24);
        _menuStrip.TabIndex = 0;
        _menuStrip.Text = "menuStrip";
        // 
        // _commandStrip
        // 
        _commandStrip.AutoSize = false;
        _commandStrip.BackColor = Color.White;
        _commandStrip.GripStyle = ToolStripGripStyle.Hidden;
        _commandStrip.ImageScalingSize = new Size(28, 28);
        _commandStrip.Location = new Point(0, 24);
        _commandStrip.Name = "_commandStrip";
        _commandStrip.Padding = new Padding(8, 6, 8, 0);
        _commandStrip.RenderMode = ToolStripRenderMode.System;
        _commandStrip.Size = new Size(1500, 128);
        _commandStrip.TabIndex = 1;
        _commandStrip.Text = "commandStrip";
        // 
        // _statusStrip
        // 
        _statusStrip.Location = new Point(0, 958);
        _statusStrip.Name = "_statusStrip";
        _statusStrip.Size = new Size(1500, 22);
        _statusStrip.SizingGrip = false;
        _statusStrip.TabIndex = 2;
        _statusStrip.Text = "statusStrip";
        // 
        // _shellSplit
        // 
        _shellSplit.Dock = DockStyle.Fill;
        _shellSplit.Location = new Point(0, 152);
        _shellSplit.Name = "_shellSplit";
        _shellSplit.Orientation = Orientation.Horizontal;
        // 
        // _shellSplit.Panel1
        // 
        _shellSplit.Panel1.Controls.Add(_topSplit);
        _shellSplit.Panel1MinSize = 0;
        // 
        // _shellSplit.Panel2
        // 
        _shellSplit.Panel2.Controls.Add(_errorHost);
        _shellSplit.Panel2MinSize = 165;
        _shellSplit.Size = new Size(1500, 806);
        _shellSplit.SplitterDistance = 572;
        _shellSplit.TabIndex = 3;
        // 
        // _topSplit
        // 
        _topSplit.Dock = DockStyle.Fill;
        _topSplit.Location = new Point(0, 0);
        _topSplit.Name = "_topSplit";
        // 
        // _topSplit.Panel1
        // 
        _topSplit.Panel1.Controls.Add(_libraryHost);
        _topSplit.Panel1MinSize = 260;
        // 
        // _topSplit.Panel2
        // 
        _topSplit.Panel2.Controls.Add(_editorAndPropsSplit);
        _topSplit.Panel2MinSize = 500;
        _topSplit.Size = new Size(1500, 572);
        _topSplit.SplitterDistance = 996;
        _topSplit.TabIndex = 0;
        // 
        // _libraryHost
        // 
        _libraryHost.BackColor = Color.White;
        _libraryHost.Dock = DockStyle.Fill;
        _libraryHost.Location = new Point(0, 0);
        _libraryHost.Name = "_libraryHost";
        _libraryHost.Size = new Size(996, 572);
        _libraryHost.TabIndex = 0;
        // 
        // _editorAndPropsSplit
        // 
        _editorAndPropsSplit.Dock = DockStyle.Fill;
        _editorAndPropsSplit.Location = new Point(0, 0);
        _editorAndPropsSplit.Name = "_editorAndPropsSplit";
        // 
        // _editorAndPropsSplit.Panel1
        // 
        _editorAndPropsSplit.Panel1.Controls.Add(_editorHost);
        _editorAndPropsSplit.Panel1MinSize = 420;
        // 
        // _editorAndPropsSplit.Panel2
        // 
        _editorAndPropsSplit.Panel2.Controls.Add(_propertiesHost);
        _editorAndPropsSplit.Panel2MinSize = 250;
        _editorAndPropsSplit.Size = new Size(500, 572);
        _editorAndPropsSplit.SplitterDistance = 121;
        _editorAndPropsSplit.TabIndex = 0;
        // 
        // _editorHost
        // 
        _editorHost.BackColor = Color.White;
        _editorHost.Dock = DockStyle.Fill;
        _editorHost.Location = new Point(0, 0);
        _editorHost.Name = "_editorHost";
        _editorHost.Size = new Size(121, 100);
        _editorHost.TabIndex = 0;
        // 
        // _propertiesHost
        // 
        _propertiesHost.BackColor = Color.White;
        _propertiesHost.Dock = DockStyle.Fill;
        _propertiesHost.Location = new Point(0, 0);
        _propertiesHost.Name = "_propertiesHost";
        _propertiesHost.Size = new Size(25, 100);
        _propertiesHost.TabIndex = 0;
        // 
        // _errorHost
        // 
        _errorHost.BackColor = Color.White;
        _errorHost.Dock = DockStyle.Fill;
        _errorHost.Location = new Point(0, 0);
        _errorHost.Name = "_errorHost";
        _errorHost.Size = new Size(1500, 230);
        _errorHost.TabIndex = 0;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1500, 980);
        Controls.Add(_shellSplit);
        Controls.Add(_commandStrip);
        Controls.Add(_menuStrip);
        Controls.Add(_statusStrip);
        KeyPreview = true;
        MainMenuStrip = _menuStrip;
        MinimumSize = new Size(1100, 720);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "NAIware Rule Editor";
        _shellSplit.Panel1.ResumeLayout(false);
        _shellSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_shellSplit).EndInit();
        _shellSplit.ResumeLayout(false);
        _topSplit.Panel1.ResumeLayout(false);
        _topSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_topSplit).EndInit();
        _topSplit.ResumeLayout(false);
        _editorAndPropsSplit.Panel1.ResumeLayout(false);
        _editorAndPropsSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_editorAndPropsSplit).EndInit();
        _editorAndPropsSplit.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
