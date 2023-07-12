using System.Net.Http.Json;
using BlazorLibrary.Helpers;
using BlazorLibrary.Models;
using BlazorLibrary.ServiceColection;
using Google.Protobuf;
using LibraryProto.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using ReplaceLibrary;
using SharedLibrary;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static System.Net.WebRequestMethods;

namespace BlazorLibrary.FolderForInherits
{
    public class PushInherits : LayoutComponentBase
    {
        [Inject]
        HttpClient Http { get; set; } = default!;

        [Inject]
        NavigationManager MyNavigationManager { get; set; } = default!;

        [Inject]
        IJSRuntime JSRuntime { get; set; } = default!;

        private IJSObjectReference? _jsPush;

        public async Task PushOnInitAsync()
        {
            await GetSubscriptionSetting();
        }

        private async Task GetSubscriptionSetting()
        {
            try
            {
                bool reconnect = false;

                _jsPush = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorLibrary/script/PushInherits.js");

                var checkSubscription = await _jsPush.InvokeAsync<string>("blazorPushNotifications.checkSubscription");

                if (!string.IsNullOrEmpty(checkSubscription))
                {
                    NotificationSubscription request = new();
                    request.Url = checkSubscription;
                    request.IpClient = MyNavigationManager.BaseUri;

                    var result = await Http.PostAsJsonAsync("api/v1/GetSubscriptionSetting", request);
                    if (result.IsSuccessStatusCode)
                    {
                        try
                        {
                            var r = await result.Content.ReadFromJsonAsync<bool>();
                            if (r == false)
                            {
                                reconnect = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }

                if (reconnect || string.IsNullOrEmpty(checkSubscription))
                {
                    if (!string.IsNullOrEmpty(checkSubscription))
                        await _jsPush.InvokeVoidAsync("blazorPushNotifications.reconnectSubscription");
                    _ = RequestNotificationSubscriptionAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
           
        }

        private async Task RequestNotificationSubscriptionAsync()
        {
            var result = await Http.PostAsJsonAsync("api/v1/CreateVAPIDKeys", MyNavigationManager.BaseUri);

            if (result.IsSuccessStatusCode)
            {
                var publicKey = await result.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(publicKey))
                {
                    if (_jsPush != null)
                    {
                        var subscription = await _jsPush.InvokeAsync<NotificationSubscription>("blazorPushNotifications.requestSubscription", publicKey);
                        if (subscription != null)
                        {
                            try
                            {
                                subscription.IpClient = MyNavigationManager.BaseUri;
                                _ = Http.PostAsJsonAsync("api/v1/SaveSettingSubscription", subscription);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                }
            }
        }

        public async Task PushDisposeAsync()
        {
            if (_jsPush != null)
                await _jsPush.DisposeAsync();
        }

    }
}
