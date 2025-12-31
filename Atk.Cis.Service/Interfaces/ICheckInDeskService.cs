using Atk.Cis.Service.Models;
using Atk.Cis.Service.Dtos;
namespace Atk.Cis.Service.Interfaces;

public interface ICheckInDeskService
{
    Task<bool> IsCheckedIn(string code, CancellationToken cancellationToken = default);
    Task<string> SignUp(string firstName, string lastName, DateTimeOffset birthday, CancellationToken cancellationToken = default);
    Task<string> CheckIn(string code, CancellationToken cancellationToken = default);
    Task<string> CheckOut(string code, CancellationToken cancellationToken = default);
    Task<string> CleanupStaleSessions(TimeSpan maxDuration, CancellationToken cancellationToken = default);
    Task<string> GetBarcode(string firstName, string lastName, DateTimeOffset birthday, CancellationToken cancellationToken = default);
    Task<List<User>> GetUsers(CancellationToken cancellationToken = default);
    Task<List<UserSessionDto>> GetUserSessions(CancellationToken cancellationToken = default);
}
