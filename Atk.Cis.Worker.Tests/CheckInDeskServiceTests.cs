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

    [Fact]
    public async Task GetUsers_ReturnsAllUsers()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Margaret",
                LastName = "Hamilton",
                Birthday = new DateTimeOffset(1936, 8, 17, 0, 0, 0, TimeSpan.Zero),
                Code = "ham",
            },
            new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Donald",
                LastName = "Knuth",
                Birthday = new DateTimeOffset(1938, 1, 10, 0, 0, 0, TimeSpan.Zero),
                Code = "knd",
            });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var users = await service.GetUsers();

        Assert.Equal(2, users.Count);
        Assert.Contains(users, user => user.Code == "ham");
        Assert.Contains(users, user => user.Code == "knd");
    }

    [Fact]
    public async Task GetUserSessions_ReturnsSessionsWithDisplayNameAndClosedBy()
    {
        var userId = Guid.NewGuid();
        var openedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var closedAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = userId,
            FirstName = "Radia",
            LastName = "Perlman",
            Birthday = new DateTimeOffset(1951, 12, 18, 0, 0, 0, TimeSpan.Zero),
            Code = "per",
        });
        context.UserSessions.Add(new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            OpenedAt = openedAt,
            ClosedAt = closedAt,
            ClosedBy = ClosedByType.Worker,
        });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var sessions = await service.GetUserSessions();

        var session = Assert.Single(sessions);
        Assert.Equal(openedAt, session.OpenedAt);
        Assert.Equal(closedAt, session.ClosedAt);
        Assert.Equal("Radia Perlman", session.UserDisplayName);
        Assert.Equal(ClosedByType.Worker.ToString(), session.ClosedBy);
    }

    [Fact]
    public async Task SignUp_CreatesUserAndReturnsBarcodeSvg()
    {
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Lovelace",
            Birthday = new DateTimeOffset(1815, 12, 10, 0, 0, 0, TimeSpan.Zero),
            Code = "loa",
        });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.SignUp(
            "Alan",
            "Lovelace",
            new DateTimeOffset(1912, 6, 23, 0, 0, 0, TimeSpan.Zero));

        var createdUser = await context.Users.SingleAsync(user => user.FirstName == "Alan");
        Assert.Equal("loa1", createdUser.Code);
        Assert.Contains("<svg", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetBarcode_ReturnsFriendlyMessage_WhenUserMissing()
    {
        await using var context = CreateContext();
        var service = new CheckInDeskService(context);

        var result = await service.GetBarcode(
            "Unknown",
            "User",
            new DateTimeOffset(1990, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal("We couldn't find a match for those details. Please check and try again.", result);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"check-in-desk-tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}
