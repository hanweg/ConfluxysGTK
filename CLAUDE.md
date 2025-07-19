# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ConfluxysGTK is a Linux document management system built with C# .NET 9.0 and GTK#. It specializes in PDF ingestion and structured data extraction through a template system.

## Essential Commands

### Build and Run
```bash
# Build the project
dotnet build

# Run from source
dotnet run

# Create standalone Linux executable
dotnet publish -c Release --self-contained -r linux-x64

# Run tests (if implemented)
dotnet test
```

### Development
```bash
# Restore dependencies
dotnet restore

# Clean build artifacts
dotnet clean

# Generate test PDFs for development
cd sample_pdfs && python generate_test_pdfs.py
```

## Architecture Overview

### Service Layer Pattern
The application follows a service-based architecture with clear separation:
- **Services/** - Business logic (DatabaseService, DocumentService, etc.)
- **Models/** - Data models (Document.cs)
- **Windows/** - GTK# UI windows (MainWindow, TemplateCreationWindow, etc.)

### Key Services
1. **DatabaseService.cs**: SQLite operations with FTS5 full-text search
2. **DocumentService.cs**: PDF ingestion and text extraction using PdfPig
3. **DocumentReprocessingService.cs**: Template-based regex extraction
4. **QueryHistoryService.cs**: SQL query history management

### Template System
The template system (TemplateCreationWindow.cs) uses a 3-step wizard:
1. Select document type (filter by regex)
2. Select fields from document preview
3. Review and create template with generated regex patterns

Templates create dynamic SQLite tables for structured data extraction.

### Database Schema
- **documents**: Main document storage with FTS5
- **query_history**: SQL query tracking
- **document_templates**: Template definitions
- **document_template_fields**: Field extraction patterns
- Dynamic tables created per template (e.g., `template_Recipe_data`)

## GTK# Specifics

This is a port from Windows WPF. Key GTK# considerations:
- TreeView for data grids (with known selection issues)
- TextView for text display
- Notebook widget for tabbed views
- Dark theme via CSS in MainWindow constructor

## Known Issues (from README.md)

1. Cell selection/right-click context menu issues in GTK# TreeView
2. Need to add ingestion for any text file format (.txt, .md, .csv, .json, .xml)
3. Full-text search results are truncated
4. Template creation Step 3 should show actual regex
5. Need user-editable regex in template creation/editing
6. Database Explorer state not persisted across sessions

## Testing

Use the sample PDF generator to create test documents:
```bash
python sample_pdfs/generate_test_pdfs.py
```

This creates 30 test PDFs (recipes and poems) with consistent type identifiers for template testing.

## File Patterns

When searching:
- UI code: `Windows/*.cs`, `MainWindow.cs`
- Business logic: `Services/*.cs`
- Database operations: Look for `DatabaseService.cs` methods
- PDF processing: `DocumentService.cs`, uses PdfPig library
- Template logic: `TemplateCreationWindow.cs`, `DocumentReprocessingService.cs`