using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Confluxys.Models;

namespace Confluxys.Services;

public class DocumentReprocessingService
{
    private readonly DatabaseService _databaseService;
    private readonly DocumentService _documentService;

    public DocumentReprocessingService()
    {
        _databaseService = new DatabaseService();
        _documentService = new DocumentService();
    }

    public int ReprocessAllDocuments()
    {
        int processedCount = 0;
        
        var documentTypes = _databaseService.GetDocumentTypes();
        var documents = _documentService.GetDocuments();
        
        foreach (var documentType in documentTypes)
        {
            var fields = _databaseService.GetDocumentFields(documentType.Id);
            
            _databaseService.ExecuteNonQuery($"DELETE FROM [{documentType.TableName}]");
            
            foreach (var document in documents)
            {
                if (DocumentMatchesType(document, documentType))
                {
                    var extractedValues = ExtractFieldValues(document, fields);
                    
                    if (extractedValues.Any())
                    {
                        InsertExtractedData(documentType.TableName, document.Id, extractedValues);
                        processedCount++;
                    }
                }
            }
        }
        
        return processedCount;
    }

    private bool DocumentMatchesType(Document document, DocumentType documentType)
    {
        return document.LayoutText.Contains(documentType.IdentifierText, StringComparison.InvariantCultureIgnoreCase) ||
               document.RawText.Contains(documentType.IdentifierText, StringComparison.InvariantCultureIgnoreCase);
    }

    private Dictionary<string, string> ExtractFieldValues(Document document, List<DocumentField> fields)
    {
        var extractedValues = new Dictionary<string, string>();
        
        foreach (var field in fields)
        {
            try
            {
                var regex = new Regex(field.RegexPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                var match = regex.Match(document.LayoutText);
                
                if (!match.Success)
                {
                    match = regex.Match(document.RawText);
                }
                
                if (match.Success && match.Groups.Count > 1)
                {
                    extractedValues[field.ColumnName] = match.Groups[1].Value.Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting field {field.FieldName}: {ex.Message}");
            }
        }
        
        return extractedValues;
    }

    private void InsertExtractedData(string tableName, int documentId, Dictionary<string, string> values)
    {
        var columns = new List<string> { "DocumentId" };
        var valuesList = new List<string> { documentId.ToString() };
        
        foreach (var kvp in values)
        {
            columns.Add($"[{kvp.Key}]");
            valuesList.Add($"'{kvp.Value.Replace("'", "''")}'");
        }
        
        var query = $"INSERT INTO [{tableName}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", valuesList)})";
        
        _databaseService.ExecuteNonQuery(query);
    }
}