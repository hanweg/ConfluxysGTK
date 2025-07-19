using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using Confluxys.Models;
using Confluxys.Services;

namespace Confluxys
{
    public class TemplateManagementWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private List<DocumentType> _documentTypes = new();
        
        // UI Components
        private TreeView _templatesTreeView = null!;
        private ListStore _templatesListStore = null!;
        private TextView _detailsTextView = null!;
        private Button _deleteButton = null!;
        private Button _editButton = null!;
        private Label _statusLabel = null!;

        public TemplateManagementWindow(DatabaseService databaseService) : base("Manage Templates")
        {
            _databaseService = databaseService;
            
            SetDefaultSize(800, 600);
            SetPosition(WindowPosition.Center);
            DeleteEvent += (o, args) => { args.RetVal = true; Destroy(); };

            CreateUI();
            LoadTemplates();
        }

        private void CreateUI()
        {
            var vbox = new Box(Orientation.Vertical, 5);
            vbox.BorderWidth = 10;

            // Main content - HPaned
            var hpaned = new Paned(Orientation.Horizontal);
            hpaned.Position = 400;

            // Left panel - Template list
            var leftPanel = CreateTemplateListPanel();
            hpaned.Pack1(leftPanel, false, false);

            // Right panel - Template details
            var rightPanel = CreateDetailsPanel();
            hpaned.Pack2(rightPanel, true, false);

            vbox.PackStart(hpaned, true, true, 0);

            // Button bar
            var buttonBox = new Box(Orientation.Horizontal, 5);
            buttonBox.Halign = Align.End;

            _editButton = new Button("Edit Template");
            _editButton.Clicked += OnEditClicked;
            _editButton.Sensitive = false;
            buttonBox.PackStart(_editButton, false, false, 0);

            _deleteButton = new Button("Delete Template");
            _deleteButton.Clicked += OnDeleteClicked;
            _deleteButton.Sensitive = false;
            buttonBox.PackStart(_deleteButton, false, false, 0);

            var closeButton = new Button("Close");
            closeButton.Clicked += (s, e) => Destroy();
            buttonBox.PackStart(closeButton, false, false, 0);

            vbox.PackStart(buttonBox, false, false, 0);

            // Status bar
            _statusLabel = new Label("Select a template to view details");
            _statusLabel.Halign = Align.Start;
            vbox.PackStart(_statusLabel, false, false, 0);

            Add(vbox);
        }

        private Widget CreateTemplateListPanel()
        {
            var frame = new Frame("Templates");
            var scrolledWindow = new ScrolledWindow();
            scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

            _templatesListStore = new ListStore(typeof(string), typeof(string), typeof(string), typeof(DocumentType));
            _templatesTreeView = new TreeView(_templatesListStore);

            _templatesTreeView.AppendColumn("Name", new CellRendererText(), "text", 0);
            _templatesTreeView.AppendColumn("Table", new CellRendererText(), "text", 1);
            _templatesTreeView.AppendColumn("Created", new CellRendererText(), "text", 2);

            _templatesTreeView.Selection.Changed += OnTemplateSelectionChanged;

            scrolledWindow.Add(_templatesTreeView);
            frame.Add(scrolledWindow);

            return frame;
        }

        private Widget CreateDetailsPanel()
        {
            var frame = new Frame("Template Details");
            var scrolledWindow = new ScrolledWindow();
            scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

            _detailsTextView = new TextView();
            _detailsTextView.Editable = false;
            _detailsTextView.WrapMode = WrapMode.Word;

            scrolledWindow.Add(_detailsTextView);
            frame.Add(scrolledWindow);

            return frame;
        }

        private void LoadTemplates()
        {
            _templatesListStore.Clear();
            _documentTypes = _databaseService.GetDocumentTypes();

            foreach (var docType in _documentTypes)
            {
                _templatesListStore.AppendValues(
                    docType.Name, 
                    docType.TableName, 
                    docType.CreatedDate.ToString("yyyy-MM-dd"),
                    docType);
            }

            _statusLabel.Text = $"Loaded {_documentTypes.Count} templates";
        }

        private void OnTemplateSelectionChanged(object? sender, EventArgs e)
        {
            TreeIter iter;
            if (_templatesTreeView.Selection.GetSelected(out iter))
            {
                var docType = _templatesListStore.GetValue(iter, 3) as DocumentType;
                if (docType != null)
                {
                    DisplayTemplateDetails(docType);
                    _editButton.Sensitive = true;
                    _deleteButton.Sensitive = true;
                }
            }
            else
            {
                _detailsTextView.Buffer.Text = "";
                _editButton.Sensitive = false;
                _deleteButton.Sensitive = false;
            }
        }

        private void DisplayTemplateDetails(DocumentType docType)
        {
            var fields = _databaseService.GetDocumentFields(docType.Id);
            
            var details = $"Template: {docType.Name}\n";
            details += $"Description: {docType.Description}\n";
            details += $"Table Name: {docType.TableName}\n";
            details += $"Created: {docType.CreatedDate}\n\n";
            
            details += $"Identifier Text:\n{docType.IdentifierText}\n\n";
            
            details += $"Fields ({fields.Count}):\n";
            foreach (var field in fields)
            {
                details += $"\n{field.SortOrder}. {field.FieldName} ({field.DataType})\n";
                details += $"   Column: {field.ColumnName}\n";
                details += $"   Context: {field.ContextText.Substring(0, Math.Min(50, field.ContextText.Length))}...\n";
                details += $"   Value: {field.FieldText}\n";
                details += $"   Regex: {field.RegexPattern}\n";
            }

            // Show document count
            var documentCount = GetDocumentCountForType(docType.TableName);
            details += $"\n\nExtracted Documents: {documentCount}";

            _detailsTextView.Buffer.Text = details;
        }

        private int GetDocumentCountForType(string tableName)
        {
            try
            {
                var query = $"SELECT COUNT(*) FROM [{tableName}]";
                var result = _databaseService.ExecuteQuery(query);
                if (result.Rows.Count > 0)
                {
                    return Convert.ToInt32(result.Rows[0][0]);
                }
            }
            catch
            {
                // Table might not exist yet
            }
            return 0;
        }

        private void OnEditClicked(object? sender, EventArgs e)
        {
            TreeIter iter;
            if (_templatesTreeView.Selection.GetSelected(out iter))
            {
                var docType = _templatesListStore.GetValue(iter, 3) as DocumentType;
                if (docType != null)
                {
                    // Open template creation window in edit mode
                    var editWindow = new TemplateCreationWindow(_databaseService, docType);
                    editWindow.TransientFor = this;
                    editWindow.Modal = true;
                    editWindow.ShowAll();
                    
                    // Refresh templates when window is closed
                    editWindow.Destroyed += (s, args) => LoadTemplates();
                }
            }
        }

        private void OnDeleteClicked(object? sender, EventArgs e)
        {
            TreeIter iter;
            if (_templatesTreeView.Selection.GetSelected(out iter))
            {
                var docType = _templatesListStore.GetValue(iter, 3) as DocumentType;
                if (docType != null)
                {
                    var dialog = new MessageDialog(this,
                        DialogFlags.DestroyWithParent,
                        MessageType.Question,
                        ButtonsType.YesNo,
                        $"Are you sure you want to delete the template '{docType.Name}'?\n\nThis will also delete all extracted data in table '{docType.TableName}'.");
                    
                    dialog.Title = "Confirm Delete";
                    var response = dialog.Run();
                    dialog.Destroy();

                    if (response == (int)ResponseType.Yes)
                    {
                        try
                        {
                            DeleteTemplate(docType);
                            LoadTemplates();
                            _statusLabel.Text = $"Template '{docType.Name}' deleted successfully";
                        }
                        catch (Exception ex)
                        {
                            ShowErrorDialog("Delete Error", $"Failed to delete template: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void DeleteTemplate(DocumentType docType)
        {
            // Delete fields first
            _databaseService.ExecuteQuery($"DELETE FROM DocumentFields WHERE DocumentTypeId = {docType.Id}");
            
            // Delete document type
            _databaseService.ExecuteQuery($"DELETE FROM DocumentTypes WHERE Id = {docType.Id}");
            
            // Drop the associated table
            try
            {
                _databaseService.ExecuteQuery($"DROP TABLE IF EXISTS [{docType.TableName}]");
            }
            catch
            {
                // Table might not exist
            }
        }

        private void ShowInfoDialog(string title, string message)
        {
            var dialog = new MessageDialog(this,
                DialogFlags.DestroyWithParent,
                MessageType.Info,
                ButtonsType.Ok,
                message);
            dialog.Title = title;
            dialog.Run();
            dialog.Destroy();
        }

        private void ShowErrorDialog(string title, string message)
        {
            var dialog = new MessageDialog(this,
                DialogFlags.DestroyWithParent,
                MessageType.Error,
                ButtonsType.Ok,
                message);
            dialog.Title = title;
            dialog.Run();
            dialog.Destroy();
        }
    }
}