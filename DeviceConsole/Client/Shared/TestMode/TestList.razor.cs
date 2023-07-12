using System.ComponentModel;
using System.Net.Http.Json;
using BlazorLibrary.GlobalEnums;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using SMDataServiceProto.V1;
using UUZSDataProto.V1;
using Microsoft.AspNetCore.Components;
using SCSChLService.Protocol.Grpc.Proto.V1;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Utilities;

using static BlazorLibrary.Shared.Main;
using ChannelInfo = SCSChLService.Protocol.Grpc.Proto.V1.ChannelInfo;
using SharedLibrary.Interfaces;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.Models;
using BlazorLibrary.Shared;

namespace DeviceConsole.Client.Shared.TestMode
{
    partial class TestList
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_SZS;

        //private List<PortInfoTag>? m_vecPorts = null;

        private List<ControllingDevice> vecDbDevs = new();

        List<ChannelInfo>? GetRealList => realDev?.GetRealList;

        private PortInfoTag? SelectVecPorts = null;

        private PortsConfig ConfigAllPorts = new();

        List<GetSheduleListItem> SheduleList = new();

        private readonly Dictionary<SoundFormat, string> m_SoundFormat = new();

        private readonly List<Block> vecBlock = new();

        private bool m_NewRealDevices = false;
        private bool m_OldDBDevices = false;
        private bool m_DirtyRealDevices = false;

        private bool IsOld = false;

        private bool IsNew = false;

        private bool IsChange = false;

        private bool IsAdd = false;

        private bool IsDelete = false;

        private string TitleName = "";

        private int StaffId = 0;

        public NewPortModel NewPort = new();
        public class NewPortModel
        {
            public uint PortNo { get; set; } = 0;
            public string IpAdress { get; set; } = "0.0.0.0";
            //public int DevType { get; set; } = 0x0010;
            public SoundFormat fmt { get; set; } = SoundFormat.SoundPcm8Khz16Bit;
            public int Shedule { get; set; }
        }

        TableVirtualize<PortInfoTag>? table;

        RealDeviceList? realDev;

        protected override async Task OnInitializedAsync()
        {
            TitleName = UUZSDataRep["IDS_STRING_TESTING"];

            if (SubsystemID == SubsystemType.SUBSYST_SZS)
            {
                TitleName = UUZSRep["IDS_STRING_CONFIG_UUZS"];
            }
            else if (SubsystemID == SubsystemType.SUBSYST_P16x)
            {
                TitleName = UUZSRep["IDS_STRING_CONFIG_P16X"];
            }

            ThList = new Dictionary<int, string>
            {
                { 0, UUZSRep["IDS_STRING_CONNECT"] },
                { -2, UUZSRep["IDS_STRING_CONNECT_TYPE"] },
                { 1, UUZSRep["IDS_STRING_CONNECTED"] },
                { -4, UUZSRep["IDS_STRING_SOUND"] },
                { -5, UUZSRep["IDS_STRING_CONTROL"] }
            };

            StaffId = await _User.GetLocalStaff();

            FillDictionary();
            await GetSheduleList();
            await ConfigReadAllPorts();
            await GetDbDevices();
            _ = SetDevStatus();

            NewPort.Shedule = SheduleList?.FirstOrDefault(x => x.Duration.ToTimeSpan().Minutes == 15)?.SheduleID ?? 0;


            HintItems.Add(new HintItem(nameof(FiltrModel.Connect), UUZSRep["IDS_STRING_CONNECT"], TypeHint.Input));
            HintItems.Add(new HintItem(nameof(FiltrModel.Channel), UUZSRep["IDS_STRING_CHANNELS_COUNT"], TypeHint.Number));
            //HintItems.Add(new HintItem(nameof(FiltrModel.Channel), UUZSRep["IDS_STRING_CONNECTED"], TypeHint.Number));

            await OnInitFiltr(RefreshTable, SubsystemID == SubsystemType.SUBSYST_SZS ? FiltrName.FiltrTestSzsDevice : FiltrName.FiltrTestP16Device);

        }
        ItemsProvider<PortInfoTag> GetProvider => new ItemsProvider<PortInfoTag>(ThList, LoadChildList, request);

        private async ValueTask<IEnumerable<PortInfoTag>> LoadChildList(GetItemRequest req)
        {
            List<PortInfoTag> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetPortsInfo", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<PortInfoTag>>() ?? new();
            }
            else
            {
                MessageView?.AddError("", UUZSRep["IDS_STRING_CANNOT_SUBSYSTEM_CONNECT"]);
            }
            return newData;
        }


        private async Task RefreshTable()
        {
            SelectVecPorts = null;
            if (table != null)
                await table.ResetData();
        }

        private async Task UpdateList()
        {
            if (realDev != null)
                await realDev.RefreshTable();
            await ConfigReadAllPorts();
            await CallRefreshData();
            _ = SetDevStatus();
        }

        private void FillDictionary()
        {
            //m_SoundFormat.Add(SoundFormat.SoundUnknown, UUZSRep["IDS_STRING_UNKNOWN"]);
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

        private SoundFormat GetSoundFormat(uint PortNo)
        {
            SoundFormat fmt = SoundFormat.SoundUnknown;

            var dwDevType = GetDevType(PortNo);

            switch (dwDevType)
            {
                case DevType.XPORT:
                case DevType.UXPORT:
                case DevType.XPORTTCP:
                case DevType.XPORTUDP:
                {
                    if (ConfigAllPorts.Rs232Uuzses.Any(x => x.Key == PortNo))
                    {
                        fmt = ConfigAllPorts.Rs232Uuzses.First(x => x.Key == PortNo).Value.AudioPortParams.Fmt;
                    }
                    else if (ConfigAllPorts.Rs232Uzses.Any(x => x.Key == PortNo))
                    {
                        fmt = ConfigAllPorts.Rs232Uzses.First(x => x.Key == PortNo).Value.AudioPortParams.Fmt;
                    }
                    else if (ConfigAllPorts.TcpUuzses.Any(x => x.Key == PortNo))
                    {
                        fmt = ConfigAllPorts.TcpUuzses.First(x => x.Key == PortNo).Value.AudioPortParams.Fmt;
                    }
                    else if (ConfigAllPorts.TcpUzses.Any(x => x.Key == PortNo))
                    {
                        fmt = ConfigAllPorts.TcpUzses.First(x => x.Key == PortNo).Value.AudioPortParams.Fmt;
                    }
                    else if (ConfigAllPorts.UdpUzses.Any(x => x.Key == PortNo))
                    {
                        fmt = ConfigAllPorts.UdpUzses.First(x => x.Key == PortNo).Value.AudioPortParams.Fmt;
                    }
                }
                break;
            }

            return fmt;
        }

        private string GetTypeName(uint PortNo)
        {

            var dwDevType = GetDevType(PortNo);

            return dwDevType switch
            {
                DevType.NONE => "",
                DevType.SZS => "RS-232",
                DevType.UXPORT => "HSCOM",
                DevType.XPORT => "XPort",
                DevType.XPORTUDP => "XPortUDP",
                DevType.XPORTTCP => "XPortTCP",
                DevType.RAD_MOD => UUZSRep["IDS_STRING_RADIOMODEM"],
                DevType.P16x => SMDataRep["SUBSYST_P16x"],
                _ => UUZSRep["IDS_STRING_PORT_BUSY"]
            };
        }

        private async Task SaveSoundFormat(ChangeEventArgs e)
        {
            if (SelectVecPorts == null)
                return;

            var s = e.Value?.ToString();

            if (s == null)
                return;

            var fmt = System.Enum.Parse<SoundFormat>(s);


            PortUniversalRecord request = new();

            bool isFind = false;

            if (ConfigAllPorts.Rs232Uuzses.Any(x => x.Key == SelectVecPorts.PortNo))
            {
                request.ClassicUuzsOnRs232 = ConfigAllPorts.Rs232Uuzses.First(x => x.Key == SelectVecPorts.PortNo).Value;
                request.ClassicUuzsOnRs232.AudioPortParams.Fmt = fmt;
                isFind = true;
            }
            else if (ConfigAllPorts.Rs232Uzses.Any(x => x.Key == SelectVecPorts.PortNo))
            {
                request.UzsOnRs232 = ConfigAllPorts.Rs232Uzses.First(x => x.Key == SelectVecPorts.PortNo).Value;
                request.UzsOnRs232.AudioPortParams.Fmt = fmt;
                isFind = true;
            }
            else if (ConfigAllPorts.TcpUuzses.Any(x => x.Key == SelectVecPorts.PortNo))
            {
                request.ClassicUuzsOnTcp = ConfigAllPorts.TcpUuzses.First(x => x.Key == SelectVecPorts.PortNo).Value;
                request.ClassicUuzsOnTcp.AudioPortParams.Fmt = fmt;
                isFind = true;
            }
            else if (ConfigAllPorts.TcpUzses.Any(x => x.Key == SelectVecPorts.PortNo))
            {
                request.UzsOnTcp = ConfigAllPorts.TcpUzses.First(x => x.Key == SelectVecPorts.PortNo).Value;
                request.UzsOnTcp.AudioPortParams.Fmt = fmt;
                isFind = true;
            }
            else if (ConfigAllPorts.UdpUzses.Any(x => x.Key == SelectVecPorts.PortNo))
            {
                request.UzsOnUdp = ConfigAllPorts.UdpUzses.First(x => x.Key == SelectVecPorts.PortNo).Value;
                request.UzsOnUdp.AudioPortParams.Fmt = fmt;
                isFind = true;
            }

            if (isFind)
                await ConfigAddOrReplacePort(request);

        }

        private uint GetDevType(uint PortNo)
        {
            uint devType = 0;

            if (ConfigAllPorts.Rs232Uuzses.Any(x => x.Key == PortNo))
            {
                devType = DevType.SZS;
            }
            else if (ConfigAllPorts.Rs232Uzses.Any(x => x.Key == PortNo))
            {
                devType = DevType.XPORT;
            }
            else if (ConfigAllPorts.TcpUuzses.Any(x => x.Key == PortNo))
            {
                devType = DevType.UXPORT;
            }
            else if (ConfigAllPorts.TcpUzses.Any(x => x.Key == PortNo))
            {
                devType = DevType.XPORTTCP;
            }
            else if (ConfigAllPorts.UdpUzses.Any(x => x.Key == PortNo))
            {
                devType = DevType.XPORTUDP;
            }
            return devType;
        }

        private async Task SaveShedule(int SheduleId)
        {
            if (SelectVecPorts == null)
                return;
            await SetShedule(SheduleId, SelectVecPorts.PortNo);
        }

        private async Task SetShedule(int SheduleId, uint PortNo)
        {
            SetShedule hwobj = new();
            hwobj.OBJKey = new() { ObjID = new() };
            hwobj.OBJKey.ObjID.ObjID = (int)PortNo;
            hwobj.OBJKey.ObjID.StaffID = StaffId;
            hwobj.OBJKey.ObjID.SubsystemID = SubsystemType.SUBSYST_SZS;
            hwobj.OBJKey.ObjType = (int)HMT.Uuzs;
            hwobj.Shedule = 1;
            hwobj.Status = SheduleId > 0 ? 1 : 0;
            await Http.PostAsJsonAsync("api/v1/SetDeviceShedule", hwobj);
        }

        private async Task GetSheduleList()
        {
            var result = await Http.PostAsync("api/v1/GetSheduleList", null);
            if (result.IsSuccessStatusCode)
            {
                SheduleList = await result.Content.ReadFromJsonAsync<List<GetSheduleListItem>>() ?? new();
            }
        }

        private async Task AddPort()
        {
            NewPort.IpAdress = IpAddressUtilities.ParseEndPoint(NewPort.IpAdress) ?? string.Empty;
            if (!string.IsNullOrEmpty(NewPort.IpAdress) && NewPort.PortNo > 0)
            {
                PortUniversalRecord request = new();
                request.ClassicUuzsOnTcp = new()
                {
                    AudioPortParams = new() { PortNo = IpAddressUtilities.StringToUint(NewPort.IpAdress), Fmt = NewPort.fmt },
                    ConnectParams = new() { RemoteEndpoint = NewPort.IpAdress + ":" + NewPort.PortNo }
                };

                await ConfigAddOrReplacePort(request);
                await SetShedule(NewPort.Shedule, IpAddressUtilities.StringToUint(NewPort.IpAdress));
                await UpdateList();
            }
            IsAdd = false;
        }



        private async Task ConfigDeletePort()
        {
            if (SelectVecPorts != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/ConfigDeletePort", new UInt32Value() { Value = SelectVecPorts.PortNo }, ComponentDetached);
                if (!result.IsSuccessStatusCode)
                    MessageView?.AddError("", GsoRep["IDS_E_DELETE"]);
                else
                {
                    if (vecDbDevs.Any(x => x.PortNo == SelectVecPorts.PortNo))
                    {
                        vecDbDevs.First(x => x.PortNo == SelectVecPorts.PortNo).Status = (int)LineChannelState.CHAN_OLD;
                        m_OldDBDevices = true;
                    }
                    SelectVecPorts = null;
                    await ConfigReadAllPorts();
                }
            }
            IsDelete = false;

        }

        /// <summary>
        /// прочитать и отдать настройки всех портов конфигурации
        /// </summary>
        /// <returns></returns>
        private async Task ConfigReadAllPorts()
        {
            var result = await Http.PostAsync("api/v1/ConfigReadAllPorts", null, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var json = await result.Content.ReadAsStringAsync();
                ConfigAllPorts = PortsConfig.Parser.ParseJson(json);
            }
            else
                MessageView?.AddError("", UUZSRep["IDS_STRING_CANNOT_SUBSYSTEM_CONNECT"]);
        }

        private async Task ConfigAddOrReplacePort(PortUniversalRecord request)
        {
            var result = await Http.PostAsJsonAsync("api/v1/ConfigAddOrReplacePort", JsonFormatter.Default.Format(request), ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var b = await result.Content.ReadFromJsonAsync<BoolValue>();
                if (b?.Value != true)
                    MessageView?.AddError("", GSOFormRep["IDS_E_SAVE_SETTING_DEVICE"]);
            }
            else
            {
                MessageView?.AddError("", GSOFormRep["IDS_E_SAVE_SETTING_DEVICE"]);
            }
        }

        /// <summary>
        /// Получить описание устройств из БД
        /// </summary>
        /// <returns></returns>
        private async Task GetDbDevices()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetControllingDeviceInfo", new OBJ_ID(), ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                vecDbDevs = await result.Content.ReadFromJsonAsync<List<ControllingDevice>>() ?? new();
            }
            else
                MessageView?.AddError("", UUZSRep["IDS_ERRGETDEVINFO"]);
        }

        /// <summary>
        /// Установка статуса устройств
        /// </summary>
        private async Task SetDevStatus()
        {
            m_NewRealDevices = false;
            m_OldDBDevices = false;
            m_DirtyRealDevices = false;

            var t = Task.Delay(1000);
            //если идет загрузка, ждем секунду
            while (GetRealList == null && !t.IsCompletedSuccessfully)
            {
                await Task.Delay(100);
            }

            if (GetRealList == null)
                return;

            vecDbDevs.ForEach(x =>
            {
                switch (x.Type)
                {
                    case DevType.SZS:
                    case DevType.P16x:
                        x.Status = (int)LineChannelState.CHAN_OLD;
                        break;
                }
            });

            foreach (var realDev in GetRealList)
            {
                var dbDev = vecDbDevs.FirstOrDefault(x => x.SerNo == realDev.DevSerNo);

                if (dbDev != null)
                {
                    realDev.OwnStatus = ChannelOwnStatus.Free;
                    dbDev.Status = (int)LineChannelState.CHAN_OK;


                    if (realDev.PortNo != dbDev.PortNo)
                    {
                        dbDev.PortNo = (int)realDev.PortNo;
                        //TODO не соответствие статуса
                        realDev.OwnStatus = ChannelOwnStatus.OwnedByOther;
                        dbDev.Status = (int)LineChannelState.CHAN_MOVED_PORT;
                        m_DirtyRealDevices = true;
                    }
                    if (realDev.PortDevIdx != dbDev.OrderOnPort)
                    {
                        dbDev.OrderOnPort = (int)realDev.PortDevIdx;
                        realDev.OwnStatus = ChannelOwnStatus.OwnedByOther;
                        dbDev.Status = (int)LineChannelState.CHAN_MOVED_ON_PORT;
                        m_DirtyRealDevices = true;
                    }
                }
                else
                {
                    realDev.OwnStatus = ChannelOwnStatus.Free;
                    m_NewRealDevices = true;
                }
            }

            if (vecDbDevs.Any(x => x.Status == (int)LineChannelState.CHAN_OLD))
            {
                m_OldDBDevices = true;
            }

        }

        private void Leave()
        {
            if (m_OldDBDevices)
            {
                IsOld = true;
            }
            if (m_NewRealDevices)
            {
                IsNew = true;
            }
            if (m_DirtyRealDevices)
            {
                IsNew = true;
            }
        }

        private async Task UpdateDirtyRealDevices()
        {
            var l = vecDbDevs.Where(x => x.Status == (int)LineChannelState.CHAN_MOVED_ON_PORT || x.Status == (int)LineChannelState.CHAN_MOVED_PORT);

            var UpdateList = l.Select(x => new ControllingDevice(x) { Status = 1/*CONNECTED*/ }).ToList();

            if (UpdateList.Any())
                await SetControllingDeviceInfo(UpdateList);

            foreach (var device in l)
            {
                device.Status = (int)LineChannelState.CHAN_OK;
            }
            m_DirtyRealDevices = false;
            IsChange = false;
        }

        private async Task DeleteOldDBDevices()
        {
            foreach (var item in vecDbDevs.Where(x => x.Status == (int)LineChannelState.CHAN_OLD))
            {
                var result = await Http.PostAsJsonAsync("api/v1/DeleteControllingDevice", new OBJ_ID() { ObjID = item.ChannelID, SubsystemID = SubsystemID, StaffID = item.DeviceID }, ComponentDetached);
                if (!result.IsSuccessStatusCode)
                {
                    MessageView?.AddError("", UUZSRep["IDS_STRING_ERR_DEL_CONTROL_DEVICE"]);
                }
                else
                {
                    var response = await result.Content.ReadFromJsonAsync<UInt32Value>();
                    if (response?.Value > 0)
                    {
                        await ConfigDeletePort(response);
                    }
                    await GetDbDevices();
                }
            }
            m_OldDBDevices = false;
            IsOld = false;
        }

        private async Task ConfigDeletePort(UInt32Value request)
        {
            //and ConfigDeletePort
            await Http.PostAsJsonAsync("api/v1/ConfigDeletePort", request, ComponentDetached);
        }

        private async Task SaveNewRealDevices()
        {

            if (GetRealList == null)
                return;

            await GetBlockInfo();

            bool bOk = true;

            List<ControllingDevice> pDataList = new();

            foreach (var realdev in GetRealList.Where(x => x.OwnStatus == 0))
            {
                bOk = true;
                ControllingDevice pData = new();
                pData.ChannelID = 0;
                pData.DeviceID = await FindBlockOnPort(realdev.PortNo);

                if (pData.DeviceID == 0)
                    break;

                switch (realdev.DevType)
                {
                    case DevType.P16x: pData.Name = GetNameDev(realdev.DevType, realdev.DevSerNo); break;
                    case DevType.SZS: pData.Name = GetNameDev(realdev.DevType, realdev.DevSerNo); break;
                    default:
                    {
                        bOk = false;
                        MessageView?.AddError(IpAddressUtilities.UintToString(realdev.PortNo), GSOFormRep["IDS_E_DEVICE_TYPE"]);
                    };
                    break;
                }

                if (bOk)
                {
                    pData.Type = (int)realdev.DevType;
                    pData.Ver = (int)realdev.DevVer;
                    pData.SerNo = realdev.DevSerNo;
                    pData.PortNo = realdev.PortNo;
                    pData.OrderOnPort = (int)realdev.PortDevIdx;
                    pData.Status = 1/*CONNECTED*/;
                    pData.CountCh = 1;
                    pDataList.Add(pData);
                }

            }

            if (pDataList.Any() && bOk)
            {
                await SetControllingDeviceInfo(pDataList);
            }

            if (pDataList.Any() && bOk)
            {
                m_NewRealDevices = false;
            }
            IsNew = false;
        }

        private string GetNameDev(uint TypeDev, uint Serno)
        {
            return TypeDev switch
            {
                DevType.P16x => $"{UUZSRep["IDS_STRING_RECIVER_P16X"]} {Serno}",
                DevType.SZS => $"{SMDataRep["SUBSYST_SZS"]} {Serno}",
                _ => GSOFormRep["IDS_E_DEVICE_TYPE"]
            };
        }

        private async Task SetControllingDeviceInfo(List<ControllingDevice> request)
        {
            var result = await Http.PostAsJsonAsync("api/v1/SetControllingDeviceInfoUUZS", request, ComponentDetached);
            if (!result.IsSuccessStatusCode)
            {
                MessageView?.AddError("", UUZSRep["IDS_STRING_ERR_SAVE_DEVICE"]);
            }
            await GetDbDevices();
        }

        private async Task<int> FindBlockOnPort(uint PortNo)
        {

            if (GetRealList == null)
                return 0;

            IntID blockID = new();

            Block block = new();
            block.BlockID = 0;
            block.SubsystemID = SubsystemType.SUBSYST_SZS;
            block.ComputerID = 0;
            block.Status = 1/*CONNECTED*/;
            block.AsoTypeConnect = 0;
            block.AsoConnID = 0;
            block.SerNo = 0;
            block.Port = PortNo;
            block.OrderOnPort = 0;
            block.ChannelsCount = 0;
            block.Name = UUZSRep["IDS_STRING_BLOCK_ADD_AUTOMATIC"];

            bool bUuzs = false;
            bool bP16x = false;

            if (GetRealList?.Any(x => x.DevType == DevType.P16x) ?? false)
                bP16x = true;
            if (GetRealList?.Any(x => x.DevType == DevType.SZS) ?? false)
                bUuzs = true;

            if (bP16x && !bUuzs)
                block.SubsystemID = SubsystemType.SUBSYST_P16x;

            blockID = await AddBlock(block);

            if (blockID.ID > 0)
            {
                block.BlockID = blockID.ID;
                vecBlock.Add(block);
            }
            else
            {
                MessageView?.AddMessage(IpAddressUtilities.UintToString(PortNo), $"{UUZSRep["IDS_STRING_NEW_BLOCK_CANNOT_ADD"]}, {UUZSRep["IDS_STRING_PORT_ALREADY_USED"]}");
            }

            return blockID.ID;
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

        private async Task GetBlockInfo()
        {
            OBJ_ID request = new()
            {
                ObjID = 0,
                StaffID = 0,
                SubsystemID = SubsystemType.SUBSYST_SZS
            };


            var result = await Http.PostAsJsonAsync("api/v1/GetBlockInfo", request, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var b = await result.Content.ReadFromJsonAsync<List<Block>>();
                if (b != null)
                    vecBlock.AddRange(b);
                else
                {
                    MessageView?.AddError("", StartUIRep["IDS_EGETDEVICEINFO"]);
                }
            }
            else
            {
                MessageView?.AddError("", StartUIRep["IDS_EGETDEVICEINFO"]);
            }
        }

    }
}
