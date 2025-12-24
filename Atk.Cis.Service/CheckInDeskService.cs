using Atk.Cis.Service.Enums;
using Atk.Cis.Service.Data;
using Atk.Cis.Service.Interfaces;
using Atk.Cis.Service.Models;

namespace Atk.Cis.Service;

public class CheckInDeskService : ICheckInDeskService
{

    private AppDbContext _dbContext;

    public CheckInDeskService(AppDbContext realOne)
    {
        _dbContext = realOne;
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

    public async Task<string> CheckIn(string barcode)
    {
        var user = _dbContext.Users.SingleOrDefault(x => x.Code == barcode.ToLower());

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
        checkInSession.Status = SessionStatus.Closed;
        checkInSession.ClosedAt = DateTimeOffset.Now;
        _ = _dbContext.SaveChangesAsync();
        return $"{user.FirstName} {user.LastName} checked out.";
    }

    private string GenerateCode(string firstName, string lastName)
    {
        var code = (lastName[0..2] + firstName[0]).ToLower();

        if (!_dbContext.Users.Any(x => x.Code == code)) return code;

        var counter = 1;
        code = code + counter;

        while (_dbContext.Users.Any(x => x.Code == code))
        {
            counter++;
            code = code[0..2] + counter;
        }
        return code;
    }
}
