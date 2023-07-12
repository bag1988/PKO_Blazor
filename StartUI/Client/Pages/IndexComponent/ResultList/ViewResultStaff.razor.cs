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
    partial class ViewResultStaff
    {        
        TableVirtualize<CResultsCache>? table;

        protected override async Task OnInitializedAsync()
        {
            request.LSortOrder = 4;
            request.ObjID.SubsystemID = SubsystemType.SUBSYST_GSO_STAFF;
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["IDS_SITUATIONCOLUMN"] },
                { 1, StartUIRep["IDS_STAFF_OBJ"] },
                { 2, StartUIRep["IDS_START_TIME"] },
                { 3, StartUIRep["IDS_END_TIME"] },
                { 4, StartUIRep["IDS_STATUS"] },
                { 5, StartUIRep["IDS_SUCC"] },
                { 6, StartUIRep["IDS_FAIL"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.SitName), StartUIRep["IDS_SITUATIONCOLUMN"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.ObjName), StartUIRep["IDS_STAFF_OBJ"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpObjName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.TimeStartRange), StartUIRep["IDS_START_TIME"], TypeHint.Time));

            HintItems.Add(new HintItem(nameof(FiltrModel.TimeEndRange), StartUIRep["IDS_END_TIME"], TypeHint.Time));

            HintItems.Add(new HintItem(nameof(FiltrModel.StatusName), StartUIRep["IDS_STATUS"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpStateName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.CountSuccess), StartUIRep["IDS_SUCC"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.CountFail), StartUIRep["IDS_FAIL"], TypeHint.Number));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrViewResultStaff);
        }

        ItemsProvider<CResultsCache> GetProvider => new ItemsProvider<CResultsCache>(ThList, LoadChildList, request, new List<int>() { 23, 22, 8, 8, 23, 8, 8 });


        private async ValueTask<IEnumerable<CResultsCache>> LoadChildList(GetItemRequest req)
        {
            List<CResultsCache> newData = new();
            try
            {
                var result = await Http.PostAsJsonAsync("api/v1/CreateResultsListCache", req);
                if (result.IsSuccessStatusCode)
                {
                    newData = await result.Content.ReadFromJsonAsync<List<CResultsCache>>() ?? new();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateResultsListCacheStaff {ex.Message}");
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpSitName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetSitStaffForResultListCache", new StringValue() { Value = req.BstrFilter });
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
            var result = await Http.PostAsJsonAsync("api/v1/GetObjStaffForResultListCache", new StringValue() { Value = req.BstrFilter });
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
            var result = await Http.PostAsJsonAsync("api/v1/GetAllStateCUForResultListCache", new StringValue() { Value = req.BstrFilter });
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
