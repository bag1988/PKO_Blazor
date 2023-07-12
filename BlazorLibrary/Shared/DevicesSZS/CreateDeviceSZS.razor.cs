using System.Net.Http.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using SMDataServiceProto.V1;
using UUZSDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using static BlazorLibrary.Shared.Main;

namespace BlazorLibrary.Shared.DevicesSZS
{
    partial class CreateDeviceSZS
    {
        [Parameter]
        public OBJ_ID DeviceObj_ID { get; set; } = new();

        [Parameter]
        public int DevType { get; set; } = 0;

        [Parameter]
        public EventCallback CallBack { get; set; }

        private DeviceInfoEx? Model;

        private CShedule? SelectShedule = null;

        private List<DeviceClass>? m_vDeviceClass;

        private List<Restrict> RestrictList = new();

        private List<GetSheduleListItem>? SheduleList = null;

        private List<IntAndString> LineTypes = new();

        private List<Objects> LocationList = new();

        private List<CSubDevice> ZoneList = new();

        private List<SMDataServiceProto.V1.Line> L_List = new();

        private Dictionary<int, string>? ThList;

        private int SheduleID = 1;

        private bool IsGlobalNum = false;
        bool IsProcessing = false;

        private bool CheckState = false;

        private bool CheckStatesEx = false;

        private bool IsDeleteLine = false;

        private Int64 DeviceCode = 0;

        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { -5, "" },
                { -1, UUZSRep["IDS_STRING_TYPE"] },
                { -2, UUZSRep["IDS_STRING_NUMBER"] },
                { -3, UUZSRep["IDS_STRING_LOCATION"] },
                { -4, UUZSRep["IDS_STRING_PRIORITY"] }
            };


            await GetSheduleList();

            await GetLineTypeList();

            await GetZones();

            await GetLocationInfo();

            await GetDeviceClassByType();

            if (DeviceObj_ID.ObjID > 0)
            {
                DeviceCode = DeviceObj_ID.StaffID;
                DeviceCode <<= 32;
                Int32 suffix = ((0x05A5 & 0xffff) | (DeviceObj_ID.ObjID & 0xffff) << 16);
                DeviceCode += suffix;
                DeviceCode >>= 1;


                await GetInfo();
                await GetDeviceShedule();
            }
            else
            {
                Model = new DeviceInfoEx
                {
                    DeviceInfo = new DeviceInfo
                    {
                        CDeviceInfo = new CDeviceInfo
                        {
                            StaffID = DeviceObj_ID.StaffID
                          ,
                            Status = 1
                          ,
                            Prior = 1
                          ,
                            ZoneCount = 15
                        }
                    }
                };
                SheduleID = SheduleList?.FirstOrDefault(x => x.Duration.ToTimeSpan().Minutes == 15)?.SheduleID ?? 0;
            }

        }

        private async Task OnOK()
        {
            if (Model?.DeviceInfo?.CDeviceInfo != null)
            {
                IsProcessing = true;
                bool IsReturn = false;
                if (IsGlobalNum && Model.DeviceInfo.CDeviceInfo.GlobNum == 0)
                {
                    MessageView?.AddError("", UUZSRep["IDS_STRING_ENTER_GLOBAL_NUMBER"]);
                    IsReturn = true;
                }
                else if (!IsGlobalNum)
                {
                    Model.DeviceInfo.CDeviceInfo.GlobNum = 0;
                }


                if (string.IsNullOrEmpty(Model.DeviceInfo.CDeviceInfo.DevName))
                {
                    MessageView?.AddError("", UUZSRep["IDS_STRING_ENTER_NAME_DEVICE"]);
                    IsReturn = true;
                }

                if (Model.DeviceInfo.CDeviceInfo.DevID == 0)
                {
                    MessageView?.AddError("", UUZSRep["IDS_STRING_ENTER_SERIAL_NUMBER"]);
                    IsReturn = true;
                }
                else if (DeviceObj_ID.ObjID == 0)
                {
                    var b = await CheckDeviceID(new OBJ_ID(DeviceObj_ID) { ObjID = Model.DeviceInfo.CDeviceInfo.DevID });

                    if (b)
                    {
                        MessageView?.AddError("", UUZSRep["ERROR_SERIAL_NUMBER"]);
                        IsReturn = true;
                    }
                }

                if (Model.DeviceInfo.CSheduleArray == null || !Model.DeviceInfo.CSheduleArray.Any())
                {
                    MessageView?.AddError("", UUZSRep["IDS_STRING_ADD_LINE_FOR_DEVICE"]);
                    IsReturn = true;
                }
                else
                {
                    foreach (var item in Model.DeviceInfo.CSheduleArray)
                    {
                        string name = LineTypes.FirstOrDefault(x => x.Number == item.BaseType)?.Str ?? UUZSRep["IDS_STRING_ERR_IN_DATA"];
                        if (string.IsNullOrEmpty(item.ConnParam))
                        {
                            IsReturn = true;
                            MessageView?.AddError(name, UUZSRep["IDS_STRING_ENTER_LINE_NUMBER"]);
                        }
                        else if (!ConnParamTest(item.ConnParam))
                        {
                            IsReturn = true;
                            MessageView?.AddError(name, UUZSRep["IDS_STRING_ENTER_LINE_NUMBER"]);
                        }
                        else if (item.BaseType == (int)BaseLineType.LINE_TYPE_DIAL_UP && item.LocationID == 0)
                        {
                            IsReturn = true;
                            MessageView?.AddError(name, UUZSRep["IDS_STRING_SELECT_LOCATION"]);
                        }
                        else if (item.PriorType == 0)
                        {
                            IsReturn = true;
                            MessageView?.AddError(name, UUZSRep["IDS_STRING_ENTER_PRIORITY_FOR_LINE_TYPE"]);
                        }
                    }
                }

                if (IsReturn)
                {
                    IsProcessing = false;
                    return;
                }


                if (Model.DeviceInfo.CSheduleArray != null)
                    foreach (var item in Model.DeviceInfo.CSheduleArray)
                    {
                        if (item.DevClassID == 0)
                        {
                            item.DevClassID = m_vDeviceClass?.FirstOrDefault(x => x.LineType == item.BaseType)?.DevClassID ?? 0;
                        }
                        item.DevID = Model.DeviceInfo.CDeviceInfo.DevID;
                        item.StaffID = Model.DeviceInfo.CDeviceInfo.StaffID;
                        if (item.BaseType == (int)BaseLineType.LINE_TYPE_DIAL_UP)
                        {
                            item.LocationStaffID = LocationList.FirstOrDefault(x => x.OBJID.ObjID == item.LocationID)?.OBJID.StaffID ?? DeviceObj_ID.StaffID;
                        }
                    }

                var result = await Http.PostAsJsonAsync("api/v1/UpdateDeviceEx", JsonFormatter.Default.Format(Model));
                if (result.IsSuccessStatusCode)
                {
                    var b = await result.Content.ReadFromJsonAsync<BoolValue>();
                    if (b?.Value != true)
                        MessageView?.AddError("", UUZSRep["IDS_ERROR_SETDEVICE"]);
                    else
                    {
                        _ = SetSubDevice();
                        _ = SetDeviceShedule();
                        await CallEvent();
                    }
                }
                else
                {
                    MessageView?.AddError("", UUZSRep["IDS_ERROR_SETDEVICE"]);
                }
            }
            IsProcessing = false;
        }

        private async Task SetSubDevice()
        {
            ZoneList.ForEach(x =>
            {
                x.ParentDevID = new OBJ_ID(DeviceObj_ID) { ObjID = Model?.DeviceInfo?.CDeviceInfo?.DevID ?? 0 };
                if (!string.IsNullOrEmpty(x.DevName))
                    x.Visiable = 1;
                else
                    x.Visiable = 0;
            });
            await Http.PostAsJsonAsync("api/v1/SetSubDevice", ZoneList).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var b = await x.Result.Content.ReadFromJsonAsync<BoolValue>();
                    if (b?.Value != true)
                        MessageView?.AddError("", UUZSRep["IDS_ERROR_ZONESET"]);
                }
                else
                {
                    MessageView?.AddError("", UUZSRep["IDS_ERROR_ZONESET"]);
                }
            });
        }


        private async Task SetDeviceShedule()
        {
            OBJ_Key OBJ_Key = new();
            OBJ_Key.ObjID = new OBJ_ID(DeviceObj_ID) { ObjID = Model?.DeviceInfo?.CDeviceInfo?.DevID ?? 0 };
            OBJ_Key.ObjType = (int)HMT.Uzs;
            await Http.PostAsJsonAsync("api/v1/SetDeviceShedule", new SetShedule() { OBJKey = OBJ_Key, Shedule = SheduleID, Status = !CheckState ? 0 : CheckStatesEx ? 2 : 1 });
        }


        /// <summary>
        /// Получаем инфо о устройстве
        /// </summary>
        /// <returns></returns>
        private async Task GetInfo()
        {
            if (DeviceObj_ID.ObjID > 0)
            {
                await Http.PostAsJsonAsync("api/v1/GetDeviceInfoEx", DeviceObj_ID).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        var json = await x.Result.Content.ReadAsStringAsync();
                        Model = DeviceInfoEx.Parser.ParseJson(json);

                        IsGlobalNum = Model?.DeviceInfo?.CDeviceInfo?.GlobNum > 0;

                        await GetRestrictList(Model?.DeviceInfo?.CSheduleArray?.FirstOrDefault());
                        StateHasChanged();
                    }
                    else
                    {
                        Model = new();
                        MessageView?.AddError("", UUZSRep["IDS_ERRGETDEVINFO"]);
                    }
                });
            }
        }


        private void ChangeLine(ChangeEventArgs e, CShedule item)
        {
            int.TryParse((e.Value?.ToString()), out int idLine);

            var ChangeItem = Model?.DeviceInfo?.CSheduleArray.FirstOrDefault(x => x.Equals(item));
            if (ChangeItem != null)
            {
                if (idLine > 0)
                {
                    var r = L_List.FirstOrDefault(x => x.LineID == idLine);
                    if (r != null)
                    {
                        ChangeItem.ConnParam = idLine.ToString();
                        ChangeItem.Phone = $"{r.LineName} ({r.Phone})";
                    }
                }
                else
                {
                    ChangeItem.ConnParam = "";
                    ChangeItem.Phone = "";
                }
            }

        }

        private void AddLineList(int LineType)
        {
            if (Model == null)
                return;

            CShedule shedule = new();
            shedule.BaseType = LineType;
            shedule.PriorType = 1;
            Model.DeviceInfo.CSheduleArray.Add(shedule);
        }

        private void DeleteLineList()
        {
            if (SelectShedule == null)
                return;
            Model?.DeviceInfo?.CSheduleArray?.Remove(SelectShedule);
            SelectShedule = null;
            IsDeleteLine = false;
            RestrictList.Clear();
        }

        //Классы устройств данного типа
        private async Task GetDeviceClassByType()
        {
            await Http.PostAsJsonAsync("api/v1/GetDeviceClassList_Uuzs", new IntID() { ID = DevType }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    m_vDeviceClass = await x.Result.Content.ReadFromJsonAsync<List<DeviceClass>>();
                }
                else
                {
                    MessageView?.AddError("", UUZSRep["IDS_ERRGETCLASS"]);
                }
            });
        }


        private async Task<bool> CheckDeviceID(OBJ_ID request)
        {
            bool isFind = false;
            await Http.PostAsJsonAsync("api/v1/CheckDeviceID", request).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var b = await x.Result.Content.ReadFromJsonAsync<BoolValue>();
                    if (b?.Value == true)
                        isFind = true;
                }
            });

            return isFind;
        }


        private async Task GetZones()
        {
            await Http.PostAsJsonAsync("api/v1/GetDeviceSubDevice", DeviceObj_ID).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    ZoneList = await x.Result.Content.ReadFromJsonAsync<List<CSubDevice>>() ?? new();
                }
                else
                {
                    MessageView?.AddError("", UUZSRep["IDS_STRING_ERR_GET_DATA"]);
                }
            });

            if (!ZoneList.Any())
            {
                for (int i = 0; i < 15; i++)
                {
                    CSubDevice subDevice = new();
                    subDevice.Order = i;
                    subDevice.Status = 1;
                    subDevice.Level = 1;
                    subDevice.Visiable = 1;
                    ZoneList.Add(subDevice);
                }
            }

        }

        private async Task GetRestrictList(CShedule? item)
        {
            SelectShedule = item;

            await Http.PostAsJsonAsync("api/v1/GetRestrictList", new IntID() { ID = item?.BaseType ?? 0 }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    RestrictList = await x.Result.Content.ReadFromJsonAsync<List<Restrict>>() ?? new();
                    RestrictList = RestrictList.Where(x => x.RestrictType != 2).ToList();
                }
            });

            if (item != null && item.BaseType != (int)BaseLineType.LINE_TYPE_DIAL_UP)
                await GetLineList(item.BaseType);

        }

        private bool IsChecked(Restrict item)
        {
            bool isChecked = false;
            if (item.RestrictType == 0)
                isChecked = ((SelectShedule?.GlobType >> item.BitNumber) & 0x01) > 0;
            if (item.RestrictType == 1)
                isChecked = ((SelectShedule?.UserType >> item.BitNumber) & 0x01) > 0;
            //if (item.RestrictType == 2)
            //    isChecked = ((SelectShedule?.PriorType >> item.BitNumber) & 0x01) > 0;
            return isChecked;
        }

        private void SetRestrictBitStatus(Restrict item)
        {
            if (SelectShedule == null)
                return;

            int BitNumber = item.BitNumber;

            int i = 0;
            i |= (1 << BitNumber);

            if (item.RestrictType == 0)
            {
                if (((SelectShedule.GlobType >> BitNumber) & 0x01) > 0)
                {
                    SelectShedule.GlobType -= i;
                }
                else
                {
                    SelectShedule.GlobType += i;
                }
            }
            else if (item.RestrictType == 1)
            {
                if (((SelectShedule.UserType >> BitNumber) & 0x01) > 0)
                {
                    SelectShedule.UserType -= i;
                }
                else
                {
                    SelectShedule.UserType += i;
                }
            }
            //else if (item.RestrictType == 2)
            //{
            //    if (((SelectShedule.PriorType >> BitNumber) & 0x01) > 0)
            //    {
            //        SelectShedule.PriorType -= i;
            //    }
            //    else
            //    {
            //        SelectShedule.PriorType += i;
            //    }
            //}
        }

        private async Task GetDeviceShedule()
        {
            OBJ_Key OBJ_Key = new OBJ_Key();
            OBJ_Key.ObjID = new OBJ_ID(DeviceObj_ID);
            OBJ_Key.ObjType = (int)HMT.Uzs;
            await Http.PostAsJsonAsync("api/v1/GetDeviceShedule", OBJ_Key).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var r = await x.Result.Content.ReadFromJsonAsync<GetShedule>() ?? new();
                    CheckState = r.Status > 0 ? true : false;
                    SheduleID = r.Shedule == 0 ? 1 : r.Shedule;
                    if (CheckState)
                        CheckStatesEx = r.Status == 2;
                }
            });
        }

        private async Task GetSheduleList()
        {
            await Http.PostAsync("api/v1/GetSheduleList", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    SheduleList = await x.Result.Content.ReadFromJsonAsync<List<GetSheduleListItem>>() ?? new();
                }
            });
        }

        private async Task GetLineTypeList()
        {
            await Http.PostAsJsonAsync("api/v1/GetLineTypeList", new IntID() { ID = SubsystemType.SUBSYST_SZS }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    LineTypes = await x.Result.Content.ReadFromJsonAsync<List<IntAndString>>() ?? new();
                }
                else
                    LineTypes = new();
            });
        }

        private async Task GetLocationInfo()
        {
            await Http.PostAsJsonAsync("api/v1/GetObjects_ILocation", new OBJ_ID() { StaffID = DeviceObj_ID.StaffID }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    LocationList = await x.Result.Content.ReadFromJsonAsync<List<Objects>>() ?? new();
                }
                else
                    LocationList = new();
            });
        }

        private async Task GetLineList(int LineType)
        {
            await Http.PostAsJsonAsync("api/v1/GetObjects_ILine", new OBJ_ID() { ObjID = LineType, SubsystemID = SubsystemType.SUBSYST_SZS }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    L_List = await x.Result.Content.ReadFromJsonAsync<List<SMDataServiceProto.V1.Line>>() ?? new();
                }
                else
                    MessageView?.AddError("", UUZSRep["IDS_ERRGETLINE"]);
            });
        }


        private bool ConnParamTest(string pText)
        {
            for (int i = 0; i < pText.Length; i++)
            {
                if ((pText[i] >= '0' && pText[i] <= '9') || pText[i] == 'w' || pText[i] == 'W' || pText[i] == ',' || (pText[i] >= 'A' && pText[i] <= 'F') || pText[i] == '*' || pText[i] == '#')
                    continue;
                else
                    return false;
            }
            return true;
        }

        private async Task CallEvent()
        {
            if (CallBack.HasDelegate)
                await CallBack.InvokeAsync();
        }

    }
}
