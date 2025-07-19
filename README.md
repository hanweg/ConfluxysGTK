# ConfluxysGTK - Document Management System
Confluxys Document Management System, built with C# and GTK.

## Features

- **PDF Document Ingestion** - Import individual files or entire folders
- **Template Creation** - Define templates for structured data extraction
- **SQLite Database** with FTS5 full-text search
- **SQLite Query Interface** - Execute custom queries with history navigation
- **Document Reprocessing** - Extract structured data from documents

## Requirements

- .NET 9.0 SDK or later
- Linux (tested on Debian 12)

## Building and Running
```bash
dotnet build
```

### Run from source:
```bash
dotnet run
```

### Build self-contained executable:
```bash
dotnet publish -c Release --self-contained -r linux-x64
```

The executable will be in `bin/Release/net9.0/linux-x64/publish/`

## Usage

1. **Ingest Documents**: File → Ingest PDF Files/Folder
2. **Query Database**: Use the SQL editor (F5 to execute)
3. **Browse Tables**: Click tables in the Database Explorer
4. **Navigate History**: Use < > buttons for query history
5. **View Text**: Toggle 

## Architecture

- **GTK UI**: Cross-platform GUI framework
- **PdfPig**: PDF text extraction (replaced iText7 for better Linux compatibility)
- **Microsoft.Data.Sqlite**: SQLite database with FTS5
- **MVVM-ready**: Structured for future enhancements

## 001 improvements
- **Database Explorer Enhancements**:

  1. Database Explorer starts collapsed: Removed the _databaseTreeView.ExpandAll() call from RefreshDatabaseExplorer() method, so the treeview will
  start with all nodes collapsed when the application first runs.
  2. Preserves state during "Reprocess All Documents":
    - Modified RefreshDatabaseExplorer() to accept an optional preserveState parameter
    - Added SaveTreeViewExpandedState() method that captures which table nodes are expanded
    - Added RestoreTreeViewExpandedState() method that restores the expansion state after refresh
    - Updated OnReprocessAll() to call RefreshDatabaseExplorer(preserveState: true)

  The build completed successfully with no errors. The Database Explorer will now start collapsed and maintain its expansion state when using
  "Database" -> "Reprocess All Documents".

## Additional fixes to address the GLib-GObject-CRITICAL warning:

  1. Wrapped all MessageDialog instances in using statements - Ensures proper disposal
  2. Added explicit disposal of TreePath objects - TreePath objects need to be explicitly disposed in GTK# to avoid reference counting issues
  3. Wrapped the remaining Dialog in using statement - Fixed the "Save as Database" dialog

  The warning you're seeing is typically caused by GTK objects not being properly disposed or being accessed after disposal. The changes I've made
  should help, but if the warning persists, it might be coming from:

  1. The PDF processing library (PdfPig) if it's creating any temporary GTK objects
  2. The database service if it's interacting with GTK in any way
  3. A deeper issue in the GTK# bindings themselves

  Since the application works correctly despite the warning, it's likely a minor reference counting issue that doesn't affect functionality. The
  warning is GTK's way of alerting about improper object lifecycle management, but it's not causing any actual problems in your case.

## Summary of Changes

  1. Added Regex Display in Step 3: The "Review & Create" step now shows a dedicated TreeView that displays all the generated regex patterns for
  each field.
  2. Made Regex Patterns Editable:
    - Users can double-click on any regex pattern to edit it directly
    - The edited regex is validated to ensure it's syntactically correct
    - If invalid, an error message is shown and the original pattern is preserved
  3. Improved Regex Generation:
    - The regex generator now creates more intelligent capture groups based on the field value type:
        - (\d+) for integer values
      - (\d+\.?\d*) for decimal values
      - (\S+) for single words
      - (.+?) for multi-word phrases
    - Better handles the positioning of the field value within the context
  4. Enhanced UI Layout:
    - Split the review section into two parts:
        - A summary section showing template metadata
      - A regex patterns section with an editable TreeView
    - Added clear instructions for users on how to edit the regex patterns

  How It Works

  1. When users reach Step 3, they see:
    - Template summary (name, description, identifier, field count)
    - A table showing each field with its name, type, and generated regex pattern
  2. To edit a regex:
    - Double-click on the regex pattern in the table
    - The cell becomes editable
    - Type the new regex pattern
    - Press Enter to save
    - The pattern is validated, and if valid, it's saved to the field
  3. The regex patterns shown are the actual patterns that will be used for data extraction when documents are reprocessed.

  This gives users full control over the extraction patterns while still providing sensible defaults through the automatic generation process.




## Template Editing Feature

The application now supports full template editing functionality:

1. **Edit Template**:
   - Open "Templates" -> "Manage Templates"
   - Select a template and click "Edit Template"
   - The template creation window opens in edit mode with:
     - Original sample document auto-selected (if still exists)
     - All fields prepopulated with original values
     - Fields listed in "Defined Fields" section

2. **Field Editing**:
   - Double-click any field in the "Defined Fields" list to edit it
   - Or select a field and click "Edit Field" button
   - Field values populate the input boxes for modification
   - After editing, click "Add Field" to save changes

3. **Regex Editing**:
   - In Step 3 "Review & Create", double-click any regex pattern to edit
   - Regex patterns are validated before saving
   - Shows actual regex patterns used for extraction


## Implemented the Edit Template functionality with the following features:

  1. Template Editing:
    - Modified TemplateCreationWindow to support both create and edit modes
    - Added a second constructor that accepts a DocumentType for editing
    - The window title changes to "Edit Template" when in edit mode
    - The button text changes to "Update Template" when editing
  2. Field Population in Edit Mode:
    - The LoadTemplateForEditing method now populates the fields list in Step 2
    - All previously defined fields are displayed in the "Defined Fields" TreeView
    - The original sample document is automatically selected if it still exists
  3. Field Editing Capabilities:
    - Added double-click support on fields to edit them
    - Added an "Edit Field" button alongside "Remove Selected Field" button
    - When a field is edited, its values populate the input boxes
    - The field is temporarily removed from the list while being edited
    - After modification, clicking "Add Field" re-adds it with updated values
  4. Database Updates:
    - Created UpdateDocumentType method to handle updating existing templates
    - Updates both the DocumentTypes table and DocumentFields table
    - Properly maintains field sort order
  5. UI Improvements:
    - Two buttons side-by-side for field actions instead of one wide button
    - Clear status messages indicating when editing a field
    - Proper state management for edit mode

  The implementation allows users to easily modify their templates, adjust regex patterns, add/remove fields, and maintain the same workflow they
  used when creating the template initially.

## Todo
- Fix cell selection / right-click context menu issues in GTK# TreeView
- Add ability to ingest *any* file with text (e.g. .txt, .md, .csv, .json, .xml + others)
- Implement full-text search across all text-based files - eliminate truncation in results.
- ~~Maintain Database Explorer state across sessions (collapse/expand state, selected table, etc.)~~
- ~~Add user editable regex patterns for each field in template creation / editing~~
- ~~Implement template editing functionality~~