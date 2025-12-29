using Atk.Cis.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Atk.Cis.Web.Pages;

public class SignUpModel : PageModel
{
    private readonly ICheckInDeskService _checkInDeskService;

    public SignUpModel(ICheckInDeskService checkInDeskService)
    {
        _checkInDeskService = checkInDeskService;
    }

    [BindProperty]
    public string FirstName { get; set; } = string.Empty;

    [BindProperty]
    public string LastName { get; set; } = string.Empty;

    [BindProperty]
    public DateTime Birthday { get; set; } = DateTime.Today;

    public string StatusMessage { get; private set; } = "Enter details to create an account.";

    public void OnGet()
    {
    }

    public async Task OnPostAsync()
    {
        var birthday = new DateTimeOffset(DateTime.SpecifyKind(Birthday, DateTimeKind.Utc));
        StatusMessage = await _checkInDeskService.SignUp(FirstName, LastName, birthday, HttpContext.RequestAborted);
    }
}
