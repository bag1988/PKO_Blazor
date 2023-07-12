using System.Net.Http.Json;
using AsoCommonProto.V1;
using AsoDataProto.V1;
using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Components;
using SCSChLService.Protocol.Grpc.Proto.V1;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Pages.ASO.ControllingDevice
{
    partial class CreateDevice
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        [Parameter]
        public int? DeviceID { get; set; }

        [Parameter]
        public EventCallback<bool?> CallBack { get; set; }
                
        private AsoControllingDeviceInfo? Model;

        private AsoControllingDeviceInfo? OldModel;

        private List<ControllerDescription>? ControllList;
        private List<ControllerDescription>? OriginalControllList;

        private List<AsoLineBindingEx>? LineInfo;
        private List<AsoLineBindingEx>? OriginalLineInfo;

        private List<BindingLine>? FreeLine;

        private List<GetSheduleListItem>? SheduleList;

        List<int> СhannelsList = new();

        private Dictionary<int, string> PortList = new();
        private Dictionary<int, string> m_ctrlTypeController = new();

        private string TitleError = "";

        private string DeviceComm = "";

        //Нужно отображение канальности устройства
        //IDC_STATIC_MAX_CHANNELS, IDC_MAX_CHANNELS
        private bool ShowMaxChannels = false;

        //Нужно отображение пользователя и пароля
        //IDC_STATIC_USER_NAME, IDC_USER, IDC_STATIC_PASSW, IDC_PASSWORD, IDC_STATIC_USER
        private bool ShowUserPassw = false;
        //Нужно отображение типа контроллера
        //IDC_STATECONTROLLER, IDC_CONTROLLERID, IDC_CONTROLLER_TXT, IDC_TYPECONTROLLERID, IDC_TYPECONTROLLER_TXT
        private bool EnableTypeController = false;

        private bool ViewParamVoIP = false;

        private bool EditPortID = false; // IDC_EDIT_PORTID

        private bool PortID = false; //IDC_PORTID

        private bool CheckState = false;

        private bool OldCheckState = false;

        private int SheduleID = 1;
        private int OldSheduleID = 1;

        private int m_ddxChannelsPerController = 0;

        private Dictionary<int, string>? ThList;

        private int IndexController = 1;

        private int PortId = -1;

        private int TypeTable = 1;
        private int StaffId = 0;

        protected override async Task OnInitializedAsync()
        {
            StaffId = await _User.GetLocalStaff();
            ThList = new Dictionary<int, string>
            {
                { -1, AsoRep["IDS_STRING_CHANNEL"] },
                { -2, AsoRep["IDS_STRING_CONNECTING"] },
                { -3, AsoRep["IDS_STRING_PERMITTING"] }
            };

            TitleError = AsoRep[DeviceID != null ? "IDS_REG_BLOCK_UPDATE" : "IDS_REG_BLOCK_INSERT"];

            await GetSheduleList();
            await GGetControllerInfo();
            if (DeviceID != null)
            {
                await GetInfo();
            }
            else
            {
                ControllList = new();
                ControllerDescription controllDescript = new();
                controllDescript.ControllerType = 0x00080005;// ASO_TYPE_ASO43
                controllDescript.CountChannel = 4;
                controllDescript.Enable = true;
                controllDescript.DeviceIndex = 1;
                GetParamController(ref controllDescript);
                ControllList.Add(controllDescript);

                Model = new();
                Model.TypeConnect = 0x08;//ASO_TC_USB
                Model.CountControllers = 1;
                Model.Enable = 1;
                Model.ConnID = -1;
                Model.CountChannel = (Model.CountControllers * controllDescript.CountChannel);

                await SetTypeConnect(Model.TypeConnect);
                SheduleID = SheduleList?.FirstOrDefault(x => x.Duration.ToTimeSpan().Minutes == 15)?.SheduleID ?? 0;
            }

            OldModel = new AsoControllingDeviceInfo(Model);
        }

        private async Task GetInfo()
        {
            if (DeviceID != null)
            {
                await Http.PostAsJsonAsync("api/v1/GGetDeviceInfo", new OBJ_ID() { ObjID = DeviceID ?? 0 }).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        Model = await x.Result.Content.ReadFromJsonAsync<AsoControllingDeviceInfo>() ?? new();


                        if (Model.TypeConnect != 0)
                            await SetTypeConnect(Model.TypeConnect);
                        else
                            MessageView?.AddError("", UUZSRep["IDS_ERRGETDEVINFO"]);

                        PortId = Model.ConnID;

                        if (Model.PortIndex != -1)
                        {
                            PortId = Model.PortIndex;
                        }


                        await GetDeviceShedule();


                    }
                    else
                    {
                        Model = new();
                        MessageView?.AddError("", UUZSRep["IDS_ERRGETDEVINFO"]);
                    }
                });
            }
        }

        private async Task OnAdd()
        {
            if (Model != null)
            {

                if (string.IsNullOrEmpty(Model.DeviceName))
                {
                    MessageView?.AddError(TitleError, DeviceRep["ErrorNull"] + " " + GsoRep["IDS_STRING_NAME"]);
                    return;
                }
                if (Model.TypeConnect == 0)
                {
                    MessageView?.AddError(TitleError, AsoRep["IDS_STRING_SEL_DEV_CONN_TYPE"]);
                    return;
                }

                if (!string.IsNullOrEmpty(Model.Password) && Model.Password.Length > 31)
                {
                    MessageView?.AddError(TitleError, DeviceRep["MAX_LENGTH_PASSWORD"]);
                    return;
                }

                //if (Model.TypeConnect == 0x0A/*ASO_TC_GSMT*/ || Model.TypeConnect == 0x0C/*ASO_TC_ASOGSM*/)
                //{
                //    Model.ConnID = 0;
                //}

                switch (Model.TypeConnect)
                {
                    case 0x0D://ASO_TC_DCOM:
                        break;

                    case 0x06://ASO_TC_SMTP:
                    case 0x09://ASO_TC_SNMP:
                    case 0x07://ASO_TC_SMSGATE:
                    {
                        if (string.IsNullOrEmpty(Model.DeviceComm))
                        {
                            MessageView?.AddError(TitleError, AsoRep["IDS_STRING_ENTER_NAME_SMTP"]);
                            return;
                        }

                        if (!(PortId > -1 && PortId <= 0xFFFF))
                        {
                            MessageView?.AddError(TitleError, AsoRep["IDS_STRING_ENTER_PORT"] + " [0..65535]");
                            return;
                        }
                        else
                        {
                            Model.ConnID = 1;
                            Model.PortIndex = PortId;
                        }
                    }
                    break;

                    case 0x0E://ASO_TC_WSDL:
                    {
                        if (string.IsNullOrEmpty(Model.DeviceComm))
                        {
                            MessageView?.AddError(TitleError, AsoRep["IDS_STRING_ENTER_PATH_WSDL"] + " (WSDL)!");
                            return;
                        }
                    }
                    break;

                    case 0x0B://ASO_TC_VOIP:
                    {
                        if (string.IsNullOrEmpty(Model.Domain))
                        {
                            MessageView?.AddError(TitleError, AsoRep["IDS_STRING_ENTER_ATS"]);
                            return;
                        }
                        else
                        {
                            Model.ConnID = 0;
                            Model.PortIndex = PortId;
                        }
                    }
                    break;

                    default:
                    {
                        if (PortId == -1)
                        {
                            MessageView?.AddError(TitleError, AsoRep["IDS_STRING_SEL_PORT"]);
                            return;
                        }
                    }
                    break;
                }

                if (!await GetLocationInfo())
                {
                    MessageView?.AddError(TitleError, AsoRep["IDS_E_GETLOCATION"]);
                    return;
                }


                UpdatingControllingDeviceFlags flags = new();
                flags.UControllingDevice = !Model.Equals(OldModel);
                flags.UControllersDescript = false;

                if (ControllList != null && OriginalControllList != null)
                {
                    flags.UControllersDescript = !ControllList.SequenceEqual(OriginalControllList);
                }
                flags.UCountChannel = false;
                flags.ULines = false;
                if (LineInfo != null && OriginalLineInfo != null)
                {
                    flags.UCountChannel = LineInfo.Count != OriginalLineInfo.Count;
                    flags.ULines = !LineInfo.SequenceEqual(OriginalLineInfo);
                }


                if (flags.UControllingDevice == true || flags.UControllersDescript == true || flags.UCountChannel == true || flags.ULines == true)
                {

                    SetControllingDevice request = new SetControllingDevice();

                    request.ControllingDevice = Model;
                    request.flags = flags;


                    if (flags.UControllersDescript == true || flags.UCountChannel == true)
                    {
                        //Добавляем в модель список ControllingDevice

                        request.ControllDesc = new(ControllList ?? new());
                    }


                    await Http.PostAsJsonAsync("api/v1/SetControllingDeviceInfo", request).ContinueWith(async x =>
                    {
                        if (x.Result.IsSuccessStatusCode)
                        {
                            if (flags.ULines == true || flags.UCountChannel == true)
                            {
                                if (LineInfo != null && OriginalLineInfo != null)
                                {
                                    // Удалить только измененные привязки
                                    foreach (var line in OriginalLineInfo.Except(LineInfo).ToList())
                                    {
                                        await Http.PostAsJsonAsync("api/v1/DeleteLineBinding", new IntID() { ID = line.AsoLineBinding.LineBinding.LineID }).ContinueWith(x =>
                                        {
                                            if (!x.Result.IsSuccessStatusCode)
                                            {
                                                MessageView?.AddError(AsoRep["IDS_ERRORCAPTION"], AsoRep["IDS_E_SETLINEBINDING"]);
                                            }
                                        });
                                    }

                                    foreach (var line in LineInfo)
                                    {
                                        if (line.AsoLineBinding.LineBinding.LineID != 0)
                                        {
                                            LineBinding request = new() { LineID = line.AsoLineBinding.LineBinding.LineID, ChannelID = line.AsoLineBinding.LineBinding.ChannelID, DeviceID = line.AsoLineBinding.LineBinding.DeviceID };

                                            await Http.PostAsJsonAsync("api/v1/SetLineBinding", request).ContinueWith(async x =>
                                            {
                                                if (x.Result.IsSuccessStatusCode)
                                                {
                                                    await Http.PostAsJsonAsync("api/v1/SetLineStatus", new CSetLineStatus() { LineID = line.AsoLineBinding.LineBinding.LineID, Status = line.AsoLineBinding.Enable });
                                                }
                                                else
                                                {
                                                    MessageView?.AddError(AsoRep["IDS_ERRORCAPTION"], AsoRep["IDS_E_SETLINEBINDING"]);
                                                }
                                            });
                                        }
                                    }
                                }
                            }
                            await CallEvent(true);

                        }
                        else
                        {
                            MessageView?.AddError(TitleError, AsoRep["IDS_EFAILSAVECHANGES"]);
                        }
                    });
                }
                else if (CheckState == OldCheckState && OldSheduleID == SheduleID)
                {
                    MessageView?.AddError(TitleError, AsoRep["NotChangeSave"]);
                    return;
                }

            }
            else
            {
                MessageView?.AddError(TitleError, AsoRep["IDS_STRING_INFO_ABOUT_DEVICE_NOT_COMPLETE"]);
            }

            await SetDeviceShedule();


        }

        private async Task GGetControllerInfo()
        {
            if (DeviceID != null)
            {
                await Http.PostAsJsonAsync("api/v1/GGetControllerInfo", new OBJ_ID() { ObjID = DeviceID.Value }).ContinueWith(async x =>
                  {
                      if (x.Result.IsSuccessStatusCode)
                      {
                          ControllList = await x.Result.Content.ReadFromJsonAsync<List<ControllerDescription>>() ?? new();
                          OriginalControllList = new List<ControllerDescription>(ControllList?.Select(x => new ControllerDescription(x)) ?? new List<ControllerDescription>());
                      }
                  });
            }
            else
            {
                OriginalControllList = new();
                ControllList = new();
            }
        }

        private void ChangeBindingLine(ChangeEventArgs e, int Channel)
        {
            int.TryParse(e.Value?.ToString(), out var LineID);
            if (LineInfo != null && Model != null && !LineInfo.Any(x => x.AsoLineBinding.LineBinding.ChannelID == Channel && x.AsoLineBinding.LineBinding.LineID == LineID))
            {
                if (LineInfo.Any(x => x.AsoLineBinding.LineBinding.ChannelID == Channel))
                {
                    var l = LineInfo.FirstOrDefault(x => x.AsoLineBinding.LineBinding.ChannelID == Channel);
                    if (l != null)
                    {
                        if (FreeLine == null)
                            FreeLine = new();

                        LineInfo.Remove(l);
                        FreeLine.Add(new BindingLine() { LineID = l.AsoLineBinding.LineBinding.LineID, LineName = l.LineName, LineTypeName = l.LineTypeName });
                        FreeLine = FreeLine.OrderBy(x => x.LineID).ToList();

                    }
                }
                if (LineID != 0)
                {
                    if (FreeLine != null)
                    {
                        var l = FreeLine.FirstOrDefault(x => x.LineID == LineID);
                        if (l != null && !LineInfo.Any(x => x.AsoLineBinding.LineBinding.LineID == LineID))
                        {
                            FreeLine.Remove(l);
                            LineInfo.Add(new AsoLineBindingEx() { AsoLineBinding = new() { LineBinding = new() { LineID = l.LineID, ChannelID = Channel, DeviceID = Model.DeviceID }, Enable = 1 }, LineName = l.LineName, LineTypeName = l.LineTypeName });
                        }
                    }
                }
                StateHasChanged();
            }
        }

        private void ChangeInputChannels(int id)
        {
            if (id == 0) id = 1;
            if (Model == null)
                return;
            if (!(id > 0 && id <= 1024))
                m_ddxChannelsPerController = Model.CountChannel;
            else if (Model.TypeConnect == 0x0B/*ASO_TC_VOIP*/ && Model.CountChannel != id)
                ReInitControllers(Model.TypeConnect, ControllList?.FirstOrDefault()?.ControllerType ?? 0, id);
        }

        private void ChangePortId(int id)
        {
            if (Model == null) return;
            PortId = id;
            Model.ConnID = Model.PortIndex = -1;
            int dwTypeConnect = Model.TypeConnect;
            if (dwTypeConnect == 0x04/*ASO_TC_LPT*/)
                Model.ConnID = id;
            else if (dwTypeConnect == 0x02/*ASO_TC_COM*/ || dwTypeConnect == 0x08 /*ASO_TC_USB*/)
                Model.ConnID = id;
            else if (dwTypeConnect == 0x0A/*ASO_TC_GSMT*/ || dwTypeConnect == 0x0C/*ASO_TC_ASOGSM*/)
                Model.PortIndex = id;
        }

        private async Task SetDeviceShedule()
        {
            if (CheckState != OldCheckState || OldSheduleID != SheduleID)
            {
                OBJ_Key OBJ_Key = new();
                OBJ_Key.ObjID = new OBJ_ID() { ObjID = Model?.DeviceID ?? 0, SubsystemID = SubsystemID, StaffID = StaffId };
                OBJ_Key.ObjType = (int)HMT.Aso;
                await Http.PostAsJsonAsync("api/v1/SetDeviceShedule", new SetShedule() { OBJKey = OBJ_Key, Shedule = SheduleID, Status = CheckState ? 1 : 0 });
                await CallEvent(true);
            }

        }

        private async Task GetDeviceShedule()
        {
            OBJ_Key OBJ_Key = new OBJ_Key();
            OBJ_Key.ObjID = new OBJ_ID() { ObjID = Model?.DeviceID ?? 0, SubsystemID = SubsystemID, StaffID = StaffId };
            OBJ_Key.ObjType = (int)HMT.Aso;
            await Http.PostAsJsonAsync("api/v1/GetDeviceShedule", OBJ_Key).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var r = await x.Result.Content.ReadFromJsonAsync<GetShedule>() ?? new();

                    OldCheckState = CheckState = r.Status > 0 ? true : false;
                    OldSheduleID = SheduleID = r.Shedule == 0 ? 1 : r.Shedule;
                    StateHasChanged();
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

        private async Task GetFreeLineList()
        {
            await Http.PostAsync("api/v1/GetFreeLineList", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    FreeLine = await x.Result.Content.ReadFromJsonAsync<List<BindingLine>>() ?? new();
                }
                else
                    MessageView?.AddError("", GSOFormRep["IDS_E_GET_FREE_LINE"]);
            });
            await SetFreeLineAsync();
        }

        private async Task SetFreeLineAsync()
        {
            if (FreeLine != null && LineInfo != null && Model != null)
            {
                FreeLine.RemoveAll(r => LineInfo.Any(x => x.AsoLineBinding.LineBinding.LineID == r.LineID));

                FreeLine.RemoveAll(r =>
                {
                    bool bIsCorrect = false;
                    switch (r.LineType)
                    {
                        case (int)BaseLineType.LINE_TYPE_DIAL_UP:
                        {
                            switch (Model.TypeConnect)
                            {
                                case 0x02://ASO_TC_COM:
                                case 0x04://ASO_TC_LPT:
                                case 0x08://ASO_TC_USB:
                                case 0x0B://ASO_TC_VOIP:
                                case 0x0C://ASO_TC_ASOGSM:
                                    return false;
                            }
                        }
                        break;
                        case (int)BaseLineType.LINE_TYPE_GSM_TERMINAL: bIsCorrect = Model.TypeConnect == 0x0A/*ASO_TC_GSMT*/; break;
                        case (int)BaseLineType.LINE_TYPE_SMTP: bIsCorrect = Model.TypeConnect == 0x06/*ASO_TC_SMTP*/; break;
                        case (int)BaseLineType.LINE_TYPE_DCOM: bIsCorrect = Model.TypeConnect == 0x0D/*ASO_TC_DCOM*/; break;
                        case (int)BaseLineType.LINE_TYPE_WSDL: bIsCorrect = Model.TypeConnect == 0x0E/*ASO_TC_WSDL*/; break;
                    }
                    return !bIsCorrect;
                });
                StateHasChanged();
            }
        }


        private async Task<Line> GetLineInfo(int id)
        {
            Line response = new();
            await Http.PostAsJsonAsync("api/v1/GetLineInfo", new IntID() { ID = id }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    response = await x.Result.Content.ReadFromJsonAsync<Line>() ?? new();
                }
            });
            return response;
        }


        private async Task GGetLineInfo(int id)
        {

            await Http.PostAsJsonAsync("api/v1/GGetLineInfo", new OBJ_ID() { ObjID = id }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    LineInfo = await x.Result.Content.ReadFromJsonAsync<List<AsoLineBindingEx>>() ?? new();
                    OriginalLineInfo = new List<AsoLineBindingEx>(LineInfo?.Select(x => new AsoLineBindingEx(x)) ?? new List<AsoLineBindingEx>());
                }
            });
            await GetFreeLineList();
        }

        private async Task SetTypeConnect(int id)
        {
            if (Model == null)
                Model = new();
            Model.TypeConnect = id;
            await GGetLineInfo(Model.DeviceID);

            IndexController = 1;

            ShowMaxChannels = false;
            EnableTypeController = false;
            ShowUserPassw = false;
            EditPortID = false;
            PortID = false;
            DeviceComm = GsoRep["IDS_STRING_SIT_COMMENT"];

            switch (id)
            {
                case 0x02:// ASO_TC_COM:
                {
                    await FillComPortList(Model.ConnID);
                    PortID = true;
                    ShowMaxChannels = true;
                    EnableTypeController = true;
                    ReInitControllers(id, 0x00080004/*ASO_TYPE_ASO6*/, 8/*CHANNELS_ASO8*/);
                }
                break;

                case 0x08:// ASO_TC_USB:
                {
                    await FillComPortList(Model.ConnID);
                    ShowMaxChannels = true;
                    PortID = true;
                    EnableTypeController = true;
                    ReInitControllers(id, 0x00080005/*ASO_TYPE_ASO43*/, 4/*CHANNELS_ASO43*/);
                }
                break;

                case 0x0B:// ASO_TC_VOIP:
                {
                    PortId = 0;
                    EditPortID = true;
                    EnableTypeController = true;
                    ShowUserPassw = true;
                    m_ddxChannelsPerController = Model.CountChannel;
                    ReInitControllers(id, 0x00080010/*ASO_TYPE_ASOVOIP*/, Model.CountChannel);
                }
                break;

                case 0x04:// ASO_TC_LPT:
                {
                    if (DeviceID == null && Model.ConnID == 0) Model.ConnID = 1;
                    FillLptPortList();
                    PortID = true;
                    EnableTypeController = true;
                    ShowMaxChannels = true;
                    ReInitControllers(id, 0x00080003/*ASO_TYPE_ASO5*/, 8/*CHANNELS_ASO8*/);
                }
                break;

                case 0x06:// ASO_TC_SMTP:
                {
                    PortId = 1;
                    Model.ConnID = 1;
                    if (DeviceID == null)
                        Model.ConnID = 1;
                    EditPortID = true;
                    ShowUserPassw = true;
                    DeviceComm = DeviceRep["IpServer"];//Имя сервера (IP):
                    ReInitControllers(id, 0x00080006/*ASO_TYPE_SMTP*/, 1/*CHANNELS_ASO_SMTP*/);
                }
                break;

                case 0x0D:// ASO_TC_DCOM:
                {
                    if (DeviceID == null)
                        Model.ConnID = 0;
                    ShowUserPassw = true;
                    ReInitControllers(id, 0x00080012/*ASO_TYPE_DCOM*/, 1/*CHANNELS_ASO_DCOM*/);
                }
                break;

                case 0x0E:// ASO_TC_WSDL:
                {
                    if (DeviceID == null)
                        Model.ConnID = 0;
                    ShowUserPassw = true;
                    DeviceComm = DeviceRep["WsdlServer"];//Веб-сервис (WSDL):
                    ReInitControllers(id, 0x00080013/*ASO_TYPE_WSDL*/, 1/*CHANNELS_ASO_WSDL*/);
                }
                break;

                case 0x07:// ASO_TC_SMSGATE:
                {
                    EditPortID = true;
                    ShowUserPassw = true;
                    DeviceComm = DeviceRep["IpServer"];//Имя сервера (IP):
                }
                break;

                case 0x09:// ASO_TC_SNMP:
                {
                    EditPortID = true;
                    ShowUserPassw = true;
                    DeviceComm = DeviceRep["IpServer"];//Имя сервера (IP):
                    ReInitControllers(id, 0x00080009/*ASO_TYPE_SNMP*/, 1/*CHANNELS_ASO_SNMP*/);
                }
                break;

                case 0x0A:// ASO_TC_GSMT:
                {
                    if (DeviceID == null)
                        Model.ConnID = 1;
                    await FillComPortList(Model.ConnID);
                    PortID = true;
                    ShowUserPassw = true;
                    ReInitControllers(id, 0x00080007/*ASO_TYPE_GSM*/, 1/*CHANNELS_ASO_GSM*/);
                }
                break;

                case 0x0C:// ASO_TC_ASOGSM:
                {
                    if (DeviceID == null)
                        Model.ConnID = 0;
                    await FillComPortList(Model.ConnID);
                    PortID = true;
                    EnableTypeController = true;
                    ReInitControllers(id, 0x00080011 /*ASO_TYPE_SIM300*/, 1/*CHANNELS_ASO_GSM*/);
                }
                break;
            }
            StateHasChanged();
        }

        // Заполнить список портов для первых трех LPT - других пока не встречали
        private void FillLptPortList()
        {
            PortList = new();
            for (int i = 1; i < 4; i++)
            {
                PortList.Add(i, "LPT " + i);
            }
        }

        // Заполнить список портов COM портами
        private async Task FillComPortList(int dwDefaultPort)
        {
            PortList = new();

            await Http.PostAsync("api/v1/EnumPortsEx", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var r = await x.Result.Content.ReadFromJsonAsync<List<ComPort>>() ?? new();
                    foreach (var p in r)
                    {
                        PortList.Add((int)p.PortNum, p.PortName);
                    }
                }
                else
                    MessageView?.AddError("", UUZSRep["IDS_STRING_CANNOT_SUBSYSTEM_CONNECT"]);
            });
        }

        private void ReInitControllers(int dwDeviceConnectType, int dwControllerType, int dwChannelsPerController)
        {

            int dwChannelsCount = 0;
            if (ControllList?.Any() ?? false && Model != null)
            {
                // Оставить только один контроллер для следующих типов
                switch (dwDeviceConnectType)
                {
                    case 0x06:// ASO_TC_SMTP:
                    case 0x0D:// ASO_TC_DCOM:
                    case 0x0E:// ASO_TC_WSDL:
                    case 0x09:// ASO_TC_SNMP:
                    case 0x0A:// ASO_TC_GSMT:
                    case 0x0C:// ASO_TC_ASOGSM:
                    case 0x0B:// ASO_TC_VOIP:
                    {
                        ControllList = ControllList.Take(1).ToList();
                    }
                    break;
                }

                foreach (var item in ControllList)
                {
                    int dwType = dwControllerType;
                    switch (item.ControllerType)
                    {
                        case 0x00080001:// ASO_TYPE_ASO8:
                        case 0x00080002:// ASO_TYPE_ASOM:
                        case 0x00080003:// ASO_TYPE_ASO5:
                        {
                            if (dwDeviceConnectType == 0x04/* ASO_TC_LPT*/)
                                dwType = item.ControllerType;
                        }
                        break;
                    }

                    item.ControllerType = dwType;
                    item.CountChannel = dwChannelsPerController;
                    dwChannelsCount += item.CountChannel;

                }

                Model!.CountControllers = ControllList.Count();
                if (Model.CountChannel != dwChannelsCount)
                {
                    if (Model.CountChannel < dwChannelsCount)
                        AddLineDescr(dwChannelsCount);

                    Model.CountChannel = dwChannelsCount;
                    if (LineInfo?.Any() ?? false)
                    {
                        DeleteBinding(dwChannelsCount);
                    }

                }

            }
            FillMaxChannels(dwChannelsPerController);
            SetMaxChannels(dwChannelsCount);
        }

        private void AddLineDescr(int MaxChannels)
        {
            if (OriginalLineInfo != null && Model != null)
            {
                if (LineInfo == null)
                    LineInfo = new();

                foreach (var item in OriginalLineInfo.Where(x => x.AsoLineBinding.LineBinding.ChannelID <= MaxChannels).ToList())
                {
                    if (!LineInfo.Any(x => x.AsoLineBinding.LineBinding.LineID == item.AsoLineBinding.LineBinding.LineID))
                    {
                        LineInfo.Add(item);

                        if (FreeLine?.Any(x => x.LineID == item.AsoLineBinding.LineBinding.LineID) ?? false)
                        {
                            FreeLine.RemoveAll(x => x.LineID == item.AsoLineBinding.LineBinding.LineID);
                        }
                    }
                }

            }

        }

        private void SetMaxChannels(int dwMaxChannels)
        {
            if (Model != null && OriginalControllList != null && ControllList != null && ControllList.Any())
            {
                if (Model.CountChannel < dwMaxChannels)
                {
                    AddLineDescr(dwMaxChannels);
                    Model.CountChannel = dwMaxChannels;
                    Model.CountControllers = dwMaxChannels / ControllList.First().CountChannel;

                    while (ControllList.Count < Model.CountControllers)
                    {
                        ControllerDescription controllDescript = new();
                        if (OriginalControllList.Count > ControllList.Count)
                        {
                            // Взять оригинальные значения контроллера
                            controllDescript = OriginalControllList[ControllList.Count];
                        }
                        else
                        {
                            // Подставить новые
                            controllDescript.Enable = true;
                            controllDescript.ControllerType = ControllList[ControllList.Count - 1].ControllerType;
                            controllDescript.CountChannel = ControllList[ControllList.Count - 1].CountChannel;
                            controllDescript.DeviceIndex = (ControllList[ControllList.Count - 1].DeviceIndex + 1);
                            controllDescript.DeviceID = ControllList[ControllList.Count - 1].DeviceID;
                            GetParamController(ref controllDescript);
                        }
                        ControllList.Add(controllDescript);
                    }
                }

                if (Model.CountChannel > dwMaxChannels)
                {
                    DeleteBinding(dwMaxChannels);
                    Model.CountChannel = dwMaxChannels;
                    Model.CountControllers = dwMaxChannels / ControllList.First().CountChannel;
                    ControllList = ControllList.Take(Model.CountControllers).ToList();
                }
            }
            SetControllerIndex(0);
        }

        private void DeleteBinding(int MaxChannels)
        {
            if (LineInfo != null && LineInfo.Any())
            {
                if (FreeLine == null)
                    FreeLine = new();
                foreach (var line in LineInfo.Where(x => x.AsoLineBinding.LineBinding.ChannelID > MaxChannels).ToList())
                {
                    if (line.AsoLineBinding.LineBinding.LineID != 0)
                    {
                        // Нужно освободить привязку удаляемой линии
                        FreeLine.Add(new BindingLine() { LineID = line.AsoLineBinding.LineBinding.LineID, LineName = line.LineName, LineTypeName = line.LineTypeName });
                        LineInfo.Remove(line);
                    }
                }
                FreeLine = FreeLine.OrderBy(x => x.LineID).ToList();
            }
        }

        private void SetControllerIndex(int dwControllerIndex)
        {
            IndexController = 1;
            var pControllDescript = ControllList?.ElementAtOrDefault(dwControllerIndex);
            if (pControllDescript == null)
                return;

            m_ctrlTypeController = new();

            switch (Model?.TypeConnect)
            {
                case 0x02:// ASO_TC_COM:
                {
                    m_ctrlTypeController.Add(0x00080004, "K6");//ASO_TYPE_ASO6
                }
                break;

                case 0x04://ASO_TC_LPT:
                {
                    m_ctrlTypeController.Add(0x00080001, "K3");//ASO_TYPE_ASO8
                    m_ctrlTypeController.Add(0x00080002, "K4");//ASO_TYPE_ASOM
                    m_ctrlTypeController.Add(0x00080003, "K5");//ASO_TYPE_ASO5
                }
                break;


                case 0x08://ASO_TC_USB:
                {
                    m_ctrlTypeController.Add(0x00080005, "K43");//ASO_TYPE_ASO43
                }
                break;

                case 0x0B://ASO_TC_VOIP:
                {
                    m_ctrlTypeController.Add(0x00080010, "VoIP");//ASO_TYPE_ASOVOIP
                }
                break;

                case 0x0C://ASO_TC_ASOGSM:
                {
                    m_ctrlTypeController.Add(0x00080011, "SIMCOM");//ASO_TYPE_SIM300
                }
                break;

                case 0x0A://ASO_TC_GSMT:
                {
                    m_ctrlTypeController.Add(0x00080007, "GSM Terminal");//ASO_TYPE_GSM
                }
                break;


                case 0x09://ASO_TC_SNMP:
                {
                    m_ctrlTypeController.Add(0x00080009, "SNMP");//ASO_TYPE_SNMP
                }
                break;

                case 0x06://ASO_TC_SMTP:
                {
                    m_ctrlTypeController.Add(0x00080006, "E-Mail");//ASO_TYPE_SMTP
                }
                break;

                case 0x0D://ASO_TC_DCOM:
                {
                    m_ctrlTypeController.Add(0x00080012, "DCOM");//ASO_TYPE_DCOM
                }
                break;

                case 0x0E://ASO_TC_WSDL:
                {
                    m_ctrlTypeController.Add(0x00080013, "WSDL");//ASO_TYPE_WSDL
                }
                break;

            }
        }

        //Заполнить максимальную канальность устройства
        private void FillMaxChannels(int step)
        {
            if (step == 0)
            {
                step = 4;
                if (DeviceID != null)
                    MessageView?.AddError(TitleError, DeviceRep["ErrorGetInfo"]);
            }

            СhannelsList = new List<int>();
            for (int index = step; index <= 64; index += step)
            {
                СhannelsList.Add(index);
            }
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

        private bool SetParamValue(int paramCode, string? e = null)
        {
            if (IndexController == 0 || ControllList == null || !ControllList.Any(x => x.DeviceIndex == IndexController) || e == null)
                return false;

            var pControllDescript = ControllList.First(x => x.DeviceIndex == IndexController);

            int offset = ParamController.FindIndex(x => x.DwCommand == paramCode);
            if (offset == -1)
                return false;

            var p = ParamController.First(x => x.DwCommand == paramCode);
            int paramValue = (int)(float.Parse(e.Replace(".", ",")) * (p.DwDevide / p.DwRatio));
            switch (offset)
            {
                case 0: pControllDescript.TdelayBC = paramValue; break;
                case 1: pControllDescript.CountCall = paramValue; break;
                case 2: pControllDescript.PeriodDSP = paramValue; break;
                case 3: pControllDescript.CountDTMF = paramValue; break;
                case 4: pControllDescript.TtoneErr = paramValue; break;
                case 5: pControllDescript.TvoiceContrl = paramValue; break;
                case 6: pControllDescript.TtoneWaitBusy = paramValue; break;
                case 7: pControllDescript.TpulsDial = paramValue; break;
                case 8: pControllDescript.TpauseDial = paramValue; break;
                case 9: pControllDescript.TpauseBDial = paramValue; break;
                case 10: pControllDescript.TpausePuls = paramValue; break;
                case 11: pControllDescript.TDTMFsign = paramValue; break;
                case 12: pControllDescript.TDTMFpause = paramValue; break;
                case 13: pControllDescript.TwToneAD = paramValue; break;
                case 14: pControllDescript.TwToneAMATSD = paramValue; break;
                case 15: pControllDescript.TpWithoutA = paramValue; break;
                case 16: pControllDescript.TwTicAnsw = paramValue; break;
                case 17: pControllDescript.TvoiceMess = paramValue; break;
                case 18: pControllDescript.TwToneAM = paramValue; break;
                case 19: pControllDescript.MaxTpausBusy = paramValue; break;
                case 20: pControllDescript.TtoneBefWT = paramValue; break;
                case 21: pControllDescript.TdelayTransient = paramValue; break;
                case 22: pControllDescript.TdelayStart = paramValue; break;
                default: return false;
            }
            return true;
        }

        private int GetParamValue(int paramCode)
        {
            int _value = 0;
            if (IndexController == 0 || ControllList == null || !ControllList.Any(x => x.DeviceIndex == IndexController))
                return 0;

            var pControllDescript = ControllList.First(x => x.DeviceIndex == IndexController);

            paramCode = ParamController.FindIndex(x => x.DwCommand == paramCode);

            switch (paramCode)
            {
                case 0: _value = pControllDescript.TdelayBC; break;
                case 1: _value = pControllDescript.CountCall; break;
                case 2: _value = pControllDescript.PeriodDSP; break;
                case 3: _value = pControllDescript.CountDTMF; break;
                case 4: _value = pControllDescript.TtoneErr; break;
                case 5: _value = pControllDescript.TvoiceContrl; break;
                case 6: _value = pControllDescript.TtoneWaitBusy; break;
                case 7: _value = pControllDescript.TpulsDial; break;
                case 8: _value = pControllDescript.TpauseDial; break;
                case 9: _value = pControllDescript.TpauseBDial; break;
                case 10: _value = pControllDescript.TpausePuls; break;
                case 11: _value = pControllDescript.TDTMFsign; break;
                case 12: _value = pControllDescript.TDTMFpause; break;
                case 13: _value = pControllDescript.TwToneAD; break;
                case 14: _value = pControllDescript.TwToneAMATSD; break;
                case 15: _value = pControllDescript.TpWithoutA; break;
                case 16: _value = pControllDescript.TwTicAnsw; break;
                case 17: _value = pControllDescript.TvoiceMess; break;
                case 18: _value = pControllDescript.TwToneAM; break;
                case 19: _value = pControllDescript.MaxTpausBusy; break;
                case 20: _value = pControllDescript.TtoneBefWT; break;
                case 21: _value = pControllDescript.TdelayTransient; break;
                case 22: _value = pControllDescript.TdelayStart; break;
            }
            return _value;
        }

        private void GetParamController(ref ControllerDescription ctrlDescr)
        {
            ctrlDescr.TdelayBC = ParamController[0].DwParam;
            ctrlDescr.CountCall = ParamController[1].DwParam;
            ctrlDescr.PeriodDSP = ParamController[2].DwParam;
            ctrlDescr.CountDTMF = ParamController[3].DwParam;
            ctrlDescr.TtoneErr = ParamController[4].DwParam;
            ctrlDescr.TvoiceContrl = ParamController[5].DwParam;
            ctrlDescr.TtoneWaitBusy = ParamController[6].DwParam;
            ctrlDescr.TpulsDial = ParamController[7].DwParam;
            ctrlDescr.TpauseDial = ParamController[8].DwParam;
            ctrlDescr.TpauseBDial = ParamController[9].DwParam;
            ctrlDescr.TpausePuls = ParamController[10].DwParam;
            ctrlDescr.TDTMFsign = ParamController[11].DwParam;
            ctrlDescr.TDTMFpause = ParamController[12].DwParam;
            ctrlDescr.TwToneAD = ParamController[13].DwParam;
            ctrlDescr.TwToneAMATSD = ParamController[14].DwParam;
            ctrlDescr.TpWithoutA = ParamController[15].DwParam;
            ctrlDescr.TwTicAnsw = ParamController[16].DwParam;
            ctrlDescr.TvoiceMess = ParamController[17].DwParam;
            ctrlDescr.TwToneAM = ParamController[18].DwParam;
            ctrlDescr.MaxTpausBusy = ParamController[19].DwParam;
            ctrlDescr.TtoneBefWT = ParamController[20].DwParam;
            ctrlDescr.TdelayTransient = ParamController[21].DwParam;
            ctrlDescr.TdelayStart = ParamController[22].DwParam;
        }


        private List<ParamControllerModel> ParamController
        {
            get
            {
                return new List<ParamControllerModel>()
                {
                    new (){DwCommand = 0xf0, SzNameParam= AsoRep["TdelayBC"], DwParam=100, DwRatio=10, DwDevide=1000, SzDim="с"},//Длительность паузы между звонками
                    new (){DwCommand = 0xf9, SzNameParam= AsoRep["STRING_CountCall"], DwParam=8, DwRatio=1, DwDevide= 1, SzDim=""},//Число звонков для принятия решения о не ответе
                    new (){DwCommand = 0xf8, SzNameParam= AsoRep["PeriodDSP"],DwParam= 20, DwRatio=1, DwDevide= 1, SzDim="мс"},//Период обращения к DSP
                    new (){DwCommand = 0xf7, SzNameParam= AsoRep["CountDTMF"],DwParam= 6,DwRatio= 1, DwDevide= 1,SzDim= ""},//Число цифр приема DTMF-ответа
                    new (){DwCommand = 0x00, SzNameParam= AsoRep["TtoneErr"], DwParam=150,DwRatio= 10,  DwDevide=1000, SzDim="с"},//Длительность тона при ошибке АТС после набора номера
                    new (){DwCommand = 0x01, SzNameParam= AsoRep["TvoiceContrl"], DwParam=100,DwRatio= 10, DwDevide= 1000, SzDim="с"},//Длительность контроля голоса
                    new (){DwCommand = 0x02, SzNameParam= AsoRep["TtoneWaitBusy"],DwParam= 1000, DwRatio=10, DwDevide= 1000, SzDim="с"},//Длительность ожидания тона после занятия линии
                    new (){DwCommand = 0x03, SzNameParam= AsoRep["TpulsDial"],DwParam= 6, DwRatio=10,  DwDevide=1000, SzDim="с"},//Длительность импульса набора
                    new (){DwCommand = 0x04, SzNameParam= AsoRep["TpauseDial"], DwParam=4, DwRatio=10, DwDevide= 1000, SzDim="с"},//Длительность паузы набора
                    new (){DwCommand = 0x05, SzNameParam= AsoRep["TpauseBDial"], DwParam=75, DwRatio=10,  DwDevide=1000, SzDim="с"},//Пауза между набором цифр
                    new (){DwCommand = 0x06, SzNameParam= AsoRep["TpausePuls"], DwParam=500, DwRatio=10, DwDevide= 1000,SzDim= "с"},//Длительность паузы при наборе
                    new (){DwCommand = 0x07, SzNameParam= AsoRep["TDTMFsign"],DwParam= 7, DwRatio=10, DwDevide= 1000,SzDim= "с"},//Длительность сигнала DTMF набора
                    new (){DwCommand = 0x08, SzNameParam= AsoRep["TDTMFpause"], DwParam=6,DwRatio= 10, DwDevide= 1000, SzDim="с"},//Длительность паузы DTMF набора
                    new (){DwCommand = 0x09, SzNameParam= AsoRep["TwToneAMATSD"], DwParam=1500, DwRatio=10,  DwDevide=1000,SzDim= "с"},//Длит. ожидания тона после набора номера на ГТС
                    new (){DwCommand = 0x12, SzNameParam= AsoRep["TwToneAD"],DwParam= 6000,DwRatio= 10,  DwDevide=1000, SzDim="с"},//Длит. ожидания тона после набора номера на АМТС
                    new (){DwCommand = 0x0a, SzNameParam= AsoRep["TpWithoutA"],DwParam= 50,DwRatio=10, DwDevide=1000, SzDim="с"},//Пауза при отключении анализа тона после подъема трубки
                    new (){DwCommand = 0x0b, SzNameParam= AsoRep["TwPassword"], DwParam=1500,DwRatio=10,  DwDevide=1000,SzDim= "с"},//Длительность ожидания подтверждения //TODO было 500
                    new (){DwCommand = 0x0c, SzNameParam= AsoRep["TvoiceMess"],DwParam= 2000,DwRatio= 10, DwDevide= 1000,SzDim= "с"},//Длительность голосового сообщения в канале
                    new (){DwCommand = 0x0d, SzNameParam= AsoRep["TwToneAM"], DwParam=700, DwRatio=10,  DwDevide=1000, SzDim="с"},//Длительность ожидания тона после КПВ
                    new (){DwCommand = 0x0e, SzNameParam= AsoRep["MaxTpausBusy"],DwParam= 90, DwRatio=10, DwDevide= 1000,SzDim= "с"},//Максимальная длительность паузы при Занято
                    new (){DwCommand = 0x0f, SzNameParam= AsoRep["TtoneBefWT"],DwParam= 100, DwRatio=10,  DwDevide=1000, SzDim="с"},//Длительность тона перед подтверждением
                    new (){DwCommand = 0x10, SzNameParam= AsoRep["TdelayTransient"],DwParam= 30,DwRatio=10,  DwDevide=1000, SzDim="с"},//Задержка для завершения переходного процесса
                    new (){DwCommand = 0x11,SzNameParam= AsoRep["TdelayStart"], DwParam=30,DwRatio=10,  DwDevide=1000, SzDim="с"}//Задержка после старта системы
                };
            }
        }

        private async Task<bool> GetLocationInfo()
        {
            bool response = false;
            await Http.PostAsJsonAsync("api/v1/GetObjects_ILocation", new OBJ_ID() { StaffID = StaffId }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var b = await x.Result.Content.ReadFromJsonAsync<List<Objects>>() ?? new();

                    if (b.Count > 0)
                    {
                        response = true;
                    }
                }
            });
            return response;
        }
    }
}
