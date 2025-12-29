using Atk.Cis.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Atk.Cis.Web.Pages;

public class CheckOutModel : PageModel
{
    private readonly ICheckInDeskService _checkInDeskService;

    public CheckOutModel(ICheckInDeskService checkInDeskService)
    {
        _checkInDeskService = checkInDeskService;
    }

    [BindProperty]
    public string Code { get; set; } = string.Empty;

    public string StatusMessage { get; private set; } = "Enter a code to check out.";

    public void OnGet()
    {
    }

    public async Task OnPostAsync()
    {
        StatusMessage = await _checkInDeskService.CheckOut(Code, HttpContext.RequestAborted);
    }
}
