using Atk.Cis.Service.Interfaces;
using Atk.Cis.Service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Atk.Cis.Web.Pages.Admin;

public class DataModel : PageModel
{
    private readonly ICheckInDeskService _checkInDeskService;
    private readonly IConfiguration _configuration;

    public DataModel(ICheckInDeskService checkInDeskService, IConfiguration configuration)
    {
        _checkInDeskService = checkInDeskService;
        _configuration = configuration;
    }

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string StatusMessage { get; private set; } = "Enter the admin password to view data.";

    public List<User> Users { get; private set; } = new();

    public List<UserSession> UserSessions { get; private set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        var expectedPassword = _configuration.GetValue<string>("Admin:Password");

        if (string.IsNullOrWhiteSpace(expectedPassword) || Password != expectedPassword)
        {
            StatusMessage = "Invalid admin password.";
            return Page();
        }

        Users = await _checkInDeskService.GetUsers();
        UserSessions = await _checkInDeskService.GetUserSessions();
        StatusMessage = "Loaded admin data.";
        return Page();
    }
}
