
using System.ComponentModel;
using System.Net.Http.Json;
using BlazorLibrary.Shared;
using GateServiceProto.V1;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.Models;
using static BlazorLibrary.Shared.Main;
using SMDataServiceProto.V1;
using Google.Protobuf;
using System.Text.RegularExpressions;
using System;
using SharedLibrary.Interfaces;

namespace StartUI.Client.Shared
{
    partial class MainLayout : IAsyncDisposable, IPubSubMethod
    {
        public static SettingApp Settings = new();

        private Main? elem = new();

        private ConfigStart configStart = new();

        public List<ServiceMessage>? ServiceLogs = null;

        int SubSystemID
        {
            get
            {
                return elem?.SubsystemID ?? 0;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await GetConfStart();

            if (elem != null)
            {
                if (SubSystemID < SubsystemType.SUBSYST_ASO || SubSystemID > SubsystemType.SUBSYST_P16x)
                {
                    elem.ChangeSubSystem(SubsystemType.SUBSYST_ASO);
                }

                if (SubSystemID == SubsystemType.SUBSYST_P16x && (!MyNavigationManager.Uri.Contains("PuNotifyLog") && !MyNavigationManager.Uri.Contains("EventLog")))
                {
                    elem.ChangeSubSystem(SubsystemType.SUBSYST_ASO);
                }
                else if (SubSystemID != SubsystemType.SUBSYST_ASO && (MyNavigationManager.Uri.Contains("HistoryCall") || MyNavigationManager.Uri.Contains("ViewChannel")))
                {
                    elem.ChangeSubSystem(SubsystemType.SUBSYST_ASO);
                }
            }

            MyNavigationManager.LocationChanged += MyNavigationManager_LocationChanged;

            var reference = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("CloseWindows", reference);

            _ = Http.PostAsJsonAsync("api/v1/allow/WriteLog", new WriteLog2Request() { Source = (int)GSOModules.StartUI_Module, EventCode = 179, SubsystemID = 0, UserID = await _User.GetUserId() });
            var result = await Http.PostAsync("api/v1/GetSetting", null);
            if (result.IsSuccessStatusCode)
            {
                Settings = await result.Content.ReadFromJsonAsync<SettingApp>() ?? new();
            }
            else
                MessageView?.AddError("", StartUIRep["IDS_ERRORCAPTION"]);

            _ = PushOnInitAsync();
            _ = GetItems_IServiceMessages();
            _ = _HubContext.SubscribeAsync(this);
        }

        private void MyNavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
        {
            if (elem != null)
            {
                if (SubSystemID == SubsystemType.SUBSYST_P16x && (!e.Location.Contains("PuNotifyLog") && !e.Location.Contains("EventLog")))
                {
                    elem.ChangeSubSystem(SubsystemType.SUBSYST_ASO);
                    StateHasChanged();
                }
                else if (SubSystemID != SubsystemType.SUBSYST_ASO && (e.Location.Contains("HistoryCall") || e.Location.Contains("ViewChannel")))
                {
                    elem.ChangeSubSystem(SubsystemType.SUBSYST_ASO);
                    StateHasChanged();
                }
                else if (e.Location.Contains("?systemId"))
                {
                    elem.CheckQuery(e.Location);
                    StateHasChanged();
                }
            }
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_AddLogs(string Value)
        {
            await GetItems_IServiceMessages();
            StateHasChanged();
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_StartSessionSubCu(CUStartSitInfo Value)
        {
            await GetItems_IServiceMessages();
            StateHasChanged();
        }

        private async Task GetConfStart()
        {
            await Http.PostAsync("api/v1/allow/GetConfStart", null).ContinueWith(async (x) =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    configStart = await x.Result.Content.ReadFromJsonAsync<ConfigStart>() ?? new();
                }
            });
        }

        private async Task GetItems_IServiceMessages()
        {
            await Http.PostAsJsonAsync("api/v1/GetItems_IServiceMessages", new GetItemRequest() { ObjID = new(), CountData = 1 }).ContinueWith(async (x) =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var json = await x.Result.Content.ReadAsStringAsync();
                    try
                    {
                        var model = JsonParser.Default.Parse<ServiceMessageList>(json);

                        if (model != null)
                        {
                            ServiceLogs = model.Array.ToList();
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"Error convert data to ServiceMessageList");
                    }
                    StateHasChanged();
                }
            });

        }

        private async Task SaveSatting()
        {
            await Http.PostAsJsonAsync("api/v1/SaveSetting", Settings).ContinueWith(x =>
            {
                if (!x.Result.IsSuccessStatusCode)
                {
                    MessageView?.AddError("", StartUIRep["IDS_ERRORCAPTION"]);
                }
            });
        }

        private void ChangeSubSystem(ChangeEventArgs e)
        {
            int.TryParse(e.Value?.ToString(), out int NewSubSystemID);

            if (elem != null)
            {
                Body = null;
                elem.ChangeSubSystem(NewSubSystemID);

                if (NewSubSystemID != SubsystemType.SUBSYST_ASO && (MyNavigationManager.Uri.Contains("HistoryCall") || MyNavigationManager.Uri.Contains("ViewChannel")))
                {
                    MyNavigationManager.NavigateTo("/");
                }
                else
                    MyNavigationManager.NavigateTo(MyNavigationManager.Uri);
            }
        }

        [JSInvokable]
        public async Task CloseWindows()
        {
            await Http.PostAsJsonAsync("api/v1/allow/WriteLog", new WriteLog2Request() { Source = (int)GSOModules.StartUI_Module, EventCode = 180, SubsystemID = 0, UserID = await _User.GetUserId() });
        }


        public string GetTitle()
        {
            string title = Rep["SensorM"];

            switch (SubSystemID)
            {
                case SubsystemType.SUBSYST_ASO: return StartUIRep["IDS_ASOTITLE"];
                case SubsystemType.SUBSYST_SZS: return StartUIRep["IDS_UUZSTITLE"];
                case SubsystemType.SUBSYST_GSO_STAFF: return StartUIRep["IDS_STAFFTITLE"];
                case SubsystemType.SUBSYST_P16x: return StartUIRep["IDS_P16xTITLE"];
            }

            return title;
        }

        public ValueTask DisposeAsync()
        {
            _ = PushDisposeAsync();
            MyNavigationManager.LocationChanged -= MyNavigationManager_LocationChanged;
            return _HubContext.DisposeAsync();
        }
    }
}
