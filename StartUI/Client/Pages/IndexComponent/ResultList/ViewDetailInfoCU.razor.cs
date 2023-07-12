using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using GsoReporterProto.V1;
using StaffDataProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SMSSGsoProto.V1;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Table;
using FiltersGSOProto.V1;
using LibraryProto.Helpers;
using BlazorLibrary.Helpers;
using BlazorLibrary.Shared;
using System.ComponentModel;
using System.Text.Json;
using Google.Protobuf;
using SharedLibrary.Interfaces;
using SharedLibrary;
using BlazorLibrary.GlobalEnums;

namespace StartUI.Client.Pages.IndexComponent.ResultList
{
    partial class ViewDetailInfoCU : IAsyncDisposable, IPubSubMethod
    {
        [Parameter]
        public RenderFragment? TSticky { get; set; }

        [Parameter]
        public bool SetSubscribe { get; set; } = false;

        TableVirtualize<CGetResExStat>? table;

        int SessionId = 0;

        protected override async Task OnInitializedAsync()
        {
            request.LSortOrder = 2;
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["IDS_SITUATIONCOLUMN"] },
                { 1, StartUIRep["IDS_SUBSYSTCOLUMN"] },
                { 2, StartUIRep["IDS_STAFF_OBJ"] },
                { 3, StartUIRep["IDS_EVENT_TYPE"] },
                { 4, StartUIRep["IDS_PROPERTY"] },
                { 5, StartUIRep["IDS_ADDRESS"] },
                { 6, StartUIRep["IDS_GLOB_NUM"] },
                { 7, StartUIRep["IDS_TIME"] },
                { 8, StartUIRep["IDS_CHANNEL_STATE"] },
                { 9, StartUIRep["IDS_CU_NAME"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.SitName), StartUIRep["IDS_SITUATIONCOLUMN"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.PpSitSubsystemid), StartUIRep["IDS_SUBSYSTCOLUMN"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSubName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.ObjName), StartUIRep["IDS_STAFF_OBJ"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpObjName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.ObjType), StartUIRep["IDS_EVENT_TYPE"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpObjTypeName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.ObjDefine), StartUIRep["IDS_PROPERTY"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpObjDefine)));

            HintItems.Add(new HintItem(nameof(FiltrModel.ObjAddress), StartUIRep["IDS_ADDRESS"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpObjAddress)));

            HintItems.Add(new HintItem(nameof(FiltrModel.TimeRange), StartUIRep["IDS_TIME"], TypeHint.Time));

            HintItems.Add(new HintItem(nameof(FiltrModel.Status), StartUIRep["IDS_CHANNEL_STATE"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpStateName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.UnitName), StartUIRep["IDS_CU_NAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpCuName)));

            await OnInitFiltr(RefreshTable, SetSubscribe ? FiltrName.FiltrViewStatDetaliStaff : FiltrName.FiltrViewResultDetaliStaff);

            if (SetSubscribe)
                _ = _HubContext.SubscribeAsync(this);
        }


        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteExStatistics(byte[] Value)
        {
            if (SetSubscribe)
            {
                try
                {
                    var result = CGetResExStat.Parser.ParseFrom(Value);

                    if (result.RecID > 0)
                    {
                        if (table != null)
                        {
                            await table.AddItem(result, x =>
                            {
                                return x.RecID == result.RecID;
                            });
                        }
                    }
                }
                catch
                {
                    //Console.WriteLine("Error parse CGetResExStat method Fire_InsertDeleteExStatistics");
                }
            }
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateExStatistics(byte[] Value)
        {
            if (SetSubscribe)
            {
                try
                {
                    var result = CGetResExStat.Parser.ParseFrom(Value);
                    if (result.RecID > 0)
                    {
                        if (table != null)
                        {
                            await table.ForEachItems(x =>
                            {
                                if (x.RecID == result.RecID)
                                {
                                    x.StatusName = result.StatusName;
                                    x.CurTime = result.CurTime;
                                    return;
                                }
                            });
                        }
                    }
                }
                catch
                {
                    //Console.WriteLine("Error parse CGetResExStat method Fire_UpdateExStatistics");
                }
            }
        }

        ItemsProvider<CGetResExStat> GetProvider => new ItemsProvider<CGetResExStat>(ThList, LoadChildList, request, new List<int>() { 10, 5, 20, 15, 10, 10, 5, 5, 15, 5 });

        private async ValueTask<IEnumerable<CGetResExStat>> LoadChildList(GetItemRequest req)
        {
            List<CGetResExStat> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_ICUExResult", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<CGetResExStat>>() ?? new();
                if (SessionId == 0 && newData.Count > 0)
                    SessionId = newData.First().SessID;
            }
            else
            {
                MessageView?.AddError("", StartUIRep["IDS_EFAILGETSESSIONINFO"]);
            }
            return newData;
        }
                
        private async ValueTask<IEnumerable<Hint>> LoadHelpCuName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SessionId > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetCuNameForICUExResult", new IntAndString() { Number = SessionId, Str = req.BstrFilter }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                    if (response?.Count > 0)
                    {
                        newData.AddRange(response.Select(x => new Hint(x.Str)));
                    }
                }
            }
            return newData ?? new();
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpSitName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SessionId > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetSituationForICUExResult", new IntAndString() { Number = SessionId, Str = req.BstrFilter }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                    if (response?.Count > 0)
                    {
                        newData.AddRange(response.Select(x => new Hint(x.Str)));
                    }
                }
            }
            return newData ?? new();
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpObjAddress(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SessionId > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetObjAddressForICUExResult", new IntAndString() { Number = SessionId, Str = req.BstrFilter }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                    if (response?.Count > 0)
                    {
                        newData.AddRange(response.Select(x => new Hint(x.Str)));
                    }
                }
            }
            return newData ?? new();
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpObjTypeName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SessionId > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetObjTypeForICUExResult", new IntAndString() { Number = SessionId, Str = req.BstrFilter }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                    if (response?.Count > 0)
                    {
                        newData.AddRange(response.Select(x => new Hint(x.Str)));
                    }
                }
            }
            return newData ?? new();
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpObjDefine(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SessionId > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetObjDefineForICUExResultAsync", new IntAndString() { Number = SessionId, Str = req.BstrFilter }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                    if (response?.Count > 0)
                    {
                        newData.AddRange(response.Select(x => new Hint(x.Str)));
                    }
                }
            }
            return newData ?? new();
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpObjName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SessionId > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetObjNameForICUExResult", new IntAndString() { Number = SessionId, Str = req.BstrFilter }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                    if (response?.Count > 0)
                    {
                        newData.AddRange(response.Select(x => new Hint(x.Str)));
                    }
                }
            }
            return newData ?? new();
        }

        private ValueTask<IEnumerable<Hint>> LoadHelpSubName(GetItemRequest req)
        {
            List<Hint>? newData = new()
            {
                new Hint(SMDataRep["SUBSYST_ASO"], "1"),
                new Hint(SMDataRep["SUBSYST_SZS"], "2"),
                new Hint(SMDataRep["SUBSYST_GSO_STAFF"], "3")
            };
            return new(newData ?? new());
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpStateName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SessionId > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetStatusFromICUExResultBySessId", new IntAndString() { Number = SessionId, Str = req.BstrFilter }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadFromJsonAsync<List<CGetAllStateBySessId>>();

                    if (response?.Count > 0)
                    {
                        newData.AddRange(response.Select(x => new Hint(x.Resultname, x.Status.ToString())));
                    }
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
