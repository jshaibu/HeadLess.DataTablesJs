using HeadLess.DataTablesJs.Models;

namespace HeadLess.DataTablesJs.Interfaces;

public interface IDataTablesList
{
    DataTableResult<T> ProcessRequest<T>(DataTablesRequest request, List<T> data);
}
