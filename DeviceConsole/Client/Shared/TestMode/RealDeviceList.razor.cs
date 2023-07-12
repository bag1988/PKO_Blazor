using System.Net.Http.Json;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Table;
using Microsoft.AspNetCore.Components;
using SCSChLService.Protocol.Grpc.Proto.V1;
using SharedLibrary;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Shared.TestMode
{
    partial class RealDeviceList
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_SZS;

        TableVirtualize<ChannelInfo>? table;

        protected override async Task OnInitializedAsync()
        {
            if (SubsystemID == SubsystemType.SUBSYST_SZS)
                request.NObjType = DevType.SZS;
            else if (SubsystemID == SubsystemType.SUBSYST_P16x)
                request.NObjType = DevType.P16x;

            ThList = new Dictionary<int, string>
            {
                { 0, UUZSRep["IDS_STRING_CONNECT"] },
                { 1, "№ п/п" },
                { -3, UUZSRep["IDS_STRING_SERIAL_NUMBER"] },
                { 2, UUZSRep["IDS_STRING_VERSION"] },
                { -5, UUZSRep["IDS_STRING_TYPE"] },
                { -6, UUZSRep["IDS_STRING_COMMENTS"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.Connect), UUZSRep["IDS_STRING_CONNECT"], TypeHint.Input));
            HintItems.Add(new HintItem(nameof(FiltrModel.Number), "№ п/п", TypeHint.Number));
            HintItems.Add(new HintItem(nameof(FiltrModel.Version), UUZSRep["IDS_STRING_VERSION"], TypeHint.Number));
            HintItems.Add(new HintItem(nameof(FiltrModel.Serial), UUZSRep["IDS_STRING_SERIAL_NUMBER"], TypeHint.Input));

            await OnInitFiltr(RefreshTable, SubsystemID == SubsystemType.SUBSYST_SZS ? FiltrName.FiltrTestRealSzsDevice : FiltrName.FiltrTestRealP16Device);

        }

        ItemsProvider<ChannelInfo> GetProvider => new ItemsProvider<ChannelInfo>(ThList, LoadChildList, request);

        /// <summary>
        /// Получить описание устройств из драйвера SCSChLService
        /// </summary>
        /// <returns></returns>
        private async ValueTask<IEnumerable<ChannelInfo>> LoadChildList(GetItemRequest req)
        {
            List<ChannelInfo> newData = new();

            var result = await Http.PostAsJsonAsync("api/v1/GetChannelsInfo", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<ChannelInfo>>() ?? new();
            }
            else
            {
                MessageView?.AddError("", UUZSRep["IDS_STRING_CANNOT_SUBSYSTEM_CONNECT"]);
            }
            return newData;
        }


        public async Task RefreshTable()
        {
            if (table != null)
                await table.ResetData();
        }


        public List<ChannelInfo>? GetRealList => table?.GetCurrentItems;

        private string GetRealTypeName(uint devType)
        {
            return devType switch
            {
                DevType.SZS => SMDataRep["SUBSYST_SZS"],
                DevType.UZS => SMDataRep["SUBSYST_SZ"],
                DevType.P16x => SMDataRep["SUBSYST_P16x"],
                _ => UUZSRep["IDS_STRING_UNKNOWN"]
            };
        }
    }
}
