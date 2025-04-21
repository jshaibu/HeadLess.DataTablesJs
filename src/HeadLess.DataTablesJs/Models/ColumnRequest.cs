namespace HeadLess.DataTablesJs.Models;
public class ColumnRequest
{
    public string? Data { get; set; }
    public string? Name { get; set; }
    public bool Searchable { get; set; }
    public bool Orderable { get; set; }
    public SearchRequest Search { get; set; } = new SearchRequest();
}