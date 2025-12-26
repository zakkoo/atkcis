using Atk.Cis.Service.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Atk.Cis.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ICheckInDeskService _checkInDeskService;

    public IndexModel(ICheckInDeskService checkInDeskService)
    {
        _checkInDeskService = checkInDeskService;
    }

    public string StatusMessage { get; private set; } = "Ready.";
}
