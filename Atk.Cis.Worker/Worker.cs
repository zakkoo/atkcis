using Atk.Cis.Service.Interfaces;
using NetCoreAudio.Interfaces;
using Atk.Cis.Service.Enums;

namespace Atk.Cis.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IPlayer _player;
    private readonly IServiceScopeFactory _scopeFactory;
    private ICheckInDeskService? _desk;
    private readonly IConfiguration _config;
    private DateTimeOffset? _lastRunSessionCleanup;
    private readonly string _checkedInAudioPath = "Assets/checked-in.wav";
    private readonly string _checkedOutAudioPath = "Assets/checked-out.wav";
    private readonly string _oopsAudioPath = "Assets/oops.wav";
    private readonly bool _isAudioOn = false;
    private readonly int _sessionCleanupIntervalMinutes = 480;
    private readonly int _maxDurationMinutes = 480;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, IConfiguration config,
            IPlayer player)
    {
        _logger = logger;
        _config = config;
        _scopeFactory = scopeFactory;
        _player = player;

        _isAudioOn = _config.GetValue<bool>("AudioOn");
        _sessionCleanupIntervalMinutes = _config.GetValue<int>("SessionCleanup:WorkerIntervalMinutes");
        _maxDurationMinutes = _config.GetValue<int>("SessionCleanup:MaxDurationMinutes");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ATK - Check-In System");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_lastRunSessionCleanup == null || _lastRunSessionCleanup.Value.AddMinutes(_sessionCleanupIntervalMinutes) < DateTimeOffset.Now)
            {
                _lastRunSessionCleanup = DateTimeOffset.Now;
                using var scope = _scopeFactory.CreateScope();
                _desk = scope.ServiceProvider.GetRequiredService<ICheckInDeskService>();
                var result = await _desk.CleanupStaleSessions(TimeSpan.FromMinutes(_maxDurationMinutes), stoppingToken);
                _logger.LogInformation(result);
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

                try
                {
                    var isCheckedIn = _desk.IsCheckedIn(input, stoppingToken);
                    var result = string.Empty;
                    if (await isCheckedIn)
                    {
                        result = await _desk.CheckOut(input, stoppingToken);
                        PlaySound(AudioType.CheckedIn);
                    }
                    else
                    {
                        result = await _desk.CheckIn(input, stoppingToken);
                        PlaySound(AudioType.CheckedOut);
                    }
                    _logger.LogInformation(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    PlaySound(AudioType.Failed);
                }
            }
            await Task.Delay(500, stoppingToken);
        }
    }

    private void PlaySound(AudioType type)
    {
        try
        {
            if (_isAudioOn)
            {
                if (type == AudioType.CheckedOut)
                {
                    _player.Play(_checkedOutAudioPath);
                }
                else if (type == AudioType.CheckedIn)
                {
                    _player.Play(_checkedInAudioPath);
                }
                else if (type == AudioType.Failed)
                {
                    _player.Play(_oopsAudioPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
    private async void HandleCommand(string command, CancellationToken token)
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
                _logger.LogInformation(":status | :help | :quit");
                break;
            case "test":
                {
                    using var scope = _scopeFactory.CreateScope();
                    _desk = scope.ServiceProvider.GetRequiredService<ICheckInDeskService>();
                    var result = await _desk.SignUp("Zakaria", "Agoulif", DateTimeOffset.Now, token);
                    _logger.LogInformation(result);
                }
                break;
            default:
                _logger.LogWarning("Unknown command: {Command}", command);
                break;
        }
    }
}
