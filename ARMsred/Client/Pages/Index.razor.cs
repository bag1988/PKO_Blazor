using System.Net.Http.Json;
using System.Threading.Channels;
using ARMsred.Client.Shared;
using BlazorLibrary.Helpers;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Audio;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GateServiceProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using System.Reflection.Metadata;
using BlazorLibrary.ServiceColection;

namespace ARMsred.Client.Pages
{
    partial class Index : IAsyncDisposable
    {
        [CascadingParameter]
        public MainLayout? Layout { get; set; }
        P16xGroup? GroupId { get; set; }

        int SelectCommand = -1;

        int DisplayModeIndex = 0;

        List<P16xGroupCommand>? CommandList;

        List<P16xGateDevice> sitItemList { get; set; } = new();

        List<OBJ_ID>? SelectSitList { get; set; }

        List<Registration> CuHistoryListConnect { get; set; } = new();

        Registration CurrentServer { get; set; } = new();

        Registration DefaultServer { get; set; } = new();

        List<CGetRegList> ReservCuList { get; set; } = new();

        string StateConnectCU = "";

        bool IsResetConnectCu = false;

        bool IsStartNotify = false;

        bool CustomModeStart = false;

        OBJ_ID RecordMsgId = new();

        uint SessionRecords = 0;

        SourceMsgNotify typeMsgNotify = SourceMsgNotify.No;

        StopState curStopState = StopState.TEST;

        AudioPlayerStream? player = default!;

        int RepeatCount = 1;

        int StaffId = 0;

        List<UploadMessage> uploadMessages { get; } = new();

        public class UploadMessage
        {
            public UploadMessage(uint sessionRecords, string urlFile, List<OBJ_ID> listCu)
            {
                UrlFile = urlFile;
                SessionRecords = sessionRecords;
                ListCu = listCu;
            }

            public uint SessionRecords;
            public string UrlFile;
            public Channel<byte[]> ChannelRecords = System.Threading.Channels.Channel.CreateUnbounded<byte[]>();
            public CancellationTokenSource TokenSource = new();
            public Task? WriteBuffer;
            public long Uploaded;
            public List<OBJ_ID>? ListCu;
        }


        string ActiveUrlFile = string.Empty;

        //long _uploaded = 0;

        RecordAudio? recordAudio;

        TableLayoutPanelPRDDestination? table = default;

        ScenListWithGroupsControl? sitList = default;

        List<P16xGateDevice>? stateArray
        {
            get
            {
                return table?.stateArray;
            }
            set
            {
                if (table != null)
                {
                    table.stateArray = value;
                }
            }

        }

        string GetCuName
        {
            get
            {
                if (string.IsNullOrEmpty(CurrentServer.CUName) && string.IsNullOrEmpty(CurrentServer.UNC))
                    return "";
                return $" - {CurrentServer.CUName} {(string.IsNullOrEmpty(CurrentServer.UNC) ? "" : $"({CurrentServer.UNC})")}";
            }
        }

        private TimeSpan TimeRecord = TimeSpan.FromMinutes(0);

        private SndSetting SettingRec = new();
        private AudioRecordSetting setting = new();

        readonly int BufferSizeSignal = 24000;

        protected override async Task OnInitializedAsync()
        {
            SetStaffUNC(string.Empty);
            await GetCuLocalName();
            await ResetConnection();

            _ = _HubContext.SubscribeAsync(this);
        }

        private async Task GetCuLocalName()
        {
            await Http.PostAsync("api/v1/remote/GetCuLocalName", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    CurrentServer = await x.Result.Content.ReadFromJsonAsync<Registration>() ?? new();
                    DefaultServer = new(CurrentServer);
                }
            });
        }

        private async Task GetReservCUList()
        {
            await Http.PostAsync("api/v1/remote/GetItems_IRegistration", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var response = await x.Result.Content.ReadFromJsonAsync<List<CGetRegList>>() ?? new();
                    ReservCuList = response.Where(x => x.OutLong == 5 || x.OutLong == 4).ToList();
                }
            });
        }

        async Task StartConnectRemote(int StaffRemote)
        {
            IsResetConnectCu = true;
            Http.CancelPendingRequests();
            StateConnectCU = string.Format(ARMRep["ArmOdGetInfo"], StaffRemote);//АРМ оперативного дежурного. Получение информации для подключения...
            //MessageView?.AddMessage("", StateConnectCU);
            StateHasChanged();
            var regInfo = await GetStaffAccess(new IntID() { ID = StaffRemote });
            await ConnectCu(regInfo);
            IsResetConnectCu = false;
        }

        async Task BackConnect(Registration regInfo)
        {
            IsResetConnectCu = true;
            Http.CancelPendingRequests();
            StateConnectCU = string.Format(ARMRep["ArmOdConnectTo"], string.IsNullOrEmpty(regInfo.UNC) ? regInfo.CUName : regInfo.UNC);//АРМ оперативного дежурного. Подключение к {0}

            StateHasChanged();
            await ConnectCu(regInfo);
            IsResetConnectCu = false;
        }

        async Task ConnectCu(Registration regInfo)
        {
            StateHasChanged();
            //удаляем из списка пу к которому подключаемся
            if (CuHistoryListConnect.Contains(regInfo))
            {
                var indexElem = CuHistoryListConnect.IndexOf(regInfo);
                var countRemove = CuHistoryListConnect.Count - indexElem;
                CuHistoryListConnect.RemoveRange(indexElem, countRemove);
            }

            if (string.IsNullOrEmpty(regInfo.Login))
            {
                MessageView?.AddError(regInfo.UNC, ARMRep["ERROR_LOGIN"]);
                return;
            }
            StateConnectCU = string.Format(ARMRep["ArmOdConntdWaitEntSys"], regInfo.UNC);//АРМ оперативного дежурного {0}. Ожидание входа в систему...

            StateHasChanged();
            SetStaffUNC(regInfo.UNC);

            var bAuth = await SetLogin(new RequestLogin() { Password = regInfo.Passw, User = regInfo.Login });
            //await Task.Delay(1000);
            if (bAuth)
            {
                StateConnectCU = string.Format(ARMRep["ArmOdConn2Status"], new object[] { (string.IsNullOrEmpty(regInfo.UNC) ? "127.0.0.1" : regInfo.UNC), regInfo.Login });//АРМ оперативного дежурного. Подключено к {0} [{1}]               
                StateHasChanged();
                //добавляем в историю текущее подключение
                if (!CuHistoryListConnect.Contains(CurrentServer))
                    CuHistoryListConnect.Add(new Registration(CurrentServer));

                CurrentServer = regInfo;
                await ResetConnection();
            }
            else
            {
                MessageView?.AddError("", string.Format(ARMRep["ServerLoginRejRecon"], regInfo.UNC));//Сервер {0}. Вход отклонен. Проверьте настройки

                SetStaffUNC(string.Empty);
            }

            if (!DefaultServer.Equals(CurrentServer))
            {
                if (!CuHistoryListConnect.Contains(DefaultServer))
                    CuHistoryListConnect.Insert(0, DefaultServer);
            }

        }

        void SetStaffUNC(string Unc)
        {
            Http.DefaultRequestHeaders.AddHeader(CookieName.UncRemoteCu, Unc);
        }

        async Task ResetConnection()
        {
            if (Layout != null)
                await Layout.UpdateAppPortInfo();
            StaffId = await _User.GetLocalStaff();
            await GetSndSettingExRec();
            await GetReservCUList();

        }

        async Task<Registration> GetStaffAccess(IntID request)
        {
            Registration response = new();
            await Http.PostAsJsonAsync("api/v1/remote/GetStaffAccess", request).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    response = await x.Result.Content.ReadFromJsonAsync<Registration>() ?? new();
                }
            });

            return response;
        }

        private async Task<bool> SetLogin(RequestLogin request)
        {
            var result = await AuthenticationService.RemoteLogin(request);

            return result;
        }

        private async Task GetList()
        {
            CommandList = null;

            if (GroupId == null)// режим ретрансляции П160
            {
                await SetP16xMode(2);
            }
            else
            {
                await Http.PostAsJsonAsync("api/v1/remote/GetGroupCommandList", new IntID() { ID = GroupId.GroupID }, ComponentDetached).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        CommandList = await x.Result.Content.ReadFromJsonAsync<List<P16xGroupCommand>>();
                    }
                });
                await SetP16xMode(3);
            }

            if (CommandList == null)
                CommandList = new();

        }

        // Установить режим работы П16х.
        public async Task<bool> SetP16xMode(byte mode)
        {
            try
            {
                var snd = new OBJ_ID() { StaffID = StaffId, ObjID = (int)SoundSettingsType.P16SoundSettingType, SubsystemID = SubsystemType.SUBSYST_P16x };

                var p16xSett = await GetSndSettingEx(snd);
                if (p16xSett != null)
                {

                    p16xSett.AutoConfirm = mode;
                    SetSndSettingExRequest request = new()
                    {
                        MInfo = UnsafeByteOperations.UnsafeWrap(p16xSett.ToBytes()),
                        MInterface = p16xSett.Interfece,
                        OBJID = snd
                    };
                    await SetSndSetting(request);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        async Task SetP16xGateState(P16xGateDevice request)
        {
            await Http.PostAsJsonAsync("api/v1/remote/SetState", request, ComponentDetached).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var result = await x.Result.Content.ReadFromJsonAsync<BoolValue>();
                }
            });
        }
        private async Task<SndSettingP16> GetSndSettingEx(OBJ_ID request)
        {
            SndSettingP16 response = new();
            await Http.PostAsJsonAsync("api/v1/remote/GetSndSettingEx", request, ComponentDetached).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var json = await x.Result.Content.ReadAsStringAsync();

                    var m = JsonParser.Default.Parse<GetSndSettingExResponse>(json);

                    response = new(m.Info.Memory.ToArray());
                    response.Interfece = m.Interface;

                }
            });
            return response;
        }
        private async Task SetSndSetting(SetSndSettingExRequest request)
        {
            await Http.PostAsJsonAsync("api/v1/remote/SetSndSettingEx", JsonFormatter.Default.Format(request), ComponentDetached);
        }

        async Task SetGroupId(P16xGroup? id = null)
        {
            GroupId = id;
            await Task.Yield();
            sitItemList = new();
            SelectCommand = -1;
            if (table != null)
                await table.Refresh();
            await GetList();
        }

        void SetCommand(P16xGroupCommand item)
        {
            if (!string.IsNullOrEmpty(item.CommandName))
                SetCommand(item.Command);
        }
        void SetCommand(int command)
        {
            SelectCommand = command;
            switch (SelectCommand)
            {
                case 1:
                case 2:
                case 3:
                case 6: ChangeModeMsg(SourceMsgNotify.No); break;
                case 4:
                case 5: ChangeModeMsg(SourceMsgNotify.Microphone); break;
            }
        }
        async Task CheckStart()
        {
            if (DisplayModeIndex == 0 && (SelectCommand == -1 || sitItemList.Count == 0))
                return;

            if (DisplayModeIndex == 1 && (!SelectSitList?.Any() ?? true))
                return;

            if (!await _User.GetCanStartStopNotify())//не достаточно прав
            {
                MessageView?.AddError("", ARMRep["NoRights2Launch"]);
                return;
            }

            switch (SelectCommand)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                    IsStartNotify = true;
                    return;
            }

            await ButtonStart();

        }

        public void SelectPYPRD(int i)
        {
            if (stateArray == null)
            {
                sitItemList = new();
            }
            else
            {
                switch (i)
                {
                    case -1: sitItemList = !sitItemList.Any() ? stateArray.ToList() : new(); break;
                    case 0: sitItemList = stateArray.Where(x => x.DevID != 0).ToList(); break;
                    case 1: sitItemList = stateArray.Where(x => x.DevID == 0 && x.StaffID != 0).ToList(); break;
                }
            }
            SetStopState();

        }

        private async Task ButtonStart()
        {
            if (CustomModeStart)
            {
                await StopStream();
            }

            SessionRecords = 0;

            if (DisplayModeIndex == 1)
            {
                if (sitList?.HasSelectedActiveScens == true)
                {
                    if (typeMsgNotify == SourceMsgNotify.Microphone)
                        return;

                    if (sitList?.GetActivSitId?.Count > 0)
                    {
                        await StopNotify(sitList.GetActivSitId);
                    }
                    return;
                }
            }

            ////выбор источника!!!!!!!!!
            RecordMsgId = new();
            int repeat = RepeatCount;

            if (typeMsgNotify != SourceMsgNotify.Records)
                repeat = -1;

            if (typeMsgNotify != SourceMsgNotify.No)
            {
                RecordMsgId = await Rem_CreateMsgInFile(ActiveUrlFile) ?? new();
                if (RecordMsgId.ObjID == 0)
                {
                    return;
                }
                Task writeBuffer;

                List<OBJ_ID> objs = new();

                if (DisplayModeIndex == 0 && sitItemList.Count > 0)
                {
                    objs = new(sitItemList.Select(x => new OBJ_ID() { ObjID = x.DevID, StaffID = x.StaffID }));
                }
                else if (DisplayModeIndex == 1 && (SelectSitList?.Any() ?? false))
                {
                    objs = SelectSitList;
                }

                uploadMessages.Add(new UploadMessage(SessionRecords, ActiveUrlFile, objs));
                if (typeMsgNotify == SourceMsgNotify.Records)
                {
                    writeBuffer = Rem_WriteManualBuffer(SessionRecords);
                }
                else
                {
                    writeBuffer = StartIsOK();
                }
                uploadMessages.Single(x => x.SessionRecords == SessionRecords).WriteBuffer = writeBuffer;

            }

            if (SelectCommand == 6 && DisplayModeIndex == 0)
            {
                // отдельная тема для 6-й команды
                switch (curStopState)
                {
                    case StopState.ABORT:
                    {
                        // получить список ПУ с активным оповещением
                        List<OBJ_ID> sitIdList = GetSitItemList?.Where(x => x.StaffID != 0 && (uint)x.StaffID != 0xFFFFFFFF && x.ActiveNotify > 0)
                            .Select(x => new OBJ_ID { ObjID = x.StaffSessID, SubsystemID = SubsystemType.SUBSYST_GSO_STAFF }).ToList() ?? new();

                        if (sitIdList.Count > 0)
                        {
                            await StopNotify(sitIdList);
                        }

                        // получить список УЗС с активным оповещением
                        List<OBJ_ID> sitUZSList = GetSitItemList?.Where(x => x.DevID != 0 && (uint)x.StaffID == 0xFFFFFFFF && x.ActiveNotify > 0)
                            .Select(x => new OBJ_ID { ObjID = x.StaffSessID, SubsystemID = SubsystemType.SUBSYST_SZS }).ToList() ?? new();

                        if (sitUZSList.Count > 0)
                            await StopNotify(sitUZSList);

                    }
                    break;
                    case StopState.STOP:
                    {
                        // получить список активных ПРД
                        var sitList = GetSitItemList?.Where(x => x.DevID != 0 && (uint)x.StaffID != 0xFFFFFFFF && x.LastCmd > 0 && x.LastCmd < 6)
                            .Select(x => new P16xGateDevice(x) { StaffID = 0 }).ToList() ?? new();

                        if (sitList.Count > 0)
                            await StartNotifyCU(sitList, SelectCommand, RecordMsgId, repeat, GroupId?.GroupTypeID ?? 0);
                    }
                    break;
                    case StopState.TEST:
                        await DefaultSend(sitItemList, SelectCommand, RecordMsgId, repeat, GroupId?.GroupTypeID ?? 0);
                        break;
                }
            }
            else
                await DefaultSend(sitItemList, SelectCommand, RecordMsgId, repeat, GroupId?.GroupTypeID ?? 0);

            IsStartNotify = false;
        }

        private async Task StopNotify(List<OBJ_ID> sitList)
        {
            StartNotify request = new();
            request.ListOBJ = sitList;
            request.UnitID = new() { ObjID = 8 };
            try
            {
                await Http.PostAsJsonAsync("api/v1/remote/StopNotify", request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StopNotify {ex.Message}");
            }
        }

        void SetDisplayModeIndex(int index)
        {
            DisplayModeIndex = index;
            SelectSitList = null;
            sitItemList = new();
            SelectCommand = -1;
        }

        void SetStopState()
        {
            curStopState = CalcStopState;
            StateHasChanged();
        }

        StopState CalcStopState
        {
            get
            {
                if (CustomModeStart)
                    return StopState.STOP;

                if (sitItemList.Count == 0)
                    return StopState.TEST;

                if (GetSitItemList == null)
                    return StopState.TEST;

                // перебираем все ПРД
                if (GetSitItemList.Any(x => x.DevID != 0 && (uint)x.StaffID != 0xFFFFFFFF && x.LastCmd > 0 && x.LastCmd < 6))
                {
                    return StopState.STOP;
                }

                // перебираем все ПУ
                if (GetSitItemList.Any(x => ((x.StaffID != 0 && (uint)x.StaffID != 0xFFFFFFFF) || (x.DevID != 0 && (uint)x.StaffID == 0xFFFFFFFF)) && x.ActiveNotify != 0))
                {
                    return StopState.ABORT;
                }
                return StopState.TEST;
            }
        }

        IEnumerable<P16xGateDevice>? GetSitItemList
        {
            get
            {
                return stateArray?.Where(x => sitItemList.Any(s => s.ObjectID == x.ObjectID));
            }
        }

        private async Task<OBJ_ID?> Rem_CreateMsgInFile(string urlFile)
        {
            byte[] format = Array.Empty<byte>();
            string fileName = "";
            if (typeMsgNotify == SourceMsgNotify.Records)
            {
                fileName = $"{ARMRep["CREATE_FROM_FILE"]} {recordAudio?.OldFileName ?? "No file name"} {DateTime.Now.ToString("yyyy.MM.d_HH.mm.ss")}";

                format = await Http.GetFormatBlobAsync(urlFile);

                WavHeaderModel wavHeader = new(format);
                format = wavHeader.ToBytesOnlyHeader();

                if (wavHeader.ChunkHeaderSize < 8000)
                {
                    MessageView?.AddError("", ARMRep["NO_SOUND"]);
                    return null;
                }
            }
            else if (SettingRec.SndFormat != null)
            {
                fileName = $"Written {DateTime.Now}";
                format = SettingRec.SndFormat.ToBytes();
            }
            else
                return null;

            MsgInfo newMsg = new()
            {
                Msg = new()
                {
                    StaffID = StaffId,
                    SubsystemID = SubsystemType.SUBSYST_GSO_STAFF,// ПУ
                    ObjID = 0
                }
            };

            newMsg.Param = new()
            {
                ConnID = 0,
                ConnType = 0,
                DopParam = 65535,
                MsgType = (int)MessageType.MessageSound,
                MsgName = ARMRep["MESSAGE_MIDDLE_LEVEL"],
                MsgComm = $"{fileName}",
                Format = UnsafeByteOperations.UnsafeWrap(format)
            };

            OBJ_ID newMsgId = new(newMsg.Msg);

            string filePath = await GetParams(nameof(ParamsSystem.ARMPath));
            if (string.IsNullOrEmpty(filePath))
                filePath = "ARMPath";

            if (!string.IsNullOrEmpty(CurrentServer.CUName))
            {
                filePath = Path.Combine(filePath, CurrentServer.CUName.Replace(" ", "_"));
            }

            filePath = Path.Combine(filePath, $"P16xARMMSG_{DateTime.Now.ToString("yyyy.MM.d_HH.mm.ss")}.wav");

            CreateMsgInFile requestGate = new();
            requestGate.MsgInfo = newMsg;
            requestGate.FilePath = filePath;
            requestGate.Format = UnsafeByteOperations.UnsafeWrap(format);

            string? json = JsonFormatter.Default.Format(requestGate);
            var result = await Http.PostAsJsonAsync("api/v1/remote/Rem_CreateMsgInFile", json, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<CreateMsgInFileResponse>() ?? new();
                if (response.MessageId.ObjID > 0)
                {
                    newMsgId = response.MessageId;
                    SessionRecords = response.SoundId;
                }
            }
            else
            {
                MessageView?.AddError("", GsoRep["IDS_ERRSAVEDIRECTIVE"]);
            }

            if (newMsgId.ObjID != 0)
            {
                return newMsgId;
            }
            return null;
        }

        private async Task<string> GetParams(string nameParam)
        {
            string result = "";
            await Http.PostAsJsonAsync("api/v1/remote/GetParams", new StringValue() { Value = nameParam }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var g = await x.Result.Content.ReadFromJsonAsync<StringValue>() ?? new();
                    result = g.Value;
                }
            });
            return result;
        }

        private async Task Rem_WriteManualBuffer(uint sessionRecords)
        {
            var elem = uploadMessages.Single(x => x.SessionRecords == sessionRecords);

            byte[] format = await Http.GetFormatBlobAsync(elem.UrlFile);

            WavHeaderModel wavHeader = new(format);

            if (string.IsNullOrEmpty(elem.UrlFile) || !wavHeader.WAVE.SequenceEqual("WAVE".ToCharArray()))
            {
                MessageView?.AddMessage("", GsoRep["ERROR_READ_FILE"]);
                await Rem_CloseWriteManualCmd(sessionRecords);
                return;
            }
            var headerLength = (int)wavHeader.GetAllHeaderLength();
            try
            {
                var writeChannel = WriteManualBuffer(sessionRecords, headerLength);
                await ReaderChannel(sessionRecords);
                await writeChannel;
            }
            catch (Exception ex)
            {
                MessageView?.AddMessage("", GsoRep["ERROR_ENCODE_FILE"]);
                Console.WriteLine($"Error {ex.Message}");
            }

        }

        async Task WriteManualBuffer(uint sessionRecords, int headerLength)
        {
            var elem = uploadMessages.Single(x => x.SessionRecords == sessionRecords);

            using var s = await Http.GetStreamAsync(elem.UrlFile);

            if (s == null)
                return;
            byte[] buffer = new byte[BufferSizeSignal];
            int readCount = 0;
            try
            {
                while ((readCount = await s.ReadAsync(buffer, elem.TokenSource.Token)) > 0)
                {
                    await elem.ChannelRecords.Writer.WriteAsync(buffer.Skip(headerLength).Take(readCount).ToArray());
                    headerLength = 0;

                    elem.Uploaded += (readCount / 1024);

                    StateHasChanged();
                    await Task.Delay(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {ex.Message}");
            }
            elem.Uploaded = 0;
            elem.ChannelRecords.Writer.TryComplete();
        }

        async Task ReaderChannel(uint sessionRecords)
        {
            try
            {
                var elem = uploadMessages.Single(x => x.SessionRecords == sessionRecords);
                var b = await _HubContext.InvokeCoreAsync<bool>("ConvertSoundDataToGsmStream", new object[] { sessionRecords, Http.DefaultRequestHeaders.GetHeader(CookieName.UncRemoteCu), elem.ChannelRecords.Reader }, elem.TokenSource.Token);
            }
            catch (Exception ex)
            {
                MessageView?.AddMessage("", GsoRep["ERROR_WRITE_SERVER"]);
                Console.WriteLine(ex.Message);
            }
            await Rem_CloseWriteManualCmd(sessionRecords);
        }

        Task Rem_CloseWriteManualCmd(uint sessionRecords)
        {
            try
            {
                UInt32Value request = new() { Value = sessionRecords };
                _ = Http.PostAsJsonAsync("api/v1/remote/Rem_CloseWriteManualCmd", request);
            }
            catch (Exception ex)
            {
                MessageView?.AddMessage("", GsoRep["ERROR_CLOSE_MSG"]);
                Console.WriteLine($"Error {ex.Message}");
            }
            StateHasChanged();
            return Task.CompletedTask;
        }

        private async Task GetSndSettingExRec()
        {
            SettingRec = await _localStorage.GetSndSettingEx(SoundSettingsType.RecSoundSettingType) ?? new();
        }

        /// <summary>
        /// Начинаем запись звука
        /// </summary>
        /// <returns></returns>
        private async Task StartIsOK()
        {
            if (SettingRec.SndFormat == null)
            {
                MessageView?.AddError("", ARMRep["ErrorRecSetting"]);
                return;
            }
            TimeRecord = TimeSpan.FromMinutes(0);
            CustomModeStart = true;

            setting.ChannelCount = SettingRec.SndFormat.Channels;
            setting.SampleRate = SettingRec.SndFormat.SampleRate;
            setting.SampleSize = SettingRec.SndFormat.SampleSize;
            setting.Label = SettingRec.Interfece;
            setting.Volum = SettingRec.SndLevel;
            _ = ReaderChannel(SessionRecords);

            var reference = DotNetObjectReference.Create(this);
            setting = await JSRuntime.InvokeAsync<AudioRecordSetting>("RecordAudio.StartStreamWorklet", setting, reference);

            SettingRec.SndFormat.Channels = setting.ChannelCount;
            SettingRec.SndFormat.SampleRate = setting.SampleRate;
            SettingRec.SndFormat.SampleSize = setting.SampleSize;

            StateHasChanged();
        }

        async Task ResetButton()
        {
            await StopStream();

            if (stateArray != null)
            {
                List<P16xGateDevice> sitList = new();
                List<P16xGateDevice> objList = new();
                lock (stateArray)
                {
                    foreach (var item in stateArray)
                    {
                        objList.Add(item);
                    }
                }

                foreach (var item in objList)
                {
                    await SetP16xGateState(new P16xGateDevice(item) { LastCmd = 0, MsgID = 0, MsgStaffID = 0 });
                    if (item.LastCmd != 0)
                    {
                        sitList.Add(item);
                    }
                }

                await StartNotifyCU(sitList, 0, new OBJ_ID(), -1, 0);
            }
            SelectCommand = -1;
            sitItemList = new();

        }

        [JSInvokable]
        public async Task StreamToAudio(byte[]? btoa)
        {
            //если запись идет больше 5 минут, останавливаем
            if (TimeRecord.TotalMinutes > 5)
            {
                await StopStream();
                return;
            }

            if (btoa != null)
            {
                TimeRecord = TimeRecord.Add(TimeSpan.FromSeconds(btoa.Length / (setting.ChannelCount * setting.SampleRate * (setting.SampleSize / 8))));
                var elem = uploadMessages.Single(x => x.SessionRecords == SessionRecords);

                foreach (var item in btoa.Chunk(BufferSizeSignal))
                {
                    await elem.ChannelRecords.Writer.WriteAsync(item, elem.TokenSource.Token);

                    elem.Uploaded += (item.Length / 1024);
                }
                StateHasChanged();
            }
        }

        [JSInvokable]
        public async Task ErrorRecord(string? errorMessage = null)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                MessageView?.AddError("", ARMRep["IDS_STRING_NEED_MIC"]);
                await StopWorklet();
            }
            else
            {
                await StopStream();
                MessageView?.AddError("", errorMessage);
            }
        }

        [JSInvokable]
        public async Task StopWorklet()
        {
            await Task.Delay(1000);
            if (uploadMessages.Any(x => x.SessionRecords == SessionRecords))
            {
                var elem = uploadMessages.Single(x => x.SessionRecords == SessionRecords);
                elem.ChannelRecords.Writer.TryComplete();
                elem.Uploaded = 0;
            }

            if (CustomModeStart)
            {
                CustomModeStart = false;
            }
            StateHasChanged();
        }

        private async Task StopStream()
        {
            if (CustomModeStart)
            {
                await JSRuntime.InvokeVoidAsync("RecordAudio.StopStream");
            }
        }

        void SetSelectSitList(List<OBJ_ID>? items)
        {
            SelectSitList = items;
        }

        public async Task StartNotifyCU(List<P16xGateDevice> sitList, int cmd, OBJ_ID msgId, int MessageRepeatCount, int CmdOffset)
        {
            if (sitList == null || sitList.Count == 0)
                return;

            foreach (P16xGateDevice sitItem in sitList)
            {
                sitItem.Confirm = 0;
                sitItem.LastCmd = cmd;
                sitItem.MsgID = msgId.ObjID;
                sitItem.MsgStaffID = msgId.StaffID;
                await SetP16xGateState(sitItem);
            }

            OBJ_ID cu = new();
            cu.ObjID = 8;
            cu.StaffID = 0;
            cu.SubsystemID = 0;

            RequestStartNotify request = new()
            {
                Cmd = cmd,
                ControlUnitID = cu,
                MsgId = msgId,
                MessageRepeatCount = MessageRepeatCount,
                CmdOffset = CmdOffset
            };

            request.SitItemList.AddRange(sitList);

            await Http.PostAsJsonAsync("api/v1/remote/P16xGateStartNotify4", JsonFormatter.Default.Format(request));

        }

        async Task DefaultSend(List<P16xGateDevice> sitItemList, int cmd, OBJ_ID msgId, int MessageRepeatCount, int CmdOffset)
        {
            if (SelectCommand >= 1 && SelectCommand <= 6)
            {
                await StartNotifyCU(sitItemList, cmd, msgId, MessageRepeatCount, CmdOffset);
            }

            //дальше для запуска локальных сценариев

            if (SelectSitList?.Count > 0)
            {
                var request = new NotificationRequest()
                {
                    UnitID = new() { ObjID = 8 },
                    MsgId = msgId,
                    RequestType = msgId.ObjID > 0 ? RequestType.CustomStartNotification : RequestType.StartNotification
                };
                request.ListOBJ.AddRange(SelectSitList);

                try
                {
                    await Http.PostAsJsonAsync("api/v1/remote/StartNotify", JsonFormatter.Default.Format(request));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"StartNotify {ex.Message}");
                }
            }
        }

        void SetRepeatCount(int count)
        {
            if (RepeatCount + count >= 1 && RepeatCount + count <= 99)
                RepeatCount += count;
        }

        void ChangeModeMsg(SourceMsgNotify type)
        {
            typeMsgNotify = type;
        }

        private async Task SetSoundsUrlPlayer(string url)
        {
            if (player != null)
            {
                await player.SetUrlSound(url, false);
            }
            await RemoveUrlFile();
            ActiveUrlFile = url;
        }


        long GetUploadProgress
        {
            get
            {
                if (DisplayModeIndex == 0 && sitItemList.Count > 0)
                {
                    return uploadMessages.FirstOrDefault(x => x.ListCu?.Any(l => sitItemList.Any(s => s.DevID == l.ObjID && s.StaffID == l.StaffID)) ?? false)?.Uploaded ?? 0;
                }
                else if (DisplayModeIndex == 1 && (SelectSitList?.Any() ?? false))
                {
                    return uploadMessages.FirstOrDefault(x => x.ListCu?.Any(l => SelectSitList.Any(s => s.ObjID == l.ObjID && s.StaffID == l.StaffID)) ?? false)?.Uploaded ?? 0;
                }
                return 0;
            }
        }

        async Task RemoveUrlFile()
        {
            if (uploadMessages.Count > 0)
            {
                var elems = uploadMessages.Where(x => x.UrlFile != ActiveUrlFile).ToList();
                foreach (var elem in elems)
                {
                    if (elem.WriteBuffer?.IsCompleted ?? true)
                    {
                        await JSRuntime.InvokeVoidAsync("RemoveBlob", elem.UrlFile);
                        elem.WriteBuffer?.Dispose();
                        elem.TokenSource.Dispose();
                        uploadMessages.RemoveAll(x => x.SessionRecords == elem.SessionRecords);
                    }
                }
            }
        }


        public ValueTask DisposeAsync()
        {
            JSRuntime.InvokeVoidAsync("RecordAudio.StopStream");
            DisposeToken();
            foreach (var item in uploadMessages)
            {
                item.TokenSource.Cancel();
                item.TokenSource.Dispose();
            }

            return _HubContext.DisposeAsync();
        }
    }
}
