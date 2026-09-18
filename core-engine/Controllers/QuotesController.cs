using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroInsurTech.CoreEngine.Controllers;

[ApiController]
[Route("api/quotes")]
public sealed class QuotesController(
    IQuoteOrchestrationService quoteOrchestrationService,
    IQuotePersistenceService quotePersistenceService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(QuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuoteResponse>> GenerateQuoteAsync(
        [FromBody] QuoteRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);

        if (validationErrors.Count > 0)
        {
            return BadRequest(new { errors = validationErrors });
        }

        var response = await quoteOrchestrationService.GenerateQuotesAsync(request, cancellationToken);
        await quotePersistenceService.SaveQuoteRequestAsync(request, response, cancellationToken);

        return Ok(response);
    }

    private static List<string> Validate(QuoteRequest? request)
    {
        var errors = new List<string>();

        if (request is null)
        {
            errors.Add("Request body is required.");
            return errors;
        }

        if (request.Client is null)
        {
            errors.Add("Client details are required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Client.FirstName))
            {
                errors.Add("Client first name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Client.LastName))
            {
                errors.Add("Client last name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Client.Email) || !request.Client.Email.Contains('@', StringComparison.Ordinal))
            {
                errors.Add("A valid client email is required.");
            }
        }

        if (request.Property is null)
        {
            errors.Add("Property details are required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.Property.Postcode))
        {
            errors.Add("Property postcode is required.");
        }

        if (request.Property.YearBuilt is < 1500 or > 2100)
        {
            errors.Add("Property year built must be between 1500 and 2100.");
        }

        if (request.Property.RebuildCost <= 0)
        {
            errors.Add("Property rebuild cost must be greater than zero.");
        }

        return errors;
    }
}
