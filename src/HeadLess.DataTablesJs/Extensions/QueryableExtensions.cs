using System.Linq.Dynamic.Core;
using HeadLess.DataTablesJs.Models;
using System.Linq.Expressions;
using System.Reflection;

namespace HeadLess.DataTablesJs.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> query,
        DataTablesRequest request
    ) where T : class
    {
        if (request.Order == null || !request.Order.Any())
            return query;

        var firstOrder = request.Order.First();
        var column = request.Columns.ElementAtOrDefault(firstOrder.Column);

        // 1️⃣ Honor front‑end’s “orderable” flag
        if (column == null || column.Orderable == false)
            return query;

        var columnName = column.Data;
        if (string.IsNullOrWhiteSpace(columnName))
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        bool ascending = string.Equals(firstOrder.Dir ?? "asc", "asc", StringComparison.OrdinalIgnoreCase);

        // 2️⃣ Special case: FullName => FirstName + " " + LastName/Surname
        if (columnName.Equals("FullName", StringComparison.OrdinalIgnoreCase))
        {
            // Find the two name properties
            var firstNameProp = typeof(T).GetProperty("FirstName",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var lastNameProp = typeof(T).GetProperty("LastName",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                                ?? typeof(T).GetProperty("Surname",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (firstNameProp == null || lastNameProp == null)
                return query; // can't build full name

            // Build expressions for x.FirstName and x.LastName
            var firstNameExpr = Expression.Property(parameter, firstNameProp);
            var lastNameExpr = Expression.Property(parameter, lastNameProp);
            var spaceExpr = Expression.Constant(" ");

            // 🔧 **Disambiguate** the string.Concat overload:
            // string.Concat(string, string) — otherwise EF/Core sees many overloads :contentReference[oaicite:0]{index=0}
            var concat2 = typeof(string).GetMethod(
                nameof(string.Concat),
                new[] { typeof(string), typeof(string) }
            );
            if (concat2 == null)
                throw new InvalidOperationException("Couldn't find string.Concat(string, string) overload.");

            // firstName + " "
            var concatFirstSpace = Expression.Call(concat2, firstNameExpr, spaceExpr);
            // (firstName + " ") + lastName
            var fullNameExpr = Expression.Call(concat2, concatFirstSpace, lastNameExpr);

            var lambda = Expression.Lambda(fullNameExpr, parameter);
            var methodName = ascending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending);

            // Build the Queryable.OrderBy(x => x.FirstName + " " + x.LastName)
            var resultExpr = Expression.Call(
                typeof(Queryable),
                methodName,
                new[] { typeof(T), typeof(string) },
                query.Expression,
                Expression.Quote(lambda)
            );

            return query.Provider.CreateQuery<T>(resultExpr);
        }

        // 3️⃣ General case: single-property sort
        var propInfo = typeof(T).GetProperty(
            columnName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
        );
        if (propInfo == null)
            return query;

        var propertyAccess = Expression.Property(parameter, propInfo);
        var lambdaExpr = Expression.Lambda(propertyAccess, parameter);
        var sortMethod = ascending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending);

        var sortedExpr = Expression.Call(
            typeof(Queryable),
            sortMethod,
            new[] { typeof(T), propInfo.PropertyType },
            query.Expression,
            Expression.Quote(lambdaExpr)
        );

        return query.Provider.CreateQuery<T>(sortedExpr);
    }
}
