using System.Net.Http.Json;
using BlazorLibrary.Shared.Audio;
using Google.Protobuf.WellKnownTypes;
using GsoReporterProto.V1;
using StaffDataProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SMSSGsoProto.V1;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;
using LibraryProto.Helpers;
using BlazorLibrary.Helpers;
using System.ComponentModel;
using SharedLibrary.Interfaces;
using Google.Protobuf;
using ReplaceLibrary;
using BlazorLibrary.GlobalEnums;

namespace StartUI.Client.Pages.IndexComponent.StatList
{
    partial class ViewStatCU : IAsyncDisposable, IPubSubMethod
    {
        TableVirtualize<CResultsCache>? table;

        protected override async Task OnInitializedAsync()
        {
            request.LSortOrder = 4;
            request.BFlagDirection = 0;
            request.ObjID.SubsystemID = SubsystemType.SUBSYST_GSO_STAFF;
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["IDS_SITUATIONCOLUMN"] },
                { 1, StartUIRep["IDS_STAFF_OBJ"] },
                { 2, StartUIRep["IDS_START_TIME"] },
                { 3, StartUIRep["IDS_CUR_TIME"] },
                { 4, StartUIRep["IDS_STATUS"] },
                { 5, StartUIRep["IDS_SUCC"] },
                { 6, StartUIRep["IDS_FAIL"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.SitName), StartUIRep["IDS_SITUATIONCOLUMN"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.CuName), StartUIRep["IDS_STAFF_OBJ"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpUnitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.TimeStartRange), StartUIRep["IDS_START_TIME"], TypeHint.Time));

            HintItems.Add(new HintItem(nameof(FiltrModel.TimeEndRange), StartUIRep["IDS_CUR_TIME"], TypeHint.Time));

            HintItems.Add(new HintItem(nameof(FiltrModel.ResultName), StartUIRep["IDS_STATUS"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpStateName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.CountSuccess), StartUIRep["IDS_SUCC"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.CountFail), StartUIRep["IDS_FAIL"], TypeHint.Number));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrViewStatStaff);

            _ = _HubContext.SubscribeAsync(this);
        }


        [Description(DaprMessage.PubSubName)]
        public async Task Fire_StartNewSituation(byte[] value)
        {
            try
            {
                if (value.Length > 0)
                {
                    var result = CResultsCache.Parser.ParseFrom(value);
                    if (result?.CUNotifyOut != null)
                    {
                        if (table != null)
                        {
                            //Console.WriteLine($"------Fire_StartNewSituation---------");
                            //Console.WriteLine($"Fire_StartNewSituation request {result}");
                            await table.AddItem(result, x =>
                            {
                                //Console.WriteLine($"Fire_StartNewSituation item {x}");
                                return (x.CUNotifyOut.SessID == result.CUNotifyOut.SessID && x.CUNotifyOut.CUSitID.Equals(result.CUNotifyOut.CUSitID) && x.CUNotifyOut.FromCUID.ObjID.ObjID == result.CUNotifyOut.FromCUID.ObjID.ObjID);
                            });
                        }
                    }
                }
            }
            catch
            {
                //Console.WriteLine("Error parse CResultsCache method Fire_StartNewSituation");
            }
        }


        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateStatListCache(byte[] value)
        {
            try
            {
                if (value.Length > 0)
                {
                    var result = CUNotifyOut.Parser.ParseFrom(value);
                    if (result?.FromCUID?.ObjID?.ObjID > 0)
                    {
                        if (table != null)
                        {
                            //Console.WriteLine($"------Fire_UpdateStatListCache------- {result}");

                            //await table.ForEachItems(x =>
                            //{
                            //    Console.WriteLine($"Find item {x}");
                            //});
                            //Console.WriteLine($"------------------------------------");

                            CResultsCache? item = null;

                            if (result.SitID.SubsystemID == SubsystemType.SUBSYST_ASO)
                            {
                                item = table.FindItemMatch(x => x.CUNotifyOut.SessID == result.SessID && x.CUNotifyOut.CUSitID.Equals(result.SitID) && x.CUNotifyOut.FromCUID.ObjType == SubsystemType.SUBSYST_ASO);
                            }
                            else
                            {
                                item = table.FindItemMatch(x => x.CUNotifyOut.SessID == result.SessID && x.CUNotifyOut.CUSitID.Equals(result.SitID) && x.CUNotifyOut.FromCUID.ObjType == result.FromCUID.ObjType);
                            }

                            if (item == null)
                            {
                                item = table.FindItemMatch(x => x.CUNotifyOut.SessID == result.SessID && x.CUNotifyOut.CUSitID.Equals(result.CUSitID) && x.CUNotifyOut.FromCUID.ObjID.Equals(result.FromCUID.ObjID) && x.CUNotifyOut.FromCUID.ObjType < 4);
                            }

                            if (item != null)
                            {
                                item.CUNotifyOut.Status = result.Status;
                                item.CUNotifyOut.CurTime = result.CurTime;
                                item.CUNotifyOut.Succ = result.Succ;
                                item.CUNotifyOut.Fail = result.Fail;
                                item.CUSitName = result.SitName;
                                item.CUNotifyOut.CUSitID = result.SitID;
                                item.CUNotifyOut.FromCUID.ObjType = result.FromCUID.ObjType;
                                item.CUNotifyOut.StartTime = result.StartTime;
                                await table.RefreshVirtualize();
                            }
                            else
                            {
                                var elem = table.FindItemMatch(x => x.CUNotifyOut.SessID == result.SessID);
                                if (elem != null)
                                {
                                    CResultsCache newItem = new()
                                    {
                                        CUNotifyOut = new CUNotifyOut(elem.CUNotifyOut)
                                        {
                                            Status = result.Status,
                                            CurTime = result.CurTime,
                                            Succ = result.Succ,
                                            Fail = result.Fail,
                                            FromCUID = result.FromCUID,
                                            CUSitID = result.SitID,
                                            CUName = result.CUName,
                                            StartTime = result.StartTime,
                                        },
                                        CUSitName = result.SitName
                                    };

                                    await table.AddItem(newItem, x =>
                                    {
                                        return (x.CUNotifyOut.SessID == result.SessID && x.CUNotifyOut.CUSitID.Equals(result.SitID) && x.CUNotifyOut.FromCUID.ObjType == result.FromCUID.ObjType);
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                //Console.WriteLine("Error parse CUNotifyOut method Fire_UpdateStatListCache");
            }
        }


        ItemsProvider<CResultsCache> GetProvider
        {
            get
            {
                return new ItemsProvider<CResultsCache>(ThList, LoadChildList, request, new List<int>() { 15, 7, 7, 20, 13, 15, 7, 7 });
            }
        }

        private async ValueTask<IEnumerable<CResultsCache>> LoadChildList(GetItemRequest req)
        {
            List<CResultsCache> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/CreateStatListCache", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<CResultsCache>>() ?? new();
            }
            return newData;
        }


        private async ValueTask<IEnumerable<Hint>> LoadHelpSitName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetSituationForStatStaff", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpUnitName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetObjForStatStaff", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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
            var result = await Http.PostAsJsonAsync("api/v1/GetAllStateCUForStat", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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
