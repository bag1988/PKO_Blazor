using System.Net.Http.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using UUZSDataProto.V1;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace BlazorLibrary.Shared.ListObject
{
    partial class CreateGroup
    {
        [Parameter]
        public OBJ_ID? DeviceObj_ID { get; set; }

        [Parameter]
        public EventCallback ActionBack { get; set; }

        private GroupInfo? Model = null;
        private GroupInfo? OldModel = null;

        private List<CLineGroupType>? m_vTypes = null;//Общая линия группы
        private List<CLineGroupDev>? m_vDevs = null;//типы оконечных устройств

        private List<CLineGroupDev>? SelectedFolders = null;//выбранные типы оконечных устройств
        private List<CLineGroupDev>? OldSelectedFolders = null;

        private List<CLineGroupDev>? SelectData = null;

        private List<CLineGroupDev>? SelectItem = null;

        private List<string>? ListObj = new();

        private bool IsViewWarning = false;

        private bool IsTimeslot = false;
        private string? IsNextOk = null;

        private bool IsDeleteAnyWay = false;

        bool IsProcessing = false;

        private TimeSpan Remained = TimeSpan.FromSeconds(10);

        int StaffId = 0;

        protected override async Task OnInitializedAsync()
        {
            m_vDevs = new();
            SelectedFolders = new();
            StaffId = await _User.GetLocalStaff();
            await GetGroupLineList();

            if (DeviceObj_ID?.ObjID > 0)
                await GetInfo();
            else
            {
                Model = new() { Prior = 1, StaffID = StaffId };
                IsDeleteAnyWay = true;
            }
            if (Model != null)
                Model.GroupID = DeviceObj_ID?.ObjID ?? 0;
        }

        private async Task OnOK()
        {
            if (Model == null)
                return;
            IsProcessing = true;

            bool isReturn = false;

            if (Model.Equals(OldModel) && (SelectedFolders?.SequenceEqual(OldSelectedFolders ?? new()) ?? true))
            {
                MessageView?.AddError("", AsoRep["NotChangeSave"]);
                isReturn = true;
            }
            else if (string.IsNullOrEmpty(Model.GroupName))
            {
                MessageView?.AddError("", UUZSRep["IDS_STRING_ENTER_NAME_GROUP"]);
                isReturn = true;
            }
            else if (Model.GroupID == 0)
            {
                MessageView?.AddError("", UUZSRep["IDS_STRING_REENTER_GROUP_NUMBER"]);
                isReturn = true;
            }
            else if (DeviceObj_ID == null || DeviceObj_ID.ObjID == 0)
            {
                var r = await CheckExistGroupID(new OBJ_ID() { ObjID = Model.GroupID, StaffID = Model.StaffID });

                if (r?.Count > 0)
                {

                    MessageView?.AddError("", Model.GroupID + " " + UUZSRep["IDS_STRING_ALREADY_ENTERED"]);
                    isReturn = true;
                }

            }
            else if (Model.TimeslotLength == 0)
            {
                MessageView?.AddError("", UUZSRep["IDS_STRING_REENTER_LEN_TIMESLOT"]);
                isReturn = true;
            }
            else if (Model.TimeslotLength < 300)
            {
                Remained = TimeSpan.FromSeconds(10);
                IsTimeslot = true;
                IsNextOk = UUZSRep["IDS_STRING_VERY_SMALL_TIMESLOT"];
                StateHasChanged();
                while (IsTimeslot)
                {
                    await Task.Delay(100);
                    Remained = Remained.Subtract(TimeSpan.FromMilliseconds(100));

                    if (Remained.TotalSeconds <= 0)
                    {
                        break;
                    }
                    StateHasChanged();
                }
                if (IsNextOk != null)
                    isReturn = true;
            }
            else if (Model.TimeslotLength > 5000)
            {
                Remained = TimeSpan.FromSeconds(10);
                IsTimeslot = true;
                IsNextOk = UUZSRep["IDS_STRING_VERY_BIG_TIMESLOT"];
                StateHasChanged();
                while (IsTimeslot)
                {
                    await Task.Delay(100);
                    Remained = Remained.Subtract(TimeSpan.FromMilliseconds(100));

                    if (Remained.TotalSeconds <= 0)
                    {
                        break;
                    }
                    StateHasChanged();
                }
                if (IsNextOk != null)
                    isReturn = true;
            }
            else if (!SelectedFolders?.Any() ?? true)
            {
                MessageView?.AddError("", UUZSRep["IDS_STRING_NOT_DEVICE_IN_GROUP"]);
                isReturn = true;
            }

            if (!isReturn)
            {
                Model.Status = 1;
                var x = OldSelectedFolders?.Except(SelectedFolders ?? new());
                if ((x?.Any() ?? false) && DeviceObj_ID?.ObjID > 0)
                {
                    await DeleteGroupItem(x.Select(x => new CGroupItemInfo()
                    {
                        GroupID = Model.GroupID,
                        StaffID = Model.StaffID,
                        DevID = x.DevID,
                        DevStaffID = StaffId
                    }).ToList());
                }

                await UpdateGroup();

                await CallAction();
            }

            IsProcessing = false;
        }

        /// <summary>
        /// получаем список выбранных елементов
        /// </summary>
        /// <returns></returns>
        private async Task GetInfo()
        {
            if (DeviceObj_ID != null)
            {
                await Http.PostAsJsonAsync("api/v1/GetGroupInfo", DeviceObj_ID).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        var json = await x.Result.Content.ReadAsStringAsync();
                        var r = CGetGroupInfo.Parser.ParseJson(json);

                        if (r.GroupInfo != null)
                        {
                            Model = r.GroupInfo;
                            OldModel = new(Model);

                        }
                        if (r.GroupDevList != null)
                        {
                            SelectedFolders = new(r.GroupDevList.Array);
                            OldSelectedFolders = new(SelectedFolders.Select(x => new CLineGroupDev(x)));
                        }

                        var request = m_vTypes?.FirstOrDefault(x => x.ConnParam == Model?.ConnParam);
                        if (request != null)
                        {
                            await GetGroupLineDevList(request);
                            StateHasChanged();
                        }
                    }
                    else
                    {
                        MessageView?.AddError("", UUZSRep["IDS_STRING_CANNOT_INFO_GROUP"]);
                    }
                });
            }
        }


        private async Task ChangeSelect(ChangeEventArgs e)
        {
            var sValue = e.Value?.ToString();

            if (string.IsNullOrEmpty(sValue))
            {
                m_vDevs = new();
                return;
            }


            var request = m_vTypes?.FirstOrDefault(x => x.ConnParam == sValue);

            if (request == null)
                return;

            if (Model != null)
                Model.ConnParam = sValue;
            SelectData = null;
            await GetGroupLineDevList(request);
        }

        private async Task RemoveSelect()
        {
            if (SelectItem == null)
                return;

            await RemoveToSelectFolders(new List<CLineGroupDev>(SelectItem));
        }

        private async Task RemoveAll()
        {
            if (SelectedFolders == null)
                return;

            await RemoveToSelectFolders(SelectedFolders.Select(x => new CLineGroupDev(x)).ToList());
        }

        private async Task RemoveToSelectFolders(List<CLineGroupDev> items)
        {
            if (SelectedFolders == null)
                return;

            var newSelect = SelectedFolders.SkipWhile(x => !items.Contains(x)).FirstOrDefault(x => !items.Contains(x));

            if (!IsDeleteAnyWay)
            {
                var b = await GetLinkObjects_IGroup();
                if (b)
                {
                    Remained = TimeSpan.FromSeconds(10);
                    IsViewWarning = true;
                    StateHasChanged();
                    while (IsViewWarning)
                    {
                        await Task.Delay(100);
                        Remained = Remained.Subtract(TimeSpan.FromMilliseconds(100));

                        if (Remained.TotalSeconds <= 0)
                        {
                            break;
                        }
                        StateHasChanged();
                    }
                }
                else
                    IsDeleteAnyWay = true;
            }

            if (IsDeleteAnyWay)
            {
                foreach (var item in items)
                {
                    SelectedFolders.Remove(item);
                }
            }

            SelectItem = null;
            if (SelectedFolders.Count > 0)
            {
                if (newSelect == null)
                {
                    SelectItem = new() { SelectedFolders.Last() };
                }
                else
                    SelectItem = new() { newSelect };
            }

            IsViewWarning = false;
        }

        private void AddSelect()
        {
            if (SelectData == null)
                return;
            AddToSelectFolders(new List<CLineGroupDev>(SelectData));
        }

        private void AddAll()
        {
            if (GetListTree == null)
                return;
            AddToSelectFolders(GetListTree);
        }

        private void AddToSelectFolders(List<CLineGroupDev> items)
        {
            if (SelectedFolders == null)
                SelectedFolders = new();

            var newSelect = GetListTree?.SkipWhile(x => !items.Contains(x)).FirstOrDefault(x => !items.Contains(x));

            foreach (var item in items)
            {
                if (!SelectedFolders.Contains(item))
                {
                    SelectedFolders.Add(item);
                    StateHasChanged();
                }
                else
                {
                    MessageView?.AddError("", item.DevName + " - " + StartUIRep["IDS_AB_BEEN_SELECTED"]);
                }
            }
            SelectData = null;

            if (GetListTree?.Count > 0)
            {
                if (newSelect == null)
                {
                    SelectData = new() { GetListTree.Last() };
                }
                else
                    SelectData = new() { newSelect };
            }
        }

        private List<CLineGroupDev>? GetListTree
        {
            get
            {
                var l = m_vDevs?.Where(x => !SelectedFolders?.Any(s => s.DevID == x.DevID && s.DevName == x.DevName) ?? false).ToList();
                return l;
            }

        }

        /// <summary>
        /// Получаем список "Общая линия группы"
        /// </summary>
        /// <returns></returns>
        private async Task GetGroupLineList()
        {

            await Http.PostAsync("api/v1/GetGroupLineList_Uuzs", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    m_vTypes = await x.Result.Content.ReadFromJsonAsync<List<CLineGroupType>>() ?? new();
                }
                else
                {
                    MessageView?.AddError("", UUZSRep["IDS_ERRGETLINE"]);
                }
            });
        }

        /// <summary>
        /// Получаем типы оконечных устройств
        /// </summary>
        /// <returns></returns>
        private async Task GetGroupLineDevList(CLineGroupType request)
        {
            m_vDevs = null;
            await Http.PostAsJsonAsync("api/v1/GetGroupLineDevList_Uuzs", request).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    m_vDevs = await x.Result.Content.ReadFromJsonAsync<List<CLineGroupDev>>() ?? new();
                }
                else
                {
                    MessageView?.AddError("", UUZSRep["IDS_STRING_CANNOT_GET_INFO_BY_TYPE_TERMINAL_DEVICE"]);
                }
            });

        }

        private async Task<bool> GetLinkObjects_IGroup()
        {
            bool response = true;
            if (DeviceObj_ID?.ObjID > 0)
            {
                await Http.PostAsJsonAsync("api/v1/GetLinkObjects_IGroup", DeviceObj_ID).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        ListObj = await x.Result.Content.ReadFromJsonAsync<List<string>>() ?? new();

                        if (!ListObj.Any())
                        {
                            response = false;
                        }
                    }
                    else
                    {
                        MessageView?.AddError("", UUZSRep["IDS_STRING_ERR_DB_GROUP"]);
                    }
                });
            }

            return response;
        }


        private async Task<CountResponse> CheckExistGroupID(OBJ_ID request)
        {

            CountResponse response = new();
            await Http.PostAsJsonAsync("api/v1/CheckExistGroupID", request).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    response = await x.Result.Content.ReadFromJsonAsync<CountResponse>() ?? new();
                }
                else
                {
                    MessageView?.AddError("", UUZSRep["IDS_STRING_ERR_CHECK_GROUP_NUMBER"]);
                }
            });
            return response;
        }


        private async Task DeleteGroupItem(List<CGroupItemInfo> request)
        {

            await Http.PostAsJsonAsync("api/v1/DeleteGroupItem", request).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var b = await x.Result.Content.ReadFromJsonAsync<BoolValue>() ?? new();
                }
                else
                {
                    MessageView?.AddError("", UUZSRep["IDS_STRING_ERR_DELETE_GROUP_ELEMENT"]);
                }
            });
        }

        private async Task UpdateGroup()
        {
            if (SelectedFolders == null || Model == null)
            {
                return;
            }
            CUpdateGroup request = new();
            request.GroupInfo = Model;
            request.CGroupItemInfoList = new();

            var x = SelectedFolders.Except(OldSelectedFolders ?? new());

            if (x.Any())
            {
                request.CGroupItemInfoList.Array.AddRange(x.Select(x => new CGroupItemInfo()
                {
                    DevID = x.DevID,
                    DevStaffID = StaffId,
                    StaffID = Model.StaffID,
                    GroupID = Model.GroupID
                }).ToList());
            }

            await Http.PostAsJsonAsync("api/v1/UpdateGroup", JsonFormatter.Default.Format(request)).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var b = await x.Result.Content.ReadFromJsonAsync<BoolValue>() ?? new();
                }
                else
                {
                    MessageView?.AddError("", UUZSRep["IDS_STRING_EDIT_NOT_COMMIT"]);
                }
            });
        }

        private async Task CallAction()
        {
            if (ActionBack.HasDelegate)
                await ActionBack.InvokeAsync();
        }

    }
}
