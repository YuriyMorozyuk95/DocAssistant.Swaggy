using System.Net;
using System.Net.Http.Headers;

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace ClientApp.MessageHandler
{
    public class CustomAuthorizationMessageHandler : AuthorizationMessageHandler
    {
        private readonly IAccessTokenProvider _provider;
        private readonly ILogger<CustomAuthorizationMessageHandler> _logger;
        private readonly string _scopes;

        public CustomAuthorizationMessageHandler(
            IAccessTokenProvider provider,
            NavigationManager navigationManager,
            IConfiguration configuration,
            ILogger<CustomAuthorizationMessageHandler> logger)
            : base(provider, navigationManager)
        {
            _provider = provider;
            _logger = logger;
            var baseUrl = configuration["AppSettings:BACKEND_URI"];
            _scopes = configuration["AzureAd:Scopes"];

            ConfigureHandler(
                authorizedUrls: new[] { baseUrl },
                scopes: new[] { _scopes }
            );
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var result = await _provider.RequestAccessToken(
                new AccessTokenRequestOptions { Scopes = new[] { _scopes } });

            if (result.TryGetToken(out var token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
            }
            else
            {
                var message = "Couldn't retrieve an access token";
                _logger.LogWarning(message);

                var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    ReasonPhrase = message,
                };
                return httpResponseMessage;
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
