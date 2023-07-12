using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using BlazorLibrary;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;
using SharedLibrary;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;
using Label.V1;

namespace DeviceConsole.Client.Pages.Additional.Notification
{
    partial class AutoNotify : IAsyncDisposable, IPubSubMethod
    {
        OGetTaskResults? ResultList { get; set; }

        CGetTaskInfo? SelectItem { get; set; }

        bool IsAdd = false;
        bool IsDelete = false;

        List<string> StartTasksInfo { get; set; } = new();

        DateTime startDate = DateTime.Now;

        readonly ReadOnlyCollection<TimeZoneInfo> timeZones = TimeZoneInfo.GetSystemTimeZones();

        TableVirtualize<CGetTaskInfo>? table;

        protected override async Task OnInitializedAsync()
        {
            ResultList = new()
            {
                CountColumn = 1,
                ColumnInfoPtr = new(),
                CountItem = 1
            };
            ResultList.ColumnInfoPtr.Array.Add(GsoRep["IDS_OBJECT"]);

            request.ObjID.StaffID = await _User.GetLocalStaff();
            ThList = new Dictionary<int, string>
            {
                { 1, TasksRep["IDS_TASK_NAME"] },//задание
                { 2, TasksRep["IDS_MO"] },
                { 3, TasksRep["IDS_TU"] },
                { 4, TasksRep["IDS_WE"] },
                { 5, TasksRep["IDS_TH"] },
                { 6, TasksRep["IDS_FR"] },
                { 7, TasksRep["IDS_SA"] },
                { 8, TasksRep["IDS_SU"] },
                { -9, TasksRep["IDS_TASK_TIME"] },
                { 10, TasksRep["IDS_SIT_NAME"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.Name), TasksRep["IDS_TASK_NAME"], TypeHint.Input));
            HintItems.Add(new HintItem(nameof(FiltrModel.DayMO), TasksRep["IDS_MO"], TypeHint.Bool, null, FiltrOperationType.BoolEqual));
            HintItems.Add(new HintItem(nameof(FiltrModel.DayTU), TasksRep["IDS_TU"], TypeHint.Bool, null, FiltrOperationType.BoolEqual));
            HintItems.Add(new HintItem(nameof(FiltrModel.DayWE), TasksRep["IDS_WE"], TypeHint.Bool, null, FiltrOperationType.BoolEqual));
            HintItems.Add(new HintItem(nameof(FiltrModel.DayTH), TasksRep["IDS_TH"], TypeHint.Bool, null, FiltrOperationType.BoolEqual));
            HintItems.Add(new HintItem(nameof(FiltrModel.DayFR), TasksRep["IDS_FR"], TypeHint.Bool, null, FiltrOperationType.BoolEqual));
            HintItems.Add(new HintItem(nameof(FiltrModel.DaySA), TasksRep["IDS_SA"], TypeHint.Bool, null, FiltrOperationType.BoolEqual));
            HintItems.Add(new HintItem(nameof(FiltrModel.DaySU), TasksRep["IDS_SU"], TypeHint.Bool, null, FiltrOperationType.BoolEqual));
            HintItems.Add(new HintItem(nameof(FiltrModel.SitName), TasksRep["IDS_SIT_NAME"], TypeHint.Input));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrAutoNotify);

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteTask(uint Value)
        {
            await CallRefreshData();
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_EndTask(uint Value)
        {
            if (SelectItem?.TaskID?.ObjID == Value)
            {
                await GetResultList();
                StateHasChanged();
            }
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_StartTask(uint Value)
        {
            if (Value == 0)
                return;

            OBJ_ID idTask = new()
            {
                ObjID = (int)Value,
                StaffID = request.ObjID.StaffID
            };
            var result = await Http.PostAsJsonAsync("api/v1/GetTaskLastStart", idTask);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<SMDataServiceProto.V1.String>();

                if (!string.IsNullOrEmpty(response?.Value))
                {
                    StartTasksInfo.Add(response.Value);
                    StateHasChanged();
                }
            }
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateTask(uint Value)
        {
            await CallRefreshData();
        }

        ItemsProvider<CGetTaskInfo> GetProvider => new ItemsProvider<CGetTaskInfo>(ThList, LoadChildList, request);

        private async ValueTask<IEnumerable<CGetTaskInfo>> LoadChildList(GetItemRequest req)
        {
            List<CGetTaskInfo> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_ITask", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<CGetTaskInfo>>() ?? new();
            }
            else
            {
                MessageView?.AddError("", TasksRep["E_GET_LIST"]);
            }
            return newData;
        }
        private async Task RefreshTable()
        {
            SelectItem = null;
            if (table != null)
                await table.ResetData();
        }
        async Task ChangeDate(ChangeEventArgs e)
        {
            var b = DateTime.TryParse(e?.Value?.ToString(), out DateTime parseDate);

            if (b)
            {
                startDate = parseDate;

                await GetResultList();

            }

        }

        async Task SetSelectItem(List<CGetTaskInfo>? items)
        {
            SelectItem = items?.LastOrDefault();
            ResultList = new()
            {
                CountColumn = 1,
                ColumnInfoPtr = new(),
                CountItem = 1
            };
            ResultList.ColumnInfoPtr.Array.Add(GsoRep["IDS_OBJECT"]);
            await GetResultList();
        }

        private async Task GetResultList()
        {
            if (SelectItem != null)
            {
                IGetTaskResults request = new()
                {
                    TaskID = SelectItem.TaskID,
                    Day = startDate.Day,
                    Month = startDate.Month,
                    Year = startDate.Year
                };

                var result = await Http.PostAsJsonAsync("api/v1/GetTaskResults", request);
                if (result.IsSuccessStatusCode)
                {
                    var json = await result.Content.ReadAsStringAsync();
                    try
                    {
                        ResultList = OGetTaskResults.Parser.ParseJson(json);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                else
                {
                    MessageView?.AddError("", TasksRep["IDS_E_GETRESULTS"]);
                }
            }

            if (ResultList?.TaskResultPtrList == null)
            {
                ResultList = new()
                {
                    CountColumn = 1,
                    ColumnInfoPtr = new(),
                    CountItem = 1,
                    TaskResultPtrList = new()
                };
                ResultList.ColumnInfoPtr.Array.Add(GsoRep["IDS_OBJECT"]);
            }
        }


        string OnSubItemPrePaint(int lStatus)
        {
            string str = "";
            if (!(lStatus == 2 || lStatus == 3 || lStatus == 4 || lStatus == 5) && lStatus != -1)//коды только для АСО, -1 = наименованию объекта оповещения (первая колонка списка)
                str = "background-color: rgb(255,255,0);";
            //else
            //{
            //    if (!(pLVCD->nmcd.dwItemSpec % 2))
            //        str = "background-color: rgb(230, 230, 240);";
            //    else
            //        pLVCD->clrTextBk = ::GetSysColor(COLOR_WINDOW);
            //}
            return str;
        }


        private async Task DeleteTask()
        {
            if (SelectItem != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/DeleteTask", SelectItem.TaskID);
                if (!result.IsSuccessStatusCode)
                {
                    MessageView?.AddError("", TasksRep["E_DELETE"]);
                }
            }
            IsDelete = false;
        }

        void AddTask()
        {
            SelectItem = null;
            IsAdd = true;
        }

        void DeleteTaskView()
        {
            if (SelectItem != null)
                IsDelete = true;
        }

        void ActionBack(bool? isUpdate = false)
        {
            if (isUpdate == true)
            {
                SelectItem = null;
            }
            IsAdd = false;
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
