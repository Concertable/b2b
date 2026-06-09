using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.DataAccess.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReadDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ReadDbContext>(opt =>
            opt.UseSqlServer(
                configuration.GetConnectionString("B2BDb"),
                sqlOpt => sqlOpt.UseNetTopologySuite()));
        services.AddScoped<IReadDbContext, ReadDbContext>();
        services.AddDataAccessSpecifications();
        return services;
    }
}
