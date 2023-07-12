using System.ComponentModel;
using System.Net.Http.Json;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.Models;
using AsoDataProto.V1;
using FiltersGSOProto.V1;
using Google.Protobuf.WellKnownTypes;
using ReplaceLibrary;
using SharedLibrary.Models;
using LibraryProto.Helpers;
using BlazorLibrary.Helpers;
using SharedLibrary.Extensions;
using Google.Protobuf;
using SharedLibrary.Interfaces;
using SharedLibrary;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary;
using Microsoft.JSInterop;
using GsoReporterProto.V1;

namespace StartUI.Client.Pages
{
    partial class EventLogView : IAsyncDisposable, IPubSubMethod
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        TableVirtualize<CEventLog>? table;

        public string GetTitle
        {
            get => request.ObjID.SubsystemID switch
            {
                1 => StartUIRep["IDS_ASOTITLE"],
                2 => StartUIRep["IDS_UUZSTITLE"],
                3 => StartUIRep["IDS_STAFFTITLE"],
                4 => StartUIRep["IDS_P16xTITLE"],
                _ => ""
            };
        }

        readonly List<int> _eventCode = new();

        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["Source"] },
                { 1, StartUIRep["RegTime"] },
                { 2, StartUIRep["Type"] },
                { -3, StartUIRep["Code"] },
                { 4, StartUIRep["Login"] },
                { -1, StartUIRep["Info"] }
            };
            request.LSortOrder = 1;
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;

            HintItems.Add(new HintItem(nameof(FiltrModel.Source), StartUIRep["Source"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpSourceName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Type), StartUIRep["Type"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpTypeName)));

            //HintItems.Add(new HintItem(nameof(FiltrModel.Info), StartUIRep["Info"], TypeHint.Input));

            HintItems.Add(new HintItem(nameof(FiltrModel.Login), StartUIRep["Login"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpUserName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Code), StartUIRep["Code"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpEventcodeName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.DateRange), StartUIRep["RegTime"], TypeHint.Date));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrLog);

            _ = _HubContext.SubscribeAsync(this);

        }
        [Description(DaprMessage.PubSubName)]
        public Task Fire_AddEvent(byte[] Value)
        {
            try
            {
                var addData = CEventLog.Parser.ParseFrom(Value);
                table?.InsertItem(0, addData);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Task.CompletedTask;
        }

        ItemsProvider<CEventLog> GetProvider
        {
            get
            {
                return new ItemsProvider<CEventLog>(ThList, LoadChildList, request, new List<int>() { 15, 10, 10, 25, 10 });
            }
        }

        private async ValueTask<IEnumerable<CEventLog>> LoadChildList(GetItemRequest req)
        {
            List<CEventLog> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_IEventLog", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<CEventLog>>() ?? new();
                _eventCode.AddRange(newData.Select(x => x.Code).Distinct().Except(_eventCode));
            }
            else
            {
                MessageView?.AddError(AsoRep["IDS_ERRORCAPTION"], StartUIRep["IDS_EGETEVENTLISTINFO"]);
            }
            return newData;
        }

        private ValueTask<IEnumerable<Hint>> LoadHelpTypeName(GetItemRequest req)
        {
            List<Hint>? newData = new()
            {
                new Hint(Rep["IDS_STRING_ERROR"], "1"),
                new Hint(Rep["IDS_STRING_WARNING"], "2"),
                new Hint(Rep["IDS_C_INFO"], "3")
            };
            return new(newData ?? new());
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpUserName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetUserNameListFromEvenLog", new IntAndString() { Number = request.ObjID.SubsystemID, Str = req.BstrFilter }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<UserName>>();

                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(x.Value)));
                }
            }
            return newData ?? new();
        }


        private async ValueTask<IEnumerable<Hint>> LoadHelpEventcodeName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            RequestCode requestCode = new() { SubSystemId = request.ObjID.SubsystemID };
            if (string.IsNullOrEmpty(req.BstrFilter))
                requestCode.CodeArray.AddRange(_eventCode);
            else
                requestCode.CodeArray.AddRange(GetAllValuesForLocalizer(req.BstrFilter));
            var result = await Http.PostAsJsonAsync("api/v1/GetEventcodeListFromEvenLogBySource", JsonFormatter.Default.Format(requestCode), ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<Eventcode>>();
                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(ReplaceCodeToString(x.Source, x.Code), x.Code.ToString())));
                }
            }
            return newData ?? new();
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpSourceName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            RequestCode requestCode = new() { SubSystemId = request.ObjID.SubsystemID };
            requestCode.CodeArray.AddRange(Rep.GetAllKeyNamesForValue(ReplaceDictionary.ErrorName, req.BstrFilter));
            var result = await Http.PostAsJsonAsync("api/v1/GetSourceListFromEvenLog", JsonFormatter.Default.Format(requestCode), ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<Source>>();
                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(Rep[BaseReplace.Get<ReplaceDictionary>(x.Value)], x.Value.ToString())));
                }
            }
            return newData ?? new();
        }

        private async Task RefreshTable()
        {
            if (table != null)
                await table.ResetData();
        }

        string ReplaceCodeToString(int source, int code)
        {
            string codeStr = string.Empty;

            codeStr = source switch
            {
                (int)GSOModules.SZSNotifyLogic_Module => SZSNotifyRep[BaseReplace.Get<SZSNotifyReplace>(code)],
                (int)GSOModules.SMP16xNL_Module => SMP16xNlRep[BaseReplace.Get<SMP16xNlReplace>(code)],
                (int)GSOModules.StaffNL_Module => StaffRep[BaseReplace.Get<StaffReplace>(code)],
                (int)GSOModules.Security_Module => DeviceRep[BaseReplace.Get<DeviceReplace>(code)],
                (int)GSOModules.DeviceConsole_Module => DeviceRep[BaseReplace.Get<DeviceReplace>(code)],
                (int)GSOModules.StartUI_Module => StartUIRep[BaseReplace.Get<StartUIReplace>(code)],
                (int)GSOModules.AsoForms_Module => AsoRep[BaseReplace.Get<ASOReplace>(code)],
                (int)GSOModules.SzsForms_Module => UUZSRep[BaseReplace.Get<UUZSReplace>(code)],
                (int)GSOModules.GsoForms_Module => GsoRep[BaseReplace.Get<GSOReplase>(code)],
                (int)GSOModules.P16Forms_Module => SMP16xFormRep[BaseReplace.Get<SMP16xFormReplace>(code)],
                (int)GSOModules.RdmForms_Module => RDMRep[BaseReplace.Get<RDMReplace>(code)],
                (int)GSOModules.ReporterForms_Module => RepoterRep[BaseReplace.Get<RepoterReplace>(code)],
                (int)GSOModules.StaffForms_Module => GSOFormRep[BaseReplace.Get<GSOFormReplase>(code)],
                (int)GSOModules.AutoTasks_Module => TasksRep[BaseReplace.Get<TasksReplace>(code)],
                (int)GSOModules.ViewStates_Module => ViewStateRep[BaseReplace.Get<ViewStateReplace>(code)],
                (int)GSOModules.SMGate_Module => SMGateRep[BaseReplace.Get<SMGateReplace>(code)],
                (int)GSOModules.SMDataService_Module => SMDataRep[BaseReplace.Get<SMDataReplace>(code)],
                (int)GSOModules.CLAsoLUGSMT_Module => ASOGSMRep[BaseReplace.Get<ASOGSMReplace>(code)],
                _ => code.ToString()
            };
            return codeStr;
        }


        IEnumerable<int> GetAllValuesForLocalizer(string value)
        {
            List<int> response = new();
            response.AddRange(GsoRep.GetAllKeyNamesForValue(GSOReplase.ErrorName, value));
            response.AddRange(DeviceRep.GetAllKeyNamesForValue(DeviceReplace.ErrorName, value));
            response.AddRange(StartUIRep.GetAllKeyNamesForValue(StartUIReplace.ErrorName, value));
            response.AddRange(AsoRep.GetAllKeyNamesForValue(ASOReplace.ErrorName, value));
            if (response.Count < 50)
            {
                response.AddRange(SZSNotifyRep.GetAllKeyNamesForValue(SZSNotifyReplace.ErrorName, value));
                response.AddRange(SMP16xNlRep.GetAllKeyNamesForValue(SMP16xNlReplace.ErrorName, value));
                response.AddRange(StaffRep.GetAllKeyNamesForValue(StaffReplace.ErrorName, value));
                response.AddRange(UUZSRep.GetAllKeyNamesForValue(UUZSReplace.ErrorName, value));
                if (response.Count < 50)
                {
                    response.AddRange(SMP16xFormRep.GetAllKeyNamesForValue(SMP16xFormReplace.ErrorName, value));
                    response.AddRange(RDMRep.GetAllKeyNamesForValue(RDMReplace.ErrorName, value));
                    response.AddRange(RepoterRep.GetAllKeyNamesForValue(RepoterReplace.ErrorName, value));
                    response.AddRange(GSOFormRep.GetAllKeyNamesForValue(GSOFormReplase.ErrorName, value));
                    if (response.Count < 50)
                    {
                        response.AddRange(TasksRep.GetAllKeyNamesForValue(TasksReplace.ErrorName, value));
                        response.AddRange(ViewStateRep.GetAllKeyNamesForValue(ViewStateReplace.ErrorName, value));
                        response.AddRange(SMGateRep.GetAllKeyNamesForValue(SMGateReplace.ErrorName, value));
                        response.AddRange(SMDataRep.GetAllKeyNamesForValue(SMDataReplace.ErrorName, value));
                        response.AddRange(ASOGSMRep.GetAllKeyNamesForValue(ASOGSMReplace.ErrorName, value));
                    }
                }
            }
            return response.Take(50);
        }


        private async Task GetReport()
        {
            List<CEventLog> newData = new();
            List<EventLogPrint> reportData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_IEventLog", new GetItemRequest(request) { CountData = 0 }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<CEventLog>>() ?? new();
            }
            else
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }

            reportData.AddRange(newData.Select(x => new EventLogPrint()
            {
                Module = Rep[BaseReplace.Get<ReplaceDictionary>(x.Source)],
                Time = x.RegTime.ToDateTime().ToLocalTime().ToString(),
                Type = Rep[BaseReplace.Get<ReplaceDictionary>(x.Type)],
                Code = ReplaceCodeToString(x.Source, x.Code),
                User = x.Login,
                Details = x.Info
            }));

            ReportInfo RepInfo = new();

            RepInfo.Name = StartUIRep["EventLog"];

            List<GetColumnsExItem>? ColumnList = new() {
            new GetColumnsExItem(){NColumnId = 4, NStatus=1,TContrName=StartUIRep["Source"], TName=StartUIRep["Source"]},//Модуль
            new GetColumnsExItem(){NColumnId = 5, NStatus=1,TContrName=StartUIRep["RegTime"], TName=StartUIRep["RegTime"]},//Время
            new GetColumnsExItem(){NColumnId = 6, NStatus=1, TContrName=StartUIRep["Type"],TName=StartUIRep["Type"]},//Тип
            new GetColumnsExItem(){NColumnId = 7, NStatus=1, TContrName=StartUIRep["Code"],TName=StartUIRep["Code"]},//Код
            new GetColumnsExItem(){NColumnId = 8, NStatus=1, TContrName=StartUIRep["Login"],TName=StartUIRep["Login"]},//Пользователь
            new GetColumnsExItem(){NColumnId = 9, NStatus=1, TContrName=StartUIRep["Info"],TName=StartUIRep["Info"]},//Подробности
            new GetColumnsExItem(){NColumnId = 200, NStatus=0, TName=""},//Компактное расположение
            new GetColumnsExItem(){NColumnId = 201, NStatus=0, TName=""}//Центрирование информации
            };

            var html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, 11, reportData, null, _OtherForReport.FiltrOther(FiltrItemsToString));

            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(html));

            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "EventLog.html", streamRef);
        }


        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
