using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using Gtk;
using Confluxys.Models;
using Confluxys.Services;

namespace Confluxys
{
    public class MainWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly DocumentService _documentService;
        private readonly QueryHistoryService _queryHistoryService;
        private readonly DocumentReprocessingService _reprocessingService;

        // UI Components
        private TreeView _databaseTreeView = null!;
        private TreeStore _databaseTreeStore = null!;
        private TextView _queryTextView = null!;
        private TextView _documentTextView = null!;
        private ScrolledWindow _queryScrolledWindow = null!;
        private TreeView _resultsTreeView = null!;
        private ListStore? _resultsListStore;
        private ScrolledWindow _resultsScrolledWindow = null!;
        private Label _statusLabel = null!;
        private ComboBoxText _viewModeCombo = null!;
        private bool _isTextViewMode = false;
        private int _documentIdColumnIndex = -1;
        private string _selectedTableName = "Documents";
        private DataTable? _currentResultsTable;

        public MainWindow() : base("Confluxys - Document Management System")
        {
            // Initialize services
            _databaseService = new DatabaseService();
            _databaseService.InitializeDatabase();
            
            _documentService = new DocumentService();
            _queryHistoryService = new QueryHistoryService();
            _reprocessingService = new DocumentReprocessingService();

            // Set window properties
            SetDefaultSize(1200, 800);
            SetPosition(WindowPosition.Center);
            DeleteEvent += OnDeleteEvent;

            // Create UI
            CreateUI();
            
            // Load initial data
            RefreshDatabaseExplorer();
        }

        private void CreateUI()
        {
            var vbox = new Box(Orientation.Vertical, 0);

            // Create menu bar
            var menuBar = CreateMenuBar();
            vbox.PackStart(menuBar, false, false, 0);

            // Create main paned container
            var hpaned = new Paned(Orientation.Horizontal);
            hpaned.Position = 300;

            // Left panel - Database Explorer
            var leftPanel = CreateDatabaseExplorer();
            hpaned.Pack1(leftPanel, false, false);

            // Right panel - Query Editor and Results
            var rightPanel = CreateRightPanel();
            hpaned.Pack2(rightPanel, true, false);

            vbox.PackStart(hpaned, true, true, 0);

            // Status bar
            _statusLabel = new Label("Ready");
            _statusLabel.Halign = Align.Start;
            vbox.PackStart(_statusLabel, false, false, 5);

            Add(vbox);
            ShowAll();
        }

        private MenuBar CreateMenuBar()
        {
            var menuBar = new MenuBar();

            // File menu
            var fileMenu = new Menu();
            var fileMenuItem = new MenuItem("File");
            fileMenuItem.Submenu = fileMenu;

            var ingestFilesItem = new MenuItem("Ingest PDF Files...");
            ingestFilesItem.Activated += OnIngestFiles;
            fileMenu.Append(ingestFilesItem);

            var ingestFolderItem = new MenuItem("Ingest Folder...");
            ingestFolderItem.Activated += OnIngestFolder;
            fileMenu.Append(ingestFolderItem);

            fileMenu.Append(new SeparatorMenuItem());

            var exitItem = new MenuItem("Exit");
            exitItem.Activated += (sender, e) => Application.Quit();
            fileMenu.Append(exitItem);

            menuBar.Append(fileMenuItem);

            // Database menu
            var databaseMenu = new Menu();
            var databaseMenuItem = new MenuItem("Database");
            databaseMenuItem.Submenu = databaseMenu;

            var reprocessItem = new MenuItem("Reprocess All Documents");
            reprocessItem.Activated += OnReprocessAll;
            databaseMenu.Append(reprocessItem);

            menuBar.Append(databaseMenuItem);

            // Templates menu
            var templatesMenu = new Menu();
            var templatesMenuItem = new MenuItem("Templates");
            templatesMenuItem.Submenu = templatesMenu;

            var createTemplateItem = new MenuItem("Create Template...");
            createTemplateItem.Activated += OnCreateTemplate;
            templatesMenu.Append(createTemplateItem);

            var manageTemplatesItem = new MenuItem("Manage Templates...");
            manageTemplatesItem.Activated += OnManageTemplates;
            templatesMenu.Append(manageTemplatesItem);

            menuBar.Append(templatesMenuItem);

            return menuBar;
        }

        private Widget CreateDatabaseExplorer()
        {
            var frame = new Frame("Database Explorer");
            var scrolledWindow = new ScrolledWindow();
            scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

            _databaseTreeStore = new TreeStore(typeof(string), typeof(TableInfo));
            _databaseTreeView = new TreeView(_databaseTreeStore);
            
            var column = new TreeViewColumn("Tables", new CellRendererText(), "text", 0);
            _databaseTreeView.AppendColumn(column);
            
            _databaseTreeView.RowActivated += OnDatabaseTreeViewRowActivated;
            _databaseTreeView.Selection.Changed += OnDatabaseTreeViewSelectionChanged;
            _databaseTreeView.ButtonPressEvent += OnDatabaseTreeViewButtonPress;
            _databaseTreeView.ButtonReleaseEvent += OnDatabaseTreeViewButtonRelease;

            scrolledWindow.Add(_databaseTreeView);
            frame.Add(scrolledWindow);

            return frame;
        }

        private Widget CreateRightPanel()
        {
            var vpaned = new Paned(Orientation.Vertical);
            vpaned.Position = 300;

            // Top - Query Editor
            var queryFrame = new Frame("Query Editor");
            var queryVBox = new Box(Orientation.Vertical, 5);

            // Query toolbar
            var toolbar = new Box(Orientation.Horizontal, 5);
            toolbar.BorderWidth = 5;

            // Toggle button for Query/Text view
            // View mode dropdown
            _viewModeCombo = new ComboBoxText();
            _viewModeCombo.AppendText("Query View");
            _viewModeCombo.AppendText("Text View");
            _viewModeCombo.AppendText("Layout View");
            _viewModeCombo.Active = 0; // Default to Query View
            _viewModeCombo.Changed += OnViewModeChanged;
            toolbar.PackStart(_viewModeCombo, false, false, 0);

            // Add separator
            toolbar.PackStart(new Separator(Orientation.Vertical), false, false, 5);

            var prevButton = new Button("<");
            prevButton.Clicked += (s, e) => {
                var prevQuery = _queryHistoryService.GetPreviousQuery();
                if (prevQuery != null)
                {
                    _queryTextView.Buffer.Text = prevQuery;
                }
            };
            toolbar.PackStart(prevButton, false, false, 0);

            var nextButton = new Button(">");
            nextButton.Clicked += (s, e) => {
                var nextQuery = _queryHistoryService.GetNextQuery();
                if (nextQuery != null)
                {
                    _queryTextView.Buffer.Text = nextQuery;
                }
            };
            toolbar.PackStart(nextButton, false, false, 0);

            var executeButton = new Button("Execute (F5)");
            executeButton.Clicked += (s, e) => ExecuteQuery();
            toolbar.PackStart(executeButton, false, false, 10);

            queryVBox.PackStart(toolbar, false, false, 0);

            // Query text editor
            _queryScrolledWindow = new ScrolledWindow();
            _queryScrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            
            _queryTextView = new TextView();
            _queryTextView.Buffer.Text = $"SELECT * FROM [{_selectedTableName}] LIMIT 100";
            _queryTextView.KeyPressEvent += (o, args) => {
                if (args.Event.Key == Gdk.Key.F5)
                {
                    ExecuteQuery();
                    args.RetVal = true;
                }
            };
            
            // Document text viewer (initially hidden)
            _documentTextView = new TextView();
            _documentTextView.Editable = false;
            _documentTextView.WrapMode = WrapMode.Word;
            
            _queryScrolledWindow.Add(_queryTextView);
            queryVBox.PackStart(_queryScrolledWindow, true, true, 0);
            
            queryFrame.Add(queryVBox);
            vpaned.Pack1(queryFrame, false, true);

            // Bottom - Results
            var resultsFrame = new Frame("Results");
            _resultsScrolledWindow = new ScrolledWindow();
            _resultsScrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            
            // Initially create empty TreeView
            _resultsTreeView = new TreeView();
            _resultsScrolledWindow.Add(_resultsTreeView);
            
            resultsFrame.Add(_resultsScrolledWindow);
            vpaned.Pack2(resultsFrame, true, true);

            return vpaned;
        }

        private void RefreshDatabaseExplorer(bool preserveState = false)
        {
            Dictionary<string, bool>? expandedStates = null;
            
            if (preserveState)
            {
                expandedStates = SaveTreeViewExpandedState();
            }
            
            _databaseTreeStore.Clear();
            
            var tables = _databaseService.GetTables();
            foreach (var table in tables)
            {
                var tableIter = _databaseTreeStore.AppendValues(table.Name, table);
                
                foreach (var column in table.Columns)
                {
                    _databaseTreeStore.AppendValues(tableIter, $"{column.Name} ({column.Type})", null);
                }
            }
            
            if (preserveState && expandedStates != null)
            {
                RestoreTreeViewExpandedState(expandedStates);
            }
            // Remove ExpandAll() to start with collapsed view
        }

        private void OnDatabaseTreeViewSelectionChanged(object? sender, EventArgs e)
        {
            TreeIter iter;
            if (_databaseTreeView.Selection.GetSelected(out iter))
            {
                var table = _databaseTreeStore.GetValue(iter, 1) as TableInfo;
                if (table != null)
                {
                    _selectedTableName = table.Name;
                    _queryTextView.Buffer.Text = $"SELECT * FROM [{table.Name}] LIMIT 100";
                }
            }
        }

        private void OnDatabaseTreeViewRowActivated(object? o, RowActivatedArgs args)
        {
            // This now handles double-click to execute the query
            ExecuteQuery();
        }

        private void OnDatabaseTreeViewButtonPress(object o, ButtonPressEventArgs args)
        {
            if (args.Event.Button == 3) // Right click
            {
                // Just prevent default handling on press
                args.RetVal = true;
            }
        }

        private void OnDatabaseTreeViewButtonRelease(object o, ButtonReleaseEventArgs args)
        {
            if (args.Event.Button == 3) // Right click
            {
                TreePath path;
                if (_databaseTreeView.GetPathAtPos((int)args.Event.X, (int)args.Event.Y, out path))
                {
                    TreeIter iter;
                    if (_databaseTreeStore.GetIter(out iter, path))
                    {
                        var table = _databaseTreeStore.GetValue(iter, 1) as TableInfo;
                        if (table != null)
                        {
                            _selectedTableName = table.Name;
                            _databaseTreeView.Selection.SelectPath(path);
                            ShowDatabaseContextMenu(table, args.Event);
                            args.RetVal = true;
                        }
                    }
                }
            }
        }

        private void ShowDatabaseContextMenu(TableInfo table, Gdk.Event evt)
        {
            var menu = new Menu();

            // Common queries
            var selectAllItem = new MenuItem($"SELECT * FROM [{table.Name}]");
            selectAllItem.Activated += (s, e) => { _queryTextView.Buffer.Text = $"SELECT * FROM [{table.Name}]"; };
            menu.Append(selectAllItem);

            var selectLimitItem = new MenuItem($"SELECT * FROM [{table.Name}] LIMIT 100");
            selectLimitItem.Activated += (s, e) => { _queryTextView.Buffer.Text = $"SELECT * FROM [{table.Name}] LIMIT 100"; };
            menu.Append(selectLimitItem);

            var countItem = new MenuItem($"SELECT COUNT(*) FROM [{table.Name}]");
            countItem.Activated += (s, e) => { _queryTextView.Buffer.Text = $"SELECT COUNT(*) FROM [{table.Name}]"; };
            menu.Append(countItem);

            menu.Append(new SeparatorMenuItem());

            // Table structure
            var describeItem = new MenuItem("Show Table Structure");
            describeItem.Activated += (s, e) => { _queryTextView.Buffer.Text = $"PRAGMA table_info([{table.Name}])"; };
            menu.Append(describeItem);

            var indexesItem = new MenuItem("Show Indexes");
            indexesItem.Activated += (s, e) => { _queryTextView.Buffer.Text = $"PRAGMA index_list([{table.Name}])"; };
            menu.Append(indexesItem);

            menu.Append(new SeparatorMenuItem());

            // Data operations
            var distinctItem = new MenuItem("SELECT DISTINCT column");
            distinctItem.Activated += (s, e) => { 
                var firstColumn = table.Columns.FirstOrDefault()?.Name ?? "column_name";
                _queryTextView.Buffer.Text = $"SELECT DISTINCT [{firstColumn}] FROM [{table.Name}]"; 
            };
            menu.Append(distinctItem);

            var groupByItem = new MenuItem("GROUP BY with COUNT");
            groupByItem.Activated += (s, e) => { 
                var firstColumn = table.Columns.FirstOrDefault()?.Name ?? "column_name";
                _queryTextView.Buffer.Text = $"SELECT [{firstColumn}], COUNT(*) as count FROM [{table.Name}] GROUP BY [{firstColumn}] ORDER BY count DESC"; 
            };
            menu.Append(groupByItem);

            var whereItem = new MenuItem("WHERE clause template");
            whereItem.Activated += (s, e) => { 
                var firstColumn = table.Columns.FirstOrDefault()?.Name ?? "column_name";
                _queryTextView.Buffer.Text = $"SELECT * FROM [{table.Name}] WHERE [{firstColumn}] = 'value'"; 
            };
            menu.Append(whereItem);

            menu.Append(new SeparatorMenuItem());

            // Advanced
            var exportItem = new MenuItem("Export Table Schema");
            exportItem.Activated += (s, e) => { _queryTextView.Buffer.Text = $"SELECT sql FROM sqlite_master WHERE type='table' AND name='{table.Name}'"; };
            menu.Append(exportItem);

            menu.ShowAll();
            menu.PopupAtPointer(evt);
        }

        private void ExecuteQuery()
        {
            var query = _queryTextView.Buffer.Text.Trim();
            if (string.IsNullOrEmpty(query))
                return;

            try
            {
                _queryHistoryService.AddQuery(query);
                var dataTable = _databaseService.ExecuteQuery(query);
                DisplayResults(dataTable);
                _statusLabel.Text = $"Query executed successfully. {dataTable.Rows.Count} rows returned.";
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Query Error", $"Error executing query: {ex.Message}");
                _statusLabel.Text = "Query failed.";
            }
        }

        private void DisplayResults(DataTable dataTable)
        {
            // Store current results table
            _currentResultsTable = dataTable;
            
            // Remove old TreeView
            if (_resultsTreeView != null)
            {
                _resultsScrolledWindow.Remove(_resultsTreeView);
                _resultsTreeView.Destroy();
            }

            // Reset DocumentId column index
            _documentIdColumnIndex = -1;

            // Create column types array
            var columnTypes = new Type[dataTable.Columns.Count];
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                columnTypes[i] = typeof(string);
            }

            // Create new ListStore with appropriate columns
            _resultsListStore = new ListStore(columnTypes);
            _resultsTreeView = new TreeView(_resultsListStore);
            
            // Enable grid lines
            _resultsTreeView.EnableGridLines = TreeViewGridLines.Both;
            
            // Enable multi-selection
            _resultsTreeView.Selection.Mode = SelectionMode.Multiple;

            // Add columns to TreeView
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                var renderer = new CellRendererText();
                renderer.Ellipsize = Pango.EllipsizeMode.End;
                
                var column = new TreeViewColumn(dataTable.Columns[i].ColumnName, renderer, "text", i);
                column.Resizable = true;
                column.Reorderable = true;
                
                // Check for DocumentId or Id column
                var columnName = dataTable.Columns[i].ColumnName;
                if (columnName.Equals("DocumentId", StringComparison.OrdinalIgnoreCase) ||
                    columnName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                {
                    _documentIdColumnIndex = i;
                }
                
                // Set column width based on column name
                if (dataTable.Columns[i].ColumnName == "RawText" || 
                    dataTable.Columns[i].ColumnName == "LayoutText")
                {
                    column.MinWidth = 300;
                    column.MaxWidth = 500;
                }
                else
                {
                    column.MinWidth = 50;
                }
                
                _resultsTreeView.AppendColumn(column);
            }

            // Add data rows
            foreach (DataRow row in dataTable.Rows)
            {
                var values = new string[dataTable.Columns.Count];
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    var value = row[i]?.ToString() ?? string.Empty;
                    // Truncate long text
                    if (value.Length > 100)
                    {
                        value = value.Substring(0, 100) + "...";
                    }
                    values[i] = value;
                }
                _resultsListStore.AppendValues(values);
            }

            // Add selection changed handler
            _resultsTreeView.Selection.Changed += OnResultsSelectionChanged;
            
            // Add button press handler for context menu
            _resultsTreeView.ButtonPressEvent += OnResultsTreeViewButtonPress;
            _resultsTreeView.ButtonReleaseEvent += OnResultsTreeViewButtonRelease;
            _resultsTreeView.PopupMenu += OnResultsTreeViewPopupMenu;
            
            _resultsScrolledWindow.Add(_resultsTreeView);
            _resultsTreeView.ShowAll();
        }

        private void OnIngestFiles(object? sender, EventArgs e)
        {
            using (var fileChooser = new FileChooserDialog(
                "Select PDF files to ingest",
                this,
                FileChooserAction.Open,
                "Cancel", ResponseType.Cancel,
                "Open", ResponseType.Accept))
            {
                fileChooser.SelectMultiple = true;
                
                var filter = new FileFilter();
                filter.Name = "PDF files";
                filter.AddPattern("*.pdf");
                fileChooser.AddFilter(filter);

                if (fileChooser.Run() == (int)ResponseType.Accept)
                {
                    var files = fileChooser.Filenames;
                    fileChooser.Hide();
                    
                    var ingestedCount = 0;
                    var errors = new List<string>();

                    foreach (var file in files)
                    {
                        try
                        {
                            _documentService.IngestDocument(file);
                            ingestedCount++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"{System.IO.Path.GetFileName(file)}: {ex.Message}");
                        }
                    }

                    var message = $"Ingested {ingestedCount} documents.";
                    if (errors.Any())
                    {
                        message += $"\n\nErrors:\n{string.Join("\n", errors)}";
                    }

                    ShowInfoDialog("Ingestion Complete", message);
                    RefreshDatabaseExplorer(preserveState: true);
                }
            }
        }

        private void OnIngestFolder(object? sender, EventArgs e)
        {
            using (var folderChooser = new FileChooserDialog(
                "Select folder to ingest PDF files from",
                this,
                FileChooserAction.SelectFolder,
                "Cancel", ResponseType.Cancel,
                "Select", ResponseType.Accept))
            {
                if (folderChooser.Run() == (int)ResponseType.Accept)
                {
                    var folderPath = folderChooser.Filename;
                    folderChooser.Hide();
                    
                    var pdfFiles = Directory.GetFiles(folderPath, "*.pdf", SearchOption.AllDirectories);
                    
                    var ingestedCount = 0;
                    var errors = new List<string>();

                    foreach (var file in pdfFiles)
                    {
                        try
                        {
                            _documentService.IngestDocument(file);
                            ingestedCount++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"{System.IO.Path.GetFileName(file)}: {ex.Message}");
                        }
                    }

                    var message = $"Ingested {ingestedCount} documents from {pdfFiles.Length} PDF files found.";
                    if (errors.Any())
                    {
                        message += $"\n\nErrors:\n{string.Join("\n", errors.Take(10))}";
                        if (errors.Count > 10)
                        {
                            message += $"\n... and {errors.Count - 10} more errors";
                        }
                    }

                    ShowInfoDialog("Ingestion Complete", message);
                    RefreshDatabaseExplorer(preserveState: true);
                }
            }
        }

        private void OnReprocessAll(object? sender, EventArgs e)
        {
            try
            {
                var count = _reprocessingService.ReprocessAllDocuments();
                ShowInfoDialog("Reprocessing Complete", $"Processed {count} documents.");
                RefreshDatabaseExplorer(preserveState: true);
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error", $"Error during reprocessing: {ex.Message}");
            }
        }

        private void OnCreateTemplate(object? sender, EventArgs e)
        {
            var templateWindow = new TemplateCreationWindow(_databaseService);
            templateWindow.TransientFor = this;
            templateWindow.Modal = true;
            templateWindow.ShowAll();
        }

        private void OnManageTemplates(object? sender, EventArgs e)
        {
            var manageWindow = new TemplateManagementWindow(_databaseService);
            manageWindow.TransientFor = this;
            manageWindow.Modal = true;
            manageWindow.ShowAll();
        }

        private void OnViewModeChanged(object? sender, EventArgs e)
        {
            var selectedMode = _viewModeCombo.ActiveText;
            
            switch (selectedMode)
            {
                case "Query View":
                    _isTextViewMode = false;
                    // Switch back to query view
                    if (_queryScrolledWindow.Child == _documentTextView)
                    {
                        _queryScrolledWindow.Remove(_documentTextView);
                        _queryScrolledWindow.Add(_queryTextView);
                        _queryScrolledWindow.ShowAll();
                    }
                    break;
                    
                case "Text View":
                case "Layout View":
                    _isTextViewMode = true;
                    // Switch to document view
                    if (_queryScrolledWindow.Child == _queryTextView)
                    {
                        _queryScrolledWindow.Remove(_queryTextView);
                        _queryScrolledWindow.Add(_documentTextView);
                        _queryScrolledWindow.ShowAll();
                    }
                    // Load document text
                    LoadSelectedDocumentText();
                    break;
            }
        }

        private void OnResultsSelectionChanged(object? sender, EventArgs e)
        {
            if (_isTextViewMode)
            {
                LoadSelectedDocumentText();
            }
        }

        private void LoadSelectedDocumentText()
        {
            // Clear the text view first
            _documentTextView.Buffer.Text = "";
            
            // Get selected row(s) - handle multi-selection mode
            var paths = _resultsTreeView.Selection.GetSelectedRows();
            if (paths.Length == 0)
            {
                _documentTextView.Buffer.Text = "No row selected.";
                return;
            }
            
            TreeIter iter;
            if (!_resultsListStore!.GetIter(out iter, paths[0]))
            {
                _documentTextView.Buffer.Text = "No row selected.";
                return;
            }
            
            Document? document = null;
            
            // Try to get document by DocumentId column
            if (_documentIdColumnIndex >= 0)
            {
                var documentIdStr = _resultsListStore?.GetValue(iter, _documentIdColumnIndex)?.ToString();
                if (!string.IsNullOrEmpty(documentIdStr) && int.TryParse(documentIdStr, out int documentId))
                {
                    document = _databaseService.GetDocumentById(documentId);
                }
            }
            
            // If no DocumentId column or no document found, try Id column
            if (document == null && _currentResultsTable != null)
            {
                for (int i = 0; i < _currentResultsTable.Columns.Count; i++)
                {
                    var columnName = _currentResultsTable.Columns[i].ColumnName;
                    if (columnName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    {
                        var idStr = _resultsListStore?.GetValue(iter, i)?.ToString();
                        if (!string.IsNullOrEmpty(idStr) && int.TryParse(idStr, out int id))
                        {
                            document = _databaseService.GetDocumentById(id);
                            break;
                        }
                    }
                }
            }
            
            if (document != null)
            {
                var selectedMode = _viewModeCombo.ActiveText;
                var documentText = selectedMode == "Layout View" ? document.LayoutText : document.RawText;
                
                _documentTextView.Buffer.Text = $"Document: {document.FileName}\n" +
                                               $"Path: {document.FilePath}\n" +
                                               $"Pages: {document.PageCount}\n" +
                                               $"Ingested: {document.IngestedDate}\n" +
                                               $"View Mode: {selectedMode}\n\n" +
                                               $"--- DOCUMENT {(selectedMode == "Layout View" ? "LAYOUT" : "TEXT")} ---\n\n" +
                                               documentText;
            }
            else
            {
                _documentTextView.Buffer.Text = "No document found for this row. Ensure the table has an 'Id' or 'DocumentId' column that references the Documents table.";
            }
        }

        private void OnResultsTreeViewButtonPress(object o, ButtonPressEventArgs args)
        {
            if (args.Event.Button == 3) // Right click
            {
                // Just prevent default handling on press
                args.RetVal = true;
            }
        }

        private void OnResultsTreeViewButtonRelease(object o, ButtonReleaseEventArgs args)
        {
            if (args.Event.Button == 3) // Right click
            {
                TreePath clickedPath;
                TreeViewColumn clickedColumn;
                
                // Get the path at the click position
                if (_resultsTreeView.GetPathAtPos((int)args.Event.X, (int)args.Event.Y, 
                    out clickedPath, out clickedColumn))
                {
                    // Check if the clicked row is already selected
                    if (!_resultsTreeView.Selection.PathIsSelected(clickedPath))
                    {
                        // If not selected, select it (single selection for right-click)
                        _resultsTreeView.Selection.UnselectAll();
                        _resultsTreeView.Selection.SelectPath(clickedPath);
                    }
                    // If already selected, keep current selection
                }
                
                ShowResultsContextMenu(args.Event);
                args.RetVal = true;
            }
        }

        private void OnResultsTreeViewPopupMenu(object o, PopupMenuArgs args)
        {
            ShowResultsContextMenu(null);
            args.RetVal = true;
        }

        private void ShowResultsContextMenu(Gdk.Event? evt)
        {
            var menu = new Menu();

            // Copy operations
            var copyItem = new MenuItem("Copy");
            copyItem.Activated += OnCopyResults;
            menu.Append(copyItem);

            var copyWithHeadersItem = new MenuItem("Copy with Headers");
            copyWithHeadersItem.Activated += OnCopyResultsWithHeaders;
            menu.Append(copyWithHeadersItem);

            menu.Append(new SeparatorMenuItem());

            // Export operations
            var exportCsvItem = new MenuItem("Export to CSV...");
            exportCsvItem.Activated += OnExportResultsToCsv;
            menu.Append(exportCsvItem);

            var saveDbItem = new MenuItem("Save as Database...");
            saveDbItem.Activated += OnSaveResultsAsDb;
            menu.Append(saveDbItem);

            var exportJsonItem = new MenuItem("Export to JSON...");
            exportJsonItem.Activated += OnExportResultsToJson;
            menu.Append(exportJsonItem);

            // Check if we have document-related columns
            if ((_documentIdColumnIndex >= 0 || HasIdColumn()) && _resultsTreeView.Selection.CountSelectedRows() > 0)
            {
                menu.Append(new SeparatorMenuItem());

                var openDocItem = new MenuItem("Open Document");
                openDocItem.Activated += OnOpenDocument;
                menu.Append(openDocItem);

                var openFolderItem = new MenuItem("Open Containing Folder");
                openFolderItem.Activated += OnOpenContainingFolder;
                menu.Append(openFolderItem);
            }

            menu.ShowAll();
            
            if (evt != null)
            {
                menu.PopupAtPointer(evt);
            }
            else
            {
                // Popup at current cursor position for keyboard shortcut
                menu.Popup();
            }
        }

        private bool HasIdColumn()
        {
            if (_currentResultsTable == null) return false;
            
            for (int i = 0; i < _currentResultsTable.Columns.Count; i++)
            {
                var columnName = _currentResultsTable.Columns[i].ColumnName;
                if (columnName.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                    columnName.Equals("DocumentId", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void OnCopyResults(object? sender, EventArgs e)
        {
            var clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
            var text = GetSelectedResultsAsText(false);
            clipboard.Text = text;
            _statusLabel.Text = "Results copied to clipboard.";
        }

        private void OnCopyResultsWithHeaders(object? sender, EventArgs e)
        {
            var clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
            var text = GetSelectedResultsAsText(true);
            clipboard.Text = text;
            _statusLabel.Text = "Results with headers copied to clipboard.";
        }

        private string GetSelectedResultsAsText(bool includeHeaders)
        {
            if (_currentResultsTable == null) return "";
            
            var sb = new System.Text.StringBuilder();
            
            // Get selected rows
            var paths = _resultsTreeView.Selection.GetSelectedRows();
            
            if (paths.Length == 0)
            {
                // If no selection, copy all
                if (includeHeaders)
                {
                    var headers = string.Join("\t", _currentResultsTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
                    sb.AppendLine(headers);
                }
                
                foreach (DataRow row in _currentResultsTable.Rows)
                {
                    var values = string.Join("\t", row.ItemArray.Select(v => v?.ToString() ?? ""));
                    sb.AppendLine(values);
                }
            }
            else
            {
                // Copy selected rows
                if (includeHeaders)
                {
                    var headers = string.Join("\t", _currentResultsTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
                    sb.AppendLine(headers);
                }
                
                foreach (var path in paths)
                {
                    TreeIter iter;
                    if (_resultsListStore!.GetIter(out iter, path))
                    {
                        var values = new List<string>();
                        for (int i = 0; i < _currentResultsTable.Columns.Count; i++)
                        {
                            values.Add(_resultsListStore.GetValue(iter, i)?.ToString() ?? "");
                        }
                        sb.AppendLine(string.Join("\t", values));
                    }
                }
            }
            
            return sb.ToString().TrimEnd();
        }

        private void OnExportResultsToCsv(object? sender, EventArgs e)
        {
            if (_currentResultsTable == null) return;
            
            using (var fileChooser = new FileChooserDialog(
                "Export to CSV",
                this,
                FileChooserAction.Save,
                "Cancel", ResponseType.Cancel,
                "Save", ResponseType.Accept))
            {
                fileChooser.CurrentName = "results.csv";
                var filter = new FileFilter();
                filter.Name = "CSV files";
                filter.AddPattern("*.csv");
                fileChooser.AddFilter(filter);
                
                if (fileChooser.Run() == (int)ResponseType.Accept)
                {
                    var filename = fileChooser.Filename;
                    fileChooser.Hide();
                    
                    try
                    {
                        using (var writer = new System.IO.StreamWriter(filename))
                        {
                            // Write headers
                            var headers = string.Join(",", _currentResultsTable.Columns.Cast<DataColumn>()
                                .Select(c => $"\"{c.ColumnName.Replace("\"", "\"\"")}\""));
                            writer.WriteLine(headers);
                            
                            // Write data
                            foreach (DataRow row in _currentResultsTable.Rows)
                            {
                                var values = string.Join(",", row.ItemArray
                                    .Select(v => $"\"{(v?.ToString() ?? "").Replace("\"", "\"\"")}\""));
                                writer.WriteLine(values);
                            }
                        }
                        
                        _statusLabel.Text = $"Exported to {System.IO.Path.GetFileName(filename)}";
                    }
                    catch (Exception ex)
                    {
                        ShowErrorDialog("Export Error", $"Failed to export CSV: {ex.Message}");
                    }
                }
            }
        }

        private void OnSaveResultsAsDb(object? sender, EventArgs e)
        {
            if (_currentResultsTable == null) return;
            
            using (var dialog = new Dialog("Save as Database", this, DialogFlags.Modal))
            {
                dialog.AddButton("Cancel", ResponseType.Cancel);
                dialog.AddButton("Save", ResponseType.Ok);
                
                var vbox = new Box(Orientation.Vertical, 5);
                vbox.BorderWidth = 10;
                
                vbox.PackStart(new Label("Table Name:"), false, false, 0);
                var tableNameEntry = new Entry("results_export");
                vbox.PackStart(tableNameEntry, false, false, 0);
                
                dialog.ContentArea.Add(vbox);
                dialog.ShowAll();
                
                if (dialog.Run() == (int)ResponseType.Ok)
                {
                    try
                    {
                        var tableName = tableNameEntry.Text;
                        if (!string.IsNullOrWhiteSpace(tableName))
                        {
                            _databaseService.CreateTableFromDataTable(_currentResultsTable, tableName);
                            RefreshDatabaseExplorer(preserveState: true);
                            _statusLabel.Text = $"Results saved to table '{tableName}'";
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowErrorDialog("Save Error", $"Failed to save as database: {ex.Message}");
                    }
                }
            }
        }

        private void OnExportResultsToJson(object? sender, EventArgs e)
        {
            if (_currentResultsTable == null) return;
            
            using (var fileChooser = new FileChooserDialog(
                "Export to JSON",
                this,
                FileChooserAction.Save,
                "Cancel", ResponseType.Cancel,
                "Save", ResponseType.Accept))
            {
                fileChooser.CurrentName = "results.json";
                var filter = new FileFilter();
                filter.Name = "JSON files";
                filter.AddPattern("*.json");
                fileChooser.AddFilter(filter);
                
                if (fileChooser.Run() == (int)ResponseType.Accept)
                {
                    var filename = fileChooser.Filename;
                    fileChooser.Hide();
                    
                    try
                    {
                        var json = DataTableToJson(_currentResultsTable);
                        System.IO.File.WriteAllText(filename, json);
                        _statusLabel.Text = $"Exported to {System.IO.Path.GetFileName(filename)}";
                    }
                    catch (Exception ex)
                    {
                        ShowErrorDialog("Export Error", $"Failed to export JSON: {ex.Message}");
                    }
                }
            }
        }

        private string DataTableToJson(DataTable table)
        {
            var rows = new List<Dictionary<string, object?>>();
            
            foreach (DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, object?>();
                foreach (DataColumn col in table.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                rows.Add(dict);
            }
            
            return System.Text.Json.JsonSerializer.Serialize(rows, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        private void OnOpenDocument(object? sender, EventArgs e)
        {
            // Get first selected row for multi-selection mode
            var paths = _resultsTreeView.Selection.GetSelectedRows();
            if (paths.Length == 0) return;
            
            TreeIter iter;
            if (!_resultsListStore!.GetIter(out iter, paths[0])) return;
            
            Document? document = null;
            
            // Try to get document by DocumentId
            if (_documentIdColumnIndex >= 0)
            {
                var documentIdStr = _resultsListStore?.GetValue(iter, _documentIdColumnIndex)?.ToString();
                if (!string.IsNullOrEmpty(documentIdStr) && int.TryParse(documentIdStr, out int documentId))
                {
                    document = _databaseService.GetDocumentById(documentId);
                }
            }
            
            // Try to get by Id column
            if (document == null && _currentResultsTable != null)
            {
                for (int i = 0; i < _currentResultsTable.Columns.Count; i++)
                {
                    var columnName = _currentResultsTable.Columns[i].ColumnName;
                    if (columnName.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                        columnName.Equals("DocumentId", StringComparison.OrdinalIgnoreCase))
                    {
                        var idStr = _resultsListStore?.GetValue(iter, i)?.ToString();
                        if (!string.IsNullOrEmpty(idStr) && int.TryParse(idStr, out int id))
                        {
                            document = _databaseService.GetDocumentById(id);
                            break;
                        }
                    }
                }
            }
            
            if (document != null && System.IO.File.Exists(document.FilePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = document.FilePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Open Error", $"Failed to open document: {ex.Message}");
                }
            }
            else
            {
                ShowErrorDialog("Open Error", "Document file not found or invalid document reference.");
            }
        }

        private void OnOpenContainingFolder(object? sender, EventArgs e)
        {
            // Get first selected row for multi-selection mode
            var paths = _resultsTreeView.Selection.GetSelectedRows();
            if (paths.Length == 0) return;
            
            TreeIter iter;
            if (!_resultsListStore!.GetIter(out iter, paths[0])) return;
            
            Document? document = null;
            
            // Try to get document (same logic as OnOpenDocument)
            if (_documentIdColumnIndex >= 0)
            {
                var documentIdStr = _resultsListStore?.GetValue(iter, _documentIdColumnIndex)?.ToString();
                if (!string.IsNullOrEmpty(documentIdStr) && int.TryParse(documentIdStr, out int documentId))
                {
                    document = _databaseService.GetDocumentById(documentId);
                }
            }
            
            // Try by Id column if DocumentId failed
            if (document == null && _currentResultsTable != null)
            {
                for (int i = 0; i < _currentResultsTable.Columns.Count; i++)
                {
                    var columnName = _currentResultsTable.Columns[i].ColumnName;
                    if (columnName.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                        columnName.Equals("DocumentId", StringComparison.OrdinalIgnoreCase))
                    {
                        var idStr = _resultsListStore?.GetValue(iter, i)?.ToString();
                        if (!string.IsNullOrEmpty(idStr) && int.TryParse(idStr, out int id))
                        {
                            document = _databaseService.GetDocumentById(id);
                            break;
                        }
                    }
                }
            }
            
            if (document != null && System.IO.File.Exists(document.FilePath))
            {
                try
                {
                    var directory = System.IO.Path.GetDirectoryName(document.FilePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = directory,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Open Error", $"Failed to open folder: {ex.Message}");
                }
            }
        }

        private Dictionary<string, bool> SaveTreeViewExpandedState()
        {
            var expandedStates = new Dictionary<string, bool>();
            
            if (_databaseTreeStore.GetIterFirst(out TreeIter iter))
            {
                do
                {
                    var tableName = _databaseTreeStore.GetValue(iter, 0) as string;
                    if (tableName != null)
                    {
                        var path = _databaseTreeStore.GetPath(iter);
                        expandedStates[tableName] = _databaseTreeView.GetRowExpanded(path);
                    }
                } while (_databaseTreeStore.IterNext(ref iter));
            }
            
            return expandedStates;
        }
        
        private void RestoreTreeViewExpandedState(Dictionary<string, bool> expandedStates)
        {
            if (_databaseTreeStore.GetIterFirst(out TreeIter iter))
            {
                do
                {
                    var tableName = _databaseTreeStore.GetValue(iter, 0) as string;
                    if (tableName != null && expandedStates.ContainsKey(tableName))
                    {
                        var path = _databaseTreeStore.GetPath(iter);
                        if (expandedStates[tableName])
                        {
                            _databaseTreeView.ExpandRow(path, false);
                        }
                    }
                } while (_databaseTreeStore.IterNext(ref iter));
            }
        }

        private void ShowInfoDialog(string title, string message)
        {
            using (var dialog = new MessageDialog(this, 
                DialogFlags.DestroyWithParent,
                MessageType.Info, 
                ButtonsType.Ok, 
                message))
            {
                dialog.Title = title;
                dialog.Run();
            }
        }

        private void ShowErrorDialog(string title, string message)
        {
            using (var dialog = new MessageDialog(this, 
                DialogFlags.DestroyWithParent,
                MessageType.Error, 
                ButtonsType.Ok, 
                message))
            {
                dialog.Title = title;
                dialog.Run();
            }
        }

        private void OnDeleteEvent(object o, DeleteEventArgs args)
        {
            Application.Quit();
            args.RetVal = true;
        }
    }
}