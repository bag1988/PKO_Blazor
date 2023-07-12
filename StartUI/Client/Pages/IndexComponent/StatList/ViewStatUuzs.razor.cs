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
using UUZSDataProto.V1;
using System.Diagnostics.CodeAnalysis;
using BlazorLibrary.GlobalEnums;

namespace StartUI.Client.Pages.IndexComponent.StatList
{
    partial class ViewStatUuzs : IAsyncDisposable, IPubSubMethod
    {
        TableVirtualize<CLVNotify>? table;

        protected override async Task OnInitializedAsync()
        {
            request.LSortOrder = 4;
            request.BFlagDirection = 0;
            request.ObjID.SubsystemID = SubsystemType.SUBSYST_SZS;
            ThList = new Dictionary<int, string>
            {
                { 0, UUZSDataRep["IDS_STRING_SITUATION"] },
                { 1, UUZSDataRep["IDS_STRING_DEVICES"] },
                { 2, UUZSRep["IDS_STRING_GROUP_NUMBER"] },
                { 3, StartUIRep["IDS_COUNT"] },
                { 4, StartUIRep["IDS_NOTIFYSTATUS"] },
                { 5, StartUIRep["IDS_TIME"] }
            };
            HintItems.Add(new HintItem(nameof(FiltrModel.SitName), UUZSDataRep["IDS_STRING_SITUATION"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.ObjName), UUZSDataRep["IDS_STRING_DEVICES"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpObjName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.GroupNumber), UUZSRep["IDS_STRING_GROUP_NUMBER"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.Count), StartUIRep["IDS_COUNT"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.ResultName), StartUIRep["IDS_NOTIFYSTATUS"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpStateName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.TimeRange), StartUIRep["IDS_TIME"], TypeHint.Time));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrViewStatUuzs);

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_StartNewSituation(byte[] value)
        {
            try
            {
                if (value.Length > 0)
                {
                    var result = CLVNotify.Parser.ParseFrom(value);
                    if (result?.DevID > 0)
                    {
                        if (table != null)
                        {
                            await table.AddItem(result, (x) =>
                            {
                                return (x.SitID.Equals(result.SitID) && x.DevID == result.DevID && x.SessID == result.SessID);
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
                    var result = CLVNotify.Parser.ParseFrom(value);
                    if (result?.DevID > 0 && result.SitID != null)
                    {
                        if (table != null)
                        {
                            await table.ForEachItems(x =>
                            {
                                if (x.SitID.Equals(result.SitID) && x.DevID == result.DevID && x.SessID == result.SessID)
                                {
                                    x.StatusName = result.StatusName;
                                    x.CmdTime = result.CmdTime;
                                    x.CallName = result.CallName;
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


        ItemsProvider<CLVNotify> GetProvider
        {
            get
            {
                return new ItemsProvider<CLVNotify>(ThList, LoadChildList, request, new List<int>() { 25, 20, 10, 10, 25, 10 });
            }
        }

        private async ValueTask<IEnumerable<CLVNotify>> LoadChildList(GetItemRequest req)
        {
            List<CLVNotify> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/CreateStatListCache", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<CLVNotify>>() ?? new();
            }
            return newData;
        }


        private async ValueTask<IEnumerable<Hint>> LoadHelpSitName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetSituationForStatUuzs", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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
            var result = await Http.PostAsJsonAsync("api/v1/GetObjForStatUuzs", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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
            var result = await Http.PostAsJsonAsync("api/v1/GetStateForStatListCache", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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
