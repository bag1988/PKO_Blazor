using System.Net.Http.Json;
using System.Text.Encodings.Web;
using BlazorLibrary.Shared.Audio;
using AsoDataProto.V1;
using LibraryProto.Helpers;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SMDataServiceProto.V1;
using BlazorLibrary.Models;
using SMP16XProto.V1;
using BlazorLibrary.Shared.Table;
using FiltersGSOProto.V1;
using Google.Protobuf.WellKnownTypes;
using BlazorLibrary.Shared.FiltrComponent;
using System.Net;
using BlazorLibrary.Helpers;
using BlazorLibrary.GlobalEnums;

namespace BlazorLibrary.Shared.NotifyLog
{
    partial class DetaliInfoCallAbon
    {
        [Parameter]
        public int? SessId { get; set; }

        [Parameter]
        public string? SitName { get; set; }

        [Parameter]
        public EventCallback ActionNext { get; set; }

        private HistoryCallItem? SelectItem;
               
        private AudioPlayerStream? player = default!;

        private bool IsData = false;
                
        TableVirtualize<HistoryCallItem>? table;

        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { -1, StartUIRep["№"] },
                { 2, StartUIRep["IDS_TIME"] },
                { 5, AsoRep["IDS_STRING_CHANNEL"] },
                { 6, StartUIRep["IDS_PHONE"] },
                { 7, StartUIRep["IDS_STATUS"] },
                { 8, StartUIRep["Answer"] }
            };
            request.LSortOrder = 1;
            request.ObjID.ObjID = SessId ?? 0;
            request.ObjID.SubsystemID = SubsystemType.SUBSYST_ASO;

            HintItems.Add(new HintItem(nameof(FiltrModel.Phone), StartUIRep["IDS_PHONE"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpPhone)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Answer), StartUIRep["Answer"], TypeHint.Input));

            HintItems.Add(new HintItem(nameof(FiltrModel.StatusCode), StartUIRep["IDS_STATUS"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpStateName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.LineName), AsoRep["IDS_STRING_CHANNEL"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpLineName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.DateRange), StartUIRep["IDS_TIME"], TypeHint.Date));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrHistoryCallResult);
        }

        ItemsProvider<HistoryCallItem> GetProvider => new ItemsProvider<HistoryCallItem>(ThList, LoadChildList, request, new List<int>() { 2, 20, 18, 20, 30 });

        private async ValueTask<IEnumerable<HistoryCallItem>> LoadChildList(GetItemRequest req)
        {
            List<HistoryCallItem> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetNotifyHistory", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<HistoryCallItem>>() ?? new();
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpPhone(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetNotifyHistoryFiltrPhone", new IntAndString() { Str = req.BstrFilter, Number = SessId ?? 0 }, ComponentDetached);
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

        // Получить из базы список линий для фильтра.
        private async ValueTask<IEnumerable<Hint>> LoadHelpLineName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetNotifyHistoryFiltrLine", new IntAndString() { Str = req.BstrFilter, Number = SessId ?? 0 }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpStateName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetAllStateFromNotifyhistory", new IntAndString() { Str = req.BstrFilter, Number = SessId ?? 0 }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<CGetAllStateBySessId>>();

                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(x.Resultname, x.Status.ToString())));
                }
            }
            return newData ?? new();
        }

        private async Task RefreshTable()
        {
            SelectItem = null;
            if (table != null)
                await table.ResetData();
        }

        private async Task DbClick()
        {
            if (SelectItem == null) return;
            IsData = false;
            await GetFile();
        }

        private void SetSelectList(List<HistoryCallItem>? items)
        {
            IsData = false;
            SelectItem = items?.LastOrDefault();
        }

        private async Task GetFile()
        {
            if (SelectItem == null)
                return;

            IsData = true;
            await Task.Yield();
            if (player != null)
            {
                await player.SetUrlSound($"api/v1/ReadSoundFromFile?filename={UrlEncoder.Default.Encode(SelectItem.UrlFile)}");
            }
        }
    }
}
