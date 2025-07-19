using System;
using System.IO;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;

namespace Confluxys.Services.TextExtractors;

public class PdfTextExtractor : ITextExtractor
{
    public string[] SupportedExtensions => new[] { ".pdf" };

    public bool CanHandle(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public (string rawText, string layoutText, int pageCount) ExtractText(string filePath)
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
}