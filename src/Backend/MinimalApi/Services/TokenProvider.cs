using System.Net.Http.Headers;

namespace MinimalApi.Services
{
    public interface ITokenProvider
    {
        string GetTokenFromHeader();
    }
    public class TokenProvider : ITokenProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetTokenFromHeader()
        {
            string authorizationHeader = _httpContextAccessor.HttpContext.Request.Headers["Authorization"];

            if (string.IsNullOrEmpty(authorizationHeader))
            {
                return null;
            }

            string[] parts = authorizationHeader.Split(' ');
            if (parts.Length == 2)
            {
                string scheme = parts[0];
                string token = parts[1];

                if (scheme == "Bearer")
                {
                    return token;
                }
            }

            return null;
        }
    }

}
