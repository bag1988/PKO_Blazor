using System.Net.Http.Json;
using System.Text.Encodings.Web;
using BlazorLibrary;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Audio;
using BlazorLibrary.Shared.Table;
using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using GsoReporterProto.V1;
using LibraryProto.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using FiltersGSOProto.V1;
using BlazorLibrary.Helpers;
using SharedLibrary.Interfaces;
using System.ComponentModel;
using SharedLibrary;
using BlazorLibrary.GlobalEnums;

namespace StartUI.Client.Pages
{
    partial class HistoryCall : IAsyncDisposable, IPubSubMethod
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = 0;

        private HistoryCallItem? SelectItem;


        private AudioPlayerStream? player = default!;

        private bool IsData = false;

        private bool IsViewDeleteHistory = false;

        private bool IsViewPrintSetting = false;

        private readonly ReporterFont? ReportFont = new() { FontSize = 2, StrName = "'Times New Roman', Times, serif", Style = 0, Size = 10, Weight = 400 };

        private DateTime? DeleteTimeStartDelete { get; set; } = DateTime.Now;

        TableVirtualize<HistoryCallItem>? table;

        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { 0, "№" },//Сессия
                { 1, StartUIRep["IDS_SITUATION"] },//Сценарий
                { 2, StartUIRep["IDS_TIME"] },//Время
                { 3, StartUIRep["OBJECT_NOTIFY"] },//Объект оповещения
                { 4, StartUIRep["IDS_DEPARTMENT"] },//Принадлежность
                { 5, StartUIRep["LINE_NAME"] },//Линия
                { 6, StartUIRep["PARAM_NAME"] },//Параметр
                { 7, StartUIRep["IDS_CMD_RESULT"] },//Результат
                { 8, StartUIRep["Answer"] }//Ответ
            };
            request.ObjID.StaffID = await _User.GetLocalStaff();


            HintItems.Add(new HintItem(nameof(FiltrModel.AbName), StartUIRep["OBJECT_NOTIFY"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpAbName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Phone), StartUIRep["PARAM_NAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpPhone)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Answer), StartUIRep["Answer"], TypeHint.Input));

            HintItems.Add(new HintItem(nameof(FiltrModel.DepName), StartUIRep["IDS_DEPARTMENT"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpDepName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.StatusCode), StartUIRep["IDS_CMD_RESULT"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpStateName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.SessName), StartUIRep["SESSION_NAME"], TypeHint.Number, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSessName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.SitName), StartUIRep["IDS_SITUATION"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.LineName), StartUIRep["LINE_NAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpLineName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.DateRange), StartUIRep["IDS_TIME"], TypeHint.Date));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrHistoryCall);

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
                    if (result?.OBJID != null && result.OBJID.ObjID > 0 && result.OBJID.StaffID > 0)
                    {
                        if (table != null)
                        {
                            await table.ForEachItems(x =>
                            {
                                if (x.AbId == result.OBJID.ObjID && x.SessID == result.OBJID.StaffID)
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

        ItemsProvider<HistoryCallItem> GetProvider => new ItemsProvider<HistoryCallItem>(ThList, LoadChildList, request, new List<int>() { 5, 10, 10, 25, 10, 5, 5, 25, 5 });


        private async ValueTask<IEnumerable<HistoryCallItem>> LoadChildList(GetItemRequest req)
        {
            List<HistoryCallItem> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetNotifyHistory", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<HistoryCallItem>>() ?? new();
            }
            return newData;
        }

        //очистка истории
        private async Task ClearASOHistory()
        {
            Timestamp request = DeleteTimeStartDelete?.ToUniversalTime().ToTimestamp() ?? new();

            var result = await Http.PostAsJsonAsync("api/v1/ClearASOHistory", request, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var countDel = await result.Content.ReadFromJsonAsync<Int32Value>();
                MessageView?.AddMessage("", $"{GsoRep["DELETE_COUNT"]}: {countDel?.Value ?? 0}");
            }
            else
            {
                MessageView?.AddError("", GsoRep["ERROR_DELETE_HISTORY_CALL"]);
            }
            IsViewDeleteHistory = false;
        }


        // Получить из базы список линий для фильтра.
        private async ValueTask<IEnumerable<Hint>> LoadHelpLineName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetNotifyHistoryFiltrLine", new IntAndString() { Str = req.BstrFilter }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(x.Str)));
                }
            }
            else
            {
                MessageView?.AddError("", GsoRep["ERROR_LINE_LIST"]);
            }
            return newData ?? new();
        }

        // Получить из базы список объектов для фильтра
        private async ValueTask<IEnumerable<Hint>> LoadHelpAbName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetNotifyHistoryFiltrObj", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(x.Str)));
                }
            }
            else
            {
                MessageView?.AddError("", GsoRep["ERROR_ABON_LIST"]);
            }
            return newData ?? new();
        }


        // Получить из базы список сеансов для фильтра
        private async ValueTask<IEnumerable<Hint>> LoadHelpSessName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetNotifyHistoryFiltrSess", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<int>>();

                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(x.ToString())));
                }
            }
            else
            {
                MessageView?.AddError("", GsoRep["ERROR_SESS_LIST"]);
            }
            return newData ?? new();
        }

        //Получить из базы список ситуаций для фильтра
        private async ValueTask<IEnumerable<Hint>> LoadHelpSitName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetNotifyHistoryFiltrSit", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(x.Str)));
                }
            }
            else
            {
                MessageView?.AddError("", GsoRep["ERROR_SIT_LIST"]);
            }
            return newData ?? new();
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpDepName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetNotifyHistoryFiltrDep", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpPhone(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetNotifyHistoryFiltrPhone", new IntAndString() { Str = req.BstrFilter }, ComponentDetached);
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
            var result = await Http.PostAsJsonAsync("api/v1/GetAllStateFromNotifyhistory", new IntAndString() { Str = req.BstrFilter }, ComponentDetached);
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

        private async Task DbClick()
        {
            if (SelectItem == null) return;
            IsData = false;
            await GetFile();
        }

        private void SetSelectList(List<HistoryCallItem>? items)
        {
            IsData = false;
            SelectItem = items?.LastOrDefault();
        }

        private async Task GetFile()
        {
            if (SelectItem == null)
                return;

            IsData = true;
            await Task.Yield();
            if (player != null)
            {
                await player.SetUrlSound($"api/v1/ReadSoundFromFile?filename={UrlEncoder.Default.Encode(SelectItem.UrlFile)}");
            }
        }

        private async Task RefreshTable()
        {
            if (table != null)
                await table.ResetData();
        }

        private async Task GetReport()
        {
            List<HistoryCallItem> reportData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetNotifyHistory", new GetItemRequest(request) { CountData = 0 }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                reportData = await result.Content.ReadFromJsonAsync<List<HistoryCallItem>>() ?? new();
            }

            if (reportData.Count == 0)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }

            reportData.ForEach(x =>
            {
                x.ResultName = x.ResultName + ". " + x.Details;
            });

            ReportInfo RepInfo = new();

            RepInfo.Name = GsoRep["HISTORY_CALL"];
            RepInfo.Font = ReportFont?.GetBase64() ?? "";

            List<GetColumnsExItem>? ColumnList = new() {
            new GetColumnsExItem(){NColumnId = 4, NStatus=1,TContrName="№", TName="№"},//Сессия
            new GetColumnsExItem(){NColumnId = 5, NStatus=1,TContrName=StartUIRep["IDS_SITUATION"], TName=StartUIRep["IDS_SITUATION"]},//Сценарий
            new GetColumnsExItem(){NColumnId = 6, NStatus=1, TContrName=StartUIRep["IDS_TIME"],TName=StartUIRep["IDS_TIME"]},//Время
            new GetColumnsExItem(){NColumnId = 7, NStatus=1, TContrName=StartUIRep["OBJECT_NOTIFY"],TName=StartUIRep["OBJECT_NOTIFY"]},//Объект оповещения
            new GetColumnsExItem(){NColumnId = 8, NStatus=1, TContrName=StartUIRep["IDS_DEPARTMENT"],TName=StartUIRep["IDS_DEPARTMENT"]},//Принадлежность
            new GetColumnsExItem(){NColumnId = 9, NStatus=1, TContrName=StartUIRep["LINE_NAME"],TName=StartUIRep["LINE_NAME"]},//Линия
            new GetColumnsExItem(){NColumnId = 10, NStatus=1, TContrName=StartUIRep["PARAM_NAME"],TName=StartUIRep["PARAM_NAME"]},//Параметр
            new GetColumnsExItem(){NColumnId = 11, NStatus=1, TContrName=StartUIRep["IDS_CMD_RESULT"],TName=StartUIRep["IDS_CMD_RESULT"]},//Результат
            new GetColumnsExItem(){NColumnId = 12, NStatus=1, TContrName=StartUIRep["Answer"],TName=StartUIRep["Answer"]},//Ответ
            new GetColumnsExItem(){NColumnId = 200, NStatus=0, TName=""},//Компактное расположение
            new GetColumnsExItem(){NColumnId = 201, NStatus=0, TName=""}//Центрирование информации
            };

            var html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, 9, reportData, null, _OtherForReport.FiltrOther(FiltrItemsToString));

            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(html));

            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "NotifySessReportAso.html", streamRef);
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }

    }
}
