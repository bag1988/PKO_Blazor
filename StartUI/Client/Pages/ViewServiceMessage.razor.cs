using System.ComponentModel;
using System.Net.Http.Json;
using BlazorLibrary;
using BlazorLibrary.Helpers;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Audio;
using BlazorLibrary.Shared.Table;
using FiltersGSOProto.V1;
using GateServiceProto.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using LibraryProto.Helpers;
using SharedLibrary.Interfaces;
using SMDataServiceProto.V1;
using SMP16XProto.V1;
using SMSSGsoProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary;
using BlazorLibrary.GlobalEnums;

namespace StartUI.Client.Pages
{
    partial class ViewServiceMessage : IAsyncDisposable, IPubSubMethod
    {        
        private List<ServiceMessage>? SelectedList = null;

        private bool ViewDopInfo = false;

        private AudioPlayerStream? player = default!;

        CUStartSitInfo? DopInfo
        {
            get
            {
                if (SelectedList?.LastOrDefault() != null)
                {
                    var byteStr = SelectedList.Last().Info;
                    if (byteStr != null && byteStr != ByteString.Empty)
                        return CUStartSitInfo.Parser.ParseFrom(byteStr);
                }
                return null;
            }
        }

        TableVirtualize<ServiceMessage>? table;

        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["IDS_TIME"] },
                { 1, StartUIRep["IDS_SIT_MSG_NAME"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.Messages), StartUIRep["IDS_SIT_MSG_NAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpMessage)));

            HintItems.Add(new HintItem(nameof(FiltrModel.DateRange), StartUIRep["IDS_TIME"], TypeHint.Date));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrServiceMessage);

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_AddLogs(string Value)
        {
            await CallRefreshData();
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_StartSessionSubCu(CUStartSitInfo Value)
        {
            await CallRefreshData();
        }

        ItemsProvider<ServiceMessage> GetProvider => new ItemsProvider<ServiceMessage>(ThList, LoadChildList, request);

        private async ValueTask<IEnumerable<ServiceMessage>> LoadChildList(GetItemRequest req)
        {
            List<ServiceMessage>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_IServiceMessages", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var json = await result.Content.ReadAsStringAsync();
                try
                {
                    var model = JsonParser.Default.Parse<ServiceMessageList>(json);

                    if (model != null)
                    {
                        newData = model.Array.ToList();
                    }
                }
                catch
                {
                    Console.WriteLine($"Error convert data to ServiceMessageList");
                }
            }
            return newData ?? new();
        }

        private async Task RefreshTable()
        {
            if (table != null)
                await table.ResetData();
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpMessage(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetMessageByServiceMessage", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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

        private async Task DeleteSelect()
        {
            if (SelectedList == null)
                return;

            await DeleteServiceLogs(SelectedList);

            SelectedList = null;
        }

        private async Task DeleteServiceLogs(List<ServiceMessage>? request)
        {
            if (request == null)
                return;

            var listIntID = request.Select(x => new IntID() { ID = x.Id });

            await Http.PostAsJsonAsync("api/v1/DeleteServiceLogs", listIntID, ComponentDetached);
        }

        private async Task ClearServiceLogs()
        {
            await Http.PostAsync("api/v1/ClearServiceLogs", null, ComponentDetached);
        }

        private void SetSelectList(List<ServiceMessage>? items)
        {
            SelectedList = items;
        }

        private async Task ViewInfo()
        {
            if (DopInfo != null)
            {
                ViewDopInfo = true;
                StateHasChanged();
                await Task.Yield();

                if (DopInfo.MsgID?.ObjID > 0 && player != null)
                {
                    await player.SetUrlSound($"api/v1/GetSoundServer?MsgId={DopInfo.MsgID.ObjID}&Staff={DopInfo.MsgID.StaffID}&System={DopInfo.MsgID.SubsystemID}&version={DateTime.Now.Second}");
                }
            }
        }

        void CloseViewInfo()
        {
            ViewDopInfo = false;
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
