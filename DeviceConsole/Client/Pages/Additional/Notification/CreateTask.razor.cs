using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Pages.Additional.Notification
{
    partial class CreateTask
    {
        [Parameter]
        public OBJ_ID? TaskId { get; set; }

        [Parameter]
        public EventCallback<bool?> ActionBack { get; set; }

        CTaskInfo OldModel { get; set; } = new();

        CTaskInfo? Model { get; set; }

        List<Objects>? m_SitList { get; set; }

        List<TaskShedule>? m_vTaskShedule { get; set; }

        List<TaskShedule>? Oldm_vTaskShedule { get; set; }

        TaskShedule? SelectTask { get; set; }

        int StaffId = 0;

        readonly ReadOnlyCollection<TimeZoneInfo> timeZones = TimeZoneInfo.GetSystemTimeZones();

        readonly string LocalZone = TimeZoneInfo.Local.Id;

        protected override async Task OnInitializedAsync()
        {
            StaffId = await _User.GetLocalStaff();
            await GetInfo();
            await GetSitList();
            await GetTimeList();
        }


        async Task GetInfo()
        {
            if (TaskId != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetTaskInfo", TaskId);
                if (result.IsSuccessStatusCode)
                {
                    Model = await result.Content.ReadFromJsonAsync<CTaskInfo>();
                }
                else
                    MessageView?.AddError("", TasksRep["IDS_E_GETDATA"]);
            }
            if (Model == null)
                Model = new()
                {
                    TaskID = new()
                    {
                        SubsystemID = SubsystemType.SUBSYST_ASO,
                        StaffID = StaffId
                    },
                    SitID = new()
                    {
                        SubsystemID = SubsystemType.SUBSYST_ASO,
                        StaffID = StaffId,
                        ObjID = -1
                    }
                };

            OldModel = new(Model);
        }


        private async Task GetSitList()
        {
            m_SitList = null;
            var result = await Http.PostAsJsonAsync("api/v1/GetObjects_ISituation", Model?.TaskID);

            if (result.IsSuccessStatusCode)
            {
                m_SitList = await result.Content.ReadFromJsonAsync<List<Objects>>();
            }
            else
            {
                MessageView?.AddError("", GsoRep["ERROR_GET_SIT_INFO"]);
            }

            if (m_SitList == null)
                m_SitList = new();
        }


        private async Task GetTimeList()
        {
            if (TaskId != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetTimeList", TaskId);

                if (result.IsSuccessStatusCode)
                {
                    m_vTaskShedule = await result.Content.ReadFromJsonAsync<List<TaskShedule>>();
                }
                else
                {
                    MessageView?.AddError("", TasksRep["IDS_E_GETDATA"]);
                }
            }

            if (m_vTaskShedule == null)
                m_vTaskShedule = new();

            Oldm_vTaskShedule = new(m_vTaskShedule.Select(x => new TaskShedule(x)));
        }


        private async Task<OBJ_ID> SetCommonTaskInfo()
        {
            if (Model != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/SetTaskInfo", Model);

                if (result.IsSuccessStatusCode)
                {
                    return await result.Content.ReadFromJsonAsync<OBJ_ID>() ?? new();
                }
                else
                {
                    MessageView?.AddError("", TasksRep["IDS_E_SAVETASK"]);
                }
            }

            return new();
        }


        private async Task SetSheduleTaskInfo()
        {
            if (m_vTaskShedule != null)
            {
                CSetTimeList request = new();
                request.TaskID = Model?.TaskID;
                request.TaskSheduleList = new();
                m_vTaskShedule.ForEach(x =>
                    {
                        x.TaskID = Model?.TaskID ?? new();
                    }
                );

                request.TaskSheduleList.Array.AddRange(m_vTaskShedule);

                var result = await Http.PostAsJsonAsync("api/v1/SetTimeList", JsonFormatter.Default.Format(request));

                if (!result.IsSuccessStatusCode)
                {
                    MessageView?.AddError("", TasksRep["IDS_E_SAVETASK"]);
                }
            }
        }


        private async Task SetTaskStatus()
        {
            if (Model?.TaskID != null)
            {
                OBJ_Key request = new()
                {
                    ObjID = Model.TaskID,
                    ObjType = 0
                };
                await Http.PostAsJsonAsync("api/v1/SetTaskStatus", request);
            }
        }


        private async Task<bool> CheckSitID()
        {
            if (Model?.TaskID != null && Model.SitID?.ObjID > 0)
            {
                CCheckTaskSit request = new() { SitID = Model.SitID, TaskID = Model.TaskID };
                var result = await Http.PostAsJsonAsync("api/v1/CheckTaskSit", request);

                if (result.IsSuccessStatusCode)
                {
                    var r = await result.Content.ReadFromJsonAsync<IntID>();
                    if (r?.ID == 1)
                        return true;
                }
            }
            return false;
        }

        private async Task HandleSubsystemChanged(ChangeEventArgs e)
        {
            if (Model?.TaskID != null)
            {
                int.TryParse(e.Value?.ToString(), out int id);
                Model.SitID.ObjID = -1;
                Model.SitID.SubsystemID = id;
                Model.TaskID.SubsystemID = id;
            }
            await GetSitList();
        }


        void AddTaskList()
        {
            if (m_vTaskShedule?.Count >= 60 * 24)
            {
                MessageView?.AddError("", TasksRep["E_ADD_TASK_LIST"]);
                return;
            }

            if (m_vTaskShedule == null)
                m_vTaskShedule = new();

            TimeSpan d = new(DateTime.Now.Hour, DateTime.Now.Minute, 0);

            int countItems = m_vTaskShedule.Count;

            while (m_vTaskShedule.Any(x => x.TaskTime.ToDateTime().TimeOfDay == d) && countItems > 0)
            {
                d = d.Add(TimeSpan.FromMinutes(1));
                --countItems;
            }

            m_vTaskShedule.Add(new TaskShedule()
            {
                TaskMode = 0,
                TaskTime = new DateTime(1899, 12, 30, d.Hours, d.Minutes, 0, DateTimeKind.Utc).ToTimestamp(),
                TaskID = Model?.TaskID ?? new(),
                TimeZoneId = LocalZone
            });
        }


        TimeOnly SelectTaskTime
        {
            get
            {
                TimeOnly result = new();

                if (SelectTask != null && SelectTask.TaskTime != null)
                {
                    var t = SelectTask.TaskTime.ToDateTime().TimeOfDay;
                    result = new(t.Hours, t.Minutes);

                }
                return result;
            }
            set
            {
                if (SelectTask != null)
                {
                    SelectTask.TaskTime = new DateTime(1899, 12, 30, value.Hour, value.Minute, 0, DateTimeKind.Utc).ToTimestamp();
                }
            }
        }

        void DeleteTaskList()
        {
            if (SelectTask == null || m_vTaskShedule == null) return;

            m_vTaskShedule.Remove(SelectTask);
            SelectTask = null;
        }

        async Task SaveItem()
        {
            if (Model == null || string.IsNullOrEmpty(Model.TaskName))
            {
                MessageView?.AddError("", TasksRep["IDS_E_EMPTYTASKNAME"]);
                return;
            }

            if (Model.SitID == null || Model.SitID.ObjID == -1)
            {
                MessageView?.AddError("", TasksRep["IDS_E_EMPTYTASKSIT"]);
                return;
            }

            if (Model.Mo == 0 && Model.Tu == 0 && Model.We == 0 && Model.Th == 0 && Model.Fr == 0 && Model.Sa == 0 && Model.Su == 0)
            {
                MessageView?.AddError("", TasksRep["IDS_E_EMPTYTASKWD"]);
                return;
            }

            if (m_vTaskShedule == null || m_vTaskShedule.Count == 0)
            {
                MessageView?.AddError("", TasksRep["IDS_E_EMPTYTASKTIME"]);
                return;
            }

            if (!await CheckSitID())
            {
                MessageView?.AddError("", TasksRep["IDS_E_EXISTSTASKSIT"]);
                return;
            }



            bool IsUpdate = false;
            if (!Model.Equals(OldModel))
            {
                Model.TaskID = await SetCommonTaskInfo();
                IsUpdate = true;
            }
            if (Model.TaskID?.ObjID > 0)
            {
                if (!m_vTaskShedule.SequenceEqual(Oldm_vTaskShedule ?? new()))
                {
                    await SetSheduleTaskInfo();
                    IsUpdate = true;
                }
                if (IsUpdate)
                    await SetTaskStatus();
            }

            await CloseModal(IsUpdate);
        }

        async Task CloseModal(bool? isUpdate = false)
        {
            if (ActionBack.HasDelegate)
            {
                await ActionBack.InvokeAsync(isUpdate);
            }
        }
    }
}
