using Atk.Cis.Service.Data;
using Atk.Cis.Service.Interfaces;
using System.IO;
using Microsoft.Data.Sqlite;
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
        {
            var databasePath = configuration["Database:Path"];

            if (!string.IsNullOrWhiteSpace(databasePath))
            {
                databasePath = Environment.ExpandEnvironmentVariables(databasePath);

                if (databasePath.StartsWith("~", StringComparison.Ordinal))
                {
                    var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var trimmedPath = databasePath.TrimStart('~', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    databasePath = Path.Combine(homeDirectory, trimmedPath);
                }
                var fullPath = Path.GetFullPath(databasePath);
                var directory = Path.GetDirectoryName(fullPath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var builder = new SqliteConnectionStringBuilder
                {
                    DataSource = fullPath
                };
                options.UseSqlite(builder.ToString());
            }
            else
            {
                throw new Exception("Check your database configuration.");
            }
        });

        return services;
    }
}
