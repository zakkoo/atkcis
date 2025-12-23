using System.Text.Json;
using System.Text.Json.Serialization;
using Atk.Cis.Service.Models;
using Atk.Cis.Service.Enums;

namespace Atk.Cis.Service.Data;


public class MockData
{
    public List<User> Users { get; set; } = new();
    public List<CheckInSession> CheckInSessions { get; set; } = new();
}

public static class MockDataLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,  // Makes it forgiving on key casing
        Converters = { new JsonStringEnumConverter<SessionStatus>() },
        WriteIndented = true
    };

    public static MockData LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Mock data file not found: {filePath}");

        string json = File.ReadAllText(filePath);

        var data = JsonSerializer.Deserialize<MockData>(json, Options);

        return data ?? throw new JsonException("Deserialization returned null.");
    }

    public static async Task<MockData> LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Mock data file not found: {filePath}");

        await using var stream = File.OpenRead(filePath);
        var data = await JsonSerializer.DeserializeAsync<MockData>(stream, Options);

        return data ?? throw new JsonException("Deserialization returned null.");
    }
}
