using Atk.Cis.Service.Enums;

namespace Atk.Cis.Service.Models;

public sealed class CheckInSession
{
    public Guid SessionId { get; init; }
    public Guid UserId { get; init; }

    public SessionStatus Status { get; set; } = SessionStatus.Open;

    public DateTimeOffset OpenedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; set; }

    public int PartnerCount { get; set; }
}
