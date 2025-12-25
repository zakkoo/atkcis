using Atk.Cis.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Atk.Cis.Web.Pages.Admin;

public class CleanupModel : PageModel
{
    private readonly ICheckInDeskService _checkInDeskService;
    private readonly IConfiguration _configuration;

    public CleanupModel(ICheckInDeskService checkInDeskService, IConfiguration configuration)
    {
        _checkInDeskService = checkInDeskService;
        _configuration = configuration;
    }

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string StatusMessage { get; private set; } = "Enter the admin password to run cleanup.";

    public int MaxDurationMinutes => _configuration.GetValue<int>("SessionCleanup:MaxDurationMinutes", 60);

    public async Task<IActionResult> OnPostAsync()
    {
        var expectedPassword = _configuration.GetValue<string>("Admin:Password");

        if (string.IsNullOrWhiteSpace(expectedPassword) || Password != expectedPassword)
        {
            StatusMessage = "Invalid admin password.";
            return Page();
        }

        StatusMessage = await _checkInDeskService.CleanupStaleSessions(TimeSpan.FromMinutes(MaxDurationMinutes));
        return Page();
    }
}
