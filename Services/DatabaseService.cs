using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;
using Confluxys.Models;

namespace Confluxys.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "confluxys.db");
        _connectionString = $"Data Source={dbPath};Mode=ReadWriteCreate;Cache=Shared";
    }

    public void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Enable WAL mode for better concurrent access
        using (var walCommand = new SqliteCommand("PRAGMA journal_mode=WAL", connection))
        {
            walCommand.ExecuteNonQuery();
        }

        var createTablesScript = @"
            CREATE TABLE IF NOT EXISTS Documents (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FilePath TEXT NOT NULL UNIQUE,
                FileName TEXT NOT NULL,
                FileHash TEXT NOT NULL,
                FileSize INTEGER NOT NULL,
                CreatedDate TEXT NOT NULL,
                ModifiedDate TEXT NOT NULL,
                IngestedDate TEXT NOT NULL,
                RawText TEXT NOT NULL,
                LayoutText TEXT NOT NULL,
                PageCount INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS DocumentTypes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Description TEXT,
                IdentifierText TEXT NOT NULL,
                TableName TEXT NOT NULL UNIQUE,
                CreatedDate TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS DocumentFields (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DocumentTypeId INTEGER NOT NULL,
                FieldName TEXT NOT NULL,
                ColumnName TEXT NOT NULL,
                ContextText TEXT NOT NULL,
                FieldText TEXT NOT NULL,
                RegexPattern TEXT NOT NULL,
                DataType TEXT NOT NULL DEFAULT 'TEXT',
                SortOrder INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (DocumentTypeId) REFERENCES DocumentTypes (Id)
            );

            CREATE TABLE IF NOT EXISTS QueryHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Query TEXT NOT NULL,
                ExecutedDate TEXT NOT NULL
            );

            CREATE VIRTUAL TABLE IF NOT EXISTS DocumentsSearch USING fts5(
                FilePath, FileName, RawText, LayoutText, 
                content='Documents', content_rowid='Id'
            );

            CREATE TRIGGER IF NOT EXISTS documents_search_insert AFTER INSERT ON Documents BEGIN
                INSERT INTO DocumentsSearch(rowid, FilePath, FileName, RawText, LayoutText) 
                VALUES (new.Id, new.FilePath, new.FileName, new.RawText, new.LayoutText);
            END;

            CREATE TRIGGER IF NOT EXISTS documents_search_delete AFTER DELETE ON Documents BEGIN
                DELETE FROM DocumentsSearch WHERE rowid = old.Id;
            END;

            CREATE TRIGGER IF NOT EXISTS documents_search_update AFTER UPDATE ON Documents BEGIN
                DELETE FROM DocumentsSearch WHERE rowid = old.Id;
                INSERT INTO DocumentsSearch(rowid, FilePath, FileName, RawText, LayoutText) 
                VALUES (new.Id, new.FilePath, new.FileName, new.RawText, new.LayoutText);
            END;
        ";

        using var command = new SqliteCommand(createTablesScript, connection);
        command.ExecuteNonQuery();
    }

    public List<TableInfo> GetTables()
    {
        var tables = new List<TableInfo>();
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var query = @"
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '%Search'
            ORDER BY name";

        using var command = new SqliteCommand(query, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var tableName = reader.GetString(0);
            var tableInfo = new TableInfo { Name = tableName };
            
            var columnsQuery = $"PRAGMA table_info('{tableName}')";
            using var columnsCommand = new SqliteCommand(columnsQuery, connection);
            using var columnsReader = columnsCommand.ExecuteReader();
            
            while (columnsReader.Read())
            {
                tableInfo.Columns.Add(new ColumnInfo
                {
                    Name = columnsReader.GetString(1),
                    Type = columnsReader.GetString(2),
                    NotNull = columnsReader.GetInt32(3) == 1,
                    PrimaryKey = columnsReader.GetInt32(5) == 1
                });
            }
            
            tables.Add(tableInfo);
        }

        return tables;
    }

    public DataTable ExecuteQuery(string query)
    {
        var dataTable = new DataTable();
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        using var command = new SqliteCommand(query, connection);
        using var reader = command.ExecuteReader();
        
        for (int i = 0; i < reader.FieldCount; i++)
        {
            dataTable.Columns.Add(reader.GetName(i), typeof(string));
        }
        
        while (reader.Read())
        {
            var row = dataTable.NewRow();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[i] = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i).ToString();
            }
            dataTable.Rows.Add(row);
        }
        
        return dataTable;
    }

    public void ExecuteNonQuery(string query)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        using var command = new SqliteCommand(query, connection);
        command.ExecuteNonQuery();
    }

    public bool DocumentExists(string filePath)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var query = "SELECT COUNT(*) FROM Documents WHERE FilePath = @filePath";
        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@filePath", filePath);
        
        var count = Convert.ToInt32(command.ExecuteScalar());
        return count > 0;
    }

    public void InsertDocument(Document document)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var query = @"
            INSERT INTO Documents (FilePath, FileName, FileHash, FileSize, CreatedDate, ModifiedDate, IngestedDate, RawText, LayoutText, PageCount)
            VALUES (@filePath, @fileName, @fileHash, @fileSize, @createdDate, @modifiedDate, @ingestedDate, @rawText, @layoutText, @pageCount)";
        
        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@filePath", document.FilePath);
        command.Parameters.AddWithValue("@fileName", document.FileName);
        command.Parameters.AddWithValue("@fileHash", document.FileHash);
        command.Parameters.AddWithValue("@fileSize", document.FileSize);
        command.Parameters.AddWithValue("@createdDate", document.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@modifiedDate", document.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@ingestedDate", document.IngestedDate.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@rawText", document.RawText);
        command.Parameters.AddWithValue("@layoutText", document.LayoutText);
        command.Parameters.AddWithValue("@pageCount", document.PageCount);
        
        command.ExecuteNonQuery();
    }

    public void SaveQueryHistory(string query)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var insertQuery = "INSERT INTO QueryHistory (Query, ExecutedDate) VALUES (@query, @executedDate)";
        using var command = new SqliteCommand(insertQuery, connection);
        command.Parameters.AddWithValue("@query", query);
        command.Parameters.AddWithValue("@executedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        
        command.ExecuteNonQuery();
    }

    public List<DocumentType> GetDocumentTypes()
    {
        var types = new List<DocumentType>();
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var query = "SELECT * FROM DocumentTypes ORDER BY Name";
        using var command = new SqliteCommand(query, connection);
        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            types.Add(new DocumentType
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                IdentifierText = reader.GetString(3),
                TableName = reader.GetString(4),
                CreatedDate = DateTime.Parse(reader.GetString(5))
            });
        }
        
        return types;
    }

    public List<DocumentField> GetDocumentFields(int documentTypeId)
    {
        var fields = new List<DocumentField>();
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var query = "SELECT * FROM DocumentFields WHERE DocumentTypeId = @typeId ORDER BY SortOrder";
        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@typeId", documentTypeId);
        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            fields.Add(new DocumentField
            {
                Id = reader.GetInt32(0),
                DocumentTypeId = reader.GetInt32(1),
                FieldName = reader.GetString(2),
                ColumnName = reader.GetString(3),
                ContextText = reader.GetString(4),
                FieldText = reader.GetString(5),
                RegexPattern = reader.GetString(6),
                DataType = reader.GetString(7),
                SortOrder = reader.GetInt32(8)
            });
        }
        
        return fields;
    }

    public void CreateDocumentTypeTable(string tableName, List<DocumentField> fields)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        CreateDocumentTypeTable(tableName, fields, connection);
    }
    
    private void CreateDocumentTypeTable(string tableName, List<DocumentField> fields, SqliteConnection connection, SqliteTransaction? transaction = null)
    {
        var columns = new List<string> { "Id INTEGER PRIMARY KEY AUTOINCREMENT", "DocumentId INTEGER NOT NULL" };
        
        foreach (var field in fields)
        {
            columns.Add($"[{field.ColumnName}] TEXT");
        }
        
        columns.Add("FOREIGN KEY (DocumentId) REFERENCES Documents(Id)");
        
        var createTableQuery = $"CREATE TABLE IF NOT EXISTS [{tableName}] ({string.Join(", ", columns)})";
        
        using var command = new SqliteCommand(createTableQuery, connection, transaction);
        command.ExecuteNonQuery();
    }

    public int CreateDocumentType(DocumentType documentType, List<DocumentField> fields)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Insert document type
            var insertTypeSql = @"
                INSERT INTO DocumentTypes (Name, Description, IdentifierText, TableName, CreatedDate)
                VALUES (@Name, @Description, @IdentifierText, @TableName, @CreatedDate);
                SELECT last_insert_rowid();";

            using var typeCommand = new SqliteCommand(insertTypeSql, connection, transaction);
            typeCommand.Parameters.AddWithValue("@Name", documentType.Name);
            typeCommand.Parameters.AddWithValue("@Description", documentType.Description);
            typeCommand.Parameters.AddWithValue("@IdentifierText", documentType.IdentifierText);
            typeCommand.Parameters.AddWithValue("@TableName", documentType.TableName);
            typeCommand.Parameters.AddWithValue("@CreatedDate", documentType.CreatedDate);

            var documentTypeId = Convert.ToInt32(typeCommand.ExecuteScalar());

            // Insert fields
            foreach (var field in fields)
            {
                var insertFieldSql = @"
                    INSERT INTO DocumentFields 
                    (DocumentTypeId, FieldName, ColumnName, ContextText, FieldText, RegexPattern, DataType, SortOrder)
                    VALUES 
                    (@DocumentTypeId, @FieldName, @ColumnName, @ContextText, @FieldText, @RegexPattern, @DataType, @SortOrder)";

                using var fieldCommand = new SqliteCommand(insertFieldSql, connection, transaction);
                fieldCommand.Parameters.AddWithValue("@DocumentTypeId", documentTypeId);
                fieldCommand.Parameters.AddWithValue("@FieldName", field.FieldName);
                fieldCommand.Parameters.AddWithValue("@ColumnName", field.ColumnName);
                fieldCommand.Parameters.AddWithValue("@ContextText", field.ContextText);
                fieldCommand.Parameters.AddWithValue("@FieldText", field.FieldText);
                fieldCommand.Parameters.AddWithValue("@RegexPattern", field.RegexPattern);
                fieldCommand.Parameters.AddWithValue("@DataType", field.DataType);
                fieldCommand.Parameters.AddWithValue("@SortOrder", field.SortOrder);
                fieldCommand.ExecuteNonQuery();
            }

            // Create the document type table (use same connection and transaction to avoid locking)
            CreateDocumentTypeTable(documentType.TableName, fields, connection, transaction);

            transaction.Commit();
            return documentTypeId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public List<Document> GetDocuments()
    {
        var documents = new List<Document>();
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var query = "SELECT * FROM Documents ORDER BY FileName";
        using var command = new SqliteCommand(query, connection);
        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            documents.Add(new Document
            {
                Id = reader.GetInt32(0),
                FilePath = reader.GetString(1),
                FileName = reader.GetString(2),
                FileHash = reader.GetString(3),
                FileSize = reader.GetInt64(4),
                CreatedDate = DateTime.Parse(reader.GetString(5)),
                ModifiedDate = DateTime.Parse(reader.GetString(6)),
                IngestedDate = DateTime.Parse(reader.GetString(7)),
                RawText = reader.GetString(8),
                LayoutText = reader.GetString(9),
                PageCount = reader.GetInt32(10)
            });
        }
        
        return documents;
    }

    public Document? GetDocumentById(int documentId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var query = "SELECT * FROM Documents WHERE Id = @id";
        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@id", documentId);
        using var reader = command.ExecuteReader();
        
        if (reader.Read())
        {
            return new Document
            {
                Id = reader.GetInt32(0),
                FilePath = reader.GetString(1),
                FileName = reader.GetString(2),
                FileHash = reader.GetString(3),
                FileSize = reader.GetInt64(4),
                CreatedDate = DateTime.Parse(reader.GetString(5)),
                ModifiedDate = DateTime.Parse(reader.GetString(6)),
                IngestedDate = DateTime.Parse(reader.GetString(7)),
                RawText = reader.GetString(8),
                LayoutText = reader.GetString(9),
                PageCount = reader.GetInt32(10)
            };
        }
        
        return null;
    }

    public void CreateTableFromDataTable(DataTable dataTable, string tableName)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        // Build CREATE TABLE statement
        var columns = new List<string>();
        foreach (DataColumn col in dataTable.Columns)
        {
            // Sanitize column name
            var columnName = col.ColumnName.Replace(" ", "_").Replace("[", "").Replace("]", "");
            columns.Add($"[{columnName}] TEXT");
        }
        
        var createTableSql = $"CREATE TABLE IF NOT EXISTS [{tableName}] ({string.Join(", ", columns)})";
        
        using (var cmd = new SqliteCommand(createTableSql, connection))
        {
            cmd.ExecuteNonQuery();
        }
        
        // Insert data
        if (dataTable.Rows.Count > 0)
        {
            var columnNames = string.Join(", ", dataTable.Columns.Cast<DataColumn>()
                .Select(c => $"[{c.ColumnName.Replace(" ", "_").Replace("[", "").Replace("]", "")}]"));
            var parameters = string.Join(", ", dataTable.Columns.Cast<DataColumn>()
                .Select((c, i) => $"@p{i}"));
            
            var insertSql = $"INSERT INTO [{tableName}] ({columnNames}) VALUES ({parameters})";
            
            using var transaction = connection.BeginTransaction();
            try
            {
                using var cmd = new SqliteCommand(insertSql, connection, transaction);
                
                // Add parameters
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    cmd.Parameters.Add($"@p{i}", SqliteType.Text);
                }
                
                // Insert each row
                foreach (DataRow row in dataTable.Rows)
                {
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        cmd.Parameters[$"@p{i}"].Value = row[i]?.ToString() ?? "";
                    }
                    cmd.ExecuteNonQuery();
                }
                
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}