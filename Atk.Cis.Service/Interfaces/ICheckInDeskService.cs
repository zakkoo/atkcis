namespace Atk.Cis.Service.Interfaces;

public interface ICheckInDeskService
{
    Task<string> SignUp(string firstName, string lastName, DateTimeOffset birthday);
    Task<string> CheckIn(string barcode);
}
