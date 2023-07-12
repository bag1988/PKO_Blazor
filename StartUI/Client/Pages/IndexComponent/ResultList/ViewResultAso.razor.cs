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
using SharedLibrary.Interfaces;
using System.ComponentModel;
using BlazorLibrary.GlobalEnums;

namespace StartUI.Client.Pages.IndexComponent.ResultList
{
    partial class ViewResultAso : IAsyncDisposable, IPubSubMethod
    {
        int SessionId = 0;
        TableVirtualize<ResultCache>? table;

        protected override async Task OnInitializedAsync()
        {
            request.LSortOrder = 3;
            request.ObjID.SubsystemID = SubsystemType.SUBSYST_ASO;
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["IDS_SITUATIONCOLUMN"] },
                { 1, StartUIRep["IDS_ABONENTNAME"] },
                { 2, StartUIRep["IDS_DEPARTMENT"] },
                { 3, StartUIRep["IDS_CHANNEL_STATE"] },
                { 4, StartUIRep["IDS_TIME"] },
                { 5, StartUIRep["IDS_PHONE"] },
                { 6, StartUIRep["IDS_COUNT"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.SitName), StartUIRep["IDS_SITUATIONCOLUMN"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.AbonName), StartUIRep["IDS_ABONENTNAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpObjName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.DepName), StartUIRep["IDS_DEPARTMENT"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpDepName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.StatusName), StartUIRep["IDS_CHANNEL_STATE"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpStateName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.TimeRange), StartUIRep["IDS_TIME"], TypeHint.Time));

            HintItems.Add(new HintItem(nameof(FiltrModel.PhoneName), StartUIRep["IDS_PHONE"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpConnName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.CountCall), StartUIRep["IDS_COUNT"], TypeHint.Number));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrViewResultAso);

            _ = _HubContext.SubscribeAsync(this);

        }


        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateSCUDresult(byte[] value)
        {
            try
            {
                if (value.Length > 0)
                {
                    var result = OBJIDAndStr.Parser.ParseFrom(value);
                    if (result?.OBJID != null && result.OBJID.ObjID > 0 && result.OBJID.StaffID == SessionId)
                    {
                        if (table != null)
                        {
                            await table.ForEachItems(x =>
                            {
                                if (x.AsoAbon.ObjID == result.OBJID.ObjID && x.SessID == result.OBJID.StaffID)
                                {
                                    x.Details = result.Str;
                                    return;
                                }
                            });
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error parse OBJIDAndStr method Fire_UpdateSCUDresult");
            }
        }

        ItemsProvider<ResultCache> GetProvider => new ItemsProvider<ResultCache>(ThList, LoadChildList, request, new List<int>() { 22, 22, 12, 21, 8, 9, 6 });


        private async ValueTask<IEnumerable<ResultCache>> LoadChildList(GetItemRequest req)
        {
            List<ResultCache> newData = new();
            try
            {
                var result = await Http.PostAsJsonAsync("api/v1/CreateResultsListCache", req);
                if (result.IsSuccessStatusCode)
                {
                    newData = await result.Content.ReadFromJsonAsync<List<ResultCache>>() ?? new();
                    if (SessionId == 0 && newData.Count > 0)
                        SessionId = newData.First().SessID;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateResultsListCacheAso {ex.Message}");
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpConnName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetConnForResultListCache", new IntAndString() { Number = SubsystemType.SUBSYST_ASO, Str = req.BstrFilter });
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpDepName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetDepForResultListCache", new StringValue() { Value = req.BstrFilter });
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
            var result = await Http.PostAsJsonAsync("api/v1/GetSituationForResultListCache", new IntAndString() { Number = SubsystemType.SUBSYST_ASO, Str = req.BstrFilter });
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
            var result = await Http.PostAsJsonAsync("api/v1/GetObjForResultListCache", new IntAndString() { Number = SubsystemType.SUBSYST_ASO, Str = req.BstrFilter });
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
            var result = await Http.PostAsJsonAsync("api/v1/GetStateForResultListCache", new IntAndString() { Number = SubsystemType.SUBSYST_ASO, Str = req.BstrFilter });
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


        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
