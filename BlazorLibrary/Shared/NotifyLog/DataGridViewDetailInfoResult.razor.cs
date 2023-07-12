using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using GsoReporterProto.V1;
using StaffDataProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SMSSGsoProto.V1;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Table;
using FiltersGSOProto.V1;
using LibraryProto.Helpers;
using BlazorLibrary.Helpers;
using BlazorLibrary.GlobalEnums;

namespace BlazorLibrary.Shared.NotifyLog
{
    partial class DataGridViewDetailInfoResult
    {
        DateTime? StartDate => SelectSession?.TSessBeg?.ToDateTime().ToLocalTime();

        DateTime? EndDate => SelectSession?.TSessEnd?.ToDateTime().ToLocalTime();

        string? SitName => SelectSession?.TSitName;

        public CSessions? SelectSession { get; set; } = null;

        TableVirtualize<CGetResExStat>? table;

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

            await OnInitFiltr(RefreshTable, FiltrName.FiltrNotifyLogDetaliStaff);
        }

        ItemsProvider<CGetResExStat> GetProvider => new ItemsProvider<CGetResExStat>(ThList, LoadChildList, request, new List<int>() { 10, 5, 20, 15, 10, 10, 5, 5, 15, 5 });

        private async ValueTask<IEnumerable<CGetResExStat>> LoadChildList(GetItemRequest req)
        {
            List<CGetResExStat> newData = new();
            if (SelectSession != null)
            {
                req.ObjID = SelectSession.ObjID;
                var result = await Http.PostAsJsonAsync("api/v1/GetItems_ICUExResult", req, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    newData = await result.Content.ReadFromJsonAsync<List<CGetResExStat>>() ?? new();
                }
                else
                {
                    MessageView?.AddError("", StartUIRep["IDS_EFAILGETSESSIONINFO"]);
                }
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpCuName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetCuNameForICUExResult", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetSituationForICUExResult", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetObjAddressForICUExResult", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetObjTypeForICUExResult", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetObjDefineForICUExResultAsync", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetObjNameForICUExResult", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetStatusFromICUExResultBySessId", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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

        private async Task GetReport()
        {
            if (SelectSession?.ObjID == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }

            List<CGetResExStat> reportData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_ICUExResult", new GetItemRequest(request) { CountData = 0 }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                reportData = await result.Content.ReadFromJsonAsync<List<CGetResExStat>>() ?? new();
            }

            if (reportData.Count == 0)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }


            OBJ_ID ReportId = new OBJ_ID() { ObjID = 7, SubsystemID = SubsystemType.SUBSYST_GSO_STAFF };

            ReportInfo? RepInfo = null;

            result = await Http.PostAsJsonAsync("api/v1/GetReportInfo", new IntID() { ID = ReportId.ObjID });
            if (result.IsSuccessStatusCode)
            {
                RepInfo = await result.Content.ReadFromJsonAsync<ReportInfo>();
            }


            if (RepInfo == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], "ReportInfo " + Rep["NoData"]);
                return;
            }

            List<GetColumnsExItem>? ColumnList = null;

            result = await Http.PostAsJsonAsync("api/v1/GetColumnsEx", ReportId);
            if (result.IsSuccessStatusCode)
            {
                ColumnList = await result.Content.ReadFromJsonAsync<List<GetColumnsExItem>>();
            }

            if (ColumnList == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], "ColumnsEx " + Rep["NoData"]);
                return;
            }

            List<CUDetaliResult>? CuDetali = reportData.Select(x => new CUDetaliResult()
            {
                SitName = x.SitName + "(" + x.SubSystName + ")",
                ObjName = x.ObjName,
                ObjType = x.ObjType,
                ObjDefine = x.ObjDefine,
                StatusName = x.StatusName,
                UnitName = x.CUName
            }).ToList();


            if (CuDetali == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }

            var html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, 7, CuDetali, _OtherForReport.CUDetaliOther(SitName, StartDate, EndDate), _OtherForReport.FiltrOther(FiltrItemsToString));

            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(html));

            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "NotifySessReportCuDetali.html", streamRef);
        }

        public async Task Refresh(CSessions? item)
        {
            ResetToken();
            SelectSession = item;
            await CallRefreshData();
            StateHasChanged();
        }

    }
}
