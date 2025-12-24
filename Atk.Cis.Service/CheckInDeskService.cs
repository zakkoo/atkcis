using Atk.Cis.Service.Enums;
using Atk.Cis.Service.Data;
using Atk.Cis.Service.Interfaces;
using Atk.Cis.Service.Models;

namespace Atk.Cis.Service;

public class CheckInDeskService : ICheckInDeskService
{

    private MockData _DbContext;
    private AppDbContext _realOne;

    public CheckInDeskService(AppDbContext realOne)
    {
        _realOne = realOne;
        _DbContext = MockDataLoader.LoadFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "MockDb.json"));
    }

    public async Task<string> SignUp(string firstName, string lastName, DateTimeOffset birthday)
    {
        var testUser = new User
        {
            DisplayName = "Zak attakk",
            Id = Guid.NewGuid(),
            PrimaryCode = "agz",
            PlusOneCode = "agz1"
        };
        _realOne.Users.Add(testUser);
        await _realOne.SaveChangesAsync();
        return "signed up!";
    }

    public async Task<string> CheckIn(string barcode)
    {
        var user = _DbContext.Users.SingleOrDefault(x => x.PrimaryCode.ToLower() == barcode.ToLower());
        var plusOne = _DbContext.Users.SingleOrDefault(x => x.PlusOneCode.ToLower() == barcode.ToLower());
        if (user != null)
        {
            var checkInSession = _DbContext.CheckInSessions.FirstOrDefault(x => x.Status == SessionStatus.Open && x.UserId == user.Id);
            if (checkInSession == null)
            {
                checkInSession = new CheckInSession
                {
                    SessionId = Guid.NewGuid(),
                    UserId = user.Id,
                    OpenedAt = DateTimeOffset.Now,
                };
                _DbContext.CheckInSessions.Add(checkInSession);
                return $"{user.DisplayName} checked in.";
            }
            checkInSession.Status = SessionStatus.Closed;
            checkInSession.ClosedAt = DateTimeOffset.Now;
            return $"{user.DisplayName} checked out.";
        }
        else if (plusOne != null)
        {
            var checkInSession = _DbContext.CheckInSessions.FirstOrDefault(x => x.Status == SessionStatus.Open && x.UserId == plusOne.Id);
            if (checkInSession == null)
            {
                checkInSession = new CheckInSession
                {
                    SessionId = Guid.NewGuid(),
                    UserId = plusOne.Id,
                    OpenedAt = DateTimeOffset.Now,
                    PartnerCount = 1
                };
                _DbContext.CheckInSessions.Add(checkInSession);
                return $"{user?.DisplayName} and {checkInSession.PartnerCount} friend(s) checked in.";
            }
            checkInSession.PartnerCount = checkInSession.PartnerCount + 1;
            return $"{user?.DisplayName} and {checkInSession.PartnerCount} friend(s) checked in.";
        }
        else
        {
            return "Invalid barcode. Check-in was cancelled.";
        }
    }
}
