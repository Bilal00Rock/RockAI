using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace RockAI.App.Services.Authentication;

public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly ITokenStorage _tokenStorage;

    public AuthenticatedHttpClientHandler(ITokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenStorage.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
