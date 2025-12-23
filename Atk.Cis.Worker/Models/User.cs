namespace Atk.Cis.Worker.Models;

public class User
{
    public Guid Id { get; init; }
    public string? DisplayName { get; set; }
    public string PrimaryCode { get; init; } = string.Empty;
    public string PlusOneCode { get; init; } = string.Empty;
}
