namespace HeadLess.DataTablesJs.Models;

public class DataTablesRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public SearchRequest Search { get; set; } = new SearchRequest();
    public List<ColumnRequest> Columns { get; set; } = new List<ColumnRequest>();
    public List<OrderRequest> Order { get; set; } = new List<OrderRequest>();
}








