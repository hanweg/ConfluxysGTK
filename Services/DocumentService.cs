using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using System.IO;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Confluxys.Models;

namespace Confluxys.Services;

public class DocumentService
{
    private readonly DatabaseService _databaseService;

    public DocumentService()
    {
        _databaseService = new DatabaseService();
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

        var (rawText, layoutText, pageCount) = ExtractTextFromPdf(filePath);
        document.RawText = rawText;
        document.LayoutText = layoutText;
        document.PageCount = pageCount;

        _databaseService.InsertDocument(document);
    }

    private string CalculateFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = File.OpenRead(filePath);
        var hashBytes = sha256.ComputeHash(fileStream);
        return Convert.ToHexString(hashBytes);
    }

    private (string rawText, string layoutText, int pageCount) ExtractTextFromPdf(string filePath)
    {
        var rawTextBuilder = new StringBuilder();
        var layoutTextBuilder = new StringBuilder();
        int pageCount = 0;

        try
        {
            using (var document = PdfDocument.Open(filePath))
            {
                pageCount = document.NumberOfPages;

                for (int pageNum = 1; pageNum <= pageCount; pageNum++)
                {
                    var page = document.GetPage(pageNum);
                    
                    rawTextBuilder.AppendLine($"--- Page {pageNum} ---");
                    rawTextBuilder.AppendLine(page.Text);
                    rawTextBuilder.AppendLine();

                    layoutTextBuilder.AppendLine($"--- Page {pageNum} ---");
                    
                    var words = page.GetWords().OrderBy(w => w.BoundingBox.Bottom).ThenBy(w => w.BoundingBox.Left);
                    
                    double? lastY = null;
                    var lineBuilder = new StringBuilder();
                    
                    foreach (var word in words)
                    {
                        if (lastY.HasValue && Math.Abs(word.BoundingBox.Bottom - lastY.Value) > 1)
                        {
                            layoutTextBuilder.AppendLine(lineBuilder.ToString().TrimEnd());
                            lineBuilder.Clear();
                        }
                        
                        lineBuilder.Append(word.Text + " ");
                        lastY = word.BoundingBox.Bottom;
                    }
                    
                    if (lineBuilder.Length > 0)
                    {
                        layoutTextBuilder.AppendLine(lineBuilder.ToString().TrimEnd());
                    }
                    
                    layoutTextBuilder.AppendLine();
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error extracting text from PDF: {ex.Message}", ex);
        }

        return (rawTextBuilder.ToString(), layoutTextBuilder.ToString(), pageCount);
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