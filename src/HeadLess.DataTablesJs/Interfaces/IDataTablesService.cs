using HeadLess.DataTablesJs.Models;

namespace HeadLess.DataTablesJs.Interfaces;

public interface IDataTablesService
{
    Task<DataTableResult<T>> GetDataAsync<T>(
        DataTablesRequest request,
        object query,
        List<string>? searchableProperties = null) where T : class;
}
