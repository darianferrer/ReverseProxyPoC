using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

namespace ReverseProxyWithTransformations.Transformers;

internal class CustomerProfileLanguageTransformer : HttpTransformer
{
    public override async ValueTask TransformRequestAsync(HttpContext httpContext,
        HttpRequestMessage proxyRequest,
        string destinationPrefix,
        CancellationToken cancellationToken)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

        var customerId = GetCustomerId(httpContext);

        // Assign the custom uri. Be careful about extra slashes when concatenating here. RequestUtilities.MakeDestinationAddress is a safe default.
        proxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress(
            destinationPrefix,
            $"/api/customers/{customerId}/language",
            httpContext.Request.QueryString);

        // Suppress the original request header, use the one from the destination Uri.
        proxyRequest.Headers.Host = null;
    }

    public override async ValueTask<bool> TransformResponseAsync(HttpContext httpContext,
        HttpResponseMessage? proxyResponse,
        CancellationToken cancellationToken)
    {
        var content = await GetContentAsync(httpContext, proxyResponse, cancellationToken);
        WriteResponse(httpContext, proxyResponse!, content);
        return await base.TransformResponseAsync(httpContext, proxyResponse, cancellationToken);
    }

    private static long GetCustomerId(HttpContext httpContext)
    {
        var queryContext = new QueryTransformContext(httpContext.Request);
        var customerIdString = queryContext.Collection["customerId"];
        var customerId = long.Parse(customerIdString);
        return customerId;
    }

    private async Task<LanguageResponse> GetContentAsync(HttpContext httpContext,
        HttpResponseMessage? proxyResponse,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId(httpContext);
        if (proxyResponse is null or { IsSuccessStatusCode: false })
        {
            return new(customerId, null, new[] { new Error("741537981", "SSOV0013", "Customer not found") }); // This is specific for 404, we need all errors here :(;
        }

        using var responseStream = await proxyResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var decompressedResponse = new GZipStream(responseStream, CompressionMode.Decompress);
        var language = await JsonSerializer.DeserializeAsync<string>(decompressedResponse, options: null, cancellationToken);
        return new(customerId, language, []);
    }

    private static void WriteResponse(HttpContext httpContext,
        HttpResponseMessage proxyResponse,
        LanguageResponse response)
    {
        var body = JsonSerializer.Serialize(response);
        var content = new StringContent(body, Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);
        httpContext.Response.ContentLength = body.Length;
        proxyResponse.Content?.Dispose();
        proxyResponse.Content = content;
    }

    private record LanguageResponse(long CustomerId, string? LanguageTag, IEnumerable<Error> Errors);

    private record Error(string AdditionalInfo, string Code, string Message);
}