using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Timers;
using BlazorLibrary.Helpers;
using BlazorLibrary.ServiceColection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SharedLibrary.Utilities;

namespace BlazorLibrary.Shared.Login
{
    partial class AuthUser
    {
        [CascadingParameter]
        private Task<AuthenticationState>? UserState { get; set; }

        [Parameter]
        public RenderFragment<string?>? Authorized { get; set; }
        [Parameter]
        public RenderFragment? Authorizing { get; set; }
        [Parameter]
        public RenderFragment? NotAuthorized { get; set; }

        private ClaimsPrincipal? user { get; set; } = null;

        private bool IsAuthenticated
        {
            get
            {
                return user?.Identity?.IsAuthenticated ?? false;
            }
        }

        protected override async Task OnParametersSetAsync()
        {            
            if (UserState == null)
                return;

            user = (await UserState).User;

            if (IsAuthenticated)
            {
                var token = await _localStorage.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    await AuthenticationService.Logout();
                }
            }
            StateHasChanged();
        }

    }
}
