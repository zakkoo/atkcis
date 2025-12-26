using Atk.Cis.Service.Data;
using Atk.Cis.Service.Interfaces;
using Atk.Cis.Service.Models;
using Barcoder.Code128;
using Barcoder.Renderer.Svg;
using Microsoft.EntityFrameworkCore;
using Atk.Cis.Service.Enums;

namespace Atk.Cis.Service;

public class CheckInDeskService : ICheckInDeskService
{

    private AppDbContext _dbContext;

    public CheckInDeskService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<List<User>> GetUsers()
    {
        return await _dbContext.Users.ToListAsync();
    }
    public async Task<string> CleanupStaleSessions(TimeSpan maxDuration)
    {
        var cutoff = DateTimeOffset.UtcNow - maxDuration;

        var sessionsToClose = (await _dbContext.CheckInSessions
                        .Where(session => session.ClosedAt == null)
                        .ToListAsync())
                    .Where(session => session.OpenedAt <= cutoff)
                    .ToList();
        if (sessionsToClose.Count == 0)
        {
            return "No stale sessions found.";
        }

        foreach (var session in sessionsToClose)
        {
            session.ClosedAt = DateTimeOffset.UtcNow;
            session.ClosedBy = ClosedByType.Worker;
        }

        await _dbContext.SaveChangesAsync();

        return $"Cleaned up {sessionsToClose.Count} stale sessions.";
    }

    public async Task<string> GetBarcode(string firstName, string lastName, DateTimeOffset birthday)
    {
        var user = _dbContext.Users.FirstOrDefault(x => x.FirstName != null && x.FirstName.ToLower() == firstName.ToLower() &&
                x.LastName != null && lastName.ToLower() == x.LastName.ToLower() &&
                x.Birthday == birthday);
        if (user == null) return string.Empty;
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

    public async Task<string> SignUp(string firstName, string lastName, DateTimeOffset birthday)
    {
        var code = GenerateCode(firstName, lastName);
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Birthday = birthday,
            Id = Guid.NewGuid(),
            Code = code,
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return await GetBarcode(user.FirstName, user.LastName, user.Birthday.GetValueOrDefault());
    }

    public async Task<string> CheckIn(string code)
    {
        var user = _dbContext.Users.SingleOrDefault(x => x.Code == code.ToLowerInvariant());

        if (user == null) return "Invalid barcode. Check-in was cancelled.";

        var checkInSession = _dbContext.CheckInSessions.FirstOrDefault(x => x.UserId == user.Id && x.ClosedAt == null);
        if (checkInSession == null)
        {
            checkInSession = new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = user.Id,
                OpenedAt = DateTimeOffset.Now,
            };
            _dbContext.CheckInSessions.Add(checkInSession);
            _ = _dbContext.SaveChangesAsync();
            return $"{user.FirstName} {user.LastName} checked in.";
        }
        return await CheckOut(code);
    }

    public async Task<string> CheckOut(string code)
    {
        var user = _dbContext.Users.SingleOrDefault(x => x.Code == code.ToLowerInvariant());
        if (user == null) return "Invalid barcode. Check-out was cancelled.";
        var checkInSession = _dbContext.CheckInSessions.FirstOrDefault(x => x.UserId == user.Id && x.ClosedAt == null);
        if (checkInSession == null) return $"Check-out failed for {user.FirstName} {user.LastName} because there is no open session.";
        checkInSession.ClosedAt = DateTimeOffset.Now;
        checkInSession.ClosedBy = ClosedByType.User;
        _ = _dbContext.SaveChangesAsync();
        return $"{user.FirstName} {user.LastName} checked out.";
    }


    private string GenerateCode(string firstName, string lastName)
    {
        var prefix = (lastName[..2] + firstName[0]).ToLowerInvariant();

        if (!_dbContext.Users.Any(x => x.Code == prefix))
            return prefix;

        var counter = 1;
        var candidate = prefix + counter;

        while (_dbContext.Users.Any(x => x.Code == candidate))
        {
            counter++;
            candidate = prefix + counter;
        }

        return candidate;
    }
}
