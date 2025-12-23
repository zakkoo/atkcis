namespace Atk.Cis.Service.Interfaces;

public interface ICheckInDeskService
{
    Task<string> CheckIn(string barcode);
}
