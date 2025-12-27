using Atk.Cis.Service;
using Atk.Cis.Service.Data;
using Atk.Cis.Service.Enums;
using Atk.Cis.Service.Models;
using Microsoft.EntityFrameworkCore;

namespace Atk.Cis.Worker.Tests;

public class CheckInDeskServiceTests
{
    [Fact]
    public async Task CheckIn_CreatesSession_WhenUserHasNoOpenSession()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = userId,
            FirstName = "Ada",
            LastName = "Lovelace",
            Birthday = new DateTimeOffset(1815, 12, 10, 0, 0, 0, TimeSpan.Zero),
            Code = "loa",
        });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.CheckIn("loa");

        var session = await context.UserSessions.SingleAsync();
        Assert.Equal(userId, session.UserId);
        Assert.Null(session.ClosedAt);
        Assert.Equal("Ada Lovelace checked in.", result);
    }

    [Fact]
    public async Task CheckOut_ClosesOpenSession_ForUser()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = userId,
            FirstName = "Grace",
            LastName = "Hopper",
            Birthday = new DateTimeOffset(1906, 12, 9, 0, 0, 0, TimeSpan.Zero),
            Code = "hog",
        });
        context.UserSessions.Add(new UserSession
        {
            SessionId = sessionId,
            UserId = userId,
            OpenedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.CheckOut("hog");

        var session = await context.UserSessions.SingleAsync();
        Assert.NotNull(session.ClosedAt);
        Assert.Equal(ClosedByType.User, session.ClosedBy);
        Assert.Equal("Grace Hopper checked out.", result);
    }

    [Fact]
    public async Task CleanupStaleSessions_ClosesSessions_OpenedBeforeCutoff()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = userId,
            FirstName = "Katherine",
            LastName = "Johnson",
            Birthday = new DateTimeOffset(1918, 8, 26, 0, 0, 0, TimeSpan.Zero),
            Code = "jok",
        });
        context.UserSessions.AddRange(
            new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = userId,
                OpenedAt = DateTimeOffset.UtcNow.AddHours(-4),
            },
            new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = userId,
                OpenedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.CleanupStaleSessions(TimeSpan.FromHours(1));

        var sessions = await context.UserSessions.OrderBy(session => session.OpenedAt).ToListAsync();
        Assert.NotNull(sessions[0].ClosedAt);
        Assert.Equal(ClosedByType.Worker, sessions[0].ClosedBy);
        Assert.Null(sessions[1].ClosedAt);
        Assert.Equal("Cleaned up 1 stale sessions.", result);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"check-in-desk-tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}
