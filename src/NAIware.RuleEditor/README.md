# NAIware Rule Editor

NAIware.RuleEditor is a Windows Forms application for creating, editing, validating, testing, and saving NAIware rule libraries. The primary UI is implemented in `MainForm.cs`, with supporting dialogs for selecting .NET types, loading test data, and viewing test results.

## Main window layout

`MainForm` builds the UI in `BuildFormLayout()`.

| UI area | Purpose | Builder method |
| --- | --- | --- |
| Menu bar | File, Library, Context, Category, Rule, Test, View, Tools, and Help commands | `BuildMenu()` |
| Command strip | Large icon shortcuts for common commands | `BuildCommandStrip()` |
| Left pane | Rule library tree, search box, and library counts | `BuildLibraryPanel()` |
| Center pane | Rule expression editor and result definition fields | `BuildEditorPanel()` |
| Right pane | Library, context, category, or rule properties | `BuildPropertiesPanel()` |
| Bottom pane | Validation issue/error list | `BuildErrorPanel()` |
| Status bar | Ready text, library name, and validation status | `BuildStatusBar()` |

The main layout is created by `BuildShell()` using nested `SplitContainer` controls.

## Startup and form lifecycle

| Event or action | Handler method | Description |
| --- | --- | --- |
| Form construction | `MainForm()` | Initializes services, calls `InitializeComponent()`, builds the layout, wires events, creates sample data, refreshes the tree, and updates status. |
| Layout construction | `BuildFormLayout()` | Creates images, assigns image lists, initializes severity values, and adds the shell/menu/toolbar/status controls. |
| Form closing | `ConfirmDiscardChanges()` | Prompts to save unsaved changes before closing. |
| Initial sample library creation | `CreateMockupSampleLibrary()` | Creates mock rule library data for initial display. |
| Tree refresh | `RefreshTree()` | Rebuilds the library tree from the current `RulesLibrary`. |

## Menu features and event handlers

### File menu

| Command | Shortcut | Handler | Description |
| --- | --- | --- | --- |
| New Library | Ctrl+N | `NewLibrary()` | Creates a new library after confirming unsaved changes. |
| Open Library | Ctrl+O | `OpenLibrary()` | Opens a rule library JSON file. |
| Save | Ctrl+S | `SaveLibrary()` | Saves to the current path, or prompts with Save As if needed. |
| Save As | Ctrl+Shift+S | `SaveLibraryAs()` | Prompts for a file path and saves the library. |
| Exit | Alt+F4 | `Close()` | Closes the form. Unsaved changes are handled by the form closing event. |

### Library menu

| Command | Shortcut | Handler | Description |
| --- | --- | --- | --- |
| Add Context From DLL | Ctrl+D | `AddContextFromDll()` | Loads public concrete types from a selected DLL and adds the selected type as a context. |
| Manage Contexts | None | Inline `MessageBox.Show(...)` | Placeholder for future functionality. |
| Validate Library | F6 | `ValidateLibrary()` | Validates the current library and populates the error list. |

### Context menu

| Command | Shortcut | Handler | Description |
| --- | --- | --- | --- |
| Add Context From DLL | Ctrl+D | `AddContextFromDll()` | Adds a new context from a selected assembly. |
| Remove Selected Context | None | `DeleteSelected()` | Deletes the selected context after confirmation. |

### Category menu

| Command | Shortcut | Handler | Description |
| --- | --- | --- | --- |
| Add Category | None | `AddCategory()` | Adds a category to the selected context. |
| Add Subcategory | None | `AddSubcategory()` | Adds a nested category under the selected category. |
| Delete Category | None | `DeleteSelected()` | Deletes the selected category after confirmation. |

### Rule menu

| Command | Shortcut | Handler | Description |
| --- | --- | --- | --- |
| Add Rule | Ctrl+R | `AddRule()` | Adds a new rule to the selected context and optional category. |
| Duplicate Rule | Ctrl+Shift+D | `DuplicateRule()` | Copies the selected rule. |
| Delete Rule | Delete | `DeleteSelected()` | Deletes the selected rule and removes category references. |
| Move Up | Ctrl+Up | `MoveSelected(-1)` | Moves the selected rule up. |
| Move Down | Ctrl+Down | `MoveSelected(1)` | Moves the selected rule down. |

### Test menu

| Command | Shortcut | Handler | Description |
| --- | --- | --- | --- |
| Run Tests | F5 | `TestRules()` | Loads test data, evaluates rules, and shows test results. |
| Test Settings | None | Inline `MessageBox.Show(...)` | Placeholder for future functionality. |
| Validate Library | F6 | `ValidateLibrary()` | Validates the current library. |

### View menu

| Command | Shortcut | Handler | Description |
| --- | --- | --- | --- |
| Expand All | None | `_ruleTree.ExpandAll()` | Expands all tree nodes. |
| Collapse All | None | `_ruleTree.CollapseAll()` | Collapses all tree nodes. |
| Error List | Ctrl+E | `_errorList.Focus()` | Moves focus to the error list. |

### Tools menu

| Command | Handler | Description |
| --- | --- | --- |
| Options | Inline `MessageBox.Show(...)` | Placeholder for future functionality. |

### Help menu

| Command | Shortcut | Handler | Description |
| --- | --- | --- | --- |
| View Documentation | F1 | Inline `MessageBox.Show(...)` | Displays a reference to `docs/windows-ui.md`. |
| About NAIware Rule Editor | None | `ShowAbout()` | Displays application information. |

## Command strip buttons

`BuildCommandStrip()` creates large toolbar buttons with `ToolButton()`.

| Button | Handler | Description |
| --- | --- | --- |
| New Library | `NewLibrary()` | Creates a new library. |
| Open Library | `OpenLibrary()` | Opens a library file. |
| Save | `SaveLibrary()` | Saves the current library. |
| Save As | `SaveLibraryAs()` | Saves to a selected file. |
| Add Context | `AddContextFromDll()` | Adds a context from a DLL. |
| Manage Contexts | Inline `MessageBox.Show(...)` | Placeholder. |
| Add Category | `AddCategory()` | Adds a category. |
| Add Subcategory | `AddSubcategory()` | Adds a subcategory. |
| Delete Category | `DeleteSelected()` | Deletes the selected item. |
| Reorder | Inline `MessageBox.Show(...)` | Explains that Move Up/Move Down reorder rules. |
| Add Rule | `AddRule()` | Adds a rule. |
| Duplicate Rule | `DuplicateRule()` | Duplicates a rule. |
| Delete Rule | `DeleteSelected()` | Deletes the selected item. |
| Move Up | `MoveSelected(-1)` | Moves the selected rule up. |
| Move Down | `MoveSelected(1)` | Moves the selected rule down. |
| Run Tests | `TestRules()` | Runs rule tests. |
| Test Settings | Inline `MessageBox.Show(...)` | Placeholder. |
| Validate Library | `ValidateLibrary()` | Validates the library. |

## Rule library tree

The left pane shows a hierarchy of library, contexts, categories, subcategories, and rules.

| UI event | Handler | Description |
| --- | --- | --- |
| Tree selection changed | `BindSelection()` | Loads selected item data into the editor and properties panel. |
| Tree right-click | `ShowTreeMenu(Point location)` | Opens the tree context menu. |
| Context menu Add Category | `AddCategory()` | Adds a category. |
| Context menu Add Subcategory | `AddSubcategory()` | Adds a subcategory. |
| Context menu Add Rule | `AddRule()` | Adds a rule. |
| Context menu Duplicate Rule | `DuplicateRule()` | Duplicates a rule. |
| Context menu Delete | `DeleteSelected()` | Deletes the selected item. |

Supporting tree methods:

| Method | Purpose |
| --- | --- |
| `GetSelectedContext()` | Finds the selected context by walking up the tree. |
| `GetSelectedCategory()` | Finds the selected category by walking up the tree. |
| `GetSelectedRule()` | Returns the selected rule, if any. |
| `AddCategoryNode()` | Adds category and rule nodes recursively. |
| `ContainsRule()` | Detects whether a rule is already represented under a category. |
| `RemoveRuleReferences()` | Removes deleted rule IDs from categories. |
| `RemoveCategory()` | Removes a category from nested category lists. |

## Expression editor

The center editor is shown for selected rules. It includes the expression text box, IntelliSense list, Insert combo box, Validate button, and result definition fields.

| UI event | Handler | Description |
| --- | --- | --- |
| Expression text changed | `OnEditorChanged()` | Writes edits to the selected rule and marks the library dirty. |
| Result code changed | `OnEditorChanged()` | Updates the selected rule result code. |
| Result message changed | `OnEditorChanged()` | Updates the selected rule result message. |
| Severity changed | `OnEditorChanged()` | Updates the selected rule severity. |
| Optional value changed | `OnEditorChanged()` | Updates the selected rule optional value. |
| Validate clicked | `ValidateLibrary()` | Runs validation. |
| Insert combo changed | `InsertSelectedSuggestion(ComboBox comboBox)` | Inserts supported operators or keywords. |
| Expression key up | `ShowIntelliSense()` | Displays suggestions for the current token. |
| Expression key down | Inline logic in `WireIntelliSenseEvents()` | Navigates, accepts, or dismisses IntelliSense. |
| IntelliSense double-click | `InsertIntelliSenseSelection()` | Inserts the selected suggestion. |

Supporting editor methods:

| Method | Purpose |
| --- | --- |
| `WireEditorEvents()` | Wires text, checkbox, numeric, and combo change events to `OnEditorChanged()`. |
| `WireIntelliSenseEvents()` | Wires expression editor keyboard behavior and IntelliSense selection. |
| `FlushSelectionToModel()` | Copies UI values back into the selected model object. |
| `UpdateEditorVisibility()` | Hides the expression editor when the selected item is not a rule. |
| `GetCurrentToken()` | Gets the token before the caret for IntelliSense lookup. |

## Properties panel

`UpdateSelectionInfoPanel()` switches the right-hand panel between library, context, category, and rule views. `ShowPropertiesView(string title, Control view)` controls which panel is visible.

### Rule properties

Built by `BuildRulePropertiesView()`.

| Field | Handler | Model update |
| --- | --- | --- |
| Name | `OnEditorChanged()` | `RuleExpression.Name` |
| Description | `OnEditorChanged()` | `RuleExpression.Description` |
| Enabled | `OnEditorChanged()` | `RuleExpression.IsActive` |
| Tags | `OnEditorChanged()` | `RuleExpression.Tags` |
| Priority | `OnEditorChanged()` | `RuleExpression.Priority` |

### Library properties

Built by `BuildLibraryPropertiesView()` and updated by `UpdateSelectionInfoPanel()`.

Displays saved state, saved timestamp, file path, version, context count, and rule count.

### Category properties

Built by `BuildCategoryPropertiesView()`.

| Field | Handler | Model update |
| --- | --- | --- |
| Name | `OnEditorChanged()` | `RuleCategory.Name` |
| Description | `OnEditorChanged()` | `RuleCategory.Description` |
| Parent display | `UpdateSelectionInfoPanel()` / `GetCategoryParentName()` | Display only |

### Context properties

Built by `BuildContextPropertiesView()`.

| Field or button | Handler | Description |
| --- | --- | --- |
| DLL Path Browse | `SelectContextTypeForCurrentSelection()` | Selects the context assembly and type. |
| Instance Name | Inline text handler in `WireContextViewEvents()` | Updates `RuleContext.Name`. |
| Serializer DLL Browse | `SelectSerializerForCurrentSelection()` | Selects a serializer assembly and serializer type. |
| Serialized File Browse | `SelectSerializedFileForCurrentSelection()` | Selects serialized JSON/XML data and hydrates an object graph. |
| Object Graph tree | `PopulateObjectGraph()` / `AddObjectGraphNodes()` | Displays hydrated object data. |

Supporting context methods:

| Method | Purpose |
| --- | --- |
| `WireContextViewEvents()` | Wires context-specific field and button events. |
| `SelectContextTypeForCurrentSelection()` | Changes the selected context type. |
| `SelectSerializerForCurrentSelection()` | Selects and validates a custom serializer. |
| `SelectSerializedFileForCurrentSelection()` | Selects data to hydrate. |
| `TryHydrateAndShowObjectGraph()` | Resolves the context type, deserializes data, and updates the object graph. |
| `DeserializeContextData()` | Uses default JSON/XML loading or the selected custom serializer. |
| `SupportsFilePathDeserialize()` | Ensures a serializer exposes `Deserialize(string filePath)`. |
| `ResolveTypeFromAssembly()` | Resolves types by full name or assembly-qualified name. |
| `PopulateObjectGraph()` | Rebuilds the object graph tree. |
| `AddObjectGraphNodes()` | Recursively adds object, property, and collection nodes. |
| `FormatValue()` | Formats node display values. |
| `IsLeafType()` | Determines whether values are scalar leaves. |
| `UpdateContextLabels()` | Updates context labels in the properties view. |

## Error list

The bottom pane displays validation results.

| UI event | Handler | Description |
| --- | --- | --- |
| Validate command | `ValidateLibrary()` | Runs validation and updates the list. |
| Error row double-click | `NavigateToSelectedIssue()` | Selects the associated rule in the tree. |

Supporting error-list methods:

| Method | Purpose |
| --- | --- |
| `PopulateIssues()` | Converts validation issues into `ListViewItem` rows. |
| `NavigateToSelectedIssue()` | Reads the selected issue and navigates to the related rule. |
| `SelectRuleNode(Guid id)` | Selects a rule node by rule ID. |
| `Flatten(TreeNodeCollection nodes)` | Enumerates tree nodes recursively. |

## Test workflow

| Step | Method | Description |
| --- | --- | --- |
| Start rule test | `TestRules()` | Resolves the context type, opens the data dialog, runs evaluation, and opens results. |
| Browse for test data | `TestDataDialog.BrowseForFile()` | Selects JSON or XML test data. |
| Load test data | `TestDataDialog.AttemptLoad()` | Deserializes the selected test data. |
| Deserialize JSON/XML | `TestDataDialog.LoadObjectFromFile()` | Loads a file into the selected context type. |
| Show results | `TestResultDialog` constructor | Builds the results dialog. |
| Select result row | `TestResultDialog.ShowDetailForSelection()` | Shows details for the selected match or mismatch. |

## Supporting dialogs

### ContextTypePickerDialog

Used by context and serializer selection flows.

| UI event | Handler | Description |
| --- | --- | --- |
| Dialog load | `ApplyFilter()` | Populates the type list. |
| Filter text changed | `ApplyFilter()` | Filters available types. |
| Type selection changed | Inline handler | Enables OK when a type is selected. |
| Type double-click | Inline handler | Accepts the selected type and closes the dialog. |

### TestDataDialog

Used by `TestRules()`.

| UI event | Handler | Description |
| --- | --- | --- |
| Browse clicked | `BrowseForFile()` | Selects a JSON/XML file. |
| Load & Run clicked | `AttemptLoad()` | Loads the selected file and closes the dialog on success. |

### TestResultDialog

Used after `TestRules()` evaluates rules.

| UI event | Handler | Description |
| --- | --- | --- |
| Result row selection changed | `ShowDetailForSelection()` | Shows detailed result diagnostics. |
| Dialog shown | Inline `Shown` handler | Sets splitter distance and selects the first result. |
| Close clicked | Button `DialogResult.OK` | Closes the dialog. |

## Persistence methods

| Method | Description |
| --- | --- |
| `NewLibrary()` | Creates a new in-memory library. |
| `OpenLibrary()` | Loads a library JSON file from disk. |
| `SaveLibrary()` | Saves to the current file path or delegates to Save As. |
| `SaveLibraryAs()` | Prompts for a path and saves. |
| `SaveTo(string path)` | Serializes the library and updates saved state. |
| `ConfirmDiscardChanges()` | Prompts before losing unsaved changes. |
| `UpdateWindowTitle()` | Shows dirty state and current file name. |

## Status and utility methods

| Method | Description |
| --- | --- |
| `UpdateCountsAndStatus(string status)` | Updates context count, rule count, library name, and ready text. |
| `UpdateSelectionInfoPanel()` | Refreshes the properties panel for the current selection. |
| `SafeFileName()` | Produces a file-safe library name. |
| `NullIfWhiteSpace()` | Converts blank strings to null. |
| `NextNumberedName()` | Generates unique default names. |
| `MenuItem()` | Creates and wires a menu item. |
| `ToolButton()` | Creates and wires a command strip button. |
| `BuildImages()` | Populates image lists. |
| `SeverityIcon()` | Draws severity icons. |
| `MakeIcon()` | Draws toolbar/tree icons. |
| `DrawPlus()` | Draws plus overlays for icons.
