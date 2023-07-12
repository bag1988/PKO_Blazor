using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using SMDataServiceProto.V1;
using StaffDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Pages.Staff.SheduleStaff
{
    partial class CreateScheduleStaff
    {
        [Parameter]
        public OBJ_ID? ShedileID { get; set; }

        [Parameter]
        public EventCallback Callback { get; set; }

        private CSheduleInfoExMsg? Model = null;

        private List<Objects> LocationList = new();

        private List<IntAndString> LineTypes = new();

        private List<IntAndString> ConnTypes = new();

        private List<Restrict> RestrictList = new();

        private List<GetSheduleListItem>? SheduleList;

        private int SheduleID = 1;
        private int OldSheduleID = 1;

        private TimeOnly StartTime = new TimeOnly(0, 0), EndTime = new TimeOnly(23, 59);

        private int StaffId = 0;

        private bool m_bCheckDeviceEnable = false, Oldm_bCheckDeviceEnable = false;

        bool IsProcessing = false;

        protected override async Task OnInitializedAsync()
        {

            if (ShedileID == null || ShedileID.StaffID == 0)
            {
                await CalBackEvent();
                return;
            }

            StartTime = new TimeOnly(0, 0, 0);
            EndTime = new TimeOnly(23, 59, 59);

            ConnTypes = new()
            {
                new IntAndString() { Number = 1, Str = GsoRep["Modem"] },//проводной модем
                new IntAndString() { Number = 2, Str = GsoRep["ISDN"] }//ISDN-адаптер
            };


            StaffId = await _User.GetLocalStaff();
            await GetSheduleList();
            await GetRestrictList();
            await GetLineTypeList();
            await GetLocationInfo();
            await GetSheduleInfoStaff();
            await GetDeviceShedule();



        }

        private async Task GetSheduleInfoStaff()
        {
            if (ShedileID?.ObjID > 0)
            {
                await Http.PostAsJsonAsync("api/v1/GetSheduleInfoStaff", ShedileID).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        Model = await x.Result.Content.ReadFromJsonAsync<CSheduleInfoExMsg>();

                        if (Model?.CSheduleInfo?.TimeRestr != null)
                        {
                            StartTime = new(Model.CSheduleInfo.TimeRestr.Btime.ToTimeSpan().Ticks);
                            EndTime = new(Model.CSheduleInfo.TimeRestr.Etime.ToTimeSpan().Ticks);
                        }

                    }
                });
            }

            if (Model == null)
                Model = new()
                {
                    CSheduleInfo = new()
                    {
                        Object = new() { StaffID = ShedileID?.StaffID ?? 0, SubsystemID = ShedileID?.SubsystemID ?? 0 },
                        WeekDays = new() { DayType = 0, WeekDay = "0000000" },
                        TimeRestr = new(),
                        ConnParams = new()
                        {
                            ConnType = (int)BaseLineType.LINE_TYPE_UNDEF,
                            LocationID = new(),
                            Prior = 1
                        },
                        LineParams = new() { BaseType = (int)BaseLineType.LINE_TYPE_DIAL_UP }
                    }
                };
        }

        private async Task CalBackEvent()
        {
            if (Callback.HasDelegate)
                await Callback.InvokeAsync();
        }

        private async Task GetLocationInfo()
        {
            await Http.PostAsJsonAsync("api/v1/GetObjects_ILocation", new OBJ_ID() { StaffID = StaffId }).ContinueWith(async x =>
              {
                  if (x.Result.IsSuccessStatusCode)
                  {
                      LocationList = await x.Result.Content.ReadFromJsonAsync<List<Objects>>() ?? new();
                  }
                  else
                      LocationList = new();
              });
        }

        private async Task GetLineTypeList()
        {
            await Http.PostAsJsonAsync("api/v1/GetLineTypeList", new IntID() { ID = SubsystemType.SUBSYST_GSO_STAFF }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    LineTypes = await x.Result.Content.ReadFromJsonAsync<List<IntAndString>>() ?? new();

                    LineTypes.RemoveAll(x => x.Number != (int)BaseLineType.LINE_TYPE_DIAL_UP && x.Number != (int)BaseLineType.LINE_TYPE_DEDICATED);

                    LineTypes.Insert(0, new IntAndString() { Number = (int)BaseLineType.LINE_TYPE_UNDEF, Str = "ЛВС" });

                }
                else
                {
                    LineTypes = new();
                    MessageView?.AddError("", GSOFormRep["IDS_EFAILGETLINETYPE"]);
                }

            });
        }

        private async Task GetRestrictList()
        {
            await Http.PostAsJsonAsync("api/v1/GetRestrictList", new IntID() { ID = (int)BaseLineType.LINE_TYPE_DIAL_UP }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    RestrictList = await x.Result.Content.ReadFromJsonAsync<List<Restrict>>() ?? new();
                }
            });
            RestrictList = RestrictList.Where(x => x.RestrictType != 2).ToList();

        }


        private bool IsChecked(Restrict item)
        {
            bool isChecked = false;
            if (item.RestrictType == 0)
                isChecked = ((Model?.CSheduleInfo?.LineParams?.GlobalType >> item.BitNumber) & 0x01) > 0;
            if (item.RestrictType == 1)
                isChecked = ((Model?.CSheduleInfo?.LineParams?.UserType >> item.BitNumber) & 0x01) > 0;
            return isChecked;
        }

        private void SetRestrictBitStatus(Restrict item)
        {
            if (Model?.CSheduleInfo?.LineParams == null)
                return;

            int BitNumber = item.BitNumber;

            int i = 0;
            i |= (1 << BitNumber);

            if (item.RestrictType == 0)
            {
                if (((Model.CSheduleInfo.LineParams.GlobalType >> BitNumber) & 0x01) > 0)
                {
                    Model.CSheduleInfo.LineParams.GlobalType -= i;
                }
                else
                {
                    Model.CSheduleInfo.LineParams.GlobalType += i;
                }
            }
            else if (item.RestrictType == 1)
            {
                if (((Model.CSheduleInfo.LineParams.UserType >> BitNumber) & 0x01) > 0)
                {
                    Model.CSheduleInfo.LineParams.UserType -= i;
                }
                else
                {
                    Model.CSheduleInfo.LineParams.UserType += i;
                }
            }

        }

        private void SetDayWeek(int item)
        {
            if (Model?.CSheduleInfo?.WeekDays == null)
                return;
            char[] dayArray = Model.CSheduleInfo.WeekDays.WeekDay == "" ? "0000000".ToCharArray() : Model.CSheduleInfo.WeekDays.WeekDay.ToCharArray();

            dayArray[item] = dayArray[item] == '0' ? '1' : '0';

            Model.CSheduleInfo.WeekDays.WeekDay = string.Join("", dayArray);
        }

        private async Task AddShedle()
        {

            if (Model?.CSheduleInfo?.ConnParams == null)
                return;

            IsProcessing = true;
            bool IsOkMpdel = true;



            if (string.IsNullOrEmpty(Model.CSheduleInfo.ConnParams.ConnParam))
            {
                IsOkMpdel = false;
                MessageView?.AddError("", GSOFormRep["IDS_E_SAVE_NOCONNPARAM"]);
            }

            if (Model.CSheduleInfo.ConnParams.ConnType == (int)BaseLineType.LINE_TYPE_DIAL_UP)
            {
                if (Model.CSheduleInfo.ConnParams.DeviceType == 0)
                {
                    IsOkMpdel = false;
                    MessageView?.AddError("", GSOFormRep["IDS_E_SAVE_DEVICE_TYPE"]);
                }

                if (Model.CSheduleInfo.ConnParams.LocationID == null || Model.CSheduleInfo.ConnParams.LocationID.ObjID == 0)
                {
                    IsOkMpdel = false;
                    MessageView?.AddError("", GSOFormRep["IDS_E_SAVE_LOCATION"]);
                }
                else
                {
                    Model.CSheduleInfo.ConnParams.LocationID.StaffID = StaffId;
                }
            }


            if (!IsOkMpdel)
            {
                IsProcessing = false;
                return;
            }


            if (Model.CSheduleInfo.TimeRestr == null)
                Model.CSheduleInfo.TimeRestr = new();

            Model.CSheduleInfo.TimeRestr.Btime = Duration.FromTimeSpan(StartTime.ToTimeSpan());
            Model.CSheduleInfo.TimeRestr.Etime = Duration.FromTimeSpan(EndTime.ToTimeSpan());


            if (Model.CSheduleInfo.TimeRestr.TimeType != 1)
            {
                Model.CSheduleInfo.TimeRestr.Btime = Duration.FromTimeSpan(new TimeOnly(0, 0).ToTimeSpan());
                Model.CSheduleInfo.TimeRestr.Etime = Duration.FromTimeSpan(new TimeOnly(23, 59).ToTimeSpan());
            }

            if (Model.CSheduleInfo.ConnParams.ConnType != (int)BaseLineType.LINE_TYPE_DIAL_UP)
            {
                Model.CSheduleInfo.ConnParams.LocationID = new();
                Model.CSheduleInfo.LineParams = new();
                Model.CSheduleInfo.ConnParams.DeviceType = 0;
            }

            var result = await Http.PostAsJsonAsync("api/v1/SetSheduleInfoStaff", Model);
            if (result.IsSuccessStatusCode)
            {
                var b = await result.Content.ReadFromJsonAsync<BoolValue>() ?? new();
                if (b.Value == true)
                {
                    if (ShedileID?.ObjID > 0)
                        await SetDeviceShedule(new OBJ_ID() { ObjID = ShedileID.StaffID, StaffID = StaffId, SubsystemID = ShedileID.SubsystemID });
                    MessageView?.AddMessage("", GSOFormRep["IDS_SAVESHEDULE_OK"]);
                }
                else
                {
                    MessageView?.AddError("", GSOFormRep["IDS_ESETSHEDULEINFO"]);
                }
            }
            else
            {
                MessageView?.AddError("", GSOFormRep["IDS_EFAILSETSHEDULEINFO"]);
            }
            await CalBackEvent();
            IsProcessing = false;
        }

        private async Task GetSheduleList()
        {
            var result = await Http.PostAsync("api/v1/GetSheduleList", null);
            if (result.IsSuccessStatusCode)
            {
                SheduleList = await result.Content.ReadFromJsonAsync<List<GetSheduleListItem>>() ?? new();
            }
        }

        private async Task GetDeviceShedule()
        {
            if (ShedileID != null)
            {
                OBJ_Key OBJ_Key = new OBJ_Key();
                OBJ_Key.ObjID = new OBJ_ID() { ObjID = ShedileID.StaffID, SubsystemID = ShedileID.SubsystemID, StaffID = StaffId };
                OBJ_Key.ObjType = (int)HMT.Staff;
                var result = await Http.PostAsJsonAsync("api/v1/GetDeviceShedule", OBJ_Key);
                if (result.IsSuccessStatusCode)
                {
                    var r = await result.Content.ReadFromJsonAsync<GetShedule>() ?? new();
                    SheduleID = r.Shedule;
                    m_bCheckDeviceEnable = Oldm_bCheckDeviceEnable = r.Status > 0 ? true : false;
                    StateHasChanged();
                }
            }
            else
                SheduleID = SheduleList?.FirstOrDefault(x => x.Duration.ToTimeSpan().Minutes == 15)?.SheduleID ?? 0;
            OldSheduleID = SheduleID;
        }

        private async Task SetDeviceShedule(OBJ_ID request)
        {
            if (m_bCheckDeviceEnable != Oldm_bCheckDeviceEnable || OldSheduleID != SheduleID)
            {
                SetShedule r = new();

                r.OBJKey = new() { ObjID = request, ObjType = (int)HMT.Staff };
                r.Shedule = SheduleID;
                r.Status = m_bCheckDeviceEnable ? 1 : 0;

                var result = await Http.PostAsJsonAsync("api/v1/SetDeviceShedule", r);
                if (result.IsSuccessStatusCode)
                {
                    var b = await result.Content.ReadFromJsonAsync<BoolValue>();
                    if (b?.Value != true)
                        MessageView?.AddError("", UUZSRep["IDS_ERROR_SETCHECK_DEVICE"]);
                }
                else
                {
                    MessageView?.AddError("", UUZSRep["IDS_ERROR_SETCHECK_DEVICE"]);
                }
            }

        }

    }
}
