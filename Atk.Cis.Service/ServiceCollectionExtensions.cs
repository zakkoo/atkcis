using Atk.Cis.Service.Data;
using Atk.Cis.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atk.Cis.Service;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCisServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICheckInDeskService, CheckInDeskService>();
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}
