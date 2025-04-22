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

namespace HeadLess.DataTablesJs.Core;

public class DataTablesQuery : IDataTablesQuery
{
    public async Task<DataTableResult<T>> GetDataAsync<T>(
        DataTablesRequest request,
        IQueryable<T> query,        
        List<string>? searchableProperties = null) where T : class
    {
        // 1. Get total records count
        int totalRecords = await query.CountAsync();

        // 2. Apply search if requested
        if (!string.IsNullOrEmpty(request.Search?.Value))
        {
            query = ApplySearch(query, request, searchableProperties);
        }

        // 3. Get filtered count
        int filteredRecords = await query.CountAsync();

        // 4. Apply sorting
        if (request.Order != null && request.Order.Any())
        {
            query = QueryableExtensions.ApplySorting(query, request);
        }

        // 5. Apply paging
        var data = await query
            .Skip(request.Start)
            .Take(request.Length)
            .ToListAsync();

        return new DataTableResult<T>
        {
            Draw = request.Draw,
            Data = data,
            TotalRecords = totalRecords,
            FilteredRecords = filteredRecords
        };
    }

    private IQueryable<T> ApplySearch<T>(
        IQueryable<T> query,
        DataTablesRequest request,
        List<string>? searchableProperties = null
    ) where T : class
    {
        string searchValue = request.Search.Value ?? "";

        if (string.IsNullOrWhiteSpace(searchValue))
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");

        searchableProperties ??= request.Columns
            .Where(c => c.Searchable && !string.IsNullOrWhiteSpace(c.Data))
            .Select(c => c.Data!)
            .ToList();

        var modelProps = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Known virtual properties that should be allowed even if not on the model
        var virtualProps = new List<string> { "fullName" };

        // Map searchableProperties to actual model properties (case-insensitive), allow virtual ones too
        searchableProperties = searchableProperties
            .Where(sp => virtualProps.Contains(sp, StringComparer.OrdinalIgnoreCase)
                || modelProps.Any(mp => mp.Name.Equals(sp, StringComparison.OrdinalIgnoreCase)))
            .Select(sp => modelProps.FirstOrDefault(mp => mp.Name.Equals(sp, StringComparison.OrdinalIgnoreCase))?.Name
                          ?? sp) // Keep virtual prop name as-is
            .ToList();


        if (!searchableProperties.Any())
            return query;       

        var predicate = BuildSearchExpression<T>(parameter, searchValue, searchableProperties);
        return query.Where(Expression.Lambda<Func<T, bool>>(predicate, parameter));
    }

    private Expression BuildSearchExpression<T>(
        ParameterExpression parameter,
        string searchValue,
        List<string> searchableProperties)
    {
        bool isDate = DateOnly.TryParse(searchValue, out var searchDate);
        bool isDateTime = DateTime.TryParse(searchValue, out var searchDateTime);
        searchValue = searchValue.ToLower();

        var expressions = new List<Expression>();
        var entityType = typeof(T);
        var props = entityType.GetProperties();

        bool includeFullName = searchableProperties?.Any(p =>
            p.Equals("FullName", StringComparison.OrdinalIgnoreCase)) == true;

        var firstNameProp = props.FirstOrDefault(p => p.Name.Equals("FirstName", StringComparison.OrdinalIgnoreCase));
        var lastNameProp = props.FirstOrDefault(p => p.Name.Equals("LastName", StringComparison.OrdinalIgnoreCase));
        var surnameProp = props.FirstOrDefault(p => p.Name.Equals("Surname", StringComparison.OrdinalIgnoreCase));

        // Explicitly search FirstName, LastName, Surname when FullName is passed
        if (includeFullName)
        {
            foreach (var nameProp in new[] { firstNameProp, lastNameProp, surnameProp }.Where(p => p != null))
            {
                var nameExpr = Expression.Property(parameter, nameProp!);
                var notNull = Expression.NotEqual(nameExpr, Expression.Constant(null, typeof(string)));
                var lower = Expression.Call(nameExpr, nameof(string.ToLower), Type.EmptyTypes);
                var contains = Expression.Call(lower, nameof(string.Contains), null, Expression.Constant(searchValue));
                expressions.Add(Expression.AndAlso(notNull, contains));
            }
        }

        foreach (var prop in props)
        {
            // Skip FullName to prevent untranslatable expressions
            if (prop.Name.Equals("FullName", StringComparison.OrdinalIgnoreCase))
                continue;

            if (searchableProperties != null && !searchableProperties.Contains(prop.Name))
                continue;

            var propExpr = Expression.Property(parameter, prop);

            if (prop.PropertyType == typeof(string))
            {
                var notNull = Expression.NotEqual(propExpr, Expression.Constant(null, typeof(string)));
                var toLower = Expression.Call(propExpr, nameof(string.ToLower), Type.EmptyTypes);
                var contains = Expression.Call(toLower, nameof(string.Contains), null, Expression.Constant(searchValue));
                expressions.Add(Expression.AndAlso(notNull, contains));
            }
            else if (prop.PropertyType == typeof(DateOnly) && isDate)
            {
                expressions.Add(Expression.Equal(propExpr, Expression.Constant(searchDate, typeof(DateOnly))));
            }
            else if (prop.PropertyType == typeof(DateOnly?) && isDate)
            {
                var hasValue = Expression.Property(propExpr, nameof(Nullable<DateOnly>.HasValue));
                var value = Expression.Property(propExpr, nameof(Nullable<DateOnly>.Value));
                var equals = Expression.Equal(value, Expression.Constant(searchDate, typeof(DateOnly)));
                expressions.Add(Expression.AndAlso(hasValue, equals));
            }
            else if (prop.PropertyType == typeof(DateTime) && isDateTime)
            {
                var dateProp = Expression.Property(propExpr, nameof(DateTime.Date));
                expressions.Add(Expression.Equal(dateProp, Expression.Constant(searchDateTime.Date, typeof(DateTime))));
            }
            else if (prop.PropertyType == typeof(DateTime?) && isDateTime)
            {
                var hasValue = Expression.Property(propExpr, nameof(Nullable<DateTime>.HasValue));
                var value = Expression.Property(propExpr, nameof(Nullable<DateTime>.Value));
                var dateProp = Expression.Property(value, nameof(DateTime.Date));
                var equals = Expression.Equal(dateProp, Expression.Constant(searchDateTime.Date, typeof(DateTime)));
                expressions.Add(Expression.AndAlso(hasValue, equals));
            }
        }

        return expressions.Count > 0
            ? expressions.Aggregate(Expression.OrElse)
            : Expression.Constant(false);
    }

}