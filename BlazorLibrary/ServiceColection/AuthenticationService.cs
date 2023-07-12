using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SharedLibrary;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;

namespace BlazorLibrary.ServiceColection
{
    public class AuthenticationService : IAuthenticationService
    {
        readonly HttpClient _http;
        readonly AuthenticationStateProvider _authStateProvider;
        readonly LocalStorage _localStorage;
        static bool IsSendLogout = false;
        public AuthenticationService(HttpClient http, AuthenticationStateProvider authStateProvider, LocalStorage localStorage)
        {
            _http = http;
            _authStateProvider = authStateProvider;
            _localStorage = localStorage;
        }
        public async Task<bool> Login(RequestLogin request)
        {
            bool response = false;
            var result = await _http.PostAsJsonAsync("api/v1/allow/AuthorizeUser", request);
            if (result.IsSuccessStatusCode)
            {
                var UserToken = await result.Content.ReadAsStringAsync();

                response = await SetTokenAsync(UserToken);
            }
            return response;
        }

        public async Task<bool> RemoteLogin(RequestLogin request)
        {
            bool response = false;
            var result = await _http.PostAsJsonAsync("api/v1/remote/AuthorizeUser", request);

            if (result.IsSuccessStatusCode)
            {
                var UserToken = await result.Content.ReadAsStringAsync();

                response = await SetTokenAsync(UserToken);
            }
            return response;
        }

        public async Task<bool> SetTokenAsync(string token)
        {
            var claims = JwtParser.ParseIEnumerableClaimsFromJwt(token);

            var userName = new AuthorizUser(claims).UserName;
            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userName))
            {
                await _localStorage.SetTokenAsync(token);
                await _localStorage.SetLastUserName(userName);
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(MetaDataName.Bearer, token);
                SetNewUser(claims);
                return true;
            }
            return false;
        }

        public void SetNewUser(IEnumerable<Claim> claims)
        {
            ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(claims);
        }

        public async Task<ClaimsPrincipal> GetUser()
        {
            return await ((AuthStateProvider)_authStateProvider).GetUser();
        }

        public async Task Logout()
        {
            if (!IsSendLogout)
            {
                IsSendLogout = true;

                ((AuthStateProvider)_authStateProvider).NotifyUserLogout();
                await _localStorage.RemoveAllAsync();
                _ = _http.PostAsync("api/v1/allow/Logout", null);
                _http.DefaultRequestHeaders.Authorization = null;

                IsSendLogout = false;
            }
        }
    }
}
