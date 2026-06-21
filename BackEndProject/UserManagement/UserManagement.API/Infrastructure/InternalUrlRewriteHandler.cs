namespace UserManagement.API.Infrastructure;

/// <summary>
/// Rewrites backchannel HTTP calls from the public OIDC issuer URL to the internal URL.
/// Needed on Oracle VM where hairpin NAT is not supported — the JWKS URI in the discovery
/// document points to the public domain, but within the container we must call localhost.
/// </summary>
internal sealed class InternalUrlRewriteHandler(string externalBase, string internalBase)
    : HttpClientHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var original = request.RequestUri?.ToString();
        if (original != null && original.StartsWith(externalBase, StringComparison.OrdinalIgnoreCase))
        {
            var rewritten = internalBase + original[externalBase.Length..];
            request.RequestUri = new Uri(rewritten);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
