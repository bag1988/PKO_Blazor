using System.Net.Http.Json;
using BlazorLibrary.Helpers;
using Google.Protobuf.WellKnownTypes;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;

namespace ARMsred.Client.Shared
{
    partial class MainLayout
    {
        //private bool expandSubNav = false;
        AppPorts _AppPortInfo = new();

        string UrlHost
        {
            get
            {
                var headerValue = Http.DefaultRequestHeaders.GetHeader(CookieName.UncRemoteCu);

                if (string.IsNullOrEmpty(headerValue))
                {
                    var localhost = MyNavigationManager.BaseUri;
                    if (localhost.Contains("localhost"))
                        headerValue = "127.0.0.1";
                    else
                    {
                        Uri myUri = new Uri(localhost);
                        IpAddressUtilities.ParseEndPoint(myUri.Authority, out string? ip, out int? port);
                        headerValue = ip;
                    }
                }
                else
                    headerValue = headerValue.Split(":")[0];

                return $"https://{headerValue}";
            }
        }


        protected override async Task OnInitializedAsync()
        {
            Http.DefaultRequestHeaders.AddHeader(CookieName.SubsystemID, SubsystemType.SUBSYST_GSO_STAFF.ToString());
            await GetAppPortInfo();
        }

        public async Task NavigateToNewTab(int port)
        {
            await JSRuntime.InvokeVoidAsync("OpenNewWindow", $"{UrlHost}:{port}");
        }


        async Task GetAppPortInfo()
        {
            await Http.PostAsJsonAsync("api/v1/remote/GetAppPortInfo", new BoolValue() { Value = true }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    _AppPortInfo = await x.Result.Content.ReadFromJsonAsync<AppPorts>() ?? new();
                }
            });
        }


        public async Task UpdateAppPortInfo()
        {
            await GetAppPortInfo();
            StateHasChanged();
        }
    }
}
