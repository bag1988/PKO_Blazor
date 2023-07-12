
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Timers;
using BlazorLibrary.Helpers;
using BlazorLibrary.Shared.Modal;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using SharedLibrary;
using SharedLibrary.Extensions;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using SharedLibrary.Interfaces;
using System.Threading;

namespace BlazorLibrary.Shared
{
    partial class Main : IAsyncDisposable, IPubSubMethod
    {
        [Parameter]
        public RenderFragment<int>? ChildContent { get; set; }

        [Parameter]
        public RenderFragment<int>? Menu { get; set; }

        [Parameter]
        public string? Title { get; set; }

        [Parameter]
        public int? Width { get; set; } = 250;

        public int SubsystemID { get; set; } = 0;

        public static MessageViewList? MessageView = default!;

        ElementReference? main;

        bool isPageLoad = false;

        protected override async Task OnInitializedAsync()
        {
            await CheckUser();

            CheckQuery();

            SubsystemID = await _User.GetSubSystemID();

            await JSRuntime.InvokeVoidAsync("HotKeys.ListenWindowKey");

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_AllUserLogout(string str)
        {
            Console.WriteLine("All users logout");
            await AuthenticationService.Logout();
        }

        private async Task CheckUser()
        {
            var s = await _localStorage.GetTokenAsync();
            bool isCheckUser = false;
            if (!string.IsNullOrEmpty(s))
            {
                if (Http.DefaultRequestHeaders.Authorization == null)
                    Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(MetaDataName.Bearer, s);

                var result = await Http.PostAsJsonAsync("api/v1/allow/CheckUser", s);
                if (result.IsSuccessStatusCode)
                {
                    isCheckUser = await result.Content.ReadFromJsonAsync<bool>();
                }
            }

            if (!isCheckUser)
            {
                await AuthenticationService.Logout();
            }
            else
            {
                _ = RefreshToken();
            }
            isPageLoad = true;
        }


        async Task<string?> IsNeedRefreshToken()
        {
            var user = await AuthenticationService.GetUser();
            if (user != null && !string.IsNullOrEmpty(user.Identity?.Name))
            {
                if (await CheckOldActive())
                {
                    var exp = user.FindFirst(c => c.Type.Equals("exp"))?.Value;
                    var expTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(exp));

                    var now = DateTimeOffset.UtcNow;
                    if (expTime.AddMinutes(-10).CompareTo(now) < 0)
                        return user.Identity?.Name;
                }
            }
            return null;
        }

        async Task RefreshToken()
        {
            string userName = "";
            userName = await IsNeedRefreshToken() ?? "";
            if (!string.IsNullOrEmpty(userName))
            {
                string urlRefresh = "api/v1/allow/RefreshUser";

                if (!string.IsNullOrEmpty(Http.DefaultRequestHeaders.GetHeader(CookieName.UncRemoteCu)))
                {
                    urlRefresh = "api/v1/remote/RefreshUser";
                }

                //Обновляем токен
                var result = await Http.PostAsJsonAsync(urlRefresh, userName);
                if (result.IsSuccessStatusCode)
                {
                    var token = await result.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(token))
                    {
                        var claims = JwtParser.ParseIEnumerableClaimsFromJwt(token);
                        var newUserName = new AuthorizUser(claims).UserName;

                        if (newUserName != userName)
                        {
                            await AuthenticationService.Logout();
                        }
                        else
                        {
                            await _localStorage.SetTokenAsync(token);
                            Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(MetaDataName.Bearer, token);
                            AuthenticationService.SetNewUser(claims);
                        }
                    }
                    else
                    {
                        await AuthenticationService.Logout();
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(30));

            _ = RefreshToken();
        }

        async Task<bool> CheckOldActive()
        {
            var dateTime = await _localStorage.GetLastActiveDateAsync();
            if (dateTime == null || dateTime?.AddHours(1).CompareTo(DateTime.Now) < 0)
            {
                await AuthenticationService.Logout();
                return false;
            }
            
            return true;
        }

        void OnSetActive()
        {
            _ = _localStorage.SetLastActiveDateAsync(DateTime.Now);
        }

        void CheckQuery()
        {
            CheckQuery(MyNavigationManager.Uri);
        }

        public void CheckQuery(string uri)
        {
            if (!string.IsNullOrEmpty(uri))
            {
                string pattern = "(?:systemId=(\\d))";

                var m = Regex.Match(uri, pattern, RegexOptions.IgnoreCase);

                if (m.Success)
                {
                    int.TryParse(m.Groups[1].Value, out int SystemID);

                    if (SystemID > 0 && SystemID <= 4)
                    {
                        ChangeSubSystem(SystemID);
                        var newUri = uri.Replace(MyNavigationManager.BaseUri, "").Replace($"?systemId={SystemID}", "");
                        MyNavigationManager.NavigateTo($"/{newUri}", false, true);
                    }
                }
            }
        }


        public void ChangeSubSystem(int NewSubSystemID)
        {
            ChangeSubSytemId(NewSubSystemID);
            StateHasChanged();
        }

        private void ChangeSubSytemId(int subsystemid)
        {
            if (SubsystemID == subsystemid)
                return;
            var b = _User.SetSubsystemId(subsystemid);
            if (!b)
            {
                MessageView?.AddError("", StartUIRep["IDS_ERRORCAPTION"]);
            }
            else
                SubsystemID = subsystemid;
        }

        public void RefrechMe()
        {
            ChildContent = null;
            StateHasChanged();
        }


        public ValueTask DisposeAsync()
        {
            return _HubContext.DisposeAsync();
        }
    }
}
