using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using SMDataServiceProto.V1;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;
using SCSChLService.Protocol.Grpc.Proto.V1;
using SharedLibrary;
using SharedLibrary.Models;
using SharedLibrary.Utilities;

using static BlazorLibrary.Shared.Main;
using ControllingDevice = SMSSGsoProto.V1.ControllingDevice;


namespace DeviceConsole.Client.Shared.Devices
{
    partial class CreateContrDevice
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_SZS;

        [Parameter]
        public OBJ_ID? DeviceObj_ID { get; set; }

        [Parameter]
        public EventCallback<bool?> CallBack { get; set; }

        private ControllingDevice? Model;

        private CControlUnit? m_ControlUnit;

        private Dictionary<long, string> m_ComboComPort = new();
        readonly Dictionary<int, string> m_ComboDevType = new();
        readonly Dictionary<int, string> m_ComboVer = new();
        readonly Dictionary<SoundFormat, string> m_SoundFormat = new();

        private long? m_wPortUDP = 0;

        private string m_IPAddressUZS = "192.168.1.1";
        private BindingLine m_recBindingLine = new();
        private BindingLine Old_RecBindingLine = new();

        private List<BindingLine>? FreeLine;

        private UDPParams UDPParams = new();

        private AudioPortParams audioParam = new();

        private Rs232UzsParamsDetails rs232Uzs = new();
        private Rs232UuzsParamsDetails rs232Uuzs = new();

        private bool IsErrorSetting = false;

        private int StaffId = 0;

        protected override async Task OnInitializedAsync()
        {
            FillDictionary();
            StaffId = await _User.GetLocalStaff();

            if (SubsystemID == SubsystemType.SUBSYST_SZS)
            {
                await FillComPortList();
            }

            if (DeviceObj_ID != null)
            {
                await GetInfo();

                if (SubsystemID == SubsystemType.SUBSYST_SZS)
                {
                    await GetBindingLine();
                }
            }
            else
            {
                Model = new();
                DeviceObj_ID = new() { StaffID = StaffId, SubsystemID = SubsystemID };
                if (SubsystemID == SubsystemType.SUBSYST_SZS)
                {
                    Model = new() { CountCh = 1, Status = 1 };

                    Model.Type = m_ComboDevType.FirstOrDefault().Key;

                    Model.PortNo = m_ComboComPort.FirstOrDefault().Key;
                    if (Model.PortNo == 0)
                        Model.PortNo = -1;

                }
                audioParam.Fmt = SoundFormat.SoundPcm8Khz16Bit;
            }

            if (Model != null)
            {
                switch (Model.Type)
                {
                    case DevType.XPORT:
                    case DevType.XPORTTCP:
                    case DevType.XPORTUDP:
                    case DevType.SZS:
                    {
                        await ConfigReadPort();
                    }
                    break;
                }

                if (Model.Type == DevType.XPORTTCP || Model.Type == DevType.SZS)
                {
                    if (UDPParams.RemotePort == 0)
                    {
                        UDPParams.RemotePort = 10001;
                    }

                    m_wPortUDP = UDPParams.RemotePort;

                    m_IPAddressUZS = ParseConnect(Model.PortNo);
                }
                if (Model.Type == DevType.XPORTUDP)
                {
                    m_wPortUDP = Model.PortNo;
                }

                if (DeviceObj_ID.ObjID > 0 && SubsystemID != SubsystemType.SUBSYST_P16x)
                    CheckSCSChlSettings();


                UDPParams.RemoteIpAddress = IpAddressUtilities.ParseEndPoint(UDPParams.RemoteIpAddress) ?? "0.0.0.0";
                UDPParams.LocalIpAddress = IpAddressUtilities.ParseEndPoint(UDPParams.LocalIpAddress) ?? "0.0.0.0";

                RefreshForm();
            }
        }

        private long GetSerialNumber()
        {
            long response = 0;
            if (SubsystemID == SubsystemType.SUBSYST_SZS)
            {
                switch (Model?.Type)
                {
                    case DevType.SZS:
                    {
                        response = Model.SerNo;
                    }
                    break;
                    case DevType.RAD_MOD:
                    {
                        response = 0x00020000 | (0xFFFF & (Model.PortNo));
                    }
                    break;
                    case DevType.XPORTUDP:
                    {
                        response = (0x00010000 | (0xFFFF & (m_wPortUDP))) ?? 0;
                    }
                    break;
                    case DevType.XPORTTCP:
                    {
                        response = IpAddressUtilities.StringToUint(m_IPAddressUZS);
                    }
                    break;

                    case DevType.XPORT:
                    {
                        response = 0x00010000 | (0xFFFF & (Model.PortNo));
                    }
                    break;
                }
            }
            else
            {
                response = Model?.SerNo ?? 0;
            }


            return response;
        }
        //private string MaskIp(string? value)
        //{
        //    if (value == null)
        //        return "";
        //    string newValue = "";
        //    var pattern = @"\d{1,3}";

        //    value = Regex.Replace(value, @"[^\d|\.]", "", RegexOptions.Multiline);
        //    var m = Regex.Matches(value, pattern, RegexOptions.Multiline);

        //    if (m.Any())
        //    {
        //        newValue = string.Join(".", m.Select(x => x.Value).Take(4));
        //    }
        //    else
        //        newValue = "0.0.0.0";

        //    return newValue;
        //}


        private void FillDictionary()
        {
            m_ComboDevType.Add(DevType.XPORT, "HSCOM Xport");
            m_ComboDevType.Add(DevType.XPORTUDP, "HSCOM UDP Xport");
            m_ComboDevType.Add(DevType.XPORTTCP, "HSCOM TCP Xport");
            m_ComboDevType.Add(DevType.RAD_MOD, $"{UUZSRep["IDS_STRING_RADIOMODEM"]} 2Р23АЦ");

            m_ComboVer.Add((int)UzsType.Uzs1, "УЗС1");
            m_ComboVer.Add((int)UzsType.Uzs23, "УЗС2/3");
            m_ComboVer.Add((int)UzsType.Uzs23WithSndCoding, "УЗС2/3 C");

            m_SoundFormat.Add(SoundFormat.SoundPcm8Khz16Bit, "PCM 8кГц 16 бит");
            m_SoundFormat.Add(SoundFormat.SoundGsm610, "GSM 610");
            m_SoundFormat.Add(SoundFormat.SoundAlaw16Khz16Bit, "PCM 16кГц A-Law");
            m_SoundFormat.Add(SoundFormat.SoundMp38Khz8Kbits, "MP3  8кГц  8 кбит/сек");
            m_SoundFormat.Add(SoundFormat.SoundMp38Khz16Kbits, "MP3  8кГц 16 кбит/сек");
            m_SoundFormat.Add(SoundFormat.SoundMp316Khz8Kbits, "MP3 16кГц  8 кбит/сек");
            m_SoundFormat.Add(SoundFormat.SoundMp316Khz16Kbits, "MP3 16кГц 16 кбит/сек");
            m_SoundFormat.Add(SoundFormat.SoundMp316Khz24Kbits, "MP3 16кГц 24 кбит/сек");
            m_SoundFormat.Add(SoundFormat.SoundMp316Khz32Kbits, "MP3 16кГц 32 кбит/сек");
            m_SoundFormat.Add(SoundFormat.SoundAmr475Kbits, "AMR 4.75 кбит/сек");
        }

        /// <summary>
        /// Обновляме форму
        /// </summary>
        private void RefreshForm()
        {
            if (Model == null)
                Model = new();
            if (DeviceObj_ID?.SubsystemID == SubsystemType.SUBSYST_SZS)
            {
                switch (Model.Type)
                {
                    case DevType.SZS:
                    {
                        if (Model.PortNo > 0xFFFF)
                        {
                            m_IPAddressUZS = ParseConnect(Model.PortNo);
                        }
                    }
                    break;
                    case DevType.XPORTTCP:
                    {
                        if (m_wPortUDP == 0 && DeviceObj_ID.ObjID == 0)
                        {
                            m_wPortUDP = 10001;
                        }
                        if (Model.PortNo <= 0)
                        {
                            m_IPAddressUZS = ParseConnect(0xC0A80101);
                        }
                    }
                    break;
                }

            }
            else if (DeviceObj_ID?.SubsystemID == SubsystemType.SUBSYST_P16x)
            {

            }
        }

        private void OnChange(ChangeEventArgs e)
        {

            if (e.Value == null || Model == null)
                return;

            int.TryParse(e.Value.ToString(), out int code);

            Model.Type = code;

            if (DeviceObj_ID?.ObjID == 0)
            {
                switch (code)
                {
                    case 1097/*IDC_COMBO_COM_PORT*/:
                    case 1096/*IDC_COMBO_DEV_TYPE*/:
                    case 1015/*IDC_PORT_UDP*/:
                    case 1125/*IDC_IPADDRESS_UZS*/:

                        break;
                }
            }
            RefreshForm();
        }

        /// <summary>
        /// Получаем инфо о устройстве
        /// </summary>
        /// <returns></returns>
        private async Task GetInfo()
        {
            if (DeviceObj_ID != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetControllingDeviceInfo", DeviceObj_ID);
                if (result.IsSuccessStatusCode)
                {
                    var b = await result.Content.ReadFromJsonAsync<List<ControllingDevice>>() ?? new();

                    Model = b.FirstOrDefault() ?? new();

                    if (Model.Type == DevType.P16x)
                    {
                        await GetControlUnitInfo();
                    }
                }
                else
                {
                    Model = new();
                    MessageView?.AddError("", UUZSRep["IDS_ERRGETDEVINFO"]);
                }
            }
        }

        /// <summary>
        /// получаем имя пу (для P16x)
        /// </summary>
        /// <returns></returns>
        private async Task GetControlUnitInfo()
        {
            if (Model != null)
            {
                OBJ_ID? response = null;
                var result = await Http.PostAsJsonAsync("api/v1/GetControlUnitKey", new OBJ_ID() { ObjID = (int)Model.SerNo, StaffID = StaffId, SubsystemID = SubsystemType.SUBSYST_P16x });
                if (result.IsSuccessStatusCode)
                {
                    response = await result.Content.ReadFromJsonAsync<OBJ_ID>();
                }
                else
                {
                    MessageView?.AddError("", GSOFormRep["IDS_E_UNIT_NAME"]);
                }

                if (response != null)
                {
                    m_ControlUnit = new();
                    m_ControlUnit.OBJID = response;
                    result = await Http.PostAsJsonAsync("api/v1/GetControlUnitInfo", response);
                    if (result.IsSuccessStatusCode)
                    {
                        m_ControlUnit.ControlUnitInfo = await result.Content.ReadFromJsonAsync<ControlUnitInfo>();
                        if (Model != null && m_ControlUnit.ControlUnitInfo != null)
                        {
                            Model.Name = m_ControlUnit.ControlUnitInfo.ControlUnitName;
                            StateHasChanged();
                        }
                    }
                    else
                    {
                        MessageView?.AddError("", GSOFormRep["IDS_E_UNIT_NAME"]);
                    }
                }
                else
                    MessageView?.AddError("", GSOFormRep["IDS_E_UNIT_NAME"]);
            }
        }


        private async Task SetControlUnit()
        {
            if (Model != null && m_ControlUnit?.ControlUnitInfo != null)
            {
                List<CControlUnit> request = new()
                {
                    m_ControlUnit
                };

                var result = await Http.PostAsJsonAsync("api/v1/SetControlUnit", request);
                if (result.IsSuccessStatusCode)
                {
                    var b = await result.Content.ReadFromJsonAsync<BoolValue>();
                    if (b?.Value != true)
                    {
                        MessageView?.AddError("", GSOFormRep["IDS_STRING_ERR_EDIT_CU"]);
                    }
                }
                else
                {
                    MessageView?.AddError("", GSOFormRep["IDS_STRING_ERR_EDIT_CU"]);
                }
            }
        }


        /// <summary>
        /// Проверяем настройки
        /// </summary>
        /// <returns></returns>
        private void CheckSCSChlSettings()
        {
            if (Model != null)
            {
                if (Model.PortNo != audioParam.PortNo)
                {
                    audioParam.PortNo = (uint)Model.PortNo;

                    if (Model.Type == DevType.XPORTUDP)
                    {
                        audioParam.PortNo = (uint?)m_wPortUDP ?? 0;
                    }
                    else if (Model.Type == DevType.XPORTTCP || Model.Type == DevType.SZS)
                    {
                        UDPParams.RemotePort = (int?)m_wPortUDP ?? 0;
                        UDPParams.RemoteIpAddress = ParseConnect(Model.PortNo);
                    }
                    IsErrorSetting = true;
                }
            }

        }

        /// <summary>
        /// Согласование настроек
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ConfigAddOrReplacePort()
        {
            bool response = false;
            if (Model != null)
            {
                if (audioParam.PortNo == 0 && Model.PortNo != 0)
                    audioParam.PortNo = (uint)Model.PortNo;
                PortUniversalRecord request = new();
                switch (Model.Type)
                {
                    case DevType.XPORTTCP:
                    {
                        request.UzsOnTcp = new()
                        {
                            ConnectParams = new XportParams() { LocalEndpoint = UDPParams.GetLocalIP(), RemoteEndpoint = UDPParams.GetRemoteIP(), ResetPolicy = XportResetPolicy.Dynamic },
                            AudioPortParams = audioParam,
                            UzsType = (UzsType)Model.Ver
                        };
                    };
                    break;
                    case DevType.XPORTUDP:
                    {
                        UDPParams.LocalPort = (int?)m_wPortUDP ?? 0;
                        request.UzsOnUdp = new()
                        {
                            ConnectParams = new XportParams() { LocalEndpoint = $"{UDPParams.LocalIpAddress}:{UDPParams.LocalPort}", RemoteEndpoint = UDPParams.GetRemoteIP(), ResetPolicy = XportResetPolicy.Never },
                            AudioPortParams = audioParam,
                            UzsType = (UzsType)Model.Ver
                        };
                    };
                    break;
                    case DevType.XPORT:
                    {
                        request.UzsOnRs232 = new()
                        {
                            ConnectParams = rs232Uzs,
                            AudioPortParams = audioParam,
                            UzsType = (UzsType)Model.Ver
                        };
                    };
                    break;
                    case DevType.SZS:
                    {
                        request.ClassicUuzsOnTcp = new()
                        {
                            ConnectParams = new XportParams() { RemoteEndpoint = UDPParams.GetRemoteIP(), ResetPolicy = XportResetPolicy.Dynamic },
                            AudioPortParams = audioParam,
                        };
                    };
                    break;
                }

                var result = await Http.PostAsJsonAsync("api/v1/ConfigAddOrReplacePort", JsonFormatter.Default.Format(request), ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var b = await result.Content.ReadFromJsonAsync<BoolValue>();
                    response = b?.Value ?? false;

                }

                if (!response)
                    MessageView?.AddError("", GSOFormRep["IDS_E_SAVE_SETTING_DEVICE"]);
            }

            IsErrorSetting = false;

            return response;
        }

        private async Task ConfigReadPort()
        {
            if (DeviceObj_ID?.ObjID > 0 && Model != null)
            {
                UInt32Value request = new() { Value = (uint)Model.PortNo };
                var result = await Http.PostAsJsonAsync("api/v1/ConfigReadPort", request, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var json = await result.Content.ReadAsStringAsync();

                    var r = PortUniversalRecord.Parser.ParseJson(json);

                    if (r != null)
                    {
                        switch (r.BodyCase)
                        {
                            case PortUniversalRecord.BodyOneofCase.UzsOnTcp:
                            {
                                UDPParams = new(r.UzsOnTcp.ConnectParams.RemoteEndpoint, r.UzsOnTcp.ConnectParams.LocalEndpoint);
                                audioParam = r.UzsOnTcp.AudioPortParams;
                                //uzsType = r.UzsOnTcp.UzsType;
                            };
                            break;
                            case PortUniversalRecord.BodyOneofCase.UzsOnUdp:
                            {
                                UDPParams = new(r.UzsOnUdp.ConnectParams.RemoteEndpoint, r.UzsOnUdp.ConnectParams.LocalEndpoint);
                                audioParam = r.UzsOnUdp.AudioPortParams;
                                //uzsType = r.UzsOnUdp.UzsType;
                            };
                            break;
                            case PortUniversalRecord.BodyOneofCase.UzsOnRs232:
                            {
                                rs232Uzs = r.UzsOnRs232.ConnectParams;
                                audioParam = r.UzsOnRs232.AudioPortParams;
                                //uzsType = r.UzsOnRs232.UzsType;
                            };
                            break;
                            case PortUniversalRecord.BodyOneofCase.ClassicUuzsOnRs232:
                            {
                                rs232Uuzs = r.ClassicUuzsOnRs232.ConnectParams;
                                audioParam = r.ClassicUuzsOnRs232.AudioPortParams;
                            };
                            break;
                            case PortUniversalRecord.BodyOneofCase.ClassicUuzsOnTcp:
                            {
                                UDPParams = new(r.ClassicUuzsOnTcp.ConnectParams.RemoteEndpoint, r.ClassicUuzsOnTcp.ConnectParams.LocalEndpoint);
                                audioParam = r.ClassicUuzsOnTcp.AudioPortParams;
                            };
                            break;
                            case PortUniversalRecord.BodyOneofCase.None:
                            {
                                audioParam = new AudioPortParams() { PortNo = 0, Fmt = SoundFormat.SoundPcm8Khz16Bit };
                                //uzsType = (UzsType)Model.Ver;
                                //UDPParams = new UDPParams("0.0.0.0:10001", "");
                            };
                            break;
                        }
                    }
                }
                else
                {
                    MessageView?.AddError("", DeviceRep["ErrorGetInfo"]);
                }
            }
            if (string.IsNullOrEmpty(UDPParams.RemoteIpAddress))
                UDPParams.RemoteIpAddress = "0.0.0.0";
            if (string.IsNullOrEmpty(UDPParams.LocalIpAddress))
                UDPParams.LocalIpAddress = "0.0.0.0";

        }

        private async Task OnAdd()
        {
            if (Model != null && DeviceObj_ID != null)
            {

                if (DeviceObj_ID.SubsystemID == SubsystemType.SUBSYST_P16x)
                {
                    if (m_ControlUnit?.ControlUnitInfo != null && Model.Name != m_ControlUnit.ControlUnitInfo.ControlUnitName)
                    {
                        m_ControlUnit.ControlUnitInfo.ControlUnitName = Model.Name;
                        await SetControlUnit();
                    }
                }

                if (DeviceObj_ID.SubsystemID == SubsystemType.SUBSYST_SZS)
                {
                    if (Model.Type == 0)
                    {
                        MessageView?.AddError("", AsoRep["IDS_STRING_SEL_DEV_CONN_TYPE"]);
                        return;
                    }

                    if (string.IsNullOrEmpty(Model.Name))
                    {
                        MessageView?.AddError("", DeviceRep["ErrorNull"] + " " + GsoRep["IDS_STRING_NAME"]);
                        return;
                    }
                    Model.SerNo = GetSerialNumber();
                    if (Model.SerNo == 0)
                    {
                        MessageView?.AddError("", GSOFormRep["IDS_E_SERIAL_NUMBER"]);
                        return;
                    }

                    if ((Model.Type == DevType.RAD_MOD) || (Model.Type == DevType.XPORT) || (Model.Type == DevType.XPORTUDP) || (Model.Type == DevType.XPORTTCP))
                    {

                        if ((Model.Type == DevType.XPORTUDP && (m_wPortUDP < 0x100 || m_wPortUDP > 0xFFFF)) || ((Model.Type == DevType.RAD_MOD || Model.Type == DevType.XPORT) && Model.PortNo <= 0))
                        {
                            MessageView?.AddError("", GSOFormRep["IDS_E_PORT_NUMBER"]);
                            return;
                        }

                        if (Model.Type == DevType.XPORTTCP)
                        {
                            Model.PortNo = IpAddressUtilities.StringToUint(m_IPAddressUZS);
                            if (Model.PortNo < 0x01000000)
                            {
                                MessageView?.AddError("", GSOFormRep["IDS_E_IP"]);
                                return;
                            }
                        }

                        Block block = new();
                        block.SubsystemID = SubsystemType.SUBSYST_SZS;
                        block.Status = 1;
                        block.Port = Model.PortNo;
                        if (Model.Type == DevType.XPORTUDP)
                            block.Port = m_wPortUDP ?? 0;
                        else if (Model.Type == DevType.XPORTTCP)
                        {
                            block.Port = Model.PortNo;
                        }

                        block.Name = $"{(Model.Type == DevType.XPORTUDP ? "NET" : "COM")}{block.Port} - {m_ComboDevType.FirstOrDefault(x => x.Key == Model.Type).Value}";



                        Model.DeviceID = (await AddBlock(block)).ID;
                    }

                    if (Model.Type == DevType.RAD_MOD)
                    {
                        Model.Ver = 0;
                    }

                    if (Model.Type == DevType.XPORTUDP)
                    {
                        Model.PortNo = m_wPortUDP ?? 0;
                    }
                    List<ControllingDevice> request = new()
                    {
                        Model
                    };
                    var result = await Http.PostAsJsonAsync("api/v1/SetControllingDeviceInfoUUZS", request, ComponentDetached);
                    if (!result.IsSuccessStatusCode)
                    {
                        MessageView?.AddError("", UUZSRep["IDS_STRING_ERR_SAVE_DEVICE"]);
                        return;
                    }
                    else
                    {
                        if (Model.Type != DevType.SZS)
                        {
                            audioParam.PortNo = (uint)Model.PortNo;

                            if (Model.Type == DevType.XPORTUDP)
                            {
                                audioParam.PortNo = (uint?)m_wPortUDP ?? 0;
                            }
                            else if (Model.Type == DevType.XPORTTCP)
                            {
                                UDPParams.RemotePort = (int?)m_wPortUDP ?? 0;
                                UDPParams.RemoteIpAddress = ParseConnect(Model.PortNo);
                            }
                            await ConfigAddOrReplacePort();
                        }
                    }
                }


                if (m_recBindingLine != null && !m_recBindingLine.Equals(Old_RecBindingLine) && Model.DeviceID > 0)
                {
                    if (Old_RecBindingLine?.LineID > 0)
                    {
                        // отсоединяем линию от устройства
                        var result = await Http.PostAsJsonAsync("api/v1/DeleteLineBinding", new IntID() { ID = Old_RecBindingLine.LineID }, ComponentDetached);
                        if (!result.IsSuccessStatusCode)
                        {
                            MessageView?.AddError("", GSOFormRep["IDS_E_DELETE_LINE_BINDING"]);
                            return;
                        }
                    }

                    if (m_recBindingLine.LineID > 0)
                    {
                        // присоединяем линию к данному устройству
                        LineBinding request = new() { LineID = m_recBindingLine.LineID, ChannelID = Model.ChannelID, DeviceID = Model.DeviceID };

                        var result = await Http.PostAsJsonAsync("api/v1/SetLineBinding", request, ComponentDetached);
                        if (result.IsSuccessStatusCode)
                        {
                            var s = await result.Content.ReadFromJsonAsync<SIntResponse>();

                            if (s?.SInt != 0)
                            {
                                MessageView?.AddError("", GSOFormRep["IDS_E_LINE_BINDING"]);
                            }
                        }
                        else
                        {
                            MessageView?.AddError("", GSOFormRep["IDS_E_LINE_BINDING"]);
                        }
                    }
                }
            }
            await CallEvent(true);
        }

        private string ParseConnect(long connect)
        {
            if (connect < 0)
                return "";
            string response = connect.ToString();
            if (Model != null && (Model.Type == DevType.XPORTTCP || Model.Type == DevType.XPORTUDP || Model.Type == DevType.SZS))
            {
                if (connect > 0xFFFF)
                {
                    byte[] bytes = BitConverter.GetBytes((uint)connect);

                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(bytes);
                    }
                    response = $"{(new IPAddress(bytes).ToString())}";
                }
            }

            return response;
        }

        private async Task<IntID> AddBlock(Block request)
        {
            IntID response = new();

            var result = await Http.PostAsJsonAsync("api/v1/AddBlock", request, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                response = await result.Content.ReadFromJsonAsync<IntID>() ?? new();
            }
            else
                MessageView?.AddError("", UUZSRep["IDS_STRING_ERR_ADD_NEW_BLOCK"]);
            return response;
        }

        /// <summary>
        /// Заполнить список портов COM портами
        /// </summary>
        /// <returns></returns>
        private async Task FillComPortList()
        {
            m_ComboComPort = new();
            var result = await Http.PostAsync("api/v1/EnumPortsEx", null, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var r = await result.Content.ReadFromJsonAsync<List<ComPort>>() ?? new();
                foreach (var p in r.Where(x => x.LinkName.Contains("ttyUSB")))
                {
                    if (!m_ComboComPort.ContainsKey((p.PortNum + 1)))
                        m_ComboComPort.Add((p.PortNum + 1), p.PortName);
                }
            }
            else
                MessageView?.AddError("", UUZSRep["IDS_STRING_CANNOT_SUBSYSTEM_CONNECT"]);
        }

        /// <summary>
        /// Получаем привязку линии
        /// </summary>
        /// <returns></returns>
        private async Task GetBindingLine()
        {
            await GetFreeLineList();
            if (DeviceObj_ID != null)
            {
                m_ComboComPort = new();
                var result = await Http.PostAsJsonAsync("api/v1/GetBindingLine", DeviceObj_ID);
                if (result.IsSuccessStatusCode)
                {
                    m_recBindingLine = await result.Content.ReadFromJsonAsync<BindingLine>() ?? new();
                    Old_RecBindingLine = new BindingLine(m_recBindingLine);

                    if (m_recBindingLine.LineID > 0)
                    {
                        if (FreeLine == null)
                            FreeLine = new();
                        FreeLine.Add(new BindingLine(m_recBindingLine));
                    }
                }
                else
                    MessageView?.AddError("", GSOFormRep["IDS_E_GET_LINE_TO_CHANNEL"]);
            }

        }

        /// <summary>
        /// Получаем свободные линии
        /// </summary>
        /// <returns></returns>
        private async Task GetFreeLineList()
        {
            var result = await Http.PostAsync("api/v1/GetFreeLineList", null);
            if (result.IsSuccessStatusCode)
            {
                FreeLine = await result.Content.ReadFromJsonAsync<List<BindingLine>>() ?? new();
            }
            else
                MessageView?.AddError("", GSOFormRep["IDS_E_GET_FREE_LINE"]);
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
