using System.Globalization;
using System.Text;
using Atk.Cis.Service.Enums;
using Atk.Cis.Service.Data;
using Atk.Cis.Service.Dtos;
using Atk.Cis.Service.Interfaces;
using Atk.Cis.Service.Models;
using Barcoder.Code128;
using Barcoder.Renderer.Svg;
using Microsoft.EntityFrameworkCore;

namespace Atk.Cis.Service;

public class CheckInDeskService : ICheckInDeskService
{

    private readonly AppDbContext _dbContext;

    public CheckInDeskService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<List<UserSessionDto>> GetUserSessions(CancellationToken cancellationToken = default)
    {
        return await (from session in _dbContext.UserSessions.AsNoTracking()
                      join user in _dbContext.Users.AsNoTracking() on session.UserId equals user.Id into userGroup
                      from user in userGroup.DefaultIfEmpty()
                      select new UserSessionDto
                      {
                          SessionId = session.SessionId,
                          UserDisplayName = $"{user.FirstName} {user.LastName}",
                          ClosedAt = session.ClosedAt,
                          OpenedAt = session.OpenedAt,
                          ClosedBy = session.ClosedBy.ToString(),
                      }).ToListAsync(cancellationToken);
    }
    public async Task<List<User>> GetUsers(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.AsNoTracking().ToListAsync(cancellationToken);
    }
    public async Task<string> CleanupStaleSessions(TimeSpan maxDuration, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.Now - maxDuration;

        var sessionsToClose = (await _dbContext.UserSessions
                                .Where(session => session.ClosedAt == null)
                                .ToListAsync(cancellationToken))
                                .Where(session => session.OpenedAt <= cutoff)
                                .ToList();

        if (sessionsToClose.Count == 0)
        {
            return "No stale sessions found.";
        }

        foreach (var session in sessionsToClose)
        {
            session.ClosedAt = DateTimeOffset.Now;
            session.ClosedBy = ClosedByType.Worker;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return $"Cleaned up {sessionsToClose.Count} stale sessions.";
    }

    public async Task<string> GetBarcode(string firstName, string lastName, DateTimeOffset birthday, CancellationToken cancellationToken = default)
    {
        var user = await GetUser(firstName, lastName, birthday, cancellationToken);
        if (user == null) return "We couldn't find a match for those details. Please check and try again.";
        var barcode = Code128Encoder.Encode(user.Code);
        var renderer = new SvgRenderer();
        using (var stream = new MemoryStream())
        using (var reader = new StreamReader(stream))
        {
            renderer.Render(barcode, stream);
            stream.Position = 0;

            string svg = reader.ReadToEnd();
            return svg;
        }
    }

    public async Task<string> SignUp(string firstName, string lastName, DateTimeOffset birthday, CancellationToken cancellationToken = default)
    {
        var user = await GetUser(firstName, lastName, birthday, cancellationToken);
        if (user != null) return "That user already exists.";

        var code = await GenerateCode(firstName, lastName, cancellationToken);

        if (code == null) return "We couldn't complete the sign-up. Please review the details and try again.";
        user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Birthday = birthday,
            Id = Guid.NewGuid(),
            Code = code,
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetBarcode(user.FirstName, user.LastName, user.Birthday.GetValueOrDefault(), cancellationToken);
    }

    public async Task<string> CheckIn(string code, CancellationToken cancellationToken = default)
    {
        var errorMessage = "That barcode isn't valid. Check-in was not completed.";
        if (string.IsNullOrEmpty(code)) return errorMessage;

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(x => x.Code == code.ToLowerInvariant(), cancellationToken);

        if (user == null) return errorMessage;

        var checkInSession = await _dbContext.UserSessions
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.ClosedAt == null, cancellationToken);
        if (checkInSession == null)
        {
            checkInSession = new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = user.Id,
                OpenedAt = DateTimeOffset.Now,
            };
            _dbContext.UserSessions.Add(checkInSession);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Check-in complete for {user.FirstName} {user.LastName}.";
        }
        return $"{user.FirstName} {user.LastName} already checked-in";
    }

    public async Task<bool> IsCheckedIn(string code, CancellationToken cancellationToken)
    {
        var errorMessage = "That barcode isn't valid";
        if (string.IsNullOrEmpty(code)) throw new Exception(errorMessage);
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(x => x.Code == code.ToLowerInvariant(), cancellationToken);
        if (user == null) throw new Exception(errorMessage);
        return await _dbContext.UserSessions
            .AnyAsync(x => x.UserId == user.Id && x.ClosedAt == null, cancellationToken);
    }

    public async Task<string> CheckOut(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(code)) return "We couldn't check out with that input. Please try again.";
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(x => x.Code == code.ToLowerInvariant(), cancellationToken);
        if (user == null) return "That barcode isn't valid. Check-out was not completed.";
        var checkInSession = await _dbContext.UserSessions
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.ClosedAt == null, cancellationToken);
        if (checkInSession == null) return $"No open session for {user.FirstName} {user.LastName}. Check-out was not completed.";
        checkInSession.ClosedAt = DateTimeOffset.Now;
        checkInSession.ClosedBy = ClosedByType.User;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return $"Check-out complete for {user.FirstName} {user.LastName}.";
    }

    private Task<User?> GetUser(string firstName, string lastName, DateTimeOffset birthday, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
        {
            return Task.FromResult<User?>(null);
        }
        var normalizedFirstName = firstName.ToLowerInvariant();
        var normalizedLastName = lastName.ToLowerInvariant();
        return _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(
            x => x.FirstName != null && x.FirstName.ToLower() == normalizedFirstName &&
                x.LastName != null && x.LastName.ToLower() == normalizedLastName &&
                x.Birthday == birthday,
            cancellationToken);
    }

    private async Task<string?> GenerateCode(string firstName, string lastName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName)) return null;
        var prefix = NormalizeString((lastName[..2] + firstName[0]).ToLowerInvariant());

        if (!await _dbContext.Users.AnyAsync(x => x.Code == prefix, cancellationToken))
            return prefix;

        var counter = 1;
        var candidate = prefix + counter;

        while (await _dbContext.Users.AnyAsync(x => x.Code == candidate, cancellationToken))
        {
            counter++;
            candidate = prefix + counter;
        }

        return candidate;
    }

    private static string NormalizeString(string input)
    {
        string normalized = input.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();
        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
