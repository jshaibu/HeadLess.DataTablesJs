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

namespace HeadLess.DataTablesJs.Core;

public class DataTablesList : IDataTablesList
{
    private readonly ILogger<DataTablesList> _logger;

    public DataTablesList(ILogger<DataTablesList> logger)
    {
        _logger = logger;
    }

    public async Task<DataTableResult<T>> ProcessRequest<T>(DataTablesRequest request, List<T> data)
    {
        try
        {
            var filteredData = data.AsQueryable();

            // Apply global search if search value is present
            //if (!string.IsNullOrEmpty(request.Search?.Value))
            //{
            filteredData = ApplyGlobalSearch(request, filteredData);
            // }

            // Apply individual column search if column searches are present
            if (request.Columns.Any(c => c.Searchable && !string.IsNullOrEmpty(c.Search?.Value)))
            {
                filteredData = ApplyColumnSearch(request, filteredData);
            }

            var recordsFiltered = filteredData.Count();

            // Apply sorting if sorting information is present
            if (request.Order != null && request.Order.Any())
            {
                filteredData = ApplySorting(request, filteredData);
            }

            // Apply pagination
            var pagedData = await filteredData.Skip(request.Start).Take(request.Length).ToListAsync();

            return new DataTableResult<T>
            {
                Draw = request.Draw,
                TotalRecords = data.Count,
                FilteredRecords = recordsFiltered,
                Data = pagedData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the DataTables request.");
            throw; // Re-throw the exception after logging it
        }
    }

    public interface ICounterable
    {
        int Counter { get; set; }
    }

    public List<TViewModel> TransformData<T, TViewModel>(List<T> data, DataTablesRequest request, Func<T, TViewModel> transform)
    {
        int counter = 1;
        if (request.Start > (request.Length - 1)) counter += request.Start;

        return data.Select(item =>
        {
            var viewModel = transform(item);
            // Assuming `viewModel` has a property named `Counter`
            if (viewModel is ICounterable counterable)
            {
                counterable.Counter = counter++;
            }
            return viewModel;
        }).ToList();
    }

    private IQueryable<T> ApplyGlobalSearch<T>(DataTablesRequest request, IQueryable<T> data)
    {
        var searchValue = request.Search?.Value?.ToLower();
        var searchableColumns = request.Columns.Where(c => c.Searchable).Select(c => c.Data).ToList();
        //Console.WriteLine(request.Columns.Count());
        if (!string.IsNullOrEmpty(searchValue) && searchableColumns.Any())
        {
            try
            {
                // Construct the search query
                var searchQuery = string.Join(" OR ", searchableColumns.Select(c => $"{c}.ToString().ToLower().Contains(@0)"));

                // Apply the search query to the data
                data = data.Where(searchQuery, searchValue);
            }
            catch (Exception ex)
            {
                // Log the exception if something goes wrong
                _logger.LogError(ex, "Error applying global search.");
                throw; // Rethrow the exception to notify the caller
            }
        }

        return data;
    }

    private IQueryable<T> ApplyColumnSearch<T>(DataTablesRequest request, IQueryable<T> data)
    {
        foreach (var column in request.Columns.Where(c => c.Searchable && !string.IsNullOrEmpty(c.Search?.Value)))
        {
            var searchValue = column?.Search?.Value?.ToLower();
            if (!string.IsNullOrEmpty(column?.Data))
            {
                data = data.Where($"{column.Data}.ToString().ToLower().Contains(@0)", searchValue);
            }
        }

        return data;
    }

    private IQueryable<T> ApplySorting<T>(DataTablesRequest request, IQueryable<T> data)
    {
        foreach (var order in request.Order)
        {
            var columnName = request.Columns[order.Column].Data;
            var direction = order.Dir?.ToLower() == "asc" ? "" : " descending";
            if (!string.IsNullOrEmpty(columnName))
            {
                data = data.OrderBy($"{columnName}{direction}");
            }
            else if (typeof(T).GetProperty("FirstName") != null) // Check if FirstName exists
            {
                data = data.OrderBy("FirstName asc");
            }
        }

        return data;
    }
}