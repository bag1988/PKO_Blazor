using System.Net.Http.Json;
using SMP16XProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SMDataServiceProto.V1;
using SMSSGsoProto.V1;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;
using Google.Protobuf.WellKnownTypes;
using LibraryProto.Helpers;
using BlazorLibrary.Helpers;
using Microsoft.JSInterop;
using static BlazorLibrary.Shared.Main;
using GsoReporterProto.V1;
using BlazorLibrary.GlobalEnums;

namespace BlazorLibrary.Shared.NotifyLog
{
    partial class DataGridViewCommonInfoSMP
    {
        DateTime? StartDate => SelectSession?.TSessBeg?.ToDateTime().ToLocalTime();

        DateTime? EndDate => SelectSession?.TSessEnd?.ToDateTime().ToLocalTime();

        public CSessions? SelectSession { get; set; } = null;
               
        TableVirtualize<CSMP16xGetItemsINotifySess>? table;

        private CSMP16xGetItemsINotifySess? SelectItem = null;

        protected override async Task OnInitializedAsync()
        {
            request.ObjID.SubsystemID = SubsystemType.SUBSYST_P16x;
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["IDS_SUBSYSTCOLUMN"] },
                { 1, StartUIRep["IDS_SITUATIONCOLUMN"] },
                { 2, StartUIRep["IDS_SESSION_B"] },
                { 3, StartUIRep["IDS_SESSION_E"] },
                { 4, StartUIRep["IDS_COUNTALL"] },
                { 5, StartUIRep["IDS_COUNTYES"] },
                { 6, StartUIRep["IDS_COUNTNO"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.SubSystem), StartUIRep["IDS_SUBSYSTCOLUMN"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSubName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.SitName), StartUIRep["IDS_SITUATIONCOLUMN"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.TimeStartRange), StartUIRep["IDS_SESSION_B"], TypeHint.Time));

            HintItems.Add(new HintItem(nameof(FiltrModel.TimeEndRange), StartUIRep["IDS_SESSION_E"], TypeHint.Time));

            HintItems.Add(new HintItem(nameof(FiltrModel.CountAll), StartUIRep["IDS_COUNTALL"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.CountSuccess), StartUIRep["IDS_COUNTYES"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.CountFail), StartUIRep["IDS_COUNTNO"], TypeHint.Number));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrNotifyLogSmp);
        }

        ItemsProvider<CSMP16xGetItemsINotifySess> GetProvider => new ItemsProvider<CSMP16xGetItemsINotifySess>(ThList, LoadChildList, request, new List<int>() { 19, 18, 15, 15, 11, 11, 11 });

        private async ValueTask<IEnumerable<CSMP16xGetItemsINotifySess>> LoadChildList(GetItemRequest req)
        {
            List<CSMP16xGetItemsINotifySess> newData = new();
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                req.ObjID = SelectSession.ObjID;
                var result = await Http.PostAsJsonAsync("api/v1/GetItems_INotifySess", req, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    newData = await result.Content.ReadFromJsonAsync<List<CSMP16xGetItemsINotifySess>>() ?? new();
                }
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpSubName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetSubSystemForNotifyLogSmp", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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
                var result = await Http.PostAsJsonAsync("api/v1/GetSituationForNotifyLogSmp", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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

        private async Task RefreshTable()
        {
            SelectItem = null;
            if (table != null)
                await table.ResetData();
        }

        public async Task Refresh(CSessions item)
        {
            ResetToken();           
            SelectSession = item;
            await CallRefreshData();
        }

        void DbClick(CSMP16xGetItemsINotifySess? item)
        {
            MyNavigationManager.NavigateTo($"/PuNotifyLog/{item?.SubSessID}?systemId={item?.SitID?.SubsystemID}");
        }

        private void SeSelectItem(List<CSMP16xGetItemsINotifySess>? list)
        {
            SelectItem = list?.FirstOrDefault();
        }

        private async Task GetReport()
        {
            if (SelectSession?.ObjID == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }
            List<CSMP16xGetItemsINotifySess> reportData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_INotifySess", new GetItemRequest(request) { ObjID = SelectSession.ObjID, CountData = 0 }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                reportData = await result.Content.ReadFromJsonAsync<List<CSMP16xGetItemsINotifySess>>() ?? new();
            }
            if (reportData.Count == 0)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }

            ReportInfo RepInfo = new();

            RepInfo.Name = SMP16xRep["REPORT_TITLE"];

            List<GetColumnsExItem>? ColumnList = new() {
            new GetColumnsExItem(){NColumnId = 3, NStatus=1, TName=SqlRep["2003"]},//Общая информация
            new GetColumnsExItem(){NColumnId = 4, NStatus=1,TContrName=StartUIRep["IDS_SUBSYSTCOLUMN"], TName=StartUIRep["IDS_SUBSYSTCOLUMN"]},//Подсистема
            new GetColumnsExItem(){NColumnId = 5, NStatus=1, TContrName=StartUIRep["IDS_SITUATION"],TName=StartUIRep["IDS_SITUATION"]},//Сценарий
            new GetColumnsExItem(){NColumnId = 6, NStatus=1, TContrName=StartUIRep["IDS_SESSION_B"],TName=StartUIRep["IDS_SESSION_B"]},//Начало
            new GetColumnsExItem(){NColumnId = 7, NStatus=1, TContrName=StartUIRep["IDS_SESSION_E"],TName=StartUIRep["IDS_SESSION_E"]},//Окончание
            new GetColumnsExItem(){NColumnId = 8, NStatus=1, TContrName=StartUIRep["IDS_COUNTALL"],TName=StartUIRep["IDS_COUNTALL"]},//Всего
            new GetColumnsExItem(){NColumnId = 9, NStatus=1, TContrName=StartUIRep["IDS_COUNTYES"],TName=StartUIRep["IDS_COUNTYES"]},//Оповещены
            new GetColumnsExItem(){NColumnId = 10, NStatus=1, TContrName=StartUIRep["IDS_COUNTNO"],TName=StartUIRep["IDS_COUNTNO"]},//Не оповещены
            new GetColumnsExItem(){NColumnId = 200, NStatus=0, TName=""},//Компактное расположение
            new GetColumnsExItem(){NColumnId = 201, NStatus=0, TName=""}//Центрирование информации
            };

            var html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, 10, reportData, _OtherForReport.SmpOther(StartDate, EndDate, SelectSession.TSitName), _OtherForReport.FiltrOther(FiltrItemsToString));

            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(html));

            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "NotifySessReportP16x.html", streamRef);

        }
    }
}
