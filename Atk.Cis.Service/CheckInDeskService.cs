using Atk.Cis.Service.Data;
using Atk.Cis.Service.Interfaces;

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
        return $"{user?.DisplayName} checked in.";
    }
}
