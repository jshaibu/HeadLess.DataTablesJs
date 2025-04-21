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

    public DataTableResult<T> ProcessRequest<T>(DataTablesRequest request, List<T> data)
    {
        try
        {
            var filteredData = data.AsQueryable();

            // Apply global search if search value is present
            filteredData = ApplyGlobalSearch(request, filteredData);

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
            var pagedData = filteredData.Skip(request.Start).Take(request.Length).ToList(); // No async needed

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

    public List<TViewModel> TransformData<T, TViewModel>(
        List<T> data,
        DataTablesRequest request,
        Func<T, TViewModel> transform
    )
    {
        int counter = 1;
        if (request.Start > (request.Length - 1)) counter += request.Start;

        return data.Select(item =>
        {
            var viewModel = transform(item);

            // Check if the viewModel implements ICounterable or has a 'Counter' property
            var counterableProperty = typeof(TViewModel).GetProperty("Counter");
            if (counterableProperty != null && counterableProperty.CanWrite)
            {
                // Assign the counter value to the 'Counter' property of the viewModel
                counterableProperty.SetValue(viewModel, counter++);
            }

            return viewModel;
        }).ToList();
    }

    private IQueryable<T> ApplyGlobalSearch<T>(DataTablesRequest request, IQueryable<T> data)
    {
        var searchValue = request.Search?.Value?.ToLower();
        var searchableColumns = request.Columns
            .Where(c => c.Searchable && !string.IsNullOrWhiteSpace(c.Data))
            .Select(c => c.Data!)
            .ToList();

        if (!string.IsNullOrEmpty(searchValue) && searchableColumns.Any())
        {
            try
            {
                var filters = new List<string>();
                var type = typeof(T);
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                     .Select(p => p.Name)
                                     .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var column in searchableColumns)
                {
                    if (column.Equals("fullName", StringComparison.OrdinalIgnoreCase))
                    {
                        // Optional checks before including the property in the filter
                        if (properties.Contains("FirstName") && properties.Contains("LastName"))
                            filters.Add("FirstName.ToLower().Contains(@0) && LastName.ToLower().Contains(@0)");

                        if (properties.Contains("FirstName"))
                            filters.Add("FirstName.ToLower().Contains(@0)");

                        if (properties.Contains("LastName"))
                            filters.Add("LastName.ToLower().Contains(@0)");

                        if (properties.Contains("FirstName") && properties.Contains("Surname"))
                            filters.Add("FirstName.ToLower().Contains(@0) && Surname.ToLower().Contains(@0)");
                    }
                    else if (properties.Contains(column))
                    {
                        filters.Add($"{column}.ToString().ToLower().Contains(@0)");
                    }
                }

                if (filters.Any())
                {
                    var searchQuery = string.Join(" OR ", filters);
                    data = data.Where(searchQuery, searchValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying global search.");
                throw;
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
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Select(p => p.Name)
                             .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var order in request.Order)
        {
            var columnName = request.Columns[order.Column].Data;

            var direction = order.Dir?.ToLower() == "asc" ? "" : " descending";

            if (!string.IsNullOrEmpty(columnName) && properties.Contains(columnName!))
            {
                try
                {
                    data = data.OrderBy($"{columnName}{direction}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sort by {ColumnName}", columnName);
                }
            }
            else
            {
                _logger.LogDebug("Skipping invalid or virtual column for sorting: {ColumnName}", columnName);
            }
        }

        if (!request.Order.Any(o =>
        {
            var col = request.Columns[o.Column].Data;
            return !string.IsNullOrEmpty(col) && properties.Contains(col!);
        }))
        {
            if (properties.Contains("FirstName"))
            {
                data = data.OrderBy("FirstName");
            }
            else if (properties.Contains("Id"))
            {
                data = data.OrderBy("Id");
            }
        }

        return data;
    }
}