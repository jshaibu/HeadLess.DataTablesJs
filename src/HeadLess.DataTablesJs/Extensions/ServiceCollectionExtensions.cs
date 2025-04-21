using HeadLess.DataTablesJs.Interfaces;
using HeadLess.DataTablesJs.Core;
using Microsoft.Extensions.DependencyInjection;

namespace HeadLess.DataTables.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHeadLessDataTables(this IServiceCollection services)
    {
        services.AddScoped<IDataTablesList, DataTablesList>();
        services.AddScoped<IDataTablesQuery, DataTablesQuery>();
        services.AddScoped<IDataTablesService, DataTablesService>();
        return services;
    }
}
