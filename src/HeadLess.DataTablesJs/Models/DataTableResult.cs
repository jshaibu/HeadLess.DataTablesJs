namespace HeadLess.DataTablesJs.Models;
public class DataTableResult<T>
{
    public int Draw { get; set; }
    public List<T>? Data { get; set; }
    public int TotalRecords { get; set; }
    public int FilteredRecords { get; set; }
}