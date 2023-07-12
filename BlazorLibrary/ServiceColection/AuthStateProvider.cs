using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SharedLibrary.Utilities;

namespace BlazorLibrary.ServiceColection
{
    public class AuthStateProvider : AuthenticationStateProvider
    {
        private readonly LocalStorage _localStorage;
        private readonly AuthenticationState _anonymous;
        public AuthStateProvider(LocalStorage localStorage)
        {
            _localStorage = localStorage;
            _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _localStorage.GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
                return _anonymous;
            return new AuthenticationState(new ClaimsPrincipal(JwtParser.ParseClaimsFromJwt(token)));
        }
        public void NotifyUserAuthentication(IEnumerable<Claim> claims)
        {
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "bearer"));
            var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
            NotifyAuthenticationStateChanged(authState);
        }
        public void NotifyUserLogout()
        {
            var authState = Task.FromResult(_anonymous);
            NotifyAuthenticationStateChanged(authState);
        }

        public async Task<ClaimsPrincipal> GetUser()
        {
            return (await GetAuthenticationStateAsync()).User;
        }
    }
}
