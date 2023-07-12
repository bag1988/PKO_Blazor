using System.ComponentModel;
using System.Net.Http.Json;
using AsoDataProto.V1;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Table;
using FiltersGSOProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.Interfaces;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Pages.ASO.ControllingDevice
{
    partial class ViewDevice : IAsyncDisposable, IPubSubMethod
    {
        readonly int SubsystemID = SubsystemType.SUBSYST_ASO;

        private ControllingDeviceItem? SelectItem = null;

        private bool? IsViewEdit = false;

        private bool? IsDelete = false;

        TableVirtualize<ControllingDeviceItem>? table;

        protected override async Task OnInitializedAsync()
        {
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;

            ThList = new Dictionary<int, string>
            {
                { 0, AsoRep["IDS_DEVNAME"] },
                { 1, AsoRep["IDS_DEVCHANN"] },
                { -1, AsoRep["IDS_DEVCONTRTYPE"] },
                { 2, AsoRep["IDS_DEVCONNTYPE"] },
                { 3, AsoRep["IDS_DEVSTATUS"] }
            };
            
            HintItems.Add(new HintItem(nameof(FiltrModel.Device), AsoRep["IDS_DEVNAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpDevName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Channel), AsoRep["IDS_DEVCHANN"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.Connect), AsoRep["IDS_DEVCONNTYPE"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpConnName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.State), AsoRep["IDS_DEVSTATUS"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpStateName)));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrAsoDevice);

            _ = _HubContext.SubscribeAsync(this);
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateControllingDevice(long Value)
        {
            await CallRefreshData();
            StateHasChanged();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteControllingDevice(long Value)
        {
            await CallRefreshData();
            StateHasChanged();
        }

        ItemsProvider<ControllingDeviceItem> GetProvider => new ItemsProvider<ControllingDeviceItem>(ThList, LoadChildList, request, new List<int>() { 40, 10, 10, 20, 20 });

        private async ValueTask<IEnumerable<ControllingDeviceItem>> LoadChildList(GetItemRequest req)
        {
            List<ControllingDeviceItem> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_IControllingDevice", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<ControllingDeviceItem>>() ?? new();
            }
            else
            {
                MessageView?.AddError("", AsoRep["IDS_EGETDEVLISTINFO"]);
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpDevName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetDeviceNameForDevices", new IntAndString() { Number = SubsystemID, Str = req.BstrFilter }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(x.Str)));
                }
            }
            return newData ?? new();
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpConnName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetConnectNameForDevices", new IntAndString() { Number = SubsystemID, Str = req.BstrFilter }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(x.Str)));
                }
            }
            return newData ?? new();
        }

        private ValueTask<IEnumerable<Hint>> LoadHelpStateName(GetItemRequest req)
        {
            List<Hint> newData = new()
            {
                new Hint(AsoDataRep["IDS_STRING_OFFED"], "0"),
                new Hint(AsoDataRep["IDS_STRING_ACTIVED"], "1")
            };

            return new(newData);
        }

        private async Task DeleteControllingDevice()
        {
            if (SelectItem != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/DeleteControllingDevice", new OBJ_ID() { ObjID = SelectItem.DeviceID, SubsystemID = SubsystemType.SUBSYST_ASO });
                if (!result.IsSuccessStatusCode)
                {
                    MessageView?.AddError("", AsoRep["IDS_E_DELASODEVICE"]);
                }
                SelectItem = null;
                IsDelete = false;
            }
        }

        private void CallBackEvent(bool? update)
        {
            IsViewEdit = false;
            SelectItem = null;
        }

        private async Task RefreshTable()
        {
            SelectItem = null;
            if (table != null)
                await table.ResetData();
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
