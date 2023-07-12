using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Table;
using FiltersGSOProto.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.Interfaces;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Shared.Devices
{
    partial class ContrDeviceList : IAsyncDisposable, IPubSubMethod
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_SZS;

        private CContrDevice? SelectItem = null;

        private bool? IsViewEdit = false;

        private bool? IsDelete = false;

        private string TitleName = "";

        TableVirtualize<CContrDevice>? table;

        protected override async Task OnInitializedAsync()
        {
            if (SubsystemID == SubsystemType.SUBSYST_P16x)
            {
                TitleName = GsoRep["IDS_STRING_DEVICE_P16x"];
                request.NObjType = 8;
            }
            else
            {
                TitleName = GsoRep["IDS_STRING_DEVICE_SZS"];
                request.NObjType = 16;
            }
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;


            ThList = new Dictionary<int, string>
            {
                { 0, GsoRep[SubsystemID == SubsystemType.SUBSYST_SZS ? "IDS_STRING_NAME" : "IDS_STRING_NAME_CU"] },
                { 1, GsoRep["IDS_STRING_PORT"] },
                { 2, GsoRep["IDS_STRING_ATTACH_BLOCK"] },
                { 3, GsoRep["IDS_STRING_NUMBER_CHANNEL_ON_PORT"] }
            };
            if (SubsystemID == SubsystemType.SUBSYST_SZS)
                ThList.Add(4, GsoRep["IDS_STRING_LINE1"]);


            HintItems.Add(new HintItem(nameof(FiltrModel.Name), GsoRep[SubsystemID == SubsystemType.SUBSYST_SZS ? "IDS_STRING_NAME" : "IDS_STRING_NAME_CU"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(request, LoadHelpDevName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Port), GsoRep["IDS_STRING_PORT"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(request, LoadHelpPortName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Block), GsoRep["IDS_STRING_ATTACH_BLOCK"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.Channel), GsoRep["IDS_STRING_NUMBER_CHANNEL_ON_PORT"], TypeHint.Number));

            if (SubsystemID == SubsystemType.SUBSYST_SZS)
            {
                HintItems.Add(new HintItem(nameof(FiltrModel.Line), GsoRep["IDS_STRING_LINE1"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(request, LoadHelpLineName)));

            }

            await OnInitFiltr(RefreshTable, SubsystemID == SubsystemType.SUBSYST_SZS ? FiltrName.FiltrSzsDevice : FiltrName.FiltrP16Device);

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

        ItemsProvider<CContrDevice> GetProvider => new ItemsProvider<CContrDevice>(ThList, LoadChildList, request, new List<int>() { 0, 20, 15, 15, 20 });

        private async ValueTask<IEnumerable<CContrDevice>> LoadChildList(GetItemRequest req)
        {
            List<CContrDevice> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_IControllingDevice", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<CContrDevice>>() ?? new();
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
            var result = await Http.PostAsJsonAsync("api/v1/GetDevNameForDevices", new IntAndString() { Number = request.NObjType, Str = req.BstrFilter }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpLineName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetLineNameForDevices", new IntAndString() { Number = request.NObjType, Str = req.BstrFilter }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpPortName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetPortNameForDevices", new IntAndString() { Number = request.NObjType, Str = req.BstrFilter }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(ParseConnect(x.Str), x.Number.ToString())));
                }
            }
            return newData ?? new();
        }

        private string ParseConnect(string connect)
        {
            string response = connect;
            string pattern = @"(\d+)$";
            if (request.ObjID.SubsystemID == SubsystemType.SUBSYST_SZS || request.ObjID.SubsystemID == SubsystemType.SUBSYST_P16x)
            {
                var m = Regex.Match(connect, pattern, RegexOptions.IgnoreCase);
                if (m.Groups.Count > 1)
                {
                    uint.TryParse(m.Groups[1].Value, out uint p);

                    if (p > 0xFFFF)
                    {
                        byte[] bytes = BitConverter.GetBytes(p);

                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(bytes);
                        }
                        response = $"TCP {(new IPAddress(bytes).ToString())}";
                    }
                    else if (p > 0xFF)
                    {
                        response = $"UDP {p}";
                    }
                    else
                    {
                        response = $"COM {p}";
                    }
                }
            }

            return response;
        }

        private async Task DeleteControllingDevice()
        {
            if (SelectItem != null)
            {
                //and ConfigDeletePort
                var result = await Http.PostAsJsonAsync("api/v1/DeleteControllingDevice", new OBJ_ID() { ObjID = SelectItem.ChannelBoardID, SubsystemID = request.ObjID.SubsystemID, StaffID = SelectItem.DeviceID }, ComponentDetached);
                if (!result.IsSuccessStatusCode)
                {
                    MessageView?.AddError("", UUZSRep["IDS_STRING_ERR_DEL_CONTROL_DEVICE"]);
                }
                else
                {
                    var response = await result.Content.ReadFromJsonAsync<UInt32Value>();
                    if (response?.Value > 0)
                        await ConfigDeletePort(response);
                }
                SelectItem = null;
                IsDelete = false;
            }
        }

        private async Task ConfigDeletePort(UInt32Value request)
        {
            //and ConfigDeletePort
            await Http.PostAsJsonAsync("api/v1/ConfigDeletePort", request, ComponentDetached);
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
