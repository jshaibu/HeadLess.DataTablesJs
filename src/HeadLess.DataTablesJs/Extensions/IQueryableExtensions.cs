using System.Linq.Dynamic.Core;
using HeadLess.DataTablesJs.Models;
using System.Linq.Expressions;
using System.Reflection;

namespace HeadLess.DataTablesJs.Extensions;

public static class IQueryableExtensions
{
    public static IQueryable<T> OrderByDynamic<T>(this IQueryable<T> query, string orderByMember, bool ascending)
    {
        var parameter = Expression.Parameter(typeof(T), "p");
        var property = Expression.PropertyOrField(parameter, orderByMember);
        var orderByExpression = Expression.Lambda(property, parameter);

        string methodName = ascending ? "OrderBy" : "OrderByDescending";

        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            new Type[] { typeof(T), property.Type },
            query.Expression,
            Expression.Quote(orderByExpression));

        return query.Provider.CreateQuery<T>(resultExpression);
    }
}