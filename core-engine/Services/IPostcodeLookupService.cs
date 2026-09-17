namespace MicroInsurTech.CoreEngine.Services;

public interface IPostcodeLookupService
{
    Task<string?> GetRegionAsync(string postcode, CancellationToken cancellationToken);
}
