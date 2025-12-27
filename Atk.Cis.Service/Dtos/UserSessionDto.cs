
namespace Atk.Cis.Service.Dtos;

public sealed class UserSessionDto
{
    public Guid SessionId { get; init; }
    public string? UserDisplayName { get; init; }
    public DateTimeOffset OpenedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public TimeSpan? Duration =>
        ClosedAt.HasValue ? ClosedAt.Value - OpenedAt : null;
    public string Status =>
        ClosedAt.HasValue ? "Closed" : "Active";
    public string? ClosedBy { get; init; } // or enum/string mapping
}
