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


## Todo
- Fix cell selection / right-click context menu issues in GTK# TreeView
- Add ability to ingest *any* file with text (e.g. .txt, .md, .csv, .json, .xml + others)
- Implement full-text search across all text-based files - eliminate truncation in results.