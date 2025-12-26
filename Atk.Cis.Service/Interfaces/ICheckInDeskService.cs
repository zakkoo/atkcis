using Atk.Cis.Service.Models;

namespace Atk.Cis.Service.Interfaces;

public interface ICheckInDeskService
{
    Task<string> SignUp(string firstName, string lastName, DateTimeOffset birthday);
    Task<string> CheckIn(string code);
    Task<string> CheckOut(string code);
    Task<string> CleanupStaleSessions(TimeSpan maxDuration);
    Task<string> GetBarcode(string firstName, string lastName, DateTimeOffset birthday);
    Task<List<User>> GetUsers();
}
