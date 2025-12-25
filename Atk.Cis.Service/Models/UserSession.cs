namespace Atk.Cis.Service.Models;

public sealed class UserSession
{
    public Guid SessionId { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset OpenedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; set; }
}
