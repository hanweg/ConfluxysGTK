namespace Confluxys.Services.TextExtractors;

public interface ITextExtractor
{
    bool CanHandle(string filePath);
    (string rawText, string layoutText, int pageCount) ExtractText(string filePath);
    string[] SupportedExtensions { get; }
}