using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using HeadLess.DataTablesJs.Models;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using HeadLess.DataTablesJs.Interfaces;
using HeadLess.DataTablesJs.Extensions;
using HeadLess.DataTablesJs.Exceptions;
using static System.Console;

namespace HeadLess.DataTablesJs.Core;

public class DataTablesService : IDataTablesService
{
    private readonly IDataTablesList _listService;
    private readonly IDataTablesQuery _queryService;
    public DataTablesService(IDataTablesList listService, IDataTablesQuery queryService)
    {
        _listService = listService;
        _queryService = queryService;
    }

    public Task<DataTableResult<T>> GetDataAsync<T>(
        DataTablesRequest request,
        object query,
        List<string>? searchableProperties = null) where T : class
    {
        if (query is List<T> list)
        {
            WriteLine("It's a List<T>");
            return Task.FromResult(_listService.ProcessRequest<T>(request: request, data: list));            
        }
        else if (query is IQueryable<T> q)
        {
            if (searchableProperties is null)
            {
                WriteLine("It's an IQueryable<T>");
                return _queryService.GetDataAsync<T>(request: request, query: q);
            }
            else
            {
                WriteLine("It's an IQueryable<T> with searchableProperties");
                return _queryService.GetDataAsync<T>(request: request, query: q, searchableProperties: searchableProperties);
            }            
        }
        else
        {
            throw new DataTablesQueryException(
                $"Unsupported query type: {query.GetType().FullName}. Expected List<{typeof(T).Name}> or IQueryable<{typeof(T).Name}>.");
        }
    }
}