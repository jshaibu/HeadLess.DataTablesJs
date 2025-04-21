using HeadLess.DataTablesJs.Models;

namespace HeadLess.DataTablesJs.Interfaces;

public interface IDataTablesQuery
{
    Task<DataTableResult<T>> GetDataAsync<T>(
        DataTablesRequest request,
        IQueryable<T> query,        
        List<string>? searchableProperties = null) where T : class;
}
