using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Confluxys.Services.TextExtractors;

public class TextExtractorFactory
{
    private readonly List<ITextExtractor> _extractors;
    
    public TextExtractorFactory()
    {
        _extractors = new List<ITextExtractor>
        {
            new PdfTextExtractor(),
            new PlainTextExtractor(),
            new CsvTextExtractor(),
            new JsonTextExtractor(),
            new XmlTextExtractor()
        };
    }
    
    public ITextExtractor GetExtractor(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath));
            
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");
        
        // Find the appropriate extractor based on file extension
        var extractor = _extractors.FirstOrDefault(e => e.CanHandle(filePath));
        
        if (extractor == null)
        {
            throw new NotSupportedException($"No text extractor available for file type: {Path.GetExtension(filePath)}");
        }
        
        return extractor;
    }
    
    public string[] GetAllSupportedExtensions()
    {
        return _extractors.SelectMany(e => e.SupportedExtensions).Distinct().ToArray();
    }
    
    public string GetFileFilterString()
    {
        var allExtensions = GetAllSupportedExtensions();
        var patterns = string.Join(";", allExtensions.Select(ext => $"*{ext}"));
        return $"All Supported Files|{patterns}";
    }
    
    public List<(string Name, string[] Extensions)> GetFileFilters()
    {
        var filters = new List<(string, string[])>
        {
            ("All Supported Files", GetAllSupportedExtensions()),
            ("PDF Documents", new[] { ".pdf" }),
            ("Text Files", new[] { ".txt", ".md", ".log", ".cfg", ".conf", ".ini", ".yaml", ".yml" }),
            ("CSV Files", new[] { ".csv", ".tsv" }),
            ("JSON Files", new[] { ".json", ".jsonl", ".ndjson" }),
            ("XML Files", new[] { ".xml", ".xhtml", ".svg", ".rss", ".atom" }),
            ("All Files", new[] { "*" })
        };
        
        return filters;
    }
}