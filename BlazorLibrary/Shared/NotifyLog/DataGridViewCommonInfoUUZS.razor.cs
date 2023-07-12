using System.Net.Http.Json;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Helpers;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Table;
using FiltersGSOProto.V1;
using Google.Protobuf.WellKnownTypes;
using GsoReporterProto.V1;
using LibraryProto.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using SMSSGsoProto.V1;
using StaffDataProto.V1;
using static BlazorLibrary.Shared.Main;

namespace BlazorLibrary.Shared.NotifyLog
{
    partial class DataGridViewCommonInfoUUZS
    {
        public CSessions? SelectSession { get; set; } = null;

        TableVirtualize<CResults>? table;
        SZSSessResultReport CountCall = new();

        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["IDS_SITUATIONCOLUMN"] },
                { 1, StartUIRep["IDS_DEVNAME"] },
                { 2, StartUIRep["IDS_PRIORITY"] },
                { 3, StartUIRep["IDS_DEPARTMENT"] },
                { 4, StartUIRep["IDS_CHANNEL_STATE"] },
                { 5, StartUIRep["IDS_TIME"] },
                { 6, StartUIRep["IDS_CONNECT"] },
                { 7, StartUIRep["IDS_COUNT"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.SitName), StartUIRep["IDS_SITUATIONCOLUMN"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.AbonName), StartUIRep["IDS_DEVNAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpDevName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Priority), StartUIRep["IDS_PRIORITY"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.Depart), StartUIRep["IDS_DEPARTMENT"], TypeHint.Input));

            HintItems.Add(new HintItem(nameof(FiltrModel.State), StartUIRep["IDS_CHANNEL_STATE"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpStateName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Connect), StartUIRep["IDS_CONNECT"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpConnParamName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Count), StartUIRep["IDS_COUNT"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.TimeRange), StartUIRep["IDS_TIME"], TypeHint.Time));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrNotifyLogUzs);
        }

        ItemsProvider<CResults> GetProvider => new ItemsProvider<CResults>(ThList, LoadChildList, request, new List<int>() { 25, 10, 10, 10, 20, 10, 10, 5 });

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

        private async ValueTask<IEnumerable<Hint>> LoadHelpDevName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (SelectSession?.ObjID?.ObjID > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetDevNameForNotifyLog", new IntAndString() { Number = SelectSession.ObjID.ObjID, Str = req.BstrFilter }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpConnParamName(GetItemRequest req)
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
                var result = await Http.PostAsJsonAsync("api/v1/SZSSessResult", new IntID() { ID = SelectSession.ObjID.ObjID }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    CountCall = await result.Content.ReadFromJsonAsync<SZSSessResultReport>() ?? new();
                }
            }
            StateHasChanged();
        }


        private async Task RefreshTable()
        {
            if (table != null)
                await table.ResetData();
            await GetCountCall();
        }

        private async Task GetReport()
        {
            if (SelectSession?.ObjID == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }
            List<CResults> reportData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_INotifySess", new GetItemRequest(request) { ObjID = SelectSession.ObjID, CountData = 0 }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                reportData = await result.Content.ReadFromJsonAsync<List<CResults>>() ?? new();
            }
            if (reportData.Count == 0)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }

            OBJ_ID ReportId = new OBJ_ID() { ObjID = 3, SubsystemID = SubsystemType.SUBSYST_ASO };

            ReportInfo RepInfo = new();

            RepInfo.Name = UUZSRep["REPORT_TITLE"];

            List<GetColumnsExItem>? ColumnList = new() {
            new GetColumnsExItem(){NColumnId = 3, NStatus=1, TName=SqlRep["2003"]},//Общая информация
            new GetColumnsExItem(){NColumnId = 4, NStatus=1,TContrName=StartUIRep["IDS_SITUATION"], TName=StartUIRep["IDS_SITUATION"]},//Сценарий
            new GetColumnsExItem(){NColumnId = 5, NStatus=1, TContrName=StartUIRep["IDS_DEVNAME"],TName=StartUIRep["IDS_DEVNAME"]},//Устройство
            new GetColumnsExItem(){NColumnId = 6, NStatus=1, TContrName=UUZSRep["IDS_STRING_ALIGMENT"],TName=UUZSRep["IDS_STRING_ALIGMENT"]},//Принадлежность
            new GetColumnsExItem(){NColumnId = 7, NStatus=1, TContrName=StartUIRep["IDS_STATUS"],TName=StartUIRep["IDS_STATUS"]},//Состояние
            new GetColumnsExItem(){NColumnId = 8, NStatus=1, TContrName=StartUIRep["IDS_TIME"],TName=StartUIRep["IDS_TIME"]},//Время
            new GetColumnsExItem(){NColumnId = 9, NStatus=1, TContrName=GsoRep["IDS_CONNECT"],TName=GsoRep["IDS_CONNECT"]},//Связь
            new GetColumnsExItem(){NColumnId = 10, NStatus=1, TContrName=StartUIRep["IDS_COUNT"],TName=StartUIRep["IDS_COUNT"]},//Попытки оповещения
            new GetColumnsExItem(){NColumnId = 200, NStatus=0, TName=""},//Компактное расположение
            new GetColumnsExItem(){NColumnId = 201, NStatus=0, TName=""}//Центрирование информации
            };

            var html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, 8, reportData, _OtherForReport.SzsOther(CountCall?.SessBeg?.ToDateTime().ToLocalTime(), CountCall?.SessEnd?.ToDateTime().ToLocalTime(), CountCall?.CountNotify, CountCall?.CountNoNotify), _OtherForReport.FiltrOther(FiltrItemsToString));

            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(html));

            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "NotifySessReportSZS.html", streamRef);

        }

        public async Task Refresh(CSessions? item)
        {
            ResetToken();
            SelectSession = item;
            await CallRefreshData();            
        }
    }
}
