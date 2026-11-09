using MicroInsurTech.CoreEngine.Dtos;

namespace MicroInsurTech.CoreEngine.Services;

public interface IComparisonLookupService
{
    Task<SavedComparisonResponse?> FindAsync(Guid reference, CancellationToken cancellationToken);
}
