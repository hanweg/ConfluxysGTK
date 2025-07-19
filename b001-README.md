# ConfluxysGTK - Document Management System
A Linux port of the Confluxys Document Management System, built with C# and GTK UI for cross-platform compatibility.

## Features

- **PDF Document Ingestion** - Import individual files or entire folders
- **SQLite Database** with FTS5 full-text search
- **SQL Query Interface** - Execute custom queries with history navigation
- **Document Reprocessing** - Extract structured data from documents
- **Dark Theme UI** - Modern, clean interface

## Requirements

- .NET 9.0 SDK or later
- Linux (tested on Debian 12)

## Building and Running

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

## Architecture

- **GTK UI**: Cross-platform GUI framework
- **PdfPig**: PDF text extraction (replaced iText7 for better Linux compatibility)
- **Microsoft.Data.Sqlite**: SQLite database with FTS5
- **MVVM-ready**: Structured for future enhancements

## Key Differences from Windows Version

- Uses GTK instead of WPF for Linux compatibility
- PdfPig library for PDF processing (fully open-source)
- Simplified UI while maintaining core functionality
- Self-contained deployment option for easy distribution

## SQL Query Results Display

The SQL query results are displayed in a custom table format using Avalonia's Grid control. This implementation replaces the previous DataGrid approach, which had issues with rendering on Linux. The new table format includes:

1. Creates a dynamic Grid control instead of using DataGrid
2. Properly renders headers and data cells with styling
3. Handles hover effects for better user interaction
4. Truncates long text to maintain table readability
5. Alternates row colors for better visual distinction

The key changes were:
- Replaced the problematic DataGrid with a custom Grid-based table
- Used proper Avalonia brush syntax (`new SolidColorBrush(Color.Parse("#color"))`)
- Created cells dynamically based on the DataTable structure
- Added proper styling and hover effects

When you execute queries like `SELECT * FROM [Documents] LIMIT 100`, the results will display properly in a table format with:
- Column headers
- Scrollable content
- Hover highlighting
- Alternating row colors
- Text truncation for long values

## Recommended Plan: Switch to GTK# for Linux Version

Given the requirement for a "working, stable and simple results grid" that will be heavily used throughout the application, switching to GTK# (GtkSharp) for the Linux version is recommended.

### Phase 1: Set up GTK# Development Environment
1. Install GTK# development packages
2. Create new GTK# project structure
3. Set up basic window with TreeView for results display

### Phase 2: Port Core Functionality
1. Copy over the existing Services (DatabaseService, DocumentService, etc.)
2. Create GTK# main window with:
    - Menu bar
    - TreeView for database explorer (left panel)
    - TextView for SQL editor (top right)
    - TreeView for results grid (bottom right)
3. Implement query execution with proper TreeView data binding

### Phase 3: Implement Advanced Features
1. Create template creation dialog using GTK# widgets
2. Port document reprocessing functionality
3. Add all missing features from C# version

### Benefits of This Approach
- **Stable TreeView**: GTK's TreeView is mature and well-tested on Linux
- **Native Performance**: GTK is the native toolkit for many Linux desktops
- **Rich Features**: TreeView supports sorting, filtering, custom renderers
- **Better Documentation**: Extensive examples and documentation available
- **Future-Proof**: Can handle complex table interactions needed for templates

### Alternative: Stick with Avalonia but Use Different Approach
If you prefer to stay with Avalonia:
1. Install the official Avalonia.Controls.DataGrid package
2. Follow proper setup with theme includes
3. Use MVVM pattern properly
4. Consider using a third-party grid like NP.Ava.Visuals

## Implemented the template functionality in the GTK# version of Confluxys. Here's a summary of what was implemented:

  Implementation Summary

  1. Templates Menu - Added a "Templates" dropdown menu to the main window with:
    - "Create Template..." option
    - "Manage Templates..." option
  2. Template Creation Window - A 3-step wizard that implements the selection mechanism:
    - Step 1: Document Type - Select identifier text that uniquely identifies the document type
    - Step 2: Field Definition - Uses a 2-pass selection process:
        - First pass: Select context text containing the field
      - Second pass: Select the specific field value within the context
    - Step 3: Review and Create - Shows a summary before creating the template
  3. Template Management Window - Allows users to:
    - View all existing templates
    - See template details including fields and regex patterns
    - Delete templates when no longer needed
    - View count of documents processed with each template
  4. Database Integration:
    - Added CreateDocumentType method to save templates
    - Added GetDocuments method to load documents for template creation
    - Templates are stored in DocumentTypes and DocumentFields tables
    - Dynamic tables are created for each document type
  5. Key Features:
    - Interactive text selection in document viewer
    - Automatic regex pattern generation based on context/value selection
    - Field type support (TEXT, INTEGER, REAL, DATE)
    - Transaction-based template creation for data integrity
    - Full integration with existing document reprocessing functionality

  The implementation preserves the 3-pass selection concept from the original C# version while adapting it to GTK#'s UI components. Users can now
  create templates through the UI and use the existing "Reprocess All Documents" feature to extract structured data from their PDF documents.

  Summary of Fixes

  1. Database Locking Issue - Fixed by:
    - Updated connection string to use Mode=ReadWriteCreate;Cache=Shared
    - Added WAL (Write-Ahead Logging) mode for better concurrent access
    - Fixed the CreateDocumentTypeTable method to use the same connection/transaction instead of creating a new one
  2. Multiple Fields Support - Already implemented but enhanced by:
    - Added clear instructions in the UI: "Define multiple fields by repeating the selection process for each field"
    - The UI already had the functionality to:
        - Add multiple fields using the "Add Field" button
      - View all defined fields in a list
      - Remove selected fields
      - Each field gets its own column in the dynamically created table

  The template creation process now supports:
  - Defining a document type with identifier text
  - Adding multiple fields, each with:
    - Custom field name
    - Data type (TEXT, INTEGER, REAL, DATE)
    - Context-based extraction using 2-pass selection
    - Automatic regex pattern generation
  - Creating a custom table with all defined field columns
  - Proper database transaction handling to avoid locking issues

  The SQLite database now uses WAL mode which provides better performance and allows multiple readers while writing, preventing the "database is
  locked" errors.

## Query/Text/Layout View Implementation

  1. View Mode Dropdown:
    - Replaced toggle button with a ComboBox dropdown offering three view modes
    - View modes: "Query View", "Text View", and "Layout View"
    - Positioned on the left side of the toolbar before history navigation
  2. View Modes:
    - Query View (default): Shows the SQL query editor for writing and executing queries
    - Text View: Shows the raw text extraction from the selected document
    - Layout View: Shows the layout-preserved text extraction from the selected document
  3. Document Loading:
    - Automatically detects if results contain "DocumentId" or "Id" columns (case-insensitive)
    - When in Text/Layout View modes and a row is selected, loads the corresponding document
    - Displays document metadata (filename, path, pages, ingestion date, view mode)
    - Shows either RawText or LayoutText based on the selected view mode
  4. User Experience:
    - Seamless switching between all three view modes via dropdown
    - Document view updates automatically when selecting different rows
    - Clear indication of which view mode is active
    - Helpful messages when required columns are missing or no row is selected
    - Read-only text view with word wrapping for comfortable reading
  5. Usage:
    - Execute a query that returns a DocumentId or Id column (e.g., SELECT * FROM Documents LIMIT 100)
    - Select "Text View" from dropdown to see raw text extraction
    - Select "Layout View" from dropdown to see layout-preserved text
    - Select "Query View" to return to SQL query editing

  This feature is particularly useful for:
  - Comparing raw text vs layout-preserved text extraction
  - Reviewing document content with preserved formatting
  - Verifying template extraction results in both text modes
  - Understanding how PDF text extraction handles complex layouts

  Note: Template creation and regex matching continue to use RawText for consistency and reliability, as layout preservation can vary between different PDF processors and settings.


  Summary of Implemented Features

  1. Grid Lines in Results Grid ✓

  - Added EnableGridLines = TreeViewGridLines.Both to show both horizontal and vertical grid lines

  2. Dynamic Default Query ✓

  - Query editor now shows SELECT * FROM [TableName] LIMIT 100 based on the currently selected table
  - Tracks _selectedTableName and updates when a table is clicked

  3. Database Explorer Context Menu ✓

  Right-click on any table to access:
  - Common Queries: SELECT , SELECT with LIMIT, COUNT()
  - Table Structure: Show table info, Show indexes
  - Data Operations: SELECT DISTINCT, GROUP BY with COUNT, WHERE clause template
  - Advanced: Export table schema

  4. Results Grid Context Menu ✓

  Right-click on results to access:
  - Copy Operations:
    - Copy (selected rows or all)
    - Copy with Headers
    - Multi-selection support enabled
  - Export Options:
    - Export to CSV (with proper escaping)
    - Save as Database (creates new table)
    - Export to JSON (formatted output)
  - Document Operations (when DocumentId/Id column present):
    - Open Document (opens PDF in default viewer)
    - Open Containing Folder

  5. Additional Features Implemented ✓

  - Multi-row selection: SelectionMode.Multiple for Excel-like selection
  - Smart column detection: Automatically finds DocumentId or Id columns
  - Proper CSV formatting: Handles quotes and special characters
  - JSON export: Pretty-printed JSON output
  - Database table creation: Save query results as permanent tables
  - Status updates: User feedback in status bar for all operations

  6. Future-Ready Architecture

  The implementation is designed to easily support:
  - Column selection (infrastructure in place)
  - Cell-level selection (can be added to TreeView)
  - Additional export formats
  - Custom query templates
  - Batch operations on selected rows

  All features integrate seamlessly with the existing application, maintaining the clean GTK# interface while significantly enhancing productivity
  for database management tasks.

## Not really true:   Global Context Menu Fix Summary

  Solution Implemented:

  1. Split Button Handling:
    - ButtonPressEvent: Simply blocks default handling with args.RetVal = true
    - ButtonReleaseEvent: Shows the context menu on button release
  2. Why This Works:
    - GTK's default behavior waits for button release to confirm a click
    - By blocking the press event and handling the release, we prevent GTK's interference
    - Context menus typically appear on button release in most desktop environments
  3. Applied to Both:
    - Database Explorer: Right-click on tables now shows menu immediately
    - Results Grid: Right-click on results now shows menu immediately

  Benefits:

  - Single right-click now shows context menus immediately
  - No selection cycling on right-click
  - Preserves multi-selection when right-clicking selected rows
  - Consistent behavior across the entire application

  This is a common pattern in GTK applications where the default button handling can interfere with custom context menus. By separating press and
  release events, we get the expected desktop application behavior.

  ## Todo
  - Fix cell selection / right-click context menu issues in GTK# TreeView
  - Add ability to ingest *any* file with text (e.g. .txt, .md, .csv, .json, .xml + others)
  - Implement full-text search across all text-based files - eliminate truncation in results.