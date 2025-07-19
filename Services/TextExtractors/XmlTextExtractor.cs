using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Confluxys.Services.TextExtractors;

public class XmlTextExtractor : ITextExtractor
{
    public string[] SupportedExtensions => new[] { ".xml", ".xhtml", ".svg", ".rss", ".atom" };

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
            string layoutText;
            
            try
            {
                // Try to parse and format XML
                var doc = XDocument.Parse(rawText);
                
                // Extract text content and structure
                var textBuilder = new StringBuilder();
                if (doc.Root != null)
                {
                    ExtractTextFromElement(doc.Root, textBuilder, 0);
                }
                
                // Also create a formatted version
                using (var stringWriter = new StringWriter())
                using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true }))
                {
                    doc.WriteTo(xmlWriter);
                    xmlWriter.Flush();
                    layoutText = stringWriter.ToString();
                }
                
                // Add extracted text at the end for better searchability
                if (textBuilder.Length > 0)
                {
                    layoutText += "\n\n--- Extracted Text Content ---\n" + textBuilder.ToString();
                }
            }
            catch
            {
                // If XML parsing fails, use raw text
                layoutText = rawText;
            }
            
            var lines = layoutText.Split('\n').Length;
            var pageCount = Math.Max(1, (lines + 59) / 60);
            
            return (rawText, layoutText, pageCount);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error reading XML file: {ex.Message}", ex);
        }
    }
    
    private void ExtractTextFromElement(XElement element, StringBuilder textBuilder, int depth)
    {
        if (element == null) return;
        
        // Add element name with indentation
        if (!string.IsNullOrWhiteSpace(element.Name.LocalName))
        {
            textBuilder.AppendLine(new string(' ', depth * 2) + element.Name.LocalName + ":");
        }
        
        // Add text content if any
        if (!string.IsNullOrWhiteSpace(element.Value) && !element.HasElements)
        {
            textBuilder.AppendLine(new string(' ', (depth + 1) * 2) + element.Value.Trim());
        }
        
        // Process attributes
        foreach (var attr in element.Attributes())
        {
            textBuilder.AppendLine(new string(' ', (depth + 1) * 2) + $"@{attr.Name}: {attr.Value}");
        }
        
        // Process child elements
        foreach (var child in element.Elements())
        {
            ExtractTextFromElement(child, textBuilder, depth + 1);
        }
    }
}