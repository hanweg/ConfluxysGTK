using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using System.IO;
using System.Linq;
using Confluxys.Models;
using Confluxys.Services.TextExtractors;

namespace Confluxys.Services;

public class DocumentService
{
    private readonly DatabaseService _databaseService;
    private readonly TextExtractorFactory _textExtractorFactory;

    public DocumentService()
    {
        _databaseService = new DatabaseService();
        _textExtractorFactory = new TextExtractorFactory();
    }

    public void IngestDocument(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        if (_databaseService.DocumentExists(filePath))
            throw new InvalidOperationException($"Document already exists: {filePath}");

        var fileInfo = new FileInfo(filePath);
        var document = new Document
        {
            FilePath = filePath,
            FileName = fileInfo.Name,
            FileHash = CalculateFileHash(filePath),
            FileSize = fileInfo.Length,
            CreatedDate = fileInfo.CreationTime,
            ModifiedDate = fileInfo.LastWriteTime,
            IngestedDate = DateTime.Now
        };

        // Use the text extractor factory to get the appropriate extractor
        var extractor = _textExtractorFactory.GetExtractor(filePath);
        var (rawText, layoutText, pageCount) = extractor.ExtractText(filePath);
        
        document.RawText = rawText;
        document.LayoutText = layoutText;
        document.PageCount = pageCount;

        _databaseService.InsertDocument(document);
    }
    
    public string[] GetSupportedExtensions()
    {
        return _textExtractorFactory.GetAllSupportedExtensions();
    }
    
    public List<(string Name, string[] Extensions)> GetFileFilters()
    {
        return _textExtractorFactory.GetFileFilters();
    }

    private string CalculateFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = File.OpenRead(filePath);
        var hashBytes = sha256.ComputeHash(fileStream);
        return Convert.ToHexString(hashBytes);
    }


    public List<Document> GetDocuments()
    {
        var documents = new List<Document>();
        var dataTable = _databaseService.ExecuteQuery("SELECT * FROM Documents ORDER BY IngestedDate DESC");

        foreach (DataRow row in dataTable.Rows)
        {
            var document = new Document
            {
                Id = Convert.ToInt32(row["Id"]),
                FilePath = row["FilePath"].ToString() ?? string.Empty,
                FileName = row["FileName"].ToString() ?? string.Empty,
                FileHash = row["FileHash"].ToString() ?? string.Empty,
                FileSize = Convert.ToInt64(row["FileSize"]),
                CreatedDate = DateTime.Parse(row["CreatedDate"].ToString() ?? DateTime.MinValue.ToString()),
                ModifiedDate = DateTime.Parse(row["ModifiedDate"].ToString() ?? DateTime.MinValue.ToString()),
                IngestedDate = DateTime.Parse(row["IngestedDate"].ToString() ?? DateTime.MinValue.ToString()),
                RawText = row["RawText"].ToString() ?? string.Empty,
                LayoutText = row["LayoutText"].ToString() ?? string.Empty,
                PageCount = Convert.ToInt32(row["PageCount"])
            };
            
            documents.Add(document);
        }

        return documents;
    }

    public Document? GetDocumentById(int id)
    {
        var dataTable = _databaseService.ExecuteQuery($"SELECT * FROM Documents WHERE Id = {id}");
        
        if (dataTable.Rows.Count == 0)
            return null;

        var row = dataTable.Rows[0];
        return new Document
        {
            Id = Convert.ToInt32(row["Id"]),
            FilePath = row["FilePath"].ToString() ?? string.Empty,
            FileName = row["FileName"].ToString() ?? string.Empty,
            FileHash = row["FileHash"].ToString() ?? string.Empty,
            FileSize = Convert.ToInt64(row["FileSize"]),
            CreatedDate = DateTime.Parse(row["CreatedDate"].ToString() ?? DateTime.MinValue.ToString()),
            ModifiedDate = DateTime.Parse(row["ModifiedDate"].ToString() ?? DateTime.MinValue.ToString()),
            IngestedDate = DateTime.Parse(row["IngestedDate"].ToString() ?? DateTime.MinValue.ToString()),
            RawText = row["RawText"].ToString() ?? string.Empty,
            LayoutText = row["LayoutText"].ToString() ?? string.Empty,
            PageCount = Convert.ToInt32(row["PageCount"])
        };
    }

    public List<Document> SearchDocuments(string searchTerm)
    {
        var documents = new List<Document>();
        var query = $@"
            SELECT d.* 
            FROM Documents d
            JOIN DocumentsSearch ds ON d.Id = ds.rowid
            WHERE DocumentsSearch MATCH '{searchTerm.Replace("'", "''")}'
            ORDER BY rank";

        var dataTable = _databaseService.ExecuteQuery(query);

        foreach (DataRow row in dataTable.Rows)
        {
            var document = new Document
            {
                Id = Convert.ToInt32(row["Id"]),
                FilePath = row["FilePath"].ToString() ?? string.Empty,
                FileName = row["FileName"].ToString() ?? string.Empty,
                FileHash = row["FileHash"].ToString() ?? string.Empty,
                FileSize = Convert.ToInt64(row["FileSize"]),
                CreatedDate = DateTime.Parse(row["CreatedDate"].ToString() ?? DateTime.MinValue.ToString()),
                ModifiedDate = DateTime.Parse(row["ModifiedDate"].ToString() ?? DateTime.MinValue.ToString()),
                IngestedDate = DateTime.Parse(row["IngestedDate"].ToString() ?? DateTime.MinValue.ToString()),
                RawText = row["RawText"].ToString() ?? string.Empty,
                LayoutText = row["LayoutText"].ToString() ?? string.Empty,
                PageCount = Convert.ToInt32(row["PageCount"])
            };
            
            documents.Add(document);
        }

        return documents;
    }
}