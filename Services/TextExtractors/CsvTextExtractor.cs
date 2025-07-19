using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Confluxys.Services.TextExtractors;

public class CsvTextExtractor : ITextExtractor
{
    public string[] SupportedExtensions => new[] { ".csv", ".tsv" };

    public bool CanHandle(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public (string rawText, string layoutText, int pageCount) ExtractText(string filePath)
    {
        try
        {
            var rawText = File.ReadAllText(filePath);
            var lines = rawText.Split('\n');
            
            // For layout text, format the CSV data in a more readable way
            var layoutBuilder = new StringBuilder();
            
            if (lines.Length > 0)
            {
                // Detect delimiter
                var delimiter = DetectDelimiter(filePath, lines[0]);
                
                // Parse CSV structure
                var rows = lines.Select(line => line.Split(delimiter)).ToList();
                
                // Calculate column widths for better layout
                var columnCount = rows.FirstOrDefault()?.Length ?? 0;
                var columnWidths = new int[columnCount];
                
                foreach (var row in rows.Take(100)) // Sample first 100 rows for width calculation
                {
                    for (int i = 0; i < Math.Min(row.Length, columnCount); i++)
                    {
                        columnWidths[i] = Math.Max(columnWidths[i], row[i].Length);
                    }
                }
                
                // Format as table
                foreach (var row in rows)
                {
                    for (int i = 0; i < Math.Min(row.Length, columnCount); i++)
                    {
                        layoutBuilder.Append(row[i].PadRight(columnWidths[i] + 2));
                    }
                    layoutBuilder.AppendLine();
                }
            }
            
            var pageCount = Math.Max(1, (lines.Length + 59) / 60);
            
            return (rawText, layoutBuilder.ToString(), pageCount);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error reading CSV file: {ex.Message}", ex);
        }
    }
    
    private char DetectDelimiter(string filePath, string firstLine)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension == ".tsv") return '\t';
        
        // Simple delimiter detection - count occurrences
        var delimiters = new[] { ',', '\t', ';', '|' };
        var counts = delimiters.ToDictionary(d => d, d => firstLine.Count(c => c == d));
        
        return counts.OrderByDescending(kvp => kvp.Value).First().Key;
    }
}