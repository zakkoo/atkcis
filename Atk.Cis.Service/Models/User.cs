namespace Atk.Cis.Service.Models;

public class User
{
    public Guid Id { get; init; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTimeOffset? Birthday { get; set; }
    public string? Code { get; init; }
}
