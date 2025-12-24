using Atk.Cis.Service;
using Atk.Cis.Service.Interfaces;

namespace Atk.Cis.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private ICheckInDeskService? _desk;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ATK - Check-In System");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            var input = Console.ReadLine();

            if (string.IsNullOrEmpty(input))
                continue;


            if (input.StartsWith(":"))
            {
                HandleCommand(input[1..], stoppingToken);
            }
            else
            {
                using var scope = _scopeFactory.CreateScope();
                _desk = scope.ServiceProvider.GetRequiredService<ICheckInDeskService>();
                var result = await _desk.CheckIn(input);
                Console.WriteLine(result);
            }


            await Task.Delay(500, stoppingToken);
        }
    }

    private void HandleCommand(string command, CancellationToken token)
    {
        switch (command)
        {
            case "status":
                _logger.LogInformation("Worker alive.");
                break;

            case "quit":
            case "exit":
                _logger.LogInformation("Shutdown requested.");
                Environment.Exit(0);
                break;
            case "last-checkin":
                _logger.LogInformation($"todo...");
                break;
            case "help":
                Console.WriteLine(":status | :help | :quit");
                break;

            default:
                _logger.LogWarning("Unknown command: {Command}", command);
                break;
        }
    }
}
