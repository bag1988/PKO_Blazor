using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using SharedLibrary;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;
using LibraryProto.Helpers;
using BlazorLibrary.Helpers;
using System.ComponentModel;
using SharedLibrary.Interfaces;
using System.Diagnostics.CodeAnalysis;
using AsoDataProto.V1;
using BlazorLibrary.GlobalEnums;

namespace StartUI.Client.Pages.IndexComponent.StatList
{
    partial class ViewStatAso : IAsyncDisposable, IPubSubMethod
    {
        TableVirtualize<StatCache>? table;

        protected override async Task OnInitializedAsync()
        {
            request.LSortOrder = 4;
            request.BFlagDirection = 0;
            request.ObjID.SubsystemID = SubsystemType.SUBSYST_ASO;
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["IDS_SITUATIONCOLUMN"] },
                { 1, StartUIRep["IDS_ABONENTNAME"] },
                { 2, StartUIRep["IDS_COUNT"] },
                { 3, StartUIRep["IDS_CHANNEL_NAME"] },
                { 4, StartUIRep["IDS_CHANNEL_STATE"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.SitName), StartUIRep["IDS_SITUATIONCOLUMN"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.ObjName), StartUIRep["IDS_ABONENTNAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpObjName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Count), StartUIRep["IDS_COUNT"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.LineName), StartUIRep["IDS_CHANNEL_NAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpLineName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.ResultName), StartUIRep["IDS_CHANNEL_STATE"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpStateName)));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrViewStatAso);

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_StartNewSituation(byte[] value)
        {
            try
            {
                if (value.Length > 0)
                {
                    var result = StatCache.Parser.ParseFrom(value);
                    if (result?.AsoAbon != null && result.AsoAbon.ObjID > 0 && result.Sit != null)
                    {
                        if (table != null)
                        {
                            await table.AddItem(result, (x) =>
                            {
                                return (x.AsoAbon.Equals(result.AsoAbon) && x.Sit.Equals(result.Sit) && x.SessID == result.SessID);
                            });
                        }
                    }
                }
            }
            catch
            {
                //Console.WriteLine("Error parse CLVNotify method Fire_StartNewSituation");
            }
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateStatListCache(byte[] value)
        {
            try
            {
                if (value.Length > 0)
                {
                    var result = StatCache.Parser.ParseFrom(value);
                    if (result?.AsoAbon != null && result.AsoAbon.ObjID > 0 && result.Sit != null)
                    {
                        if (table != null)
                        {
                            await table.ForEachItems(x =>
                            {
                                if (x.AsoAbon.Equals(result.AsoAbon) && x.Sit.Equals(result.Sit) && x.SessID == result.SessID)
                                {
                                    x.SelectName = result.SelectName;
                                    x.CountRealCall = result.CountRealCall;
                                    x.LineName = result.LineName;
                                    return;
                                }
                            });
                        }
                    }
                }
            }
            catch
            {
                //Console.WriteLine("Error parse CLVNotify method Fire_UpdateStatListCache");
            }
        }


        ItemsProvider<StatCache> GetProvider
        {
            get
            {
                return new ItemsProvider<StatCache>(ThList, LoadChildList, request, new List<int>() { 25, 28, 10, 10, 27 });
            }
        }

        private async ValueTask<IEnumerable<StatCache>> LoadChildList(GetItemRequest req)
        {
            List<StatCache> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/CreateStatListCache", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<StatCache>>() ?? new();
            }
            return newData;
        }


        private async ValueTask<IEnumerable<Hint>> LoadHelpSitName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetSituationForStatAso", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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
            var result = await Http.PostAsJsonAsync("api/v1/GetObjForStatAso", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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
            var result = await Http.PostAsJsonAsync("api/v1/GetLineForStatAso", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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
            var result = await Http.PostAsJsonAsync("api/v1/GetStateForStatListCacheAso", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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
