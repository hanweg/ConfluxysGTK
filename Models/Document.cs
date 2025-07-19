using System;
using System.Collections.Generic;

namespace Confluxys.Models;

public class Document
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public DateTime IngestedDate { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string LayoutText { get; set; } = string.Empty;
    public int PageCount { get; set; }
}

public class DocumentType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IdentifierText { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class DocumentField
{
    public int Id { get; set; }
    public int DocumentTypeId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string ContextText { get; set; } = string.Empty;
    public string FieldText { get; set; } = string.Empty;
    public string RegexPattern { get; set; } = string.Empty;
    public string DataType { get; set; } = "TEXT";
    public int SortOrder { get; set; }
}

public class TableInfo
{
    public string Name { get; set; } = string.Empty;
    public List<ColumnInfo> Columns { get; set; } = new();
}

public class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool NotNull { get; set; }
    public bool PrimaryKey { get; set; }
}