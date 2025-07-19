using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Confluxys.Services.TextExtractors;

public class JsonTextExtractor : ITextExtractor
{
    public string[] SupportedExtensions => new[] { ".json", ".jsonl", ".ndjson" };

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
            var layoutText = rawText;
            
            // Try to format JSON for better readability
            try
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                if (extension == ".jsonl" || extension == ".ndjson")
                {
                    // Handle JSON Lines format
                    var jsonLines = rawText.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line));
                    var formattedLines = jsonLines.Select(line =>
                    {
                        try
                        {
                            var json = JsonDocument.Parse(line);
                            return JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true });
                        }
                        catch
                        {
                            return line; // Return original if parsing fails
                        }
                    });
                    layoutText = string.Join("\n\n", formattedLines);
                }
                else
                {
                    // Regular JSON
                    var jsonDoc = JsonDocument.Parse(rawText);
                    layoutText = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
                }
            }
            catch
            {
                // If JSON parsing fails, use raw text
                layoutText = rawText;
            }
            
            var lines = layoutText.Split('\n').Length;
            var pageCount = Math.Max(1, (lines + 59) / 60);
            
            return (rawText, layoutText, pageCount);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error reading JSON file: {ex.Message}", ex);
        }
    }
}