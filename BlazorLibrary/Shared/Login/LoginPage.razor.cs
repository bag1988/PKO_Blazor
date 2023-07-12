using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Shared.Login;

partial class LoginPage
{
    private string? ErrorString;

    bool IsProcessing = false;

    readonly RequestLogin loginRequest = new();

    protected override async Task OnInitializedAsync()
    {
        loginRequest.User = await _localStorage.GetLastUserName() ?? "";
    }


    private async Task SetLogin()
    {
        if (string.IsNullOrEmpty(loginRequest.User))
        {
            ErrorString = Rep["EmptyLogin"];
            return;
        }
        IsProcessing = true;
        var result = await AuthenticationService.Login(loginRequest);
        loginRequest.Password = string.Empty;
        if (!result)
        {
            ErrorString = Rep["ErrorLogin"];
        }
        IsProcessing = false;
    }
}
