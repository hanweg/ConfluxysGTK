using System;
using System.IO;
using System.Linq;

namespace Confluxys.Services.TextExtractors;

public class PlainTextExtractor : ITextExtractor
{
    public string[] SupportedExtensions => new[] { ".txt", ".md", ".log", ".cfg", ".conf", ".ini", ".yaml", ".yml" };

    public bool CanHandle(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public (string rawText, string layoutText, int pageCount) ExtractText(string filePath)
    {
        try
        {
            var text = File.ReadAllText(filePath);
            
            // For plain text files, raw and layout text are the same
            // Page count is approximated based on lines (60 lines per page)
            var lines = text.Split('\n').Length;
            var pageCount = Math.Max(1, (lines + 59) / 60);
            
            return (text, text, pageCount);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error reading text file: {ex.Message}", ex);
        }
    }
}