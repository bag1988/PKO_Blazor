using System.ComponentModel;
using System.Net.Http.Json;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Audio;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GateServiceProto.V1;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using StartUI.Client.Pages.IndexComponent;
using StartUI.Client.Shared;
using static BlazorLibrary.Shared.Main;
using FiltersGSOProto.V1;
using BlazorLibrary.Shared.Table;
using SharedLibrary.Interfaces;
using SharedLibrary.PuSubModel;

namespace StartUI.Client.Pages
{

    partial class Index : IAsyncDisposable, IPubSubMethod
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        private string? TitlePage = null;

        private bool IsStartNotify = false;

        private bool IsCustomMode = false;
        private bool IsCustomModeUUZS = false;

        private bool CustomModeStart = false;

        private bool OnStopNotify = false;

        private bool CreateNoSit = false;

        private List<string> activeNotifyInfo = new();

        List<GetDrySitInfoItem>? DrySitInfoList = null;

        private bool ShowModal = false;

        private List<string> StartWarning = new();

        private SndSetting SettingRec = new();

        enum Tab
        {
            SitList = 1,
            StatList = 2,
            ResultList = 3

        }

        private Tab ActiveTab = Tab.SitList;

        private ViewSitListCache? SitList = default!;

        private List<byte> AllBytesSound = new();

        private VisualRecord? visualRec = null;

        private OBJ_ID? RecordMsgId = null;

        private TimeSpan TimeRecord = TimeSpan.FromMinutes(5);

        private AudioRecordSetting setting = new();

        private int StaffId = 0;

        private int UserSessId = 0;

        private bool IsObjNoNotify = false;

        private ProductVersion? PVersion = null!;

        protected override async Task OnInitializedAsync()
        {
            StaffId = await _User.GetLocalStaff();

            UserSessId = await _User.GetUserSessId();
            await PVersionFull();
            SetActivTab(Tab.SitList);
            await GetActivNotify();
            _ = _HubContext.SubscribeAsync(this);
        }

        private void SetActivTab(Tab item)
        {
            if (SitList != null)
                SitList.SelectList = new();
            if (item == Tab.StatList && activeNotifyInfo.Count == 0)
            {
                ActiveTab = Tab.SitList;
            }
            else if (item == Tab.ResultList && activeNotifyInfo.Count > 0)
            {
                ActiveTab = Tab.StatList;
            }
            else
                ActiveTab = item;

            switch (ActiveTab)
            {
                case Tab.StatList: TitlePage = StartUIRep["IDS_HELP_NOTIFY"]; break;
                case Tab.ResultList: TitlePage = StartUIRep["IDS_HELP_NO_NOTIFY"]; break;
                case Tab.SitList: TitlePage = StartUIRep["IDS_HELP_NO_NOTIFY"]; break;
            }
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_StartSession(FireStartSession Value)
        {
            if (Value.Subsystem == SubsystemID)
            {
                if (Value.IdSession == -1 || ActiveTab == Tab.StatList)
                {
                    return;
                }
                if (SitList != null)
                {
                    SitList.SelectList = new();
                    SitList.ClearSelect();
                }

                await GetActivNotify();
                await RefreshList();

                //Если есть активные сценарии, переключаем вкладку, обновляем статистику
                if (activeNotifyInfo.Count > 0)
                {
                    if (ActiveTab != Tab.StatList)
                        SetActivTab(Tab.StatList);
                }
                await StartIsOK();
                StateHasChanged();
            }
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_EndSession(FireEndSession Value)
        {
            if (Value.Subsystem == SubsystemID)
            {
                await GetActivNotify();
                await RefreshList();
                await StopStream();

                if (ActiveTab == Tab.StatList)
                    SetActivTab(Tab.ResultList);

                if (Value.IdSession >= 0)
                {
                    if (MainLayout.Settings.SaveReport == true && SubsystemID == SubsystemType.SUBSYST_ASO)
                    {
                        await _GenerateReportChannels.CreateReport();
                    }

                    if (MainLayout.Settings.SoundEnd == true)
                    {
                        await JSRuntime.InvokeVoidAsync("playEndNotify");
                    }
                }
                StateHasChanged();
            }
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_ErrorCreateNewSituation(ulong Value)
        {
            await StopStream();
            MessageView?.AddError("", SMGateRep["ERROR_START_NOTIFY"]);
            RecordMsgId = null;
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_ErrorContinue(ulong Value)
        {
            await Task.Run(() => MessageView?.AddError(StartUIRep["IDS_ERRORCAPTION"], StartUIRep["IDS_ERRORCONTINUE"]));

        }

        /// <summary>
        /// Запрос дооповещения
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        [Description(DaprMessage.PubSubName)]
        public Task Fire_NotifySessEvents(ulong Value)
        {
            if (MainLayout.Settings.ContinueNotify == true && (ActiveTab == Tab.ResultList || ActiveTab == Tab.StatList))
            {
                if ((int)Value == SubsystemID)
                {
                    IsObjNoNotify = true;
                }
            }
            return Task.CompletedTask;
        }

        private async Task PVersionFull()
        {
            var result = await Http.PostAsync("api/v1/allow/PVersionFull", null);
            if (result.IsSuccessStatusCode)
            {
                PVersion = await result.Content.ReadFromJsonAsync<ProductVersion>();
            }
        }

        string GetLogoSvg
        {
            get
            {
                if (PVersion == null)
                {
                    return "";
                }

                if (PVersion.CompanyName == "kae")
                    return "/KAE.svg";
                return "/Sensor_logo.svg";
            }
        }

        private async Task StartDeleteNoStandart()
        {
            DrySitInfoList = null;
            OBJ_ID request = new OBJ_ID() { ObjID = 0, SubsystemID = SubsystemID };
            var result = await Http.PostAsJsonAsync("api/v1/GetDrySitInfo", request);
            if (result.IsSuccessStatusCode)
            {
                DrySitInfoList = await result.Content.ReadFromJsonAsync<List<GetDrySitInfoItem>>();
                DrySitInfoList = DrySitInfoList?.Select(s => new GetDrySitInfoItem(s) { SitTypeID = 1 }).ToList();
            }

            if (!DrySitInfoList?.Any() ?? true)
            {
                MessageView?.AddError(StartUIRep["IDS_STRING_DEL_ALL_NONSTANDART_SIT"], GSOFormRep["IDS_NORESULT"]);
            }
        }

        private void CheckDeleteItem(int item)
        {
            if (DrySitInfoList != null && DrySitInfoList.Any(x => x.SitID == item))
            {
                DrySitInfoList.First(x => x.SitID == item).SitTypeID = DrySitInfoList.First(x => x.SitID == item).SitTypeID == 0 ? 1 : 0;
            }
        }

        private async Task DeleteNoStandart()
        {
            if (DrySitInfoList != null)
            {
                if (SitList != null)
                    SitList.SelectList = new();

                foreach (var item in DrySitInfoList.Where(x => x.SitTypeID == 1))
                {
                    await Http.PostAsJsonAsync("api/v1/DeleteSituationNONSIT", new OBJIDAndStr() { OBJID = new() { ObjID = item.SitID, StaffID = item.StaffID, SubsystemID = item.SubsystemID }, Str = item.SitName }).ContinueWith(async x =>
                               {
                                   if (x.Result.IsSuccessStatusCode)
                                   {
                                       var b = await x.Result.Content.ReadFromJsonAsync<BoolValue>();
                                       if (b?.Value == false)
                                       {
                                           MessageView?.AddError(StartUIRep["IDS_STRING_DEL_ALL_NONSTANDART_SIT"], item.SitName + " " + StartUIRep["IDS_ERRORCAPTION"]);
                                       }
                                   }
                               });
                }

                await RefreshList();
                DrySitInfoList = null;

            }
        }

        private async Task RefreshList()
        {
            if (SitList != null)
                await SitList.Refresh();
        }

        private async Task GetActivNotify()
        {
            OBJ_ID SessID = new() { ObjID = 0, SubsystemID = SubsystemID };

            activeNotifyInfo = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetAppropriateNotifyInfo", SessID);
            if (result.IsSuccessStatusCode)
            {
                var r = await result.Content.ReadFromJsonAsync<StringValue>() ?? new();

                if (!string.IsNullOrEmpty(r.Value))
                {
                    activeNotifyInfo = r.Value.Split("\n").ToList();
                }
                else
                    activeNotifyInfo = new();
            }
            else
            {
                MessageView?.AddError(StartUIRep["IDC_STATIC_ACTIVE_SIT_LIST"], AsoRep["IDS_STRING_ERR_GET_DATA"]);
            }
        }

        private void OnStartNotify()
        {
            StartWarning.Clear();

            if (SitList != null && SitList.SelectList.Any())
                ShowModal = true;
            else
            {
                IsCustomMode = false;
                MessageView?.AddError("", StartUIRep["IDS_STRING_SIT_NOT_SEL"] + "! " + StartUIRep["IDS_STRING_SEL_SIT_FOR_LAUNCH"]);
            }
        }

        private void OnStartNotifyCustom()
        {
            IsCustomMode = true;
            OnStartNotify();
        }

        private async Task CheckConfig(Action ActionNext, RequestType request)
        {
            if (SitList == null || !SitList.SelectList.Any())
            {
                return;
            }

            var result = await Http.PostAsJsonAsync("api/v1/CheckConfiguration", request);
            if (result.IsSuccessStatusCode)
            {
                var r = await result.Content.ReadFromJsonAsync<ConfState>() ?? new();

                if (r.Errors.Any(x => x != ResponseCode.Succeeded))
                {
                    MessageView?.AddError("", r.Errors.Select(x => ErrRep[x.ToString()].Value).ToList());
                }
                else if (r.Warnings.Any(x => x != ResponseCode.Succeeded))
                {
                    StartWarning.AddRange(r.Warnings.Select(x => ErrRep[x.ToString()].Value));
                }
                else
                {
                    ActionNext.Invoke();
                }
            }
        }

        private async Task StartNotify()
        {
            bool bAdd = false;
            RecordMsgId = null;
            if (SitList != null && SitList.SelectList.Any())
            {
                //добавить выбранные ситуации в таблицу БД
                var result = await Http.PostAsJsonAsync("api/v1/AddSitSeqNum", SitList.SelectList.Select(x => x.OBJID).ToList());
                if (result.IsSuccessStatusCode)
                {
                    bAdd = true;
                }
                else
                {
                    MessageView?.AddError("", StartUIRep["ErrorAddSitSeqNum"]);
                }
                if (bAdd)
                {
                    bAdd = false;
                    bool bIsExist = false;
                    //получаем кол-во объектов в ситуации??? кол-во абонентов?
                    result = await Http.PostAsJsonAsync("api/v1/IsExistNotifyObject", new IntID() { ID = SubsystemID });
                    if (result.IsSuccessStatusCode)
                    {
                        var response = await result.Content.ReadFromJsonAsync<IntID>() ?? new();
                        bIsExist = response.ID > 0 ? true : false;
                    }
                    else
                    {
                        MessageView?.AddError("", StartUIRep["IDS_STRING_BY_ACCESS_DATA_SIT"]);
                    }

                    if (bIsExist)
                    {
                        OBJ_ID UnitID = await GetControlUnitKey();

                        if (UnitID.ObjID != 0)//Start_2
                        {
                            List<SituationInfo>? cSituationInfos = null;
                            //получаем инфо о выбранных сценариях
                            result = await Http.PostAsJsonAsync("api/v1/GetSituationInfoList", SitList.SelectList.Select(x => x.OBJID).ToList());
                            if (result.IsSuccessStatusCode)
                            {
                                cSituationInfos = await result.Content.ReadFromJsonAsync<List<SituationInfo>>();

                                if (cSituationInfos != null && cSituationInfos.Any())
                                {
                                    string UrlStartNotify = "api/v1/StartNotify";
                                    StartNotify request = new();
                                    request.UnitID = UnitID;
                                    request.ListOBJ.AddRange(cSituationInfos.Select(x => x.Sit).ToList());
                                    request.SessId = UserSessId.ToString();
                                    request.SitNameList = SitList.SelectList.Select(x => x.SitName).ToList();

                                    if (IsCustomMode)
                                    {
                                        await GetSndSettingExRec();
                                        UrlStartNotify = "api/v1/CustomStartNotify";
                                        RecordMsgId = request.MsgId = await CreateMsg();
                                    }

                                    List<OBJ_ID>? response = null;
                                    result = await Http.PostAsJsonAsync(UrlStartNotify, request);
                                    if (result.IsSuccessStatusCode)
                                    {
                                        response = await result.Content.ReadFromJsonAsync<List<OBJ_ID>>() ?? null;

                                        if (response != null && response.Any())
                                        {
                                            bAdd = true;
                                            //записываем активную сесиию
                                            //m_CurNotifySessID = response;
                                            _ = _localStorage.SetCurNotifySessID(response);
                                            TitlePage = StartUIRep["IDS_HELP_NOTIFY"];
                                            //ActiveTab = Tab.StatList;
                                        }


                                    }
                                    else if (result.StatusCode == System.Net.HttpStatusCode.SeeOther)
                                    {
                                        RecordMsgId = null;
                                        await LicenseError();
                                    }
                                }
                            }
                            else
                            {
                                MessageView?.AddError("", GsoRep["IDS_EMESSAGEINFO"]);
                            }
                        }

                    }
                    else
                    {
                        MessageView?.AddError("", StartUIRep["IDS_NONOTIFYABONENT"]);
                    }
                }
                else
                    MessageView?.AddError("", StartUIRep["IDS_SITEXIST"]);
            }
            else
                MessageView?.AddError("", StartUIRep["IDS_STRING_SEL_SIT_FOR_LAUNCH"]);
            HideNotify();
        }

        private async Task StartIsOK()
        {
            if (RecordMsgId != null && RecordMsgId.ObjID > 0 && !CustomModeStart)
            {
                if (SettingRec.SndFormat == null)
                {
                    MessageView?.AddError("", DeviceRep["ErrorRecSetting"]);
                    return;
                }

                CustomModeStart = true;

                setting.ChannelCount = SettingRec.SndFormat.Channels;
                setting.SampleRate = SettingRec.SndFormat.SampleRate;
                setting.SampleSize = SettingRec.SndFormat.SampleSize;
                setting.Label = SettingRec.Interfece;
                setting.Volum = SettingRec.SndLevel;
                var reference = DotNetObjectReference.Create(this);
                NumberSecond = 0;
                setting = await JSRuntime.InvokeAsync<AudioRecordSetting>("RecordAudio.StartStreamWorklet", setting, reference);
                StateHasChanged();
            }
        }

        private async Task LicenseError()
        {
            await RemoveSitSeqNum();
            MessageView?.AddError("", StartUIRep["IDS_LICENSE_ERROR"]);
        }

        private async Task<OBJ_ID> GetControlUnitKey()
        {
            OBJ_ID UnitID = new OBJ_ID() { ObjID = 0, StaffID = StaffId, SubsystemID = SubsystemID };

            var result = await Http.PostAsJsonAsync("api/v1/GetControlUnitKey", UnitID);
            if (result.IsSuccessStatusCode)
            {
                UnitID = await result.Content.ReadFromJsonAsync<OBJ_ID>() ?? new();
            }
            return UnitID;
        }

        private void HideNotify()
        {
            IsCustomMode = false;
            ShowModal = false;
            IsStartNotify = false;
            StateHasChanged();
        }

        /// <summary>
        ///  Создаем сообщение для запуска в ручном режиме
        /// </summary>
        /// <returns></returns>
        private async Task<OBJ_ID?> CreateMsg()
        {
            if (SettingRec.SndFormat == null)
            {
                MessageView?.AddError("", DeviceRep["ErrorRecSetting"]);
                return null;
            }

            TimeRecord = TimeSpan.FromMinutes(5);
            AllBytesSound = new();
            OBJ_ID? newMsg = null;

            MsgInfo msg = new();
            msg.Param = new();
            msg.Msg = new();

            msg.Msg.StaffID = StaffId;
            msg.Msg.SubsystemID = SubsystemID;
            msg.Msg.ObjID = 0;
            msg.Param.ConnID = 0;
            msg.Param.ConnType = 0;
            msg.Param.DopParam = 65535;
            msg.Param.MsgType = (int)MessageType.MessageSound;
            msg.Param.MsgName = StaffRep["UserMsg"];
            msg.Param.MsgComm = StaffRep["Create"] + " " + DateTime.Now.ToString();

            WavHeaderModel wavHeader = new(SettingRec.SndFormat.ToBytes());

            msg.Param.Format = UnsafeByteOperations.UnsafeWrap(wavHeader.ToBytesAllHeader());

            var result = await Http.PostAsJsonAsync("api/v1/WriteMessagesCustom", JsonFormatter.Default.Format(msg));
            if (result.IsSuccessStatusCode)
            {
                newMsg = await result.Content.ReadFromJsonAsync<OBJ_ID>();
            }
            else
            {
                MessageView?.AddError("", GsoRep["IDS_STRING_CREATE_MESSAGE_ASO"]);
            }
            return newMsg;
        }

        int NumberSecond = 0;

        private async Task GetSndSettingExRec()
        {
            SettingRec = await _localStorage.GetSndSettingEx(SoundSettingsType.RecSoundSettingType) ?? new();
        }

        [JSInvokable]
        public async Task StreamToAudio(byte[]? btoa)
        {
            if (btoa != null)
            {
                var Sound = Convert.ToBase64String(btoa);

                AllBytesSound.AddRange(btoa);

                _ = Http.PostAsJsonAsync("api/v1/SetMessageParts", new MessageParts() { OBJID = RecordMsgId, Sound = Sound, NumberPart = NumberSecond }).ContinueWith(async (x) =>
                {
                    if (!x.Result.IsSuccessStatusCode)
                    {
                        await JSRuntime.InvokeVoidAsync("RecordAudio.StopStream");
                        MessageView?.AddError("", SqlRep["MesTransErr"]);
                    }
                });
                NumberSecond++;
                var d = (double)btoa.Length / (setting.ChannelCount * setting.SampleRate * (setting.SampleSize / 8));

                TimeRecord = TimeRecord.Subtract(TimeSpan.FromSeconds(d));

                if (visualRec != null)
                {
                    visualRec.Refresh(TimeSpan.FromSeconds((double)AllBytesSound.Count / (setting.ChannelCount * setting.SampleRate * (setting.SampleSize / 8))));
                }

                StateHasChanged();
            }

            //если запись идет больше 5 минут, останавливаем
            if (TimeRecord.TotalSeconds <= 0)
            {
                await StopStream();
            }
        }

        [JSInvokable]
        public async Task ErrorRecord(string? errorMessage = null)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                MessageView?.AddError("", StartUIRep["IDS_STRING_NEED_MIC"]);
                await StopWorklet();
                await StopNotify();
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
            if (CustomModeStart)
            {
                CustomModeStart = false;
                IsCustomMode = false;
                if (RecordMsgId != null)
                    await Http.PostAsJsonAsync("api/v1/SetMessageStatus", new OBJ_ID(RecordMsgId) { SubsystemID = 0 });
                AllBytesSound = new();

            }
            if (RecordMsgId != null)
                RecordMsgId = null;

            StateHasChanged();
        }

        private async Task StopStream()
        {
            if (CustomModeStart)
            {
                await JSRuntime.InvokeVoidAsync("RecordAudio.StopStream");
            }
            else
                RecordMsgId = null;
        }

        private async Task RemoveSitSeqNum()
        {
            List<OBJ_ID> request = new();
            if (SitList != null)
                request.AddRange(SitList.SelectList.Select(x => x.OBJID));

            if (request.Count > 0)
            {
                await Http.PostAsJsonAsync("api/v1/RemoveSitSeqNum", request.Distinct().ToList()).ContinueWith(x =>
                {
                    if (!x.Result.IsSuccessStatusCode)
                    {
                        MessageView?.AddError("", StartUIRep["ErrorRemoveSitSeqNum"]);
                    }
                });
            }
        }

        private async Task StopNotify()
        {
            if (!activeNotifyInfo.Any())
                return;

            var m_CurNotifySessID = await _localStorage.GetCurNotifySessID();

            m_CurNotifySessID = m_CurNotifySessID.Where(x => x.SubsystemID == SubsystemID).ToList();

            if (m_CurNotifySessID.Count == 0)
            {
                m_CurNotifySessID = new() { new OBJ_ID() { ObjID = 0, SubsystemID = SubsystemID } };
            }
            StartNotify request = new();
            request.ListOBJ = m_CurNotifySessID;
            request.SessId = UserSessId.ToString();
            request.UnitID = await GetControlUnitKey();
            try
            {
                await Http.PostAsJsonAsync("api/v1/StopNotify", request).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        var list = await x.Result.Content.ReadFromJsonAsync<List<OBJ_ID>>() ?? null;

                        if (list == null || list.Count == 0)
                        {
                            MessageView?.AddError("", SMGateRep["IDS_ERROR_STOP_NOTIFY"]);
                        }
                        else
                        {
                            _ = _localStorage.SetCurNotifySessID(list);
                            //m_CurNotifySessID = list;
                        }
                    }
                    else if (x.Result.StatusCode == System.Net.HttpStatusCode.SeeOther)
                    {
                        await LicenseError();
                    }

                    OnStopNotify = false;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StopNotify {ex.Message}");
            }
        }

        private async Task ContinueNotify()
        {
            RecordMsgId = null;
            var m_CurNotifySessID = await _localStorage.GetCurNotifySessID();

            m_CurNotifySessID = m_CurNotifySessID.Where(x => x.SubsystemID == SubsystemID).ToList();

            if (m_CurNotifySessID.Count == 0)
            {
                m_CurNotifySessID = new() { new OBJ_ID() { ObjID = 0, SubsystemID = SubsystemID } };
            }
            StartNotify request = new();
            request.ListOBJ = m_CurNotifySessID;
            request.SessId = UserSessId.ToString();
            request.UnitID = await GetControlUnitKey();

            await Http.PostAsJsonAsync("api/v1/ContinueNotify", request).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var list = await x.Result.Content.ReadFromJsonAsync<List<OBJ_ID>>() ?? null;

                    if (list == null || list.Count == 0)
                    {
                        MessageView?.AddError("", SMGateRep["ERROR_START_NOTIFY"]);
                    }
                    else
                    {
                        _ = _localStorage.SetCurNotifySessID(list);
                    }
                }
                else if (x.Result.StatusCode == System.Net.HttpStatusCode.SeeOther)
                {
                    await LicenseError();
                }
            });
            IsObjNoNotify = false;
        }

        public ValueTask DisposeAsync()
        {
            JSRuntime.InvokeVoidAsync("RecordAudio.StopStream");
            return _HubContext.DisposeAsync();
        }


    }
}
