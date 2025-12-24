using Atk.Cis.Service.Models;
using Microsoft.EntityFrameworkCore;

namespace Atk.Cis.Service.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<CheckInSession> CheckInSessions => Set<CheckInSession>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Optional: better naming for SQLite
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("Users");

        modelBuilder.Entity<CheckInSession>()
                  .ToTable("CheckInSessions")
                  .HasKey(x => x.SessionId);

        base.OnModelCreating(modelBuilder);
    }
}
