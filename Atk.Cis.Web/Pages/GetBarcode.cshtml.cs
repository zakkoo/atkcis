using Atk.Cis.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Atk.Cis.Web.Pages;

public class GetBarcodeModel : PageModel
{
    private readonly ICheckInDeskService _checkInDeskService;

    public GetBarcodeModel(ICheckInDeskService checkInDeskService)
    {
        _checkInDeskService = checkInDeskService;
    }

    [BindProperty]
    public string FirstName { get; set; } = string.Empty;

    [BindProperty]
    public string LastName { get; set; } = string.Empty;

    [BindProperty]
    public DateTime Birthday { get; set; } = DateTime.Today;

    public string StatusMessage { get; private set; } = string.Empty;

    public string BarcodeSvg { get; private set; } = string.Empty;

    public bool HasBarcode => !string.IsNullOrWhiteSpace(BarcodeSvg);

    public void OnGet()
    {
    }

    public async Task OnPostAsync()
    {
        var birthday = new DateTimeOffset(DateTime.SpecifyKind(Birthday, DateTimeKind.Utc));
        BarcodeSvg = await _checkInDeskService.GetBarcode(FirstName, LastName, birthday);

        if (string.IsNullOrWhiteSpace(BarcodeSvg))
        {
            StatusMessage = "The input you entered was wrong.";
        }
    }
}
