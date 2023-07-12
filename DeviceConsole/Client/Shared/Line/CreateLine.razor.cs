using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Google.Protobuf.WellKnownTypes;
using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Shared.Line
{
    partial class CreateLine
    {

        [CascadingParameter]
        public int SubsystemID { get; set; } = 1;
        [Parameter]
        public int? LineID { get; set; }
             
        [Parameter]
        public EventCallback<bool?> CallBack { get; set; }

        private SMDataServiceProto.V1.Line? Model = null;

        private SMDataServiceProto.V1.Line? OldModel = null;

        private List<Restrict>? RestrictList = null;

        private Restrict? SelectRestrict = null;

        private List<Objects> LocationList = new();

        private List<IntAndString> LineTypes = new();

        private BindingDevice? BindingDevice = null;

        private BindingDevice? NewBindingDevice = null;

        private string? ExtParam = null;

        private string? NewExtParam = null;

        private bool EditBinding = false;

        private bool ViewLocation = false;
        private bool EditPhone = false;
        private bool ViewParams = false;
        private bool ViewConnection = false;
        private bool ViewExt = false;

        private string TitlePhone = "";

        private string TitleError = "";

        private bool AddParam = false;
        private bool LocationInfo = false;

        private int StaffId = 0;

        int? MaxNumber { get; set; }

        protected override async Task OnInitializedAsync()
        {
            TitleError = GsoRep[LineID != null ? "IDS_REG_LINE_UPDATE" : "IDS_REG_LINE_INSERT"];
            StaffId = await _User.GetLocalStaff();
            await GetLocationInfo();
            await GetLineTypeList();

            if (LineID != null)
            {
                await GetInfo();
            }
            else
            {
                Model = new();
                Model.SubsystemID = SubsystemID;
                Model.ChannelType = LineTypes.FirstOrDefault()?.Number ?? 0;
                Model.Status = 1;
                await GetMaxPhoneLine();
            }

            await ChangeLineTypeAsync(Model?.ChannelType ?? (int)BaseLineType.LINE_TYPE_UNDEF);
            await GetBindingDevice();
            await GetLineExtParam();

            OldModel = new SMDataServiceProto.V1.Line(Model);

        }


        private async Task GetMaxPhoneLine()
        {
            if (SubsystemID == SubsystemType.SUBSYST_SZS)
            {
                var result = await Http.PostAsync("api/v1/GetMaxPhoneLine", null);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadFromJsonAsync<Int32Value>() ?? new();
                    MaxNumber = response.Value;
                }
            }
        }

        private async Task GetInfo()
        {
            if (LineID != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetLineInfo", new IntID() { ID = LineID ?? 0 });
                if (result.IsSuccessStatusCode)
                {
                    Model = await result.Content.ReadFromJsonAsync<SMDataServiceProto.V1.Line>() ?? new();

                    Model.LineID = LineID!.Value;
                }
                else
                {
                    Model = new();
                    MessageView?.AddError(TitleError, DeviceRep["ErrorGetInfo"]);
                }
            }
        }

        private async Task SetLine()
        {
            if (Model != null)
            {
                if (!OldModel?.Equals(Model) ?? false)
                {
                    if (string.IsNullOrEmpty(Model.LineName))
                    {
                        MessageView?.AddError(TitleError, DeviceRep["ErrorNull"] + " " + GSOFormRep["NameLine"]);
                        return;
                    }
                    if (Model.ChannelType == (int)BaseLineType.LINE_TYPE_UNDEF)
                    {
                        MessageView?.AddError(TitleError, DeviceRep["ErrorNull"] + " " + GSOFormRep["TypeLine"]);
                        return;
                    }

                    Model.Phone = Model.Phone.Trim();

                    if (string.IsNullOrEmpty(Model.Phone) && !ViewParams)
                    {
                        MessageView?.AddError(TitleError, DeviceRep["ErrorNull"] + " " + TitlePhone);
                        return;
                    }


                    if (Model.ChannelType == (int)BaseLineType.LINE_TYPE_DIAL_UP || Model.ChannelType == (int)BaseLineType.LINE_TYPE_GSM_TERMINAL)    // нужно местоположение
                    {
                        if (LocationList.Any(x => x.OBJID.ObjID == Model.LocationID))
                            Model.LocationStaffID = LocationList.FirstOrDefault(x => x.OBJID.ObjID == Model.LocationID)?.OBJID.StaffID ?? 0;
                        else
                        {
                            MessageView?.AddError(TitleError, DeviceRep["ErrorNull"] + " " + GSOFormRep["LocationLine"]);
                            return;
                        }
                    }
                    else
                    {
                        Model.LocationID = 0;
                        Model.LocationStaffID = 0;
                    }

                    bool MessageViews = true;
                    switch (Model.ChannelType)
                    {
                        case (int)BaseLineType.LINE_TYPE_GSM_TERMINAL:
                        {
                            // Проверить корректность адреса почтового сервера
                        }
                        break;
                        case (int)BaseLineType.LINE_TYPE_SMTP:
                        {
                            // Проверить корректность адреса почтового сервера
                        }
                        break;

                        case (int)BaseLineType.LINE_TYPE_DIAL_UP:
                        case (int)BaseLineType.LINE_TYPE_DEDICATED:
                        //case (int)BaseLineType.LINE_TYPE_HSCOM:
                        //case (int)BaseLineType.LINE_TYPE_RADIO:
                        {
                            // проверка абонентского номера на уникальность                                
                            if (OldModel?.Phone != Model.Phone)
                            {
                                await Http.PostAsJsonAsync("api/v1/CheckExistPhone_2", new CheckExistPhone_2Request() { MSzPhone = Model.Phone, MType = Model.ChannelType }).ContinueWith(async x =>
                                {
                                    if (x.Result.IsSuccessStatusCode)
                                    {
                                        CountResponse? r = await x.Result.Content.ReadFromJsonAsync<CountResponse>();

                                        if (r != null && r.Count == 0)
                                        {
                                            MessageViews = false;
                                        }
                                    }
                                });
                                if (MessageViews)
                                {
                                    MessageView?.AddError(TitleError, GSOFormRep["ErrorLineName"]);
                                    return;
                                }
                            }
                        }
                        break;

                        default:
                        {
                            // Нет посторонних символов в телефоне
                            if (Model.ChannelType != (int)BaseLineType.LINE_TYPE_WSDL && Model.ChannelType != (int)BaseLineType.LINE_TYPE_DCOM)
                            {
                                string pattern = @"^(\d)([\s|,|;]?\d+)+(\d)$";

                                if (Regex.IsMatch(Model.Phone, pattern, RegexOptions.IgnoreCase))
                                {
                                    pattern = @"([\s|,|;])";
                                    Model.Phone = Regex.Replace(Model.Phone, pattern, ";");
                                }
                                else
                                {
                                    MessageView?.AddError(TitleError, GSOFormRep["ErrorLineNumber"] + " \"" + TitlePhone + " \"");
                                    return;
                                }
                            }
                        }
                        break;
                    }


                    int? id = LineID;

                    await Http.PostAsJsonAsync(id == null ? "api/v1/AddLine" : "api/v1/EditLine", Model).ContinueWith(async x =>
                       {
                           if (x.Result.IsSuccessStatusCode)
                           {
                               if (id == null)
                               {
                                   IntID? r = await x.Result.Content.ReadFromJsonAsync<IntID>();
                                   if (r != null && r.ID != 0)
                                   {
                                       LineID = r.ID;
                                       MessageViews = false;
                                   }
                               }
                               else
                               {
                                   BoolValue? r = await x.Result.Content.ReadFromJsonAsync<BoolValue>();
                                   if (r != null && r.Value == true)
                                   {
                                       MessageViews = false;
                                   }
                               }
                           }
                       });


                    if (MessageViews)
                    {
                        MessageView?.AddError(TitleError, AsoRep["IDS_ERRORCAPTION"]);
                        return;
                    }
                }
                if (BindingDevice != null && BindingDevice.ChannelID > 0 && NewBindingDevice?.ChannelID == 0)
                {
                    await DeleteLineBinding();
                }
                else
                {
                    await SetLineBinding();
                }

                if (ExtParam != NewExtParam)
                    await SetLineExtParam();

                await CallEvent(true);
            }
        }

        private async Task SetLineBinding()
        {
            if (NewBindingDevice != null && LineID != null && !NewBindingDevice.Equals(BindingDevice) && NewBindingDevice.ChannelID > 0)
            {

                LineBinding request = new() { LineID = LineID.Value, ChannelID = NewBindingDevice.ChannelID, DeviceID = NewBindingDevice.DeviceID };


                await Http.PostAsJsonAsync("api/v1/SetLineBinding", request).ContinueWith(x =>
                {
                    if (!x.Result.IsSuccessStatusCode)
                    {
                        MessageView?.AddError(GsoRep["DescriptionBindingDevice"], AsoRep["IDS_E_SETLINEBINDING"]);
                    }
                });
            }
        }


        private async Task SetLineExtParam()
        {
            if (LineID != null)
            {
                await Http.PostAsJsonAsync("api/v1/SetLineExtParam", new CSetLineExtParam() { LineID = LineID ?? 0, ExtParam = NewExtParam }).ContinueWith(x =>
                  {
                      if (!x.Result.IsSuccessStatusCode)
                      {
                          MessageView?.AddError(GsoRep["DescriptionBindingDevice"], DeviceRep["ErrorGetInfo"]);
                      }
                  });
            }
        }


        private async Task GetLineExtParam()
        {
            if (LineID != null && ViewExt)
            {
                await Http.PostAsJsonAsync("api/v1/GetLineExtParam", new IntID() { ID = LineID ?? 0 }).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        await x.Result.Content.ReadFromJsonAsync<SMDataServiceProto.V1.String>().ContinueWith(r =>
                        {
                            NewExtParam = ExtParam = r.Result?.Value;
                            StateHasChanged();
                        });
                    }
                });
            }
        }

        private async Task DeleteLineBinding()
        {
            if (BindingDevice != null && BindingDevice.ChannelID > 0 && NewBindingDevice?.ChannelID == 0)
            {
                await Http.PostAsJsonAsync("api/v1/DeleteLineBinding", new IntID() { ID = LineID ?? 0 }).ContinueWith(x =>
                    {
                        if (!x.Result.IsSuccessStatusCode)
                        {
                            MessageView?.AddError(AsoRep["IDS_STRING_DELETE_PROCESS"], AsoRep["IDS_E_DELETE"]);
                        }
                    });

            }
        }


        private void ChangeBinding(BindingDevice? item = null)
        {
            EditBinding = false;
            if (item != null)
                NewBindingDevice = item;
        }

        private async Task GetBindingDevice()
        {
            if (LineID != null)
            {
                await Http.PostAsJsonAsync("api/v1/GetBindingDevice", new IntID() { ID = LineID ?? 0 }).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        await x.Result.Content.ReadFromJsonAsync<List<BindingDevice>>().ContinueWith(r =>
                        {
                            BindingDevice = r.Result?.FirstOrDefault();

                            if (BindingDevice == null)
                                BindingDevice = new BindingDevice() { Name = GsoRep["IDS_STRING_DEVICE_NOT_PRESENT"] };
                            NewBindingDevice = new BindingDevice(BindingDevice);
                            StateHasChanged();
                        });
                    }
                });

            }
        }

        private async Task GetRestrictList()
        {
            AddParam = false;
            await Http.PostAsJsonAsync("api/v1/GetRestrictList", new IntID() { ID = Model?.ChannelType ?? (int)BaseLineType.LINE_TYPE_UNDEF }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    RestrictList = await x.Result.Content.ReadFromJsonAsync<List<Restrict>>() ?? new();
                    StateHasChanged();
                }
            });
        }

        private async Task DeleteRestrict()
        {
            if (Model != null && SelectRestrict != null && SelectRestrict.RestrictType == 1)
            {
                await Http.PostAsJsonAsync("api/v1/DeleteRestrict", new DeleteRestrictRequest { LLineType = Model.ChannelType, LRestrictType = SelectRestrict.RestrictType, LBitNumber = SelectRestrict.BitNumber }).ContinueWith(async x =>
                      {
                          if (x.Result.IsSuccessStatusCode)
                          {
                              SelectRestrict = null;
                              await GetRestrictList();
                          }
                      });
            }
        }

        private bool IsChecked(Restrict item)
        {
            bool isChecked = false;
            if (item.RestrictType == 0)
                isChecked = ((Model?.GlobalMask >> item.BitNumber) & 0x01) > 0;
            if (item.RestrictType == 1)
                isChecked = ((Model?.UserMask >> item.BitNumber) & 0x01) > 0;
            if (item.RestrictType == 2)
                isChecked = ((Model?.PropertyMask >> item.BitNumber) & 0x01) > 0;
            return isChecked;
        }


        private void SetRestrictBitStatus(Restrict item)
        {
            if (Model == null)
                Model = new();

            int BitNumber = item.BitNumber;

            int i = 0;
            i |= (1 << BitNumber);

            if (item.RestrictType == 0)
            {
                if (((Model.GlobalMask >> BitNumber) & 0x01) > 0)
                {
                    Model.GlobalMask -= i;
                }
                else
                {
                    Model.GlobalMask += i;
                }
            }
            else if (item.RestrictType == 1)
            {
                if (((Model.UserMask >> BitNumber) & 0x01) > 0)
                {
                    Model.UserMask -= i;
                }
                else
                {
                    Model.UserMask += i;
                }
            }
            else if (item.RestrictType == 2)
            {
                if (((Model.PropertyMask >> BitNumber) & 0x01) > 0)
                {
                    Model.PropertyMask -= i;
                }
                else
                {
                    Model.PropertyMask += i;
                }
            }


        }


        private async Task GetLocationInfo()
        {
            await Http.PostAsJsonAsync("api/v1/GetObjects_ILocation", new OBJ_ID() { StaffID = StaffId }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    LocationList = await x.Result.Content.ReadFromJsonAsync<List<Objects>>() ?? new();
                }
            });
        }

        private async Task GetLineTypeList()
        {
            await Http.PostAsJsonAsync("api/v1/GetLineTypeList", new IntID() { ID = SubsystemID }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    LineTypes = await x.Result.Content.ReadFromJsonAsync<List<IntAndString>>() ?? new();
                }
            });
        }

        private async Task SetConnType(ChangeEventArgs e)
        {
            int.TryParse(e?.Value?.ToString(), out int id);
            if (Model == null)
                Model = new();
            Model.ChannelType = id;
            await ChangeLineTypeAsync(id);
        }

        private async Task ChangeLineTypeAsync(int ChannelType)
        {
            EditPhone = false;
            if (LineID == null && Model != null)
            {
                Model.Phone = "";
            }
            ViewExt = false;
            switch (ChannelType)
            {
                case (int)BaseLineType.LINE_TYPE_DIAL_UP:
                    ViewConnection = true;
                    ViewLocation = true;
                    //ViewPhone = SwitchPhone(Model?.Phone);
                    ViewParams = false;
                    ViewExt = true;
                    TitlePhone = GSOFormRep["AbonNumberLine"];
                    break;

                case (int)BaseLineType.LINE_TYPE_SMTP:
                    ViewParams = false;
                    ViewConnection = true;
                    ViewLocation = false;
                    /*if(m_Verb == verbNew) SwitchPhone(NULL);
                    else */
                    //ViewPhone = true;// SwitchPhone(Model?.Phone);
                    TitlePhone = GSOFormRep["SMTPName"];
                    break;

                case (int)BaseLineType.LINE_TYPE_GSM_TERMINAL:
                    ViewConnection = true;
                    ViewLocation = true;
                    ViewParams = true;
                    //ViewPhone = false;
                    break;

                case (int)BaseLineType.LINE_TYPE_WSDL:
                case (int)BaseLineType.LINE_TYPE_DCOM:
                case (int)BaseLineType.LINE_TYPE_SNMP_GATE:
                    ViewConnection = true;
                    ViewLocation = false;
                    ViewParams = true;
                    //ViewPhone = false;
                    break;

                case (int)BaseLineType.LINE_TYPE_DEDICATED:
                case (int)BaseLineType.LINE_TYPE_HSCOM:
                    ViewConnection = true;
                    ViewLocation = false;
                    if (LineID == null)
                    {
                        EditPhone = true;

                        if (SubsystemID == SubsystemType.SUBSYST_SZS && MaxNumber < 100)
                            MaxNumber = 100;

                        if (Model != null)
                            Model.Phone = MaxNumber.ToString();
                    }
                    //else ViewPhone = SwitchPhone(Model?.Phone);
                    ViewParams = false;
                    TitlePhone = GSOFormRep["HSCOMName"];
                    break;

                case (int)BaseLineType.LINE_TYPE_RADIO:
                    ViewConnection = true;
                    ViewLocation = false;
                    //ViewPhone = SwitchPhone(Model?.Phone);
                    ViewParams = false;
                    TitlePhone = GSOFormRep["RadioName"];
                    break;

            }
            await GetRestrictList();
            NewBindingDevice = new BindingDevice() { Name = GsoRep["IDS_STRING_DEVICE_NOT_PRESENT"] };

        }

        private async Task Close()
        {
            await CallEvent(null);
        }

        private async Task CallEvent(bool? b)
        {
            if (CallBack.HasDelegate)
                await CallBack.InvokeAsync(b);
        }

    }
}
