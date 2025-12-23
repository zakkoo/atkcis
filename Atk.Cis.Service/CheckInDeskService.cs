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


    public string CheckInOrOut(string barcode)
    {
        // returns user checked in
        //
        // returns user checked out
        //
        // returns user plus one checked in
        //
        // returns unknown barcode


        var test1 = _DbContext.Users.FirstOrDefault()?.DisplayName;

        return test1 ?? "N/A";

    }

    public async Task<string> CheckIn(string barcode)
    {
        return null;
    }
}
