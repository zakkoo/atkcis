using Atk.Cis.Service.Enums;
using Atk.Cis.Service.Data;
using Atk.Cis.Service.Interfaces;
using Atk.Cis.Service.Models;

namespace Atk.Cis.Service;

public class CheckInDeskService : ICheckInDeskService
{

    private MockData _DbContext;

    public CheckInDeskService()
    {
        _DbContext = MockDataLoader.LoadFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "MockDb.json"));
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
            return CheckInPlusOne(plusOne);
        }
        else
        {
            return "Invalid barcode. Check-in was cancelled.";
        }
    }

    private string CheckInUser(User user)
    {

        return null;
    }
    private string CheckInPlusOne(User plusOne) { return null; }

}
