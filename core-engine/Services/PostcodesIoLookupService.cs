using System.Net;
using System.Text.Json.Serialization;

namespace MicroInsurTech.CoreEngine.Services;

public sealed class PostcodesIoLookupService(HttpClient httpClient, ILogger<PostcodesIoLookupService> logger) : IPostcodeLookupService
{
    public async Task<string?> GetRegionAsync(string postcode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(postcode))
        {
            return null;
        }

        var normalizedPostcode = WebUtility.UrlEncode(postcode.Trim());

        try
        {
            var response = await httpClient.GetFromJsonAsync<PostcodeApiResponse>(
                $"/postcodes/{normalizedPostcode}",
                cancellationToken);

            return string.IsNullOrWhiteSpace(response?.Result?.Region)
                ? null
                : response.Result.Region;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Postcode lookup failed for {Postcode}", postcode);
            return null;
        }
    }

    private sealed record PostcodeApiResponse([property: JsonPropertyName("result")] PostcodeResult? Result);

    private sealed record PostcodeResult([property: JsonPropertyName("region")] string? Region);
}
