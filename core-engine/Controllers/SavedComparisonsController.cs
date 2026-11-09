using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroInsurTech.CoreEngine.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/quotes")]
public sealed class SavedComparisonsController(IComparisonLookupService lookup) : ControllerBase
{
    [HttpGet("{reference}")]
    [ProducesResponseType(typeof(SavedComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavedComparisonResponse>> GetAsync(string reference, CancellationToken cancellationToken)
    {
        if (reference.Length != 36 || !Guid.TryParseExact(reference, "D", out var id) || id == Guid.Empty)
        {
            return BadRequest(new { errors = new[] { "Comparison reference must be a valid UUID." } });
        }

        var comparison = await lookup.FindAsync(id, cancellationToken);
        return comparison is null
            ? NotFound(new { error = "Saved comparison was not found." })
            : Ok(comparison);
    }
}
