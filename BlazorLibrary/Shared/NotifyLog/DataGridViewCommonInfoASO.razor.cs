using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using AsoDataProto.V1;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Helpers;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Table;
using FiltersGSOProto.V1;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using GsoReporterProto.V1;
using LibraryProto.Helpers;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.Interfaces;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using SMSSGsoProto.V1;
using static BlazorLibrary.Shared.Main;

namespace BlazorLibrary.Shared.NotifyLog
{
    partial class DataGridViewCommonInfoASO : IAsyncDisposable, IPubSubMethod
    {
        DateTime? StartDate => SelectSession?.TSessBeg?.ToDateTime().ToLocalTime();

        DateTime? EndDate => SelectSession?.TSessEnd?.ToDateTime().ToLocalTime();

        public CSessions? SelectSession { get; set; } = null;

        private bool IsViewHistory = false;

        TableVirtualize<CResults>? table;
        CountCallASO CountCall = new();

        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["IDS_SITUATIONCOLUMN"] },
                { 1, StartUIRep["IDS_ABONENTNAME"] },
                { 2, StartUIRep["IDS_PRIORITY"] },
                { 3, StartUIRep["IDS_DEPARTMENT"] },
                { 4, StartUIRep["IDS_CHANNEL_STATE"] },
                { 5, StartUIRep["IDS_TIME"] },
                { 6, StartUIRep["IDS_PHONE"] },
                { 7, StartUIRep["IDS_COUNT"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.SitName), StartUIRep["IDS_SITUATIONCOLUMN"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.AbonName), StartUIRep["IDS_ABONENTNAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpAbonName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Priority), StartUIRep["IDS_PRIORITY"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.Depart), StartUIRep["IDS_DEPARTMENT"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpDepName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.State), StartUIRep["IDS_CHANNEL_STATE"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpStateName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Connect), StartUIRep["IDS_PHONE"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpPhoneName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Count), StartUIRep["IDS_COUNT"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.TimeRange), StartUIRep["IDS_TIME"], TypeHint.Time));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrNotifyLogAso);

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
                    if (result?.OBJID != null && result.OBJID.ObjID > 0 && result.OBJID.StaffID == SelectSession?.ObjID?.ObjID)
                    {
                        if (table != null)
                        {
                            await table.ForEachItems(x =>
                            {
                                if (x.AbID.ObjID == result.OBJID.ObjID && x.NSessID == result.OBJID.StaffID)
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

        ItemsProvider<CResults> GetProvider => new ItemsProvider<CResults>(ThList, LoadChildList, request, new List<int>() { 20, 13, 10, 15, 12, 10, 10, 10 });

        private async ValueTask<IEnumerable<CResults>> LoadChildList(GetItemRequest req)
        {
            List<CResults> newData = new();
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                req.ObjID = SelectSession.ObjID;
                var result = await Http.PostAsJsonAsync("api/v1/GetItems_INotifySess", req, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    newData = await result.Content.ReadFromJsonAsync<List<CResults>>() ?? new();
                }
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpSitName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetSituationForNotifyLog", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpAbonName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetAbonForNotifyLog", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpPhoneName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetConnParamForNotifyLog", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpDepName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetDepForNotifyLogAso", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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
                var result = await Http.PostAsJsonAsync("api/v1/GetStateNameForNotifyLog", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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

        private async Task GetCountCall()
        {
            CountCall = new();
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetSessionCountCallObjects", new GetItemRequest(request) { ObjID = new OBJ_ID() { ObjID = SelectSession.ObjID.ObjID }, CountData = 0 }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    CountCall = await result.Content.ReadFromJsonAsync<CountCallASO>() ?? new();
                }
            }
            StateHasChanged();
        }

        private void GetInfoCall()
        {
            if (SelectSession?.ObjID == null)
                return;
            if (CountCall.CountCall > 0 || CountCall.CountUnCall > 0)
                IsViewHistory = true;
        }

        private async Task GetReport()
        {
            if (SelectSession?.ObjID == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }

            OBJ_ID ReportId = new OBJ_ID() { ObjID = 3, SubsystemID = SubsystemType.SUBSYST_ASO };

            ReportInfo? RepInfo = null;

            var result = await Http.PostAsJsonAsync("api/v1/GetReportInfo", new IntID() { ID = ReportId.ObjID });
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

            List<AsoReportTime>? asoReports = null;

            List<List<string>> dopTable = new();



            result = await Http.PostAsJsonAsync("api/v1/GetINotifySessReportAso", new GetItemRequest(request) { ObjID = new(request.ObjID) { StaffID = ColumnList.FirstOrDefault(x => x.NColumnId == 100)?.NStatus ?? 0 }, CountData = 0 }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var asoReportsDate = await result.Content.ReadFromJsonAsync<List<AsoReport>>();

                if (asoReportsDate?.Count > 0)
                {
                    asoReports = asoReportsDate.Select(a => new AsoReportTime()
                    {
                        AbName = a.AbName,
                        AbPrior = a.AbPrior,
                        ConnParam = a.ConnParam,
                        CountCall = a.CountCall,
                        Date = a.Date,
                        DepName = a.DepName,
                        MsgName = a.MsgName,
                        Position = a.Position,
                        ResultName = a.ResultName,
                        Details =DateTime.Now.ToString(),// a.Details,
                        SitName = a.SitName,
                        Time = a.Time != null ? a.Time.ToDateTime().ToLocalTime().ToString("T") : ""
                    }).ToList();


                    var headerDopTable = new List<string>() { StartUIRep["IDS_CHANNEL_STATE"], GsoRep["IDS_STRING_COUNT"], GsoRep["IDS_STRING_PROCENT"] };

                    dopTable.Add(headerDopTable);
                    var v = asoReportsDate.GroupBy(x => x.ResultName).Select(x => new List<string>() { x.Key, $"{x.Count()}/{asoReportsDate.Count}", $"{Math.Ceiling(((double)x.Count() / asoReportsDate.Count) * 100)}%" });
                    dopTable.AddRange(v);



                }
            }

            if (asoReports == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }

            List<string> bodyContent = new();

            //добавляем столбец для СКУД
            if (asoReports.Any(x => !string.IsNullOrEmpty(x.Details)))
                ColumnList.Add(new GetColumnsExItem() { NColumnId = 33, NStatus = 1, TContrName = Rep["Pass"], TName = Rep["Pass"] });//Прибытие

            if (ColumnList.FirstOrDefault(x => x.NColumnId == 3)?.NStatus == 1)
            {
                bodyContent.Add(ReportGenerate.CreateHtmlTable(new string[] { GsoRep["IDS_STRING_NAME"], AsoRep["Value"] }, _OtherForReport.AsoOther(StartDate, EndDate, CountCall.CountCall, CountCall.CountUnCall), GsoRep["IDS_SHARED_INFO"]));
            }


            if (dopTable.Count > 0)
            {
                bodyContent.Add(ReportGenerate.CreateHtmlTable(dopTable.First(), dopTable.Skip(1), GsoRep["IDS_SHORT_INFO"]));
            }

            List<string> sectionContent = new();
            foreach (var groupItem in asoReports.GroupBy(x => x.SitName))
            {
                sectionContent.Add(ReportGenerate.CreateHtmlTableForProto(ColumnList, 3, groupItem.ToList(), groupItem.Key));
            }

            bodyContent.Add(ReportGenerate.CreateHtmlSection(string.Join("", sectionContent), GsoRep["IDS_SIT_INFO"]));

            bool Center = ColumnList.FirstOrDefault(x => x.NColumnId == 201)?.NStatus == 0 ? false : true;

            var html = ReportGenerate.GetHtml(bodyContent, RepInfo, Center, _OtherForReport.FiltrOther(FiltrItemsToString));

            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(html));

            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "NotifySessReportAso.html", streamRef);
        }


        private async Task GetPhonogramListBySess()
        {
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetPhonogramListBySess", new GetItemRequest(request) { ObjID = new OBJ_ID() { ObjID = SelectSession.ObjID.ObjID }, CountData = 0 }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var arrayFilePath = await result.Content.ReadFromJsonAsync<List<string>>() ?? new();

                    foreach (var filePath in arrayFilePath)
                    {
                        await JSRuntime.InvokeVoidAsync("triggerFileDownload", Path.GetFileNameWithoutExtension(filePath), $"api/v1/GetFileByPhonogram?filePath={filePath}");
                    }
                }
            }
        }


        private async Task RefreshTable()
        {
            if (table != null)
                await table.ResetData();
            await GetCountCall();
        }

        public async Task Refresh(CSessions? item)
        {
            ResetToken();
            SelectSession = item;
            await CallRefreshData();

        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
