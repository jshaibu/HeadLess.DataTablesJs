using HeadLess.DataTablesJs.Models;

namespace HeadLess.DataTablesJs.Interfaces;

public interface IDataTablesList
{
    Task<DataTableResult<T>> ProcessRequest<T>(DataTablesRequest request, List<T> data);
}
