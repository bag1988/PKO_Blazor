using System.Net.Http.Json;
using AsoDataProto.V1;
using StaffDataProto.V1;
using UUZSDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SMDataServiceProto.V1;
using GateServiceProto.V1;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Table;
using FiltersGSOProto.V1;
using LibraryProto.Helpers;
using BlazorLibrary.Helpers;
using Google.Protobuf.WellKnownTypes;
using BlazorLibrary.GlobalEnums;

namespace StartUI.Client.Pages.IndexComponent.ResultList
{
    partial class ViewResultUUZS
    {        
        TableVirtualize<CLVResult>? table;

        protected override async Task OnInitializedAsync()
        {
            request.LSortOrder = 3;
            request.ObjID.SubsystemID = SubsystemType.SUBSYST_SZS;
            ThList = new Dictionary<int, string>
            {
                { 0, UUZSDataRep["IDS_STRING_SITUATION"] },
                { 1, UUZSDataRep["IDS_STRING_DEVICES"] },
                { 2, UUZSRep["IDS_STRING_ALIGMENT"] },
                { 3, StartUIRep["IDS_NOTIFYSTATUS"] },
                { 4, StartUIRep["IDS_TIME"] },
                { 5, StartUIRep["IDS_CONNECT"] },
                { 6, StartUIRep["IDS_COUNT"] }
            };
            HintItems.Add(new HintItem(nameof(FiltrModel.SitName), UUZSDataRep["IDS_STRING_SITUATION"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.AbonName), UUZSDataRep["IDS_STRING_DEVICES"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpObjName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.DepName), UUZSRep["IDS_STRING_ALIGMENT"], TypeHint.Input));

            HintItems.Add(new HintItem(nameof(FiltrModel.StatusName), StartUIRep["IDS_NOTIFYSTATUS"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpStateName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.TimeRange), StartUIRep["IDS_TIME"], TypeHint.Time));

            HintItems.Add(new HintItem(nameof(FiltrModel.PhoneName), StartUIRep["IDS_CONNECT"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpConnName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.CountCall), StartUIRep["IDS_COUNT"], TypeHint.Number));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrViewResultUuzs);

        }

        ItemsProvider<CLVResult> GetProvider => new ItemsProvider<CLVResult>(ThList, LoadChildList, request, new List<int>() { 22, 22, 12, 21, 8, 9, 6});


        private async ValueTask<IEnumerable<CLVResult>> LoadChildList(GetItemRequest req)
        {
            List<CLVResult> newData = new();
            try
            {
                var result = await Http.PostAsJsonAsync("api/v1/CreateResultsListCache", req);
                if (result.IsSuccessStatusCode)
                {
                    newData = await result.Content.ReadFromJsonAsync<List<CLVResult>>() ?? new();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateResultsListCacheUUZS {ex.Message}");
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpConnName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetConnForResultListCache", new IntAndString() { Number = SubsystemType.SUBSYST_SZS, Str = req.BstrFilter });
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
                
        private async ValueTask<IEnumerable<Hint>> LoadHelpSitName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetSituationForResultListCache", new IntAndString() { Number = SubsystemType.SUBSYST_SZS, Str = req.BstrFilter });
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpObjName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetObjForResultListCache", new IntAndString() { Number = SubsystemType.SUBSYST_SZS, Str = req.BstrFilter });
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
            var result = await Http.PostAsJsonAsync("api/v1/GetStateForResultListCache", new IntAndString() { Number = SubsystemType.SUBSYST_SZS, Str = req.BstrFilter });
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
            if (table != null)
                await table.ResetData();
        }

    }
}
