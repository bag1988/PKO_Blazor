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
using BlazorLibrary.GlobalEnums;

namespace BlazorLibrary.Shared.NotifyLog
{
    partial class DataGridViewCommonInfo
    {
        DateTime? StartDate => SelectSession?.TSessBeg?.ToDateTime().ToLocalTime();

        DateTime? EndDate => SelectSession?.TSessEnd?.ToDateTime().ToLocalTime();

        string? SitName => SelectSession?.TSitName;

        public CSessions? SelectSession { get; set; } = null;

        private bool IsData = false;

        private AudioPlayerStream? player = default!;

        private CUResultsEx? SelectItem = null;

        TableVirtualize<CUResultsEx>? table;

        protected override async Task OnInitializedAsync()
        {
            request.LSortOrder = 6;
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["IDS_CMD_SOURCE"] },
                { 1, StartUIRep["IDS_START_TIME"] },
                { 2, StartUIRep["IDS_END_TIME"] },
                { 3, StartUIRep["IDS_SITUATIONCOLUMN"] },
                { 4, StartUIRep["IDS_STAFF_OBJ"] },
                { 5, StartUIRep["IDS_CMD_RESULT"] },
                { 6, StartUIRep["IDS_SUCC"] },
                { 7, StartUIRep["IDS_FAIL"] },
                { 8, StartUIRep["IDS_SIT_MSG_NAME"] }
            };


            HintItems.Add(new HintItem(nameof(FiltrModel.SitName), StartUIRep["IDS_SITUATIONCOLUMN"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.CuName), StartUIRep["IDS_STAFF_OBJ"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpUnitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.TimeStartRange), StartUIRep["IDS_START_TIME"], TypeHint.Time));

            HintItems.Add(new HintItem(nameof(FiltrModel.TimeEndRange), StartUIRep["IDS_END_TIME"], TypeHint.Time));

            HintItems.Add(new HintItem(nameof(FiltrModel.ResultName), StartUIRep["IDS_CMD_RESULT"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpStateName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.CountSuccess), StartUIRep["IDS_SUCC"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.CountFail), StartUIRep["IDS_FAIL"], TypeHint.Number));
                        
            await OnInitFiltr(RefreshTable, FiltrName.FiltrNotifyLogStaff);
        }

        ItemsProvider<CUResultsEx> GetProvider
        {
            get
            {
                return new ItemsProvider<CUResultsEx>(ThList, LoadChildList, request, new List<int>() { 15, 7, 7, 20, 13, 15, 7, 7 });
            }
        }

        private async ValueTask<IEnumerable<CUResultsEx>> LoadChildList(GetItemRequest req)
        {
            List<CUResultsEx> newData = new();
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                req.ObjID = SelectSession.ObjID;
                var result = await Http.PostAsJsonAsync("api/v1/GetItemsEx_INotifySess", req, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    newData = await result.Content.ReadFromJsonAsync<List<CUResultsEx>>() ?? new();
                }
                else
                {
                    MessageView?.AddError("", StartUIRep["IDS_EFAILGETSESSIONINFO"]);
                }
            }
            return newData;
        }


        private async ValueTask<IEnumerable<Hint>> LoadHelpSitName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetSituationForNotifyLogStaff", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpUnitName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetUnitNameForNotifyLogStaff", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpStateName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetAllStateCUBySessId", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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

        private async Task DbClick()
        {
            if (SelectItem == null)
                return;
            IsData = true;
            await Task.Yield();
            if (SelectItem.MsgID?.ObjID > 0 && player != null)
            {
                await player.SetUrlSound($"api/v1/GetSoundServer?MsgId={SelectItem.MsgID.ObjID}&Staff={SelectItem.MsgID.StaffID}&System={SelectItem.MsgID.SubsystemID}&version={DateTime.Now.Second}");
            }
        }

        private async Task RefreshTable()
        {
            SelectItem = null;
            if (table != null)
                await table.ResetData();
        }

        private void SeSelectItem(List<CUResultsEx>? list)
        {
            IsData = false;
            SelectItem = list?.FirstOrDefault();
        }

        private async Task GetReport()
        {
            if (SelectSession?.ObjID == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }

            List<CUResultsEx> reportData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItemsEx_INotifySess", new GetItemRequest(request) { CountData = 0 }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                reportData = await result.Content.ReadFromJsonAsync<List<CUResultsEx>>() ?? new();
            }


            if (reportData.Count == 0)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }


            OBJ_ID ReportId = new OBJ_ID() { ObjID = 5, SubsystemID = SubsystemType.SUBSYST_GSO_STAFF };

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

            List<CUResultView>? CuDetali = reportData.Select(x => new CUResultView()
            {
                BTime = x.TStartTime.ToDateTime().ToLocalTime().ToString("T"),
                ETime = x.TLastTime.ToDateTime().ToLocalTime().ToString("T"),
                SitName = x.TSitName,
                ObjName = x.TCUName,
                StatusName = x.ResultName,
                Succes = x.TSucc.ToString(),
                Fail = x.TFail.ToString()
            }).ToList();


            if (CuDetali == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }


            var html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, 5, CuDetali, _OtherForReport.CUResultView(SitName, StartDate, EndDate, reportData.Sum(x => x.TSucc), reportData.Sum(x => x.TFail)), _OtherForReport.FiltrOther(FiltrItemsToString));

            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(html));

            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "NotifySessReportCu.html", streamRef);
        }

        public async Task Refresh(CSessions? item)
        {
            ResetToken();
            IsData = false;
            SelectSession = item;
            await CallRefreshData();
        }
    }
}
