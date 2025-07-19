using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Gtk;
using Confluxys.Models;
using Confluxys.Services;

namespace Confluxys
{
    public class TemplateCreationWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private List<Document> _documents = new();
        private Document? _selectedDocument;
        private DocumentType _documentType = new();
        private List<DocumentField> _fields = new();
        private readonly bool _isEditMode;
        private readonly int? _editingTemplateId;

        // UI Components
        private TreeView _documentListTreeView = null!;
        private ListStore _documentListStore = null!;
        private TextView _documentTextView = null!;
        private TextBuffer _documentTextBuffer = null!;
        private Notebook _notebook = null!;
        private Label _statusLabel = null!;
        
        // Step 1 - Document Type
        private Entry _typeNameEntry = null!;
        private Entry _typeDescriptionEntry = null!;
        private TextView _identifierTextView = null!;
        private Button _selectIdentifierButton = null!;
        private bool _isSelectingIdentifier = false;
        
        // Step 2 - Fields
        private Entry _fieldNameEntry = null!;
        private ComboBoxText _fieldTypeCombo = null!;
        private TextView _contextTextView = null!;
        private TextView _fieldValueTextView = null!;
        private Button _selectContextButton = null!;
        private Button _selectFieldValueButton = null!;
        private Button _addFieldButton = null!;
        private TreeView _fieldsTreeView = null!;
        private ListStore _fieldsListStore = null!;
        private bool _isSelectingFieldContext = false;
        private bool _isSelectingFieldValue = false;
        
        // Step 3 - Review
        private TextView _reviewTextView = null!;
        private TreeView _regexTreeView = null!;
        private ListStore _regexListStore = null!;
        private Button _createTemplateButton = null!;

        // Text selection tracking
        private TextIter _selectionStart;
        private TextIter _selectionEnd;
        private string _selectedText = string.Empty;

        // Constructor for creating new template
        public TemplateCreationWindow(DatabaseService databaseService) : base("Create Template")
        {
            _databaseService = databaseService;
            _isEditMode = false;
            
            SetDefaultSize(1000, 700);
            SetPosition(WindowPosition.Center);
            DeleteEvent += (o, args) => { args.RetVal = true; Destroy(); };

            CreateUI();
            LoadDocuments();
        }
        
        // Constructor for editing existing template
        public TemplateCreationWindow(DatabaseService databaseService, DocumentType documentType) : base("Edit Template")
        {
            _databaseService = databaseService;
            _isEditMode = true;
            _editingTemplateId = documentType.Id;
            _documentType = documentType;
            
            SetDefaultSize(1000, 700);
            SetPosition(WindowPosition.Center);
            DeleteEvent += (o, args) => { args.RetVal = true; Destroy(); };

            CreateUI();
            LoadDocuments();
            LoadTemplateForEditing();
        }

        private void CreateUI()
        {
            var vbox = new Box(Orientation.Vertical, 5);
            vbox.BorderWidth = 10;

            // Main content - HPaned with document list and wizard
            var hpaned = new Paned(Orientation.Horizontal);
            hpaned.Position = 300;

            // Left panel - Document list
            var leftPanel = CreateDocumentListPanel();
            hpaned.Pack1(leftPanel, false, false);

            // Right panel - Template creation wizard
            var rightPanel = CreateWizardPanel();
            hpaned.Pack2(rightPanel, true, false);

            vbox.PackStart(hpaned, true, true, 0);

            // Status bar
            _statusLabel = new Label("Select a document to begin template creation");
            _statusLabel.Halign = Align.Start;
            vbox.PackStart(_statusLabel, false, false, 0);

            Add(vbox);
        }

        private Widget CreateDocumentListPanel()
        {
            var frame = new Frame("Documents");
            var scrolledWindow = new ScrolledWindow();
            scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

            _documentListStore = new ListStore(typeof(string), typeof(string), typeof(Document));
            _documentListTreeView = new TreeView(_documentListStore);

            var fileNameColumn = new TreeViewColumn("File Name", new CellRendererText(), "text", 0);
            var pageCountColumn = new TreeViewColumn("Pages", new CellRendererText(), "text", 1);
            
            _documentListTreeView.AppendColumn(fileNameColumn);
            _documentListTreeView.AppendColumn(pageCountColumn);
            
            _documentListTreeView.Selection.Changed += OnDocumentSelectionChanged;

            scrolledWindow.Add(_documentListTreeView);
            frame.Add(scrolledWindow);

            return frame;
        }

        private Widget CreateWizardPanel()
        {
            var vbox = new Box(Orientation.Vertical, 5);

            // Document text viewer
            var textFrame = new Frame("Document Content");
            var textScrolledWindow = new ScrolledWindow();
            textScrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            
            _documentTextView = new TextView();
            _documentTextView.Editable = false;
            _documentTextView.WrapMode = WrapMode.Word;
            _documentTextBuffer = _documentTextView.Buffer;
            
            // Handle text selection
            _documentTextView.ButtonReleaseEvent += OnDocumentTextViewButtonRelease;
            
            textScrolledWindow.Add(_documentTextView);
            textFrame.Add(textScrolledWindow);
            
            // Set preferred size for text viewer
            textScrolledWindow.SetSizeRequest(-1, 300);
            
            vbox.PackStart(textFrame, true, true, 0);

            // Wizard notebook
            _notebook = new Notebook();
            _notebook.ShowTabs = true;
            
            // Step 1 - Document Type
            var step1 = CreateStep1();
            _notebook.AppendPage(step1, new Label("1. Document Type"));
            
            // Step 2 - Fields
            var step2 = CreateStep2();
            _notebook.AppendPage(step2, new Label("2. Define Fields"));
            
            // Step 3 - Review
            var step3 = CreateStep3();
            _notebook.AppendPage(step3, new Label("3. Review & Create"));
            
            vbox.PackStart(_notebook, false, false, 0);

            return vbox;
        }

        private Widget CreateStep1()
        {
            var vbox = new Box(Orientation.Vertical, 5);
            vbox.BorderWidth = 10;

            // Type name
            var nameBox = new Box(Orientation.Horizontal, 5);
            nameBox.PackStart(new Label("Template Name:"), false, false, 0);
            _typeNameEntry = new Entry();
            nameBox.PackStart(_typeNameEntry, true, true, 0);
            vbox.PackStart(nameBox, false, false, 0);

            // Type description
            var descBox = new Box(Orientation.Horizontal, 5);
            descBox.PackStart(new Label("Description:"), false, false, 0);
            _typeDescriptionEntry = new Entry();
            descBox.PackStart(_typeDescriptionEntry, true, true, 0);
            vbox.PackStart(descBox, false, false, 0);

            // Identifier text selection
            vbox.PackStart(new Label("Select text that uniquely identifies this document type:"), false, false, 5);
            
            _selectIdentifierButton = new Button("Select Identifier Text");
            _selectIdentifierButton.Clicked += OnSelectIdentifierClicked;
            vbox.PackStart(_selectIdentifierButton, false, false, 0);

            var identifierFrame = new Frame("Selected Identifier Text");
            var identifierScrolled = new ScrolledWindow();
            identifierScrolled.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            identifierScrolled.SetSizeRequest(-1, 60);
            
            _identifierTextView = new TextView();
            _identifierTextView.Editable = false;
            _identifierTextView.WrapMode = WrapMode.Word;
            
            identifierScrolled.Add(_identifierTextView);
            identifierFrame.Add(identifierScrolled);
            vbox.PackStart(identifierFrame, false, false, 0);

            return vbox;
        }

        private Widget CreateStep2()
        {
            var vbox = new Box(Orientation.Vertical, 5);
            vbox.BorderWidth = 10;

            // Instructions
            var instructionLabel = new Label("Define multiple fields by repeating the selection process for each field:");
            instructionLabel.Halign = Align.Start;
            vbox.PackStart(instructionLabel, false, false, 5);
            
            // Field definition
            var fieldBox = new Box(Orientation.Horizontal, 5);
            fieldBox.PackStart(new Label("Field Name:"), false, false, 0);
            _fieldNameEntry = new Entry();
            fieldBox.PackStart(_fieldNameEntry, true, true, 0);
            
            fieldBox.PackStart(new Label("Type:"), false, false, 5);
            _fieldTypeCombo = new ComboBoxText();
            _fieldTypeCombo.AppendText("TEXT");
            _fieldTypeCombo.AppendText("INTEGER");
            _fieldTypeCombo.AppendText("REAL");
            _fieldTypeCombo.AppendText("DATE");
            _fieldTypeCombo.Active = 0;
            fieldBox.PackStart(_fieldTypeCombo, false, false, 0);
            
            vbox.PackStart(fieldBox, false, false, 0);

            // Context selection
            vbox.PackStart(new Label("Step 1: Select context text containing the field:"), false, false, 5);
            _selectContextButton = new Button("Select Context Text");
            _selectContextButton.Clicked += OnSelectContextClicked;
            vbox.PackStart(_selectContextButton, false, false, 0);

            var contextFrame = new Frame("Context Text");
            var contextScrolled = new ScrolledWindow();
            contextScrolled.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            contextScrolled.SetSizeRequest(-1, 50);
            
            _contextTextView = new TextView();
            _contextTextView.Editable = false;
            _contextTextView.WrapMode = WrapMode.Word;
            
            contextScrolled.Add(_contextTextView);
            contextFrame.Add(contextScrolled);
            vbox.PackStart(contextFrame, false, false, 0);

            // Field value selection
            vbox.PackStart(new Label("Step 2: Select the field value within the context:"), false, false, 5);
            _selectFieldValueButton = new Button("Select Field Value");
            _selectFieldValueButton.Clicked += OnSelectFieldValueClicked;
            _selectFieldValueButton.Sensitive = false;
            vbox.PackStart(_selectFieldValueButton, false, false, 0);

            var fieldValueFrame = new Frame("Field Value");
            var fieldValueScrolled = new ScrolledWindow();
            fieldValueScrolled.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            fieldValueScrolled.SetSizeRequest(-1, 30);
            
            _fieldValueTextView = new TextView();
            _fieldValueTextView.Editable = false;
            
            fieldValueScrolled.Add(_fieldValueTextView);
            fieldValueFrame.Add(fieldValueScrolled);
            vbox.PackStart(fieldValueFrame, false, false, 0);

            // Add field button
            _addFieldButton = new Button("Add Field");
            _addFieldButton.Clicked += OnAddFieldClicked;
            _addFieldButton.Sensitive = false;
            vbox.PackStart(_addFieldButton, false, false, 5);

            // Fields list
            var fieldsFrame = new Frame("Defined Fields");
            var fieldsScrolled = new ScrolledWindow();
            fieldsScrolled.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            fieldsScrolled.SetSizeRequest(-1, 100);
            
            _fieldsListStore = new ListStore(typeof(string), typeof(string), typeof(DocumentField));
            _fieldsTreeView = new TreeView(_fieldsListStore);
            
            _fieldsTreeView.AppendColumn("Field Name", new CellRendererText(), "text", 0);
            _fieldsTreeView.AppendColumn("Type", new CellRendererText(), "text", 1);
            
            // Add double-click handler to edit fields
            _fieldsTreeView.RowActivated += OnFieldRowActivated;
            
            fieldsScrolled.Add(_fieldsTreeView);
            fieldsFrame.Add(fieldsScrolled);
            vbox.PackStart(fieldsFrame, true, true, 0);

            // Field action buttons
            var buttonBox = new Box(Orientation.Horizontal, 5);
            buttonBox.Halign = Align.Center;
            
            var editFieldButton = new Button("Edit Field");
            editFieldButton.Clicked += OnEditFieldClicked;
            buttonBox.PackStart(editFieldButton, false, false, 0);
            
            var removeFieldButton = new Button("Remove Selected Field");
            removeFieldButton.Clicked += OnRemoveFieldClicked;
            buttonBox.PackStart(removeFieldButton, false, false, 0);
            
            vbox.PackStart(buttonBox, false, false, 0);

            return vbox;
        }

        private Widget CreateStep3()
        {
            var vbox = new Box(Orientation.Vertical, 5);
            vbox.BorderWidth = 10;

            vbox.PackStart(new Label("Review your template definition:"), false, false, 0);

            // Summary section
            var summaryFrame = new Frame("Template Summary");
            var summaryScrolled = new ScrolledWindow();
            summaryScrolled.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            summaryScrolled.SetSizeRequest(-1, 120);
            
            _reviewTextView = new TextView();
            _reviewTextView.Editable = false;
            _reviewTextView.WrapMode = WrapMode.Word;
            
            summaryScrolled.Add(_reviewTextView);
            summaryFrame.Add(summaryScrolled);
            vbox.PackStart(summaryFrame, false, false, 0);

            // Regex patterns section
            vbox.PackStart(new Label("Field Extraction Patterns (double-click to edit):"), false, false, 10);
            
            var regexFrame = new Frame("Regular Expressions");
            var regexScrolled = new ScrolledWindow();
            regexScrolled.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            regexScrolled.SetSizeRequest(-1, 200);
            
            // Create TreeView with editable regex column
            _regexListStore = new ListStore(typeof(string), typeof(string), typeof(string), typeof(DocumentField));
            _regexTreeView = new TreeView(_regexListStore);
            
            _regexTreeView.AppendColumn("Field Name", new CellRendererText(), "text", 0);
            _regexTreeView.AppendColumn("Type", new CellRendererText(), "text", 1);
            
            // Editable regex column
            var regexRenderer = new CellRendererText();
            regexRenderer.Editable = true;
            regexRenderer.Edited += OnRegexEdited;
            var regexColumn = new TreeViewColumn("Regex Pattern", regexRenderer, "text", 2);
            regexColumn.Expand = true;
            _regexTreeView.AppendColumn(regexColumn);
            
            // Enable row activation for editing
            _regexTreeView.RowActivated += (o, args) => {
                TreeIter iter;
                if (_regexListStore.GetIter(out iter, args.Path))
                {
                    _regexTreeView.SetCursor(args.Path, regexColumn, true);
                }
            };
            
            regexScrolled.Add(_regexTreeView);
            regexFrame.Add(regexScrolled);
            vbox.PackStart(regexFrame, true, true, 0);

            // Instructions
            var instructionLabel = new Label("Double-click a regex pattern to edit it. The pattern should capture the field value in the first capture group.");
            instructionLabel.Wrap = true;
            instructionLabel.Halign = Align.Start;
            vbox.PackStart(instructionLabel, false, false, 5);

            _createTemplateButton = new Button(_isEditMode ? "Update Template" : "Create Template");
            _createTemplateButton.Clicked += OnCreateTemplateClicked;
            vbox.PackStart(_createTemplateButton, false, false, 0);

            // Update review when switching to this tab
            _notebook.SwitchPage += (o, args) => {
                if (args.PageNum == 2) UpdateReview();
            };

            return vbox;
        }

        private void LoadDocuments()
        {
            _documents = _databaseService.GetDocuments();
            
            foreach (var doc in _documents)
            {
                _documentListStore.AppendValues(doc.FileName, doc.PageCount.ToString(), doc);
            }
        }

        private void OnDocumentSelectionChanged(object? sender, EventArgs e)
        {
            TreeIter iter;
            if (_documentListTreeView.Selection.GetSelected(out iter))
            {
                _selectedDocument = _documentListStore.GetValue(iter, 2) as Document;
                if (_selectedDocument != null)
                {
                    _documentTextBuffer.Text = _selectedDocument.RawText;
                    _statusLabel.Text = $"Selected: {_selectedDocument.FileName}";
                }
            }
        }

        private void OnDocumentTextViewButtonRelease(object o, ButtonReleaseEventArgs args)
        {
            if (_documentTextBuffer.GetSelectionBounds(out _selectionStart, out _selectionEnd))
            {
                _selectedText = _documentTextBuffer.GetText(_selectionStart, _selectionEnd, false);
                
                if (_isSelectingIdentifier)
                {
                    _identifierTextView.Buffer.Text = _selectedText;
                    _selectIdentifierButton.Label = "Select Identifier Text";
                    _isSelectingIdentifier = false;
                    _statusLabel.Text = "Identifier text selected";
                }
                else if (_isSelectingFieldContext)
                {
                    _contextTextView.Buffer.Text = _selectedText;
                    _selectContextButton.Label = "Select Context Text";
                    _isSelectingFieldContext = false;
                    _selectFieldValueButton.Sensitive = true;
                    _statusLabel.Text = "Context text selected. Now select the field value within the context.";
                }
                else if (_isSelectingFieldValue)
                {
                    // Verify that the field value is within the context
                    var contextText = _contextTextView.Buffer.Text;
                    if (contextText.Contains(_selectedText))
                    {
                        _fieldValueTextView.Buffer.Text = _selectedText;
                        _selectFieldValueButton.Label = "Select Field Value";
                        _isSelectingFieldValue = false;
                        _addFieldButton.Sensitive = true;
                        _statusLabel.Text = "Field value selected. You can now add the field.";
                    }
                    else
                    {
                        ShowErrorDialog("Invalid Selection", "The field value must be within the selected context text.");
                    }
                }
            }
        }

        private void OnSelectIdentifierClicked(object? sender, EventArgs e)
        {
            _isSelectingIdentifier = true;
            _selectIdentifierButton.Label = "Click on document text...";
            _statusLabel.Text = "Select text in the document that identifies this document type";
        }

        private void OnSelectContextClicked(object? sender, EventArgs e)
        {
            _isSelectingFieldContext = true;
            _selectContextButton.Label = "Click on document text...";
            _statusLabel.Text = "Select a larger text area that contains the field value";
        }

        private void OnSelectFieldValueClicked(object? sender, EventArgs e)
        {
            _isSelectingFieldValue = true;
            _selectFieldValueButton.Label = "Click on document text...";
            _statusLabel.Text = "Select the specific field value within the context";
        }

        private void OnAddFieldClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_fieldNameEntry.Text))
            {
                ShowErrorDialog("Validation Error", "Please enter a field name.");
                return;
            }

            var contextText = _contextTextView.Buffer.Text;
            var fieldValue = _fieldValueTextView.Buffer.Text;
            
            // Generate regex pattern
            var regexPattern = GenerateRegexPattern(contextText, fieldValue);
            
            var field = new DocumentField
            {
                FieldName = _fieldNameEntry.Text,
                ColumnName = GenerateColumnName(_fieldNameEntry.Text),
                ContextText = contextText,
                FieldText = fieldValue,
                RegexPattern = regexPattern,
                DataType = _fieldTypeCombo.ActiveText ?? "TEXT",
                SortOrder = _fields.Count + 1
            };
            
            _fields.Add(field);
            _fieldsListStore.AppendValues(field.FieldName, field.DataType, field);
            
            // Clear field inputs
            _fieldNameEntry.Text = "";
            _contextTextView.Buffer.Text = "";
            _fieldValueTextView.Buffer.Text = "";
            _selectFieldValueButton.Sensitive = false;
            _addFieldButton.Sensitive = false;
            
            _statusLabel.Text = $"Field '{field.FieldName}' added successfully";
        }

        private void OnRemoveFieldClicked(object? sender, EventArgs e)
        {
            TreeIter iter;
            if (_fieldsTreeView.Selection.GetSelected(out iter))
            {
                var field = _fieldsListStore.GetValue(iter, 2) as DocumentField;
                if (field != null)
                {
                    _fields.Remove(field);
                    _fieldsListStore.Remove(ref iter);
                    
                    // Update sort order
                    for (int i = 0; i < _fields.Count; i++)
                    {
                        _fields[i].SortOrder = i + 1;
                    }
                }
            }
        }
        
        private void OnEditFieldClicked(object? sender, EventArgs e)
        {
            TreeIter iter;
            if (_fieldsTreeView.Selection.GetSelected(out iter))
            {
                var field = _fieldsListStore.GetValue(iter, 2) as DocumentField;
                if (field != null)
                {
                    PopulateFieldForEditing(field);
                }
            }
        }
        
        private void OnFieldRowActivated(object o, RowActivatedArgs args)
        {
            TreeIter iter;
            if (_fieldsListStore.GetIter(out iter, args.Path))
            {
                var field = _fieldsListStore.GetValue(iter, 2) as DocumentField;
                if (field != null)
                {
                    PopulateFieldForEditing(field);
                }
            }
        }
        
        private void PopulateFieldForEditing(DocumentField field)
        {
            // Populate the field inputs with the selected field's values
            _fieldNameEntry.Text = field.FieldName;
            _fieldTypeCombo.Active = field.DataType switch
            {
                "TEXT" => 0,
                "INTEGER" => 1,
                "REAL" => 2,
                "DATE" => 3,
                _ => 0
            };
            _contextTextView.Buffer.Text = field.ContextText;
            _fieldValueTextView.Buffer.Text = field.FieldText;
            
            // Enable the buttons since we have context and value
            _selectFieldValueButton.Sensitive = true;
            _addFieldButton.Sensitive = true;
            
            // Remove the field from the list (it will be re-added when saved)
            _fields.Remove(field);
            
            // Update the list store
            _fieldsListStore.Clear();
            foreach (var f in _fields)
            {
                _fieldsListStore.AppendValues(f.FieldName, f.DataType, f);
            }
            
            _statusLabel.Text = $"Editing field: {field.FieldName}";
        }

        private void UpdateReview()
        {
            // Update summary text
            var review = $"Template Name: {_typeNameEntry.Text}\n";
            review += $"Description: {_typeDescriptionEntry.Text}\n";
            review += $"Identifier Text: {_identifierTextView.Buffer.Text}\n";
            review += $"Number of Fields: {_fields.Count}\n";
            
            _reviewTextView.Buffer.Text = review;
            
            // Update regex patterns list
            _regexListStore.Clear();
            foreach (var field in _fields)
            {
                _regexListStore.AppendValues(
                    field.FieldName,
                    field.DataType,
                    field.RegexPattern,
                    field
                );
            }
        }
        
        private void OnRegexEdited(object o, EditedArgs args)
        {
            TreeIter iter;
            if (_regexListStore.GetIterFromString(out iter, args.Path))
            {
                var field = _regexListStore.GetValue(iter, 3) as DocumentField;
                if (field != null)
                {
                    // Validate the regex pattern
                    try
                    {
                        var regex = new Regex(args.NewText);
                        
                        // Update the field's regex pattern
                        field.RegexPattern = args.NewText;
                        
                        // Update the list store
                        _regexListStore.SetValue(iter, 2, args.NewText);
                        
                        _statusLabel.Text = $"Regex pattern updated for field '{field.FieldName}'";
                    }
                    catch (ArgumentException ex)
                    {
                        ShowErrorDialog("Invalid Regex", $"The regex pattern is invalid: {ex.Message}");
                        // Keep the old value
                        _regexListStore.SetValue(iter, 2, field.RegexPattern);
                    }
                }
            }
        }

        private void LoadTemplateForEditing()
        {
            // Load template details
            _typeNameEntry.Text = _documentType.Name;
            _typeDescriptionEntry.Text = _documentType.Description;
            _identifierTextView.Buffer.Text = _documentType.IdentifierText;
            
            // Load fields
            _fields = _databaseService.GetDocumentFields(_documentType.Id);
            
            // Populate the fields list in the UI
            _fieldsListStore.Clear();
            foreach (var field in _fields)
            {
                _fieldsListStore.AppendValues(field.FieldName, field.DataType, field);
            }
            
            // Try to find and select the original sample document
            // First, check if any document contains the identifier text
            foreach (var doc in _documents)
            {
                if (doc.RawText.Contains(_documentType.IdentifierText))
                {
                    // Select this document in the TreeView
                    TreeIter iter;
                    if (_documentListStore.GetIterFirst(out iter))
                    {
                        do
                        {
                            var iterDoc = _documentListStore.GetValue(iter, 2) as Document;
                            if (iterDoc != null && iterDoc.Id == doc.Id)
                            {
                                _documentListTreeView.Selection.SelectIter(iter);
                                break;
                            }
                        } while (_documentListStore.IterNext(ref iter));
                    }
                    break;
                }
            }
            
            // Update the status
            _statusLabel.Text = $"Editing template: {_documentType.Name}";
        }

        private void OnCreateTemplateClicked(object? sender, EventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(_typeNameEntry.Text))
            {
                ShowErrorDialog("Validation Error", "Please enter a template name.");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(_identifierTextView.Buffer.Text))
            {
                ShowErrorDialog("Validation Error", "Please select identifier text.");
                return;
            }
            
            if (_fields.Count == 0)
            {
                ShowErrorDialog("Validation Error", "Please define at least one field.");
                return;
            }
            
            try
            {
                if (_isEditMode)
                {
                    // Update existing template
                    _documentType.Name = _typeNameEntry.Text;
                    _documentType.Description = _typeDescriptionEntry.Text;
                    _documentType.IdentifierText = _identifierTextView.Buffer.Text;
                    
                    // Update in database
                    UpdateDocumentType(_documentType, _fields);
                    
                    ShowInfoDialog("Success", $"Template '{_documentType.Name}' updated successfully!");
                }
                else
                {
                    // Create new template
                    _documentType.Name = _typeNameEntry.Text;
                    _documentType.Description = _typeDescriptionEntry.Text;
                    _documentType.IdentifierText = _identifierTextView.Buffer.Text;
                    _documentType.TableName = GenerateTableName(_typeNameEntry.Text);
                    _documentType.CreatedDate = DateTime.Now;
                    
                    // Save to database
                    var documentTypeId = _databaseService.CreateDocumentType(_documentType, _fields);
                    
                    ShowInfoDialog("Success", $"Template '{_documentType.Name}' created successfully!");
                }
                
                // Close window
                Destroy();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error", $"Failed to {(_isEditMode ? "update" : "create")} template: {ex.Message}");
            }
        }

        private string GenerateRegexPattern(string contextText, string fieldValue)
        {
            // Escape special regex characters
            var escapedContext = Regex.Escape(contextText);
            var escapedValue = Regex.Escape(fieldValue);
            
            // Find the position of the field value in the context
            var valueIndex = contextText.IndexOf(fieldValue);
            if (valueIndex == -1)
            {
                // Fallback: just replace the value with a capture group
                return escapedContext.Replace(escapedValue, @"(.+?)");
            }
            
            // Get the text before and after the field value
            var beforeValue = contextText.Substring(0, valueIndex);
            var afterValue = contextText.Substring(valueIndex + fieldValue.Length);
            
            // Escape the parts
            var escapedBefore = Regex.Escape(beforeValue);
            var escapedAfter = Regex.Escape(afterValue);
            
            // Create a more flexible capture group based on the field content
            string captureGroup;
            if (Regex.IsMatch(fieldValue, @"^\d+$"))
            {
                // Numeric field
                captureGroup = @"(\d+)";
            }
            else if (Regex.IsMatch(fieldValue, @"^\d+\.\d+$"))
            {
                // Decimal field
                captureGroup = @"(\d+\.?\d*)";
            }
            else if (fieldValue.Contains(" "))
            {
                // Multi-word field - capture until the next part of the pattern
                captureGroup = @"(.+?)";
            }
            else
            {
                // Single word - use non-whitespace capture
                captureGroup = @"(\S+)";
            }
            
            // Build the final pattern
            var pattern = escapedBefore + captureGroup + escapedAfter;
            
            return pattern;
        }

        private string GenerateColumnName(string fieldName)
        {
            // Convert field name to valid column name
            return Regex.Replace(fieldName, @"[^a-zA-Z0-9_]", "_").ToLower();
        }

        private string GenerateTableName(string typeName)
        {
            // Convert type name to valid table name
            return "doc_" + Regex.Replace(typeName, @"[^a-zA-Z0-9_]", "_").ToLower();
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
        
        private void UpdateDocumentType(DocumentType docType, List<DocumentField> fields)
        {
            // Update document type - using parameterized query to prevent SQL injection
            var updateQuery = $@"
                UPDATE DocumentTypes 
                SET Name = '{docType.Name.Replace("'", "''")}', 
                    Description = '{docType.Description.Replace("'", "''")}', 
                    IdentifierText = '{docType.IdentifierText.Replace("'", "''")}' 
                WHERE Id = {docType.Id}";
            
            _databaseService.ExecuteNonQuery(updateQuery);
            
            // Delete existing fields
            _databaseService.ExecuteQuery($"DELETE FROM DocumentFields WHERE DocumentTypeId = {docType.Id}");
            
            // Insert updated fields
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                field.SortOrder = i + 1; // Ensure correct sort order
                
                var insertQuery = $@"
                    INSERT INTO DocumentFields (DocumentTypeId, FieldName, ColumnName, ContextText, FieldText, RegexPattern, DataType, SortOrder)
                    VALUES ({docType.Id}, 
                            '{field.FieldName.Replace("'", "''")}', 
                            '{field.ColumnName.Replace("'", "''")}', 
                            '{field.ContextText.Replace("'", "''")}', 
                            '{field.FieldText.Replace("'", "''")}', 
                            '{field.RegexPattern.Replace("'", "''")}', 
                            '{field.DataType}', 
                            {field.SortOrder})";
                
                _databaseService.ExecuteNonQuery(insertQuery);
            }
        }
    }
}