using System.Collections.Generic;

namespace Confluxys.Services;

public class QueryHistoryService
{
    private readonly DatabaseService _databaseService;
    private readonly List<string> _queryHistory = new();
    private int _currentIndex = -1;

    public QueryHistoryService()
    {
        _databaseService = new DatabaseService();
        LoadHistory();
    }

    private void LoadHistory()
    {
        var dataTable = _databaseService.ExecuteQuery("SELECT Query FROM QueryHistory ORDER BY ExecutedDate DESC LIMIT 100");
        
        foreach (System.Data.DataRow row in dataTable.Rows)
        {
            _queryHistory.Add(row["Query"].ToString() ?? string.Empty);
        }
        
        if (_queryHistory.Count > 0)
        {
            _currentIndex = -1;
        }
    }

    public void AddQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return;
        
        _queryHistory.Insert(0, query);
        _currentIndex = -1;
        
        _databaseService.SaveQueryHistory(query);
    }

    public string? GetPreviousQuery()
    {
        if (_queryHistory.Count == 0)
            return null;
        
        if (_currentIndex < _queryHistory.Count - 1)
        {
            _currentIndex++;
            return _queryHistory[_currentIndex];
        }
        
        return null;
    }

    public string? GetNextQuery()
    {
        if (_queryHistory.Count == 0 || _currentIndex <= 0)
            return null;
        
        _currentIndex--;
        return _queryHistory[_currentIndex];
    }
}