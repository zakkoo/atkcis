using Atk.Cis.Service;
using Atk.Cis.Service.Data;
using Atk.Cis.Service.Enums;
using Atk.Cis.Service.Models;
using Microsoft.EntityFrameworkCore;

namespace Atk.Cis.Service.Tests;

public class CheckInDeskServiceTests
{
    // -------------------------------------------------------------------------
    // CheckIn
    // -------------------------------------------------------------------------

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
        Assert.Equal("Check-in complete for Ada Lovelace.", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CheckIn_ReturnsError_WhenCodeIsNullOrEmpty(string? code)
    {
        await using var context = CreateContext();
        var service = new CheckInDeskService(context);

        var result = await service.CheckIn(code!);

        Assert.Equal("That barcode isn't valid. Check-in was not completed.", result);
        Assert.Empty(context.UserSessions);
    }

    [Fact]
    public async Task CheckIn_ReturnsError_WhenCodeNotFound()
    {
        await using var context = CreateContext();
        var service = new CheckInDeskService(context);

        var result = await service.CheckIn("zzz");

        Assert.Equal("That barcode isn't valid. Check-in was not completed.", result);
    }

    [Fact]
    public async Task CheckIn_ReturnsAlreadyCheckedIn_WhenOpenSessionExists()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = userId,
            FirstName = "Alan",
            LastName = "Turing",
            Birthday = new DateTimeOffset(1912, 6, 23, 0, 0, 0, TimeSpan.Zero),
            Code = "tua",
        });
        context.UserSessions.Add(new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            OpenedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
        });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.CheckIn("tua");

        Assert.Equal("Alan Turing already checked-in", result);
        Assert.Single(context.UserSessions); // no new session created
    }

    [Fact]
    public async Task CheckIn_IsCaseInsensitive()
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

        var result = await service.CheckIn("LOA");

        Assert.Equal("Check-in complete for Ada Lovelace.", result);
    }

    // -------------------------------------------------------------------------
    // CheckOut
    // -------------------------------------------------------------------------

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
        Assert.Equal("Check-out complete for Grace Hopper.", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CheckOut_ReturnsError_WhenCodeIsNullOrEmpty(string? code)
    {
        await using var context = CreateContext();
        var service = new CheckInDeskService(context);

        var result = await service.CheckOut(code!);

        Assert.Equal("We couldn't check out with that input. Please try again.", result);
    }

    [Fact]
    public async Task CheckOut_ReturnsError_WhenCodeNotFound()
    {
        await using var context = CreateContext();
        var service = new CheckInDeskService(context);

        var result = await service.CheckOut("zzz");

        Assert.Equal("That barcode isn't valid. Check-out was not completed.", result);
    }

    [Fact]
    public async Task CheckOut_ReturnsError_WhenNoOpenSession()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = userId,
            FirstName = "Grace",
            LastName = "Hopper",
            Birthday = new DateTimeOffset(1906, 12, 9, 0, 0, 0, TimeSpan.Zero),
            Code = "hog",
        });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.CheckOut("hog");

        Assert.Equal("No open session for Grace Hopper. Check-out was not completed.", result);
    }

    // -------------------------------------------------------------------------
    // CleanupStaleSessions
    // -------------------------------------------------------------------------

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
    public async Task CleanupStaleSessions_ReturnsNoStale_WhenAllSessionsAreRecent()
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
        context.UserSessions.Add(new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            OpenedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.CleanupStaleSessions(TimeSpan.FromHours(1));

        Assert.Equal("No stale sessions found.", result);
        var session = await context.UserSessions.SingleAsync();
        Assert.Null(session.ClosedAt);
    }

    [Fact]
    public async Task CleanupStaleSessions_SkipsAlreadyClosedSessions()
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
        var closedAt = DateTimeOffset.UtcNow.AddMinutes(-30);
        context.UserSessions.Add(new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            OpenedAt = DateTimeOffset.UtcNow.AddHours(-4),
            ClosedAt = closedAt,
            ClosedBy = ClosedByType.User,
        });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.CleanupStaleSessions(TimeSpan.FromHours(1));

        Assert.Equal("No stale sessions found.", result);
        var session = await context.UserSessions.SingleAsync();
        Assert.Equal(closedAt, session.ClosedAt); // unchanged
    }

    // -------------------------------------------------------------------------
    // IsCheckedIn
    // -------------------------------------------------------------------------

    [Fact]
    public async Task IsCheckedIn_ReturnsTrue_WhenOpenSessionExists()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = userId,
            FirstName = "Alan",
            LastName = "Turing",
            Birthday = new DateTimeOffset(1912, 6, 23, 0, 0, 0, TimeSpan.Zero),
            Code = "tua",
        });
        context.UserSessions.Add(new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            OpenedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
        });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.IsCheckedIn("tua", default);

        Assert.True(result);
    }

    [Fact]
    public async Task IsCheckedIn_ReturnsFalse_WhenNoOpenSession()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = userId,
            FirstName = "Alan",
            LastName = "Turing",
            Birthday = new DateTimeOffset(1912, 6, 23, 0, 0, 0, TimeSpan.Zero),
            Code = "tua",
        });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.IsCheckedIn("tua", default);

        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task IsCheckedIn_Throws_WhenCodeIsNullOrEmpty(string? code)
    {
        await using var context = CreateContext();
        var service = new CheckInDeskService(context);

        await Assert.ThrowsAsync<Exception>(() => service.IsCheckedIn(code!, default));
    }

    [Fact]
    public async Task IsCheckedIn_Throws_WhenCodeNotFound()
    {
        await using var context = CreateContext();
        var service = new CheckInDeskService(context);

        await Assert.ThrowsAsync<Exception>(() => service.IsCheckedIn("zzz", default));
    }

    // -------------------------------------------------------------------------
    // SignUp
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SignUp_ReturnsBarcodeSvg_WhenNewUser()
    {
        await using var context = CreateContext();
        var service = new CheckInDeskService(context);

        var result = await service.SignUp("Ada", "Lovelace", new DateTimeOffset(1815, 12, 10, 0, 0, 0, TimeSpan.Zero));

        Assert.Contains("<svg", result);
        var user = await context.Users.SingleAsync();
        Assert.Equal("loa", user.Code);
    }

    [Fact]
    public async Task SignUp_ReturnsError_WhenUserAlreadyExists()
    {
        var birthday = new DateTimeOffset(1815, 12, 10, 0, 0, 0, TimeSpan.Zero);
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Lovelace",
            Birthday = birthday,
            Code = "loa",
        });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.SignUp("Ada", "Lovelace", birthday);

        Assert.Equal("That user already exists.", result);
        Assert.Single(context.Users);
    }

    [Fact]
    public async Task SignUp_AppendsDigit_OnCodeCollision()
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

        var result = await service.SignUp("Alice", "Longfellow", new DateTimeOffset(1990, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Contains("<svg", result);
        var newUser = await context.Users.SingleAsync(u => u.FirstName == "Alice");
        Assert.Equal("loa1", newUser.Code);
    }

    [Fact]
    public async Task SignUp_AppendsIncrementingDigit_WhenMultipleCollisions()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Ada",
                LastName = "Lovelace",
                Birthday = new DateTimeOffset(1815, 12, 10, 0, 0, 0, TimeSpan.Zero),
                Code = "loa",
            },
            new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Alice",
                LastName = "Longfellow",
                Birthday = new DateTimeOffset(1990, 1, 1, 0, 0, 0, TimeSpan.Zero),
                Code = "loa1",
            });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.SignUp("Anna", "Lombard", new DateTimeOffset(1985, 3, 15, 0, 0, 0, TimeSpan.Zero));

        Assert.Contains("<svg", result);
        var newUser = await context.Users.SingleAsync(u => u.FirstName == "Anna");
        Assert.Equal("loa2", newUser.Code);
    }

    [Fact]
    public async Task SignUp_StripsDiacritics_FromName()
    {
        await using var context = CreateContext();
        var service = new CheckInDeskService(context);

        var result = await service.SignUp("Élodie", "Hébert", new DateTimeOffset(1990, 5, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Contains("<svg", result);
        var user = await context.Users.SingleAsync();
        Assert.Equal("hee", user.Code);
    }

    // -------------------------------------------------------------------------
    // GetBarcode
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetBarcode_ReturnsSvgString_WhenUserFound()
    {
        var birthday = new DateTimeOffset(1815, 12, 10, 0, 0, 0, TimeSpan.Zero);
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Lovelace",
            Birthday = birthday,
            Code = "loa",
        });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.GetBarcode("Ada", "Lovelace", birthday);

        Assert.Contains("<svg", result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetBarcode_ReturnsError_WhenUserNotFound()
    {
        await using var context = CreateContext();
        var service = new CheckInDeskService(context);

        var result = await service.GetBarcode("Nobody", "Here", DateTimeOffset.UtcNow);

        Assert.Equal("We couldn't find a match for those details. Please check and try again.", result);
    }

    // -------------------------------------------------------------------------
    // GetUsers
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUsers_ReturnsAllUsers()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Ada",
                LastName = "Lovelace",
                Birthday = new DateTimeOffset(1815, 12, 10, 0, 0, 0, TimeSpan.Zero),
                Code = "loa",
            },
            new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Grace",
                LastName = "Hopper",
                Birthday = new DateTimeOffset(1906, 12, 9, 0, 0, 0, TimeSpan.Zero),
                Code = "hog",
            });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.GetUsers();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Code == "loa");
        Assert.Contains(result, u => u.Code == "hog");
    }

    [Fact]
    public async Task GetUsers_ReturnsEmpty_WhenNoUsers()
    {
        await using var context = CreateContext();
        var service = new CheckInDeskService(context);

        var result = await service.GetUsers();

        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // GetUserSessions
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUserSessions_ReturnsSessionsWithDisplayName()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var openedAt = DateTimeOffset.UtcNow.AddMinutes(-30);
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = userId,
            FirstName = "Ada",
            LastName = "Lovelace",
            Birthday = new DateTimeOffset(1815, 12, 10, 0, 0, 0, TimeSpan.Zero),
            Code = "loa",
        });
        context.UserSessions.Add(new UserSession
        {
            SessionId = sessionId,
            UserId = userId,
            OpenedAt = openedAt,
        });
        await context.SaveChangesAsync();

        var service = new CheckInDeskService(context);

        var result = await service.GetUserSessions();

        Assert.Single(result);
        var dto = result[0];
        Assert.Equal(sessionId, dto.SessionId);
        Assert.Equal("Ada Lovelace", dto.UserDisplayName);
        Assert.Equal(openedAt, dto.OpenedAt);
        Assert.Null(dto.ClosedAt);
    }

    [Fact]
    public async Task GetUserSessions_ReturnsEmpty_WhenNoSessions()
    {
        await using var context = CreateContext();
        var service = new CheckInDeskService(context);

        var result = await service.GetUserSessions();

        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"check-in-desk-tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}
