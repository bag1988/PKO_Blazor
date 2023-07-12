using BlazorLibrary.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;

namespace BlazorLibrary.ServiceColection;

public class GetUserInfo
{
    readonly HttpClient _http;

    readonly AuthenticationStateProvider _authState;

    readonly LocalStorage _localStorage;

    public GetUserInfo(HttpClient httpClient, AuthenticationStateProvider authState, LocalStorage localStorage)
    {
        _http = httpClient;
        _authState = authState;
        _localStorage = localStorage;
    }

    async Task<AuthorizUser?> GetUserAsync()
    {
        AuthorizUser? user = null;
        try
        {
            if ((await _authState.GetAuthenticationStateAsync()).User.Identity?.IsAuthenticated ?? false)
            {
                user = new((await _authState.GetAuthenticationStateAsync()).User.Claims);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return user;
    }

    public async Task<string?> GetName()
    {
        string? name = (await _authState.GetAuthenticationStateAsync()).User.Identity?.Name;

        return name;
    }

    public async Task<int> GetLocalStaff()
    {
        int staff = 0;

        var c = (await _authState.GetAuthenticationStateAsync()).User.Claims;
        int.TryParse(c?.FirstOrDefault(x => x.Type == nameof(AuthorizUser.LocalStaff))?.Value, out staff);

        return staff;
    }

    public async Task<int> GetUserSessId()
    {
        int UserSessID = 0;
        var c = (await _authState.GetAuthenticationStateAsync()).User.Claims;

        int.TryParse(c?.FirstOrDefault(x => x.Type == nameof(AuthorizUser.UserSessID))?.Value, out UserSessID);

        return UserSessID;
    }

    public async Task<PermisionsUser> GetAuthPerm()
    {
        PermisionsUser per = (await GetUserAsync())?.Permisions ?? new();
        return per;
    }

    public async Task<int> GetUserId()
    {
        int UserID = 0;
        var c = (await _authState.GetAuthenticationStateAsync()).User.Claims;

        int.TryParse(c?.FirstOrDefault(x => x.Type == nameof(AuthorizUser.UserID))?.Value, out UserID);

        return UserID;
    }

    public async Task<bool> GetCanStartStopNotify()
    {
        bool CanStartStopNotify = false;
        var c = (await _authState.GetAuthenticationStateAsync()).User.Claims;
        bool.TryParse(c?.FirstOrDefault(x => x.Type == nameof(AuthorizUser.CanStartStopNotify))?.Value, out CanStartStopNotify);

        return CanStartStopNotify;
    }

    public async Task<int> GetSubSystemID()
    {
        int id = 0;
        int.TryParse(_http.DefaultRequestHeaders.GetHeader(CookieName.SubsystemID), out id);
        if (id == 0)
        {
            id = await _localStorage.GetSubSystemIdAsync() ?? SubsystemType.SUBSYST_ASO;
            _http.DefaultRequestHeaders.AddHeader(CookieName.SubsystemID, id.ToString());
        }
        return id;
    }

    public bool SetSubsystemId(int subSystemId)
    {
        _http.DefaultRequestHeaders.AddHeader(CookieName.SubsystemID, subSystemId.ToString());
        _ = _localStorage.SetSubSystemIdAsync(subSystemId);
        return true;
    }
}
