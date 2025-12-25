using Atk.Cis.Service.Enums;
using Atk.Cis.Service.Data;
using Atk.Cis.Service.Interfaces;
using Atk.Cis.Service.Models;

namespace Atk.Cis.Service;

public class CheckInDeskService : ICheckInDeskService
{

    private AppDbContext _dbContext;

    public CheckInDeskService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> CleanupStaleSessions(TimeSpan maxDuration)
    {
        var cutoff = DateTime.UtcNow - maxDuration;

        var sessionsToClose = _dbContext.CheckInSessions
            .Where(s =>
                s.ClosedAt == null &&
                s.OpenedAt <= cutoff)
            .ToList();

        foreach (var session in sessionsToClose)
        {
            session.Status = SessionStatus.Closed;
            session.ClosedAt = DateTimeOffset.Now;
        }

        _ = _dbContext.SaveChangesAsync();

        return $"Cleaned up {sessionsToClose.Count()} stale sessions.";
    }

    public async Task<string> SignUp(string firstName, string lastName, DateTimeOffset birthday)
    {
        var code = GenerateCode(firstName, lastName);
        var testUser = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Id = Guid.NewGuid(),
            Code = code,
        };
        _dbContext.Users.Add(testUser);
        await _dbContext.SaveChangesAsync();
        return "signed up!";
    }

    public async Task<string> CheckIn(string code)
    {
        var user = _dbContext.Users.SingleOrDefault(x => x.Code == code.ToLowerInvariant());

        if (user == null) return "Invalid barcode. Check-in was cancelled.";

        var checkInSession = _dbContext.CheckInSessions.FirstOrDefault(x => x.Status == SessionStatus.Open && x.UserId == user.Id);
        if (checkInSession == null)
        {
            checkInSession = new CheckInSession
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
        var checkInSession = _dbContext.CheckInSessions.FirstOrDefault(x => x.Status == SessionStatus.Open && x.UserId == user.Id);
        if (checkInSession == null) return $"Check-out failed for {user.FirstName} {user.LastName} because there is no open session.";
        checkInSession.Status = SessionStatus.Closed;
        checkInSession.ClosedAt = DateTimeOffset.Now;
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
