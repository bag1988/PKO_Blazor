using System.ComponentModel;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using BlazorLibrary.Helpers;
using UUZSDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;
using LibraryProto.DerivedModels;

namespace BlazorLibrary.Shared.Situation.NextPage
{
    partial class SzsPage : IAsyncDisposable, IPubSubMethod
    {
        [Parameter]
        public EventCallback<List<CGetSitItemInfo>?> NextAction { get; set; }

        [Parameter]
        public List<CGetSitItemInfo>? ObjectList { get; set; }

        [Parameter]
        public bool? IsReadOnly { get; set; } = false;

        [Parameter]
        public bool? IsIndividualMode { get; set; } = false;

        private List<CGetSitItemInfo>? SzsList { get; set; }

        private int interceptMode = 2;

        readonly List<CSubDevice> ZoneList = new();

        private bool bPvsOn = false;

        private int IDC_SIRENA_TYPE { get; set; } = 0;

        private CGetSitItemInfo? SelectItem = null;

        private CGetSitItemInfo ItemFirst = new();

        private CGetSitItemInfo? PrdItem;
        private CGetSitItemInfo? OldPrdItem;

        private bool ViewInfoMessage = false;

        private List<Objects>? MsgList = null;

        private TimeOnly IDC_MESSAGETIME
        {
            get => new(0, (Bits.HIWORD(ItemFirst.CmdParam) / 20) / 60, (Bits.HIWORD(ItemFirst.CmdParam) / 20) % 60);
            set
            {
                ItemFirst.CmdParam = Bits.MAKELONG(Bits.LOWORD(ItemFirst.CmdParam), (value.Minute * 60 + value.Second) * 20);
                StateHasChanged();
            }
        }

        private TimeOnly IDC_SIRENATIME
        {
            get => new(0, (Bits.LOWORD(ItemFirst.CmdParam) / 20) / 60, (Bits.LOWORD(ItemFirst.CmdParam) / 20) % 60);
            set
            {
                ItemFirst.CmdParam = Bits.MAKELONG((value.Minute * 60 + value.Second) * 20, Bits.HIWORD(ItemFirst.CmdParam));
                StateHasChanged();
            }
        }

        readonly Dictionary<int, string> ThList = new();

        readonly Dictionary<int, string> ThListZone = new();

        private int WARNING_LENGTH_TIME_SIRENA = 0;

        private bool IsNewMessage = false;
        private int StaffId = 0;

        bool IsAllMsg = false;

        private bool IsNext = false;

        bool IsProcessing = false;

        private List<CCmdInfo>? m_CmdList { get; set; }

        readonly HBits Bits = new();

        private bool IsLoadPage = true;

        private bool IsCreatePrd = false;

        protected override async Task OnInitializedAsync()
        {
            ThList.Add(-1, UUZSRep["IDS_STRING_NAME_DEVICE"]);
            ThList.Add(-2, UUZSRep["NUMBER_MESSAGE"]);
            ThList.Add(-3, UUZSDataRep["IDC_SOUNDMSG"]);
            ThList.Add(-4, UUZSDataRep["IDC_STATIC_SIRENA_LEN"]);

            ThListZone.Add(0, UUZSRep["IDS_STRING_NAME_DEVICE"]);
            for (int i = 1; i <= 15; i++)
            {
                ThListZone.Add(i * -1, i.ToString());
            }

            StaffId = await _User.GetLocalStaff();

            if (ObjectList?.Any(x => x.DevType != SubsystemType.SUBSYST_PRD) ?? false)
                SzsList = ObjectList.Where(x => x.DevType != SubsystemType.SUBSYST_PRD).ToList();

            await GetMsgList();

            //Для ПРД
            if (ObjectList?.Any(x => x.DevType == SubsystemType.SUBSYST_PRD) ?? false)
            {
                PrdItem = ObjectList.First(x => x.DevType == SubsystemType.SUBSYST_PRD);
                if (PrdItem.MsgID > 0)
                    IsAllMsg = MsgList?.FirstOrDefault(x => x.OBJID.ObjID == PrdItem.MsgID)?.OBJID?.SubsystemID != SubsystemType.SUBSYST_SZS;

                OldPrdItem = new(PrdItem);
                m_CmdList = await GetCommandList(SubsystemType.SUBSYST_PRD);
                IsCreatePrd = true;
            }

            if (SzsList != null)
            {
                ItemFirst = new CGetSitItemInfo(SzsList.FirstOrDefault() ?? new());
                if (ItemFirst.MsgID > 0)
                    IsAllMsg = MsgList?.FirstOrDefault(x => x.OBJID.ObjID == ItemFirst.MsgID)?.OBJID?.SubsystemID != SubsystemType.SUBSYST_SZS;
            }
            else
            {
                ItemFirst.Param1 = Bits.RESET_BIT(ItemFirst.Param1, 19);
                ItemFirst.Param1 = Bits.RESET_BIT(ItemFirst.Param1, 20);
                ItemFirst.Param1 = Bits.RESET_BIT(ItemFirst.Param1, 17);
                ItemFirst.Param1 = Bits.SET_BIT(ItemFirst.Param1, 18);
                ItemFirst.CmdParam = Bits.MAKELONG(0, 0);
                ItemFirst.Param2 = Bits.MAKELONG(0, Bits.HIWORD(ItemFirst.Param2));
            }

            interceptMode = Bits.CHECK_BIT(ItemFirst.Param1, 19) ? 0 : Bits.CHECK_BIT(ItemFirst.Param1, 20) ? 1 : 2;
            bPvsOn = Bits.LOWORD(ItemFirst.CmdParam) == 3000 && Bits.HIWORD(ItemFirst.CmdParam) == 6000;



            if (Bits.CHECK_BIT(ItemFirst.Param1, 18))
                await GetMessageSize(ItemFirst.MsgID);

            IsLoadPage = false;

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteMessage(ulong Value)
        {
            await GetMsgList();
            Objects? elem = null;
            if (Value > 0)
            {
                elem = MsgList?.Where(x => x.OBJID != null).FirstOrDefault(x => x.OBJID.ObjID == (int)Value);
            }

            if (IsCreatePrd)
            {
                if (PrdItem != null)
                {
                    PrdItem.MsgID = elem?.OBJID?.ObjID ?? 0;
                    PrdItem.MsgStaffID = elem?.OBJID?.StaffID ?? 0;
                }
            }
            else
            {
                await GetMessageSize(elem?.OBJID?.ObjID ?? 0);
            }
            StateHasChanged();
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateMessage(ulong Value)
        {
            await GetMsgList();

            Objects? elem = null;
            if (Value > 0)
            {
                elem = MsgList?.Where(x => x.OBJID != null).FirstOrDefault(x => x.OBJID.ObjID == (int)Value);
            }

            if (IsCreatePrd)
            {
                if (PrdItem != null)
                {
                    PrdItem.MsgID = elem?.OBJID?.ObjID ?? 0;
                    PrdItem.MsgStaffID = elem?.OBJID?.StaffID ?? 0;
                }
            }
            else
            {
                await GetMessageSize(elem?.OBJID?.ObjID ?? 0);
            }
            StateHasChanged();
        }

        void SetSelectList(List<CGetSitItemInfo>? items)
        {
            if (items?.LastOrDefault() != null && items.Last().MsgID > 0 && (!SelectItem?.Equals(items.Last()) ?? true))
            {
                IsAllMsg = MsgList?.FirstOrDefault(x => x.OBJID.ObjID == items.Last()?.MsgID)?.OBJID?.SubsystemID != SubsystemType.SUBSYST_SZS;
            }
            else if ((!SelectItem?.Equals(items?.LastOrDefault()) ?? true))
                IsAllMsg = false;
            SelectItem = items?.LastOrDefault();
        }

        private void SetAllView(ChangeEventArgs e)
        {
            if (e != null && e.Value != null)
                IsAllMsg = (bool)e.Value;

            if (!IsAllMsg)
            {
                if (PrdItem != null)
                    PrdItem.MsgID = 0;
                if (ItemFirst != null)
                    ItemFirst.MsgID = 0;
                if (SelectItem != null)
                    SelectItem.MsgID = 0;
            }

        }

        private async Task OnButtonClicked(int TypeButton)
        {
            if (TypeButton == 1071/*IDC_RUNSGS*/)
            {
                ItemFirst.Param1 = Bits.RESET_BIT(ItemFirst.Param1, 19);
                ItemFirst.Param1 = Bits.RESET_BIT(ItemFirst.Param1, 20);
                interceptMode = 2;
            }
            else if (TypeButton == 1072/*IDC_TESTSGS*/)
            {
                ItemFirst.Param1 = Bits.SET_BIT(ItemFirst.Param1, 19);
                ItemFirst.Param1 = Bits.RESET_BIT(ItemFirst.Param1, 20);
                interceptMode = 0;
            }
            else if (TypeButton == 1109/*IDC_TESTSGS_EU)*/)
            {
                ItemFirst.Param1 = Bits.RESET_BIT(ItemFirst.Param1, 19);
                ItemFirst.Param1 = Bits.SET_BIT(ItemFirst.Param1, 20);
                interceptMode = 1;
            }
            else if (TypeButton == 1143/*IDC_NOTIFY_GRD*/)
            {
                if (!Bits.CHECK_BIT(ItemFirst.Param1, 31))
                    ItemFirst.Param1 = Bits.SET_BIT(ItemFirst.Param1, 31);
                else
                    ItemFirst.Param1 = Bits.RESET_BIT(ItemFirst.Param1, 31);
            }
            else if (TypeButton == 1041/*IDC_ONSIRENA*/)
            {
                if (!Bits.CHECK_BIT(ItemFirst.Param1, 17))
                    ItemFirst.Param1 = Bits.SET_BIT(ItemFirst.Param1, 17);
                else
                {
                    // Для режима БЕЗ СИРЕНЫ сообщение с ЭПУ не может быть "ВОЗДУШНАЯ ТРЕВОГА" - устанавливаем режим трансляции сообщения по каналу связи без сообщения
                    if (Bits.LOWORD(ItemFirst.Param2) == 0x0FFF)
                    {
                        ItemFirst.Param2 = Bits.MAKELONG(0, Bits.HIWORD(ItemFirst.Param2));
                        ItemFirst.MsgID = ItemFirst.MsgStaffID = 0;
                        ItemFirst.Param1 = Bits.SET_BIT(ItemFirst.Param1, 18);
                    }
                    ItemFirst.Param1 = Bits.RESET_BIT(ItemFirst.Param1, 17);
                    ItemFirst.CmdParam = Bits.MAKELONG(0, Bits.HIWORD(ItemFirst.CmdParam)); // Время звучания сирены - ноль
                }
            }
            else if (TypeButton == 1042/*IDC_RADIO1*/)
            {
                ItemFirst.Param1 = Bits.RESET_BIT(ItemFirst.Param1, 18);
                ItemFirst.MsgID = ItemFirst.MsgStaffID = 0;
            }
            else if (TypeButton == 1043/*IDC_RADIO2*/)
            {
                ItemFirst.Param1 = Bits.SET_BIT(ItemFirst.Param1, 18);
                ItemFirst.Param2 = Bits.MAKELONG(0, Bits.HIWORD(ItemFirst.Param2));
            }
            else if (TypeButton == 1095/*IDC_NOTIFY_PVS1*/)
            {
                bPvsOn = !bPvsOn;
                ItemFirst.Param1 = Bits.SET_BIT(ItemFirst.Param1, 17); // Включение сирены
                ItemFirst.Param1 = Bits.SET_BIT(ItemFirst.Param1, 18); // Передача звука по каналу связи
                ItemFirst.Param2 = Bits.MAKELONG(0, Bits.HIWORD(ItemFirst.Param2));
                if (bPvsOn)
                {
                    ItemFirst.CmdParam = Bits.MAKELONG(3000, 6000);
                }
                else
                    await GetMessageSize(ItemFirst.MsgID);
            }
        }

        private void SaveCallBack(OBJ_ID? NewMsgId = null)
        {
            IsNewMessage = false;
            ViewInfoMessage = false;
        }

        private void OnChangeSelectInput(ChangeEventArgs e)
        {
            int.TryParse(e.Value?.ToString(), out int iValue);

            if (SelectItem == null)
                return;

            if (iValue <= 0x0000)
                iValue = 1;
            else if (iValue >= 0x0FFF)
                iValue = 4094;
            SelectItem.Param2 = Bits.MAKELONG(iValue, Bits.HIWORD(SelectItem.Param2));// Установить номер фонограммы на ЭПУ

            SelectItem.CmdParam = Bits.MAKELONG(Bits.LOWORD(SelectItem.CmdParam), 0);

            SelectItem.Param1 = Bits.RESET_BIT(SelectItem.Param1, 18);
            SelectItem.MsgID = SelectItem.MsgStaffID = 0;
        }

        private string GetNameFull(CGetSitItemInfo item)
        {
            string response = item.Name;

            if (item.SZSGroupID > 0)
            {
                response = $"{item.Name} ({UUZSRep["IDS_STRING_GROUP"]} №{item.SZSGroupID})";
            }
            else
            {
                switch (item.DevType)
                {
                    case 1: response = $"{item.Name} ({SMDataRep["SUBSYST_SZS1"]})"; break;
                    case 2: response = $"{item.Name} ({SMDataRep["SUBSYST_SZS2"]})"; break;
                    case 3: response = $"{item.Name} ({SMDataRep["SUBSYST_SZS3"]})"; break;
                }
            }
            return response;
        }

        private string GetLengthMsg(CGetSitItemInfo item)
        {
            string response = "---";

            var m = (Bits.HIWORD(item.CmdParam) / 20) / 60;
            var s = (Bits.HIWORD(item.CmdParam) / 20) % 60;

            if (m > 0 || s > 0)
            {
                response = $"{m.ToString("D2")}:{s.ToString("D2")}";
            }
            return response;
        }

        private void OnChangeInput(ChangeEventArgs e, int wNotifyCode)
        {
            int.TryParse(e.Value?.ToString(), out int iValue);

            switch (wNotifyCode)
            {
                case 0:
                {
                    if (iValue <= 0x0000)
                        iValue = 1;
                    else if (iValue >= 0x0FFF)
                        iValue = 4094;
                    ItemFirst.Param2 = Bits.MAKELONG(iValue, Bits.HIWORD(ItemFirst.Param2));// Установить номер фонограммы на ЭПУ

                    ItemFirst.MsgID = 0;
                    ItemFirst.MsgStaffID = 0;

                    ItemFirst.CmdParam = Bits.MAKELONG(Bits.LOWORD(ItemFirst.CmdParam), 0);
                }
                break;

                case 1:
                {

                    ItemFirst.CmdParam = Bits.MAKELONG((Bits.LOWORD(ItemFirst.CmdParam) / 50) * 50 + iValue / 50, Bits.HIWORD(ItemFirst.CmdParam));
                }
                break;
            }
        }

        /// <summary>
        /// Событие при выборе сообщения
        /// </summary>
        /// <param name="e"></param>
        private async Task OnChangeMsg(ChangeEventArgs e)
        {
            int.TryParse(e.Value?.ToString(), out int iValue);

            if (Bits.CHECK_BIT(ItemFirst.Param1, 18))
            {
                await GetMessageSize(iValue);
            }
        }

        private async Task OnChangeSelectMsg(ChangeEventArgs e)
        {
            int.TryParse(e.Value?.ToString(), out int iValue);

            if (SelectItem == null)
                return;

            SelectItem.Param1 = Bits.SET_BIT(SelectItem.Param1, 18);
            SelectItem.Param2 = Bits.MAKELONG(0, Bits.HIWORD(SelectItem.Param2));

            await GetMessageSize(iValue);
        }


        private string GetColorTd(int dwSubItem, int param1)
        {
            string response = "info";

            var wZoneMask = Bits.LOWORD(param1);

            if (((1 << (dwSubItem - 1)) & wZoneMask) > 0)
            {
                response = "danger";
            }
            return response;
        }


        private void SetColorTd(int dwSubItem, int devId, int GroupId)
        {
            var f = SzsList?.FirstOrDefault(x => x.SZSGroupID == GroupId && x.SZSDevID == devId);

            if (f == null)
                return;

            var w = Bits.BIT_INVERT(Bits.LOWORD(f.Param1), (1 << (dwSubItem - 1)));
            uint u = (uint)f.Param1;
            u &= 0xFFFF0000;
            u |= (uint)w;

            f.Param1 = (int)u;
        }

        private async Task GetMessageSize(int MsgId)
        {
            int LengthMsg = 0;
            OBJ_ID request = new();
            if (MsgId > 0)
            {
                request = MsgList?.FirstOrDefault(x => x.OBJID.ObjID == MsgId)?.OBJID ?? new();

                var result = await Http.PostAsJsonAsync("api/v1/GetMessageShortInfo", request);
                if (result.IsSuccessStatusCode)
                {
                    var m = MsgParam.Parser.ParseJson(await result.Content.ReadAsStringAsync());
                    if (m?.MsgType == (int)MessageType.MessageSound || m?.MsgType == (int)MessageType.MessageSoundAndText)
                    {
                        WavHeaderModel format = new(m.Format.Memory.ToArray());
                        LengthMsg = (int)(format.SubChunk / format.ByteRate);
                    }
                }
                else
                {
                    MessageView?.AddError("", GsoRep["IDS_EMESSAGEOPEN"]);
                }
            }

            if (!bPvsOn)
            {
                ItemFirst.CmdParam = Bits.MAKELONG(Bits.LOWORD(ItemFirst.CmdParam), LengthMsg * 20);
                if (SelectItem != null)
                {
                    SelectItem.CmdParam = Bits.MAKELONG(Bits.LOWORD(SelectItem.CmdParam), LengthMsg * 20);
                }
            }

            if (IsIndividualMode == true)
            {
                if (SelectItem != null)
                {
                    SelectItem.MsgID = request.ObjID;
                    SelectItem.MsgStaffID = request.StaffID;
                    SelectItem.Param2 = Bits.MAKELONG(0, Bits.HIWORD(SelectItem.Param2));// Установить номер фонограммы на ЭПУ
                }
            }
            else
            {
                ItemFirst.MsgID = request.ObjID;
                ItemFirst.MsgStaffID = request.StaffID;
                ItemFirst.Param2 = Bits.MAKELONG(0, Bits.HIWORD(ItemFirst.Param2));// Установить номер фонограммы на ЭПУ
            }
            StateHasChanged();
        }

        private void ChangeSirenaMode(ChangeEventArgs e)
        {
            int.TryParse(e.Value?.ToString(), out int TypeId);

            IDC_SIRENA_TYPE = TypeId;

            if (IDC_SIRENA_TYPE == 0)
            { // Прерывистый режим сирены "ВНИМАНИЕ ВСЕМ"
                var wPlateNum = ItemFirst.Param2; // Номер фонограммы с ЭПУ
                if (ItemFirst.Param2 == 0x0FFF) // Если был включен режим звучания сирен "ВОЗДУШНАЯ ТРЕВОГА", то включить режим трансляции по каналу связи без сообщения
                {
                    ItemFirst.Param1 = Bits.SET_BIT(ItemFirst.Param1, 18);

                    ItemFirst.MsgID = ItemFirst.MsgStaffID = 0;

                    ItemFirst.Param2 = Bits.MAKELONG(0, Bits.HIWORD(ItemFirst.Param2));// Установить номер фонограммы на ЭПУ
                }
                else if (!Bits.CHECK_BIT(ItemFirst.Param1, 18))
                { // Сообщение с ЭПУ
                    if (wPlateNum <= 0)
                    {
                        wPlateNum = 1; // Минимальный номер фонограммы 1
                    }
                    else if (wPlateNum >= 0xFFF)
                    {
                        wPlateNum = 0x0FFE; // Максимальный номер фонограммы 4094
                    }
                    ItemFirst.Param2 = Bits.MAKELONG(wPlateNum, Bits.HIWORD(ItemFirst.Param2));// Установить номер фонограммы на ЭПУ
                }
            }
            else if (IDC_SIRENA_TYPE == 1)
            { // Непрерывный режим сирены "ВОЗДУШНАЯ ТРЕВОГА"
                ItemFirst.Param1 = Bits.RESET_BIT(ItemFirst.Param1, 18);
                ItemFirst.Param2 = Bits.MAKELONG(0x0FFF, Bits.HIWORD(ItemFirst.Param2));// Установить "СПЕЦИАЛИЗИРОВАННЫЙ" номер фонограммы на ЭПУ
            }

        }

        private async Task<int> GetCmdID()
        {
            var result = await GetCommandList(SubsystemType.SUBSYST_SZS);

            if (result != null && result.Any())
            {
                return result.First().CmdID;
            }
            return 0;
        }

        private async Task<List<CCmdInfo>?> GetCommandList(int ObjId)
        {
            OBJ_ID request = new OBJ_ID() { ObjID = ObjId, SubsystemID = SubsystemType.SUBSYST_SZS };
            var result = await Http.PostAsJsonAsync("api/v1/GetCmdList", request);
            if (result.IsSuccessStatusCode)
            {
                return await result.Content.ReadFromJsonAsync<List<CCmdInfo>>();
            }
            else
            {
                MessageView?.AddError("", GsoRep["IDS_ERRORGETCMDLIST"]);
            }
            return null;
        }

        private async Task GetMsgList()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetObjects_IMessage", new OBJ_ID() { StaffID = StaffId });

            if (result.IsSuccessStatusCode)
            {
                MsgList = await result.Content.ReadFromJsonAsync<List<Objects>>() ?? new();
            }

            if (MsgList == null)
                MsgList = new();

            MsgList.RemoveAll(x => x.OBJID == null || x.Type == (int)MessageType.MessageText);
        }

        private async Task Next()
        {
            IsProcessing = true;
            if (IsIndividualMode == true)
            {
                if (SzsList != null && !ZoneList.Any())
                {
                    if (SzsList.Any(x => x.ZoneCount == 0 && x.SZSGroupID == 0))
                    {
                        var r = await GetZonesCount();

                        if (r.Any())
                        {
                            foreach (var item in SzsList.Where(x => x.ZoneCount == 0 && x.SZSGroupID == 0))
                            {
                                item.ZoneCount = r.FirstOrDefault(x => x.DevID == item.SZSDevID)?.ZoneCount ?? 15;
                            }
                        }
                    }
                    foreach (var item in SzsList.Where(x => x.SZSDevID > 0 && x.SZSGroupID == 0))
                    {
                        ZoneList.AddRange(await GetZonesInfo(new OBJ_ID() { ObjID = item.SZSDevID, StaffID = item.SZSDevStaffID }));
                    }
                }

                IsNext = true;
            }
            else
            {
                await SaveSzs();
            }
            IsProcessing = false;
        }

        private async Task Cancel()
        {
            await SaveSit();
        }

        private async Task SavePrd()
        {
            if (PrdItem == null)
            {
                MessageView?.AddError("", SMP16xRep["IS_NULL_MODEL"]);
                return;
            }

            if (PrdItem.CmdID == 0)
            {
                MessageView?.AddError("", SMP16xRep["SELECT_COMMAND"]);
                return;
            }

            if (PrdItem.CmdParam == 0)
            {
                MessageView?.AddError("", SMP16xRep["SELECT_NUMBER_COMMAND"]);
                return;
            }

            if (PrdItem.MsgID > 0)
            {
                PrdItem.MsgStaffID = MsgList?.FirstOrDefault(x => x.OBJID?.ObjID == PrdItem.MsgID)?.OBJID?.StaffID ?? 0;
            }

            ObjectList?.ForEach(x =>
            {
                if (x.DevType == SubsystemType.SUBSYST_PRD)
                {
                    x.CmdID = PrdItem.CmdID;
                    x.CmdParam = PrdItem.CmdParam;
                    x.MsgID = PrdItem.MsgID;
                    x.MsgStaffID = PrdItem.MsgStaffID;
                    x.CmdSubsystemID = SubsystemType.SUBSYST_SZS;
                }
            });

            if (SzsList == null)
            {
                if (PrdItem.Equals(OldPrdItem))
                    await SaveSit();
                else
                    await SaveSit(ObjectList?.Where(x => x.DevType == SubsystemType.SUBSYST_PRD).ToList());
            }
            IsCreatePrd = false;
        }

        private async Task SaveSzs()
        {
            IsNext = false;

            var CmdID = await GetCmdID();
            if (CmdID == 0)
                return;

            if (!(await CheckParams()))
                return;

            if (ItemFirst.CmdID == 0)
            {
                ItemFirst.CmdID = CmdID;
                ItemFirst.CmdSubsystemID = SubsystemType.SUBSYST_SZS;
            }

            if (SzsList != null)
            {
                SzsList.ForEach(x =>
                {
                    TimeOnly msgLength = new(0, (Bits.HIWORD(x.CmdParam) / 20) / 60, (Bits.HIWORD(x.CmdParam) / 20) % 60);
                    var param1 = x.Param1;
                    x.CmdID = ItemFirst.CmdID;
                    x.CmdSubsystemID = ItemFirst.CmdSubsystemID;
                    x.CmdParam = ItemFirst.CmdParam;
                    x.Param1 = ItemFirst.Param1;
                    if (IsIndividualMode == false)
                    {
                        x.MsgID = ItemFirst.MsgID;
                        x.MsgStaffID = ItemFirst.MsgStaffID;
                        x.Param2 = ItemFirst.Param2;
                    }
                    else
                    {

                        var w = Bits.LOWORD(param1);
                        uint u = (uint)x.Param1;
                        u &= 0xFFFF0000;
                        u |= (uint)w;
                        x.Param1 = (int)u;

                        if (x.MsgID > 0)
                        {
                            x.Param1 = Bits.SET_BIT(x.Param1, 18);
                            x.CmdParam = Bits.MAKELONG(Bits.LOWORD(x.CmdParam), (msgLength.Minute * 60 + msgLength.Second) * 20);
                        }
                        else
                        {
                            x.Param1 = Bits.RESET_BIT(x.Param1, 18);
                            x.MsgStaffID = 0;
                        }
                    }
                });

                ObjectList?.ForEach(x =>
                {
                    if (x.DevType != SubsystemType.SUBSYST_PRD)
                    {
                        var elem = SzsList.FirstOrDefault(s => s.SZSDevID == x.SZSDevID && s.SZSDevStaffID == x.SZSDevStaffID && s.SZSGroupID == x.SZSGroupID && s.SZSGroupStaffID == x.SZSGroupStaffID);
                        if (elem != null)
                        {
                            x.CmdID = elem.CmdID;
                            x.CmdParam = elem.CmdParam;
                            x.MsgID = elem.MsgID;
                            x.MsgStaffID = elem.MsgStaffID;
                            x.CmdSubsystemID = elem.CmdSubsystemID;
                            x.Param1 = elem.Param1;
                            x.Param2 = elem.Param2;
                            x.ZoneCount = elem.ZoneCount;
                        }
                    }
                });
            }

            await SaveSit(ObjectList);


        }

        bool IsViewMessage
        {
            get
            {
                if (IsCreatePrd)
                {
                    return PrdItem?.MsgID > 0;
                }
                else
                {
                    if (IsIndividualMode == true)
                    {
                        return SelectItem?.MsgID > 0;
                    }
                    else
                    {
                        return ItemFirst.MsgID > 0;
                    }
                }
            }
        }

        int? GetCurrentMsgId
        {
            get
            {
                if (IsNewMessage)
                    return null;

                if (IsCreatePrd)
                {
                    return PrdItem?.MsgID;
                }
                else
                {
                    if (IsIndividualMode == true)
                    {
                        return SelectItem?.MsgID;
                    }
                    else
                    {
                        return ItemFirst.MsgID;
                    }
                }
            }
        }

        private async Task<bool> CheckParams()
        {
            if (SzsList == null)
                return false;

            // ПРОВЕРКА на непротиворечивость введенных данных

            // 2 минуты 45 секунд
            if (Bits.CHECK_BIT(ItemFirst.Param1, 17) && Bits.LOWORD(ItemFirst.CmdParam) / 20 > 165)
            {

                WARNING_LENGTH_TIME_SIRENA = 1;

                while (WARNING_LENGTH_TIME_SIRENA == 1)
                {
                    await Task.Delay(100);
                }

                if (WARNING_LENGTH_TIME_SIRENA == 2)
                {
                    WARNING_LENGTH_TIME_SIRENA = 0;
                }
                else
                    return false;
            }

            if (IsIndividualMode == true)
            {

                foreach (var item in SzsList)
                {
                    if (item.DevType == 1 && Bits.LOWORD(item.CmdParam) == 0)
                    {
                        MessageView?.AddError(GetNameFull(item), UUZSRep["ERROR_SZS1_SOUND"]);
                        return false;
                    }
                    else if (item.Param2 == 0 && item.MsgID == 0)
                    {
                        MessageView?.AddError(GetNameFull(item), UUZSRep["ERROR_SZS_MSG"]);
                        return false;
                    }
                    else if (item.Param2 != 0 && item.MsgID != 0)
                    {
                        MessageView?.AddError(GetNameFull(item), UUZSRep["ERROR_REPEAT_MSG"]);
                        return false;
                    }
                }
            }
            else
            // Сообщение с ЭПУ не задано
            if (!Bits.CHECK_BIT(ItemFirst.Param1, 19) && !Bits.CHECK_BIT(ItemFirst.Param1, 20) && !Bits.CHECK_BIT(ItemFirst.Param1, 18) && (Bits.LOWORD(ItemFirst.Param2) <= 0 || Bits.LOWORD(ItemFirst.Param2) > 4095))
            {
                MessageView?.AddError("", UUZSRep["ERROR_NUMBER_FONOGRAMM"]);
                return false;
            }

            return true;
        }

        private async Task<List<CSubDevice>> GetZonesInfo(OBJ_ID request)
        {
            List<CSubDevice> response = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetDeviceSubDevice", request);
            if (result.IsSuccessStatusCode)
            {
                response = await result.Content.ReadFromJsonAsync<List<CSubDevice>>() ?? new();
            }
            else
            {
                MessageView?.AddError("", UUZSRep["IDS_STRING_ERR_GET_DATA"]);
            }
            return response;
        }

        private async Task<List<CZoneInfo>> GetZonesCount()
        {
            List<CZoneInfo> response = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetZoneCount", new IntID() { ID = StaffId });
            if (result.IsSuccessStatusCode)
            {
                response = await result.Content.ReadFromJsonAsync<List<CZoneInfo>>() ?? new();
            }
            else
            {
                MessageView?.AddError("", UUZSRep["IDS_STRING_ERR_GET_DATA"]);
            }
            return response;
        }

        private async Task SaveSit(List<CGetSitItemInfo>? NewStaffList = null)
        {
            if (NextAction.HasDelegate)
                await NextAction.InvokeAsync(NewStaffList);
        }
        public ValueTask DisposeAsync()
        {
            return _HubContext.DisposeAsync();
        }
    }
}
