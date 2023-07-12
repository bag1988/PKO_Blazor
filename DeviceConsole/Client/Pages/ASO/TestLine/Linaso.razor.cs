using System.ComponentModel;
using System.Net.Http.Json;
using AsoCommonProto.V1;
using Google.Protobuf;
using AsoNlServiceProto.V1;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.PuSubModel;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;

namespace DeviceConsole.Client.Pages.ASO.TestLine
{
    partial class Linaso : IAsyncDisposable, IPubSubMethod
    {
        enum ASORetCode
        {
            ASO_RC_UNDEFINED_ANSWER = 0x00, // неизвестный ответ
            ASO_RC_ANSWER_TICKER = 0x01, // ответ с подтверждением тикером
            ASO_RC_ANSWER = 0x02, // ответ без подтверждения
            ASO_RC_NOANSWER = 0x03, // абонент не ответил
            ASO_RC_BUSY = 0x04, // абонент занят
            ASO_RC_NOTREADYLINE = 0x05, // нет тона в линии
            ASO_RC_BUSYCHANNEL = 0x06, // канал занят
            ASO_RC_READY = 0x07, // канал свободен и готов к работе
            ASO_RC_HANDSET_REMOVED = 0x08, // трубка снята абонентом
            ASO_RC_ANSWER_DTMF = 0x09, // получен DTMF ответ
            ASO_RC_ERROR_ATS = 0x0A, // ошибка соединения (нет тона после набора номера)
            ASO_RC_INTER_ERROR = 0x0B, // внутренняя ошибка контроллера (АСО-М)
            ASO_RC_BREAK_ANSWER = 0x0C, // рано положена трубка (сообщение не дослушано до конца)
            ASO_RC_ANSWER_SETUP = 0x0D, // параметр принят / подтверждение приема цифры
            ASO_RC_ANSWER_FAX = 0x0E, // ответил факс
            ASO_RC_NOTCONTROLLER = 0x0F, // контроллер отсутствует, или нет связи с блоком АСО
            ASO_RC_ANSWER_COMMIT = 0x10, // Ответ с подтверждением (DCOM)
            ASO_RC_ABON_DISCLAIM = 0x11, // Абонент отказался от соединения
            ASO_RC_LINE_CONNECT = 0x17, // трубка снята (АСО)

            CHANNEL_ATTACH = 101,//подключен
            CHANNEL_DETACH = 102,//отключен

            ASO_RC_NOFREECHANNEL = -2147483616, // нет свободных каналов
            ASO_RC_NOCHANNELS = -2147483615, // нет каналов
            ASO_RC_NOLOADNEWMESSAGE = -2147483614, // невозможно загрузить новое звуковое сообщение в связи
                                                   // с неоконченными оповещениями с предыдущим сообщением
            ASO_RC_ERRORLOADMESSAGE = -2147483613, // ошибка загрузки звукового сообщения
            ASO_RC_ERRORSETCONNECT = -2147483612, // ошибка соединения с абонентом
            ASO_RC_NOFREECHANNELGLOBALMASK = -2147483611, // нет свободных каналов с глобальной маской
            ASO_RC_NOFREECHANNELUSERMASK = -2147483610, // нет свободных каналов с пользовательской маской
            ASO_RC_NOMESSAGE = -2147483609, // нет сообщения (сообщение с ID = 0)
                                            //
                                            // Доставка сообщения через промежуточный сервер
            ASO_RC_ERROR_SERVER_CONNECT = -2147483392, // Ошибка соединения с сервером
            ASO_RC_ERROR_SERVER_LOGIN = -2147483391, // Ошибка на сервере
            ASO_RC_ERROR_SERVER_DELIVERY = -2147483390, // Ошибка доставки сообщения
                                                        //
                                                        // коды возврата AsoInit
            ASO_RC_INIT_ERR_SOUND_DEVICE = -2146437323, // ошибка инициализации звукового устройства
            ASO_RC_INIT_NULL_DATA,                      // отсутствуют некоторые из входных данных, описывающих блок АСО
            ASO_RC_INIT_LPT_DISABLE,                    // LPT устройство в системе отсутствует
            ASO_RC_INIT_ASODRV_DISABLE,                 // драйвер AsoDrv для заданного порта отсутствует
            ASO_RC_INIT_LPT_BUSY,                       // LPT порт занят другим устройством
            ASO_RC_INIT_INNER_ERROR,                    // внутренняя ошибка ЛУ
            ASO_RC_INIT_UNKNOWN_CONN_TYPE,              // неизвестный тип устройства
            ASO_RC_INIT_UNKNOWN_CONTRL_TYPE,            // неизвестный тип контроллера
            ASO_RC_INIT_ERR_MAINTHREAD_ID,              // некорректный ID главного потока ЛО
            ASO_RC_INIT_CONTRL_MISMATCH,                // ошибка сопоставления контроллера и линии
            ASO_RC_INIT_DEVICE_MISMATH,                 // ошибка сопоставления устройства и линии
            ASO_RC_INIT_HWCONNECT_ERROR,                // нет связи с блоком
            ASO_RC_INIT_PROTECT_FAILURE,                // Ошибка проверки защиты (HASP KEY NOT PRESENT OR INVALID KEY)
            ASO_RC_INIT_SUCCESS = 0x00000000, // Успешная инициализация ЛУ
            ASO_RC_INIT_ALREADY = 0x00000001, // Успешная инициализация ЛУ была ранее произведена
            NumASORetCode
        }

        enum NotifyState
        {
            NotActive = 0,
            Starting,
            Processing,
            Stopping,
        }

        NotifyState m_NotifyState = NotifyState.NotActive;

        class LineInfoEx
        {
            public string? line_name { get; set; }
            public string? AnswerNl { get; set; }
            public DeviceDescription? device { get; set; }
            public LineDescription? line { get; set; }
            public ControllerDescription? contr { get; set; }
        }

        string? Phone { get; set; }

        int Message { get; set; }
        uint RepeatCount { get; set; } = 1;
        bool PasswordConfirm { get; set; } = false;
        List<DeviceDescription>? DeviceDescriptContainer { get; set; }
        List<ControllerDescription>? ControllDescriptContainer { get; set; }
        List<LineDescription>? LineDescriptContainer { get; set; }
        List<Objects>? msgList { get; set; }
        List<LineInfoEx>? line_list { get; set; }

        List<LineInfoEx>? SelectItems { get; set; }

        readonly int[] SupportedConnect = new int[] { 0x08, 0x02, 0x04, 0x0C, 0x0A, 0x0B, 0x0E };

        string StatusStr = "";

        int StaffId { get; set; }

        bool IsStartLine = false;

        bool IsProcessing = false;

        protected override async Task OnInitializedAsync()
        {
            StaffId = await _User.GetLocalStaff();
            await GetDeviceDescript();
            await GetControllerDescript();
            await GetLineDescript();
            await GetMessageList();
            _ = OnGetLineInfo();

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_StopNl(OnStopNL Value)
        {
            StatusStr = AsoRep["DONE"];
            SetNotifyState(NotifyState.NotActive);
            StateHasChanged();
        }

        [Description(DaprMessage.PubSubName)]
        public Task Fire_OnMessageSignal(AsoNlReplaceCode Value)
        {
            StatusStr = AsoRep[Value.ToString()];
            StateHasChanged();
            return Task.CompletedTask;
        }

        [Description(DaprMessage.PubSubName)]
        public Task Fire_OnErrorSignal(string Value)
        {
            MessageView?.AddError("", Value);
            return Task.CompletedTask;
        }

        [Description(DaprMessage.PubSubName)]
        public Task Fire_OnAnswerSignal(OnAnswerSignal Value)
        {
            if (line_list?.Any(x => x.line?.DwLineID == Value.LineId) ?? false)
            {
                var elem = line_list.First(x => x.line?.DwLineID == Value.LineId);

                elem.AnswerNl = GetErrorString(Value.Code);

                if ((int)Value.Code == (int)ASORetCode.ASO_RC_ANSWER_DTMF)
                    elem.AnswerNl += ": " + Value.Answer;

                StateHasChanged();
            }
            return Task.CompletedTask;
        }

        [Description(DaprMessage.PubSubName)]
        public Task Fire_OnStateLineSignal(OnStateLineSignal Value)
        {
            if (line_list?.Any(x => x.line?.DwLineID == Value.LineId) ?? false)
            {
                var elem = line_list.First(x => x.line?.DwLineID == Value.LineId);
                switch (Value.State)
                {
                    case (int)ASORetCode.ASO_RC_BUSYCHANNEL: elem.AnswerNl = AsoRep["IS_DIAL_UP"]; break;
                    case (int)ASORetCode.ASO_RC_READY: elem.AnswerNl = AsoRep["DIALING_NUMBER"]; break;
                    case (int)ASORetCode.ASO_RC_HANDSET_REMOVED: elem.AnswerNl = AsoRep["MESSAGE_BROADCAST"]; break;
                    case (int)ASORetCode.CHANNEL_ATTACH: elem.AnswerNl = AsoRep["CHANNEL_ATTACH"]; break;
                    case (int)ASORetCode.CHANNEL_DETACH: elem.AnswerNl = AsoRep["CHANNEL_DETACH"]; break;
                }

                StateHasChanged();
            }
            return Task.CompletedTask;
        }

        private async Task OnGetLineInfo()
        {
            if (line_list == null)
                line_list = new();
            if (DeviceDescriptContainer == null || LineDescriptContainer == null || ControllDescriptContainer == null)
                return;

            foreach (var i_Line in LineDescriptContainer)
            {
                if (ComponentDetached.IsCancellationRequested)
                    return;
                if (!Convert.ToBoolean(i_Line.BEnable))
                    continue;

                foreach (var i_pDeviceDescript in DeviceDescriptContainer)
                {
                    if (ComponentDetached.IsCancellationRequested)
                        return;
                    if (i_Line.DwDeviceID == i_pDeviceDescript.DeviceID)
                    {
                        if (Convert.ToBoolean(i_pDeviceDescript.Status))
                        {
                            foreach (var i_controller in ControllDescriptContainer)
                            {
                                if (ComponentDetached.IsCancellationRequested)
                                    return;
                                if (i_controller.DeviceID == i_Line.DwDeviceID && i_Line.DwChannelIndex > (i_controller.DeviceIndex - 1) * i_pDeviceDescript.ChannelsCount
                                    && i_Line.DwChannelIndex <= i_controller.DeviceIndex * i_pDeviceDescript.ChannelsCount)
                                {
                                    if (i_controller.Enable)
                                    {
                                        var l = await GetLineInfo(i_Line.DwLineID);
                                        if (l != null)
                                        {
                                            line_list.Add(new LineInfoEx()
                                            {
                                                line_name = l.LineName,
                                                line = i_Line,
                                                contr = i_controller,
                                                device = i_pDeviceDescript
                                            });
                                            StateHasChanged();
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }

        private async Task<Line?> GetLineInfo(int lineID)
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetLineInfo", new IntID() { ID = lineID }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                return await result.Content.ReadFromJsonAsync<Line>();
            }
            return null;
        }

        private async Task GetDeviceDescript()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetDeviceDescript", new OBJ_ID());
            if (result.IsSuccessStatusCode)
            {
                DeviceDescriptContainer = await result.Content.ReadFromJsonAsync<List<DeviceDescription>>();
            }

            if (DeviceDescriptContainer == null)
            {
                MessageView?.AddError("", AsoRep["NOT_DEVICE"]);
                DeviceDescriptContainer = new();
            }
        }

        private async Task GetControllerDescript()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetControllerInfo", new IntID());
            if (result.IsSuccessStatusCode)
            {
                ControllDescriptContainer = await result.Content.ReadFromJsonAsync<List<ControllerDescription>>();
            }

            if (ControllDescriptContainer == null)
            {
                MessageView?.AddError("", AsoRep["NOT_CONTROLLER"]);
                ControllDescriptContainer = new();
            }
        }

        private async Task GetLineDescript()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetLineInfoAso", new IntID());
            if (result.IsSuccessStatusCode)
            {
                LineDescriptContainer = await result.Content.ReadFromJsonAsync<List<LineDescription>>();
            }

            if (LineDescriptContainer == null)
            {
                MessageView?.AddError("", AsoRep["NOT_LINE"]);
                LineDescriptContainer = new();
            }
        }

        private async Task GetMessageList()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetObjects_IMessage", new OBJ_ID() { StaffID = StaffId, SubsystemID = SubsystemType.SUBSYST_ASO });
            if (result.IsSuccessStatusCode)
            {
                msgList = await result.Content.ReadFromJsonAsync<List<Objects>>();
            }

            if (msgList == null)
            {
                MessageView?.AddError("", AsoRep["ERROR_GET_MESSAGE"]);
                msgList = new();
            }

            Message = msgList.FirstOrDefault()?.OBJID?.ObjID ?? 0;
        }

        async Task OnBnClickedOk()
        {
            IsProcessing = true;
            if (m_NotifyState == NotifyState.Processing)
            {
                StatusStr = AsoRep["ABORTED"];
                SetNotifyState(NotifyState.Stopping);
                await StopTestLine();
                SetNotifyState(NotifyState.NotActive);
                StatusStr = AsoRep["DONE"];
                StateHasChanged();
                IsProcessing = false;
                return;
            }

            if (string.IsNullOrEmpty(Phone))
            {
                MessageView?.AddError("", AsoRep["ERROR_NUMBER_ABON"]);
            }
            else if (!msgList?.Any(x => x.OBJID?.ObjID == Message) ?? true)
            {
                MessageView?.AddError("", AsoRep["ERROR_SELECT_MESSAGE"]);
            }
            else if (RepeatCount == 0 || RepeatCount > 50)
            {
                MessageView?.AddError("", AsoRep["ERROR_REPEAT_COUN"]);
            }
            else
            {
                line_list?.ForEach(x =>
                {
                    x.AnswerNl = string.Empty;
                });
                try
                {
                    if (SelectItems?.Count > 0)
                    {
                        var msgID = msgList?.FirstOrDefault(x => x.OBJID?.ObjID == Message)?.OBJID ?? new();
                        var b = await StartTestLine(SelectItems.Select(item => new TestConnectionConfiguration() {
                            Phone = Phone
                          , LineId = (uint) (item.line?.DwLineID ?? 0)
                          , CountCall = 1
                          , MessageId = msgID
                          , MessageRepeatCount = RepeatCount
                          , DTMF = PasswordConfirm
                        }));
                        if (b && SelectItems.Any(x => x.line?.DwLineID > 0))
                        {
                            StatusStr = AsoRep["TESTING_STARTED"];
                            SetNotifyState(NotifyState.Processing);
                        }
                    }
                }
                finally
                {
                    StateHasChanged();
                }
            }
            IsProcessing = false;

        }


        private async Task<bool> StopTestLine()
        {
            var result = await Http.PostAsync("api/v1/StopTestLine", null);
            if (!result.IsSuccessStatusCode)
            {
                MessageView?.AddError("", AsoRep["ERROR_STOP_TEST_LINE"]);
                return false;
            }
            IsStartLine = false;
            return true;
        }

        private async Task<bool> StartTestLine(IEnumerable<TestConnectionConfiguration> lineItems)
        {
            try
            {
                TestNotificationConfiguration request = new();
                request.TestConnectionConfigurationArray.AddRange(lineItems);

                var result = await Http.PostAsJsonAsync("api/v1/StartTestLine", JsonFormatter.Default.Format(request));
                if (!result.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException();
                }
                IsStartLine = true;
                return true;
            }
            catch
            {
                MessageView?.AddError("", AsoRep["ERROR_START_TEST_LINE"]);
                return false;
            }
        }

        void SetNotifyState(NotifyState dwState)
        {
            m_NotifyState = dwState;
            switch (dwState)
            {
                //case NotifyState.NotActive: _tcscpy_s(tLabel, _T("Начать")); break;
                case NotifyState.Starting:
                {
                    StatusStr = AsoRep["STARTING"];
                }
                break;

                //case NotifyState.Processing: _tcscpy_s(tLabel, _T("Остановить")); break;
                case NotifyState.Stopping:
                {
                    StatusStr = AsoRep["STOPPING"];
                }
                break;
            }
            StateHasChanged();
        }


        void SetSelectItems(List<LineInfoEx>? items)
        {
            SelectItems = items;
        }

        void SelectAll()
        {
            SelectItems = line_list;
        }

        void UnSelectAll()
        {
            SelectItems = null;
        }

        string GetLineInfo(DeviceDescription? device)
        {
            if (device == null)
                return string.Empty;
            string result = "";
            switch (device.AsoTypeConnect)
            {
                case 0x08:/* ASO_TC_USB:*/
                case 0x02:/*ASO_TC_COM:*/ result = $"{device.DeviceName} [COM{device.AsoConnID}]"; break;
                case 0x04:/* ASO_TC_LPT:*/ result = $"{device.DeviceName} [LPT{device.AsoConnID}]"; break;

                case 0x0C:/* ASO_TC_ASOGSM:*/
                case 0x0A:/* ASO_TC_GSMT:*/ result = $"{device.DeviceName} [COM{device.OrderOnPort}]"; break;

                case 0x0B:/* ASO_TC_VOIP:*/ result = $"{device.DeviceName} [ТЛФ: {device.UserName}]"; break;
                case 0x0E:/* ASO_TC_WSDL:*/result = $"{device.DeviceName} [WS : {device.DeviceComm}]"; break;
            }

            return result;
        }

        string GetButtonText
        {
            get
            {
                string result = AsoRep["START"];
                switch (m_NotifyState)
                {
                    case NotifyState.Starting: result = AsoRep["STARTING"]; break;
                    case NotifyState.Processing: result = AsoRep["PROCESSING"]; break;
                    case NotifyState.Stopping: result = AsoRep["STOPPING"]; break;
                }
                return result;
            }
        }


        string GetErrorString(uint code)
        {
            string result = string.Format(AsoRep["ERROR_CODE"], code);

            switch ((int)code)
            {
                case (int)ASORetCode.ASO_RC_UNDEFINED_ANSWER: result = AsoRep[ASORetCode.ASO_RC_UNDEFINED_ANSWER.ToString()]; break;
                case (int)ASORetCode.ASO_RC_ANSWER_TICKER: result = AsoRep[ASORetCode.ASO_RC_ANSWER_TICKER.ToString()]; break;
                case (int)ASORetCode.ASO_RC_ANSWER: result = AsoRep[ASORetCode.ASO_RC_ANSWER.ToString()]; break;
                case (int)ASORetCode.ASO_RC_NOANSWER: result = AsoRep[ASORetCode.ASO_RC_NOANSWER.ToString()]; break;
                case (int)ASORetCode.ASO_RC_BUSY: result = AsoRep[ASORetCode.ASO_RC_BUSY.ToString()]; break;
                case (int)ASORetCode.ASO_RC_NOTREADYLINE: result = AsoRep[ASORetCode.ASO_RC_NOTREADYLINE.ToString()]; break;
                case (int)ASORetCode.ASO_RC_BUSYCHANNEL: result = AsoRep[ASORetCode.ASO_RC_BUSYCHANNEL.ToString()]; break;
                case (int)ASORetCode.ASO_RC_READY: result = AsoRep[ASORetCode.ASO_RC_READY.ToString()]; break;
                case (int)ASORetCode.ASO_RC_HANDSET_REMOVED: result = AsoRep[ASORetCode.ASO_RC_HANDSET_REMOVED.ToString()]; break;
                case (int)ASORetCode.ASO_RC_ANSWER_DTMF: result = AsoRep[ASORetCode.ASO_RC_ANSWER_DTMF.ToString()]; break;
                case (int)ASORetCode.ASO_RC_ERROR_ATS: result = AsoRep[ASORetCode.ASO_RC_ERROR_ATS.ToString()]; break;
                case (int)ASORetCode.ASO_RC_INTER_ERROR: result = AsoRep[ASORetCode.ASO_RC_INTER_ERROR.ToString()]; break;
                case (int)ASORetCode.ASO_RC_BREAK_ANSWER: result = AsoRep[ASORetCode.ASO_RC_BREAK_ANSWER.ToString()]; break;
                case (int)ASORetCode.ASO_RC_ANSWER_SETUP: result = AsoRep[ASORetCode.ASO_RC_ANSWER_SETUP.ToString()]; break;
                case (int)ASORetCode.ASO_RC_ANSWER_FAX: result = AsoRep[ASORetCode.ASO_RC_ANSWER_FAX.ToString()]; break;
                case (int)ASORetCode.ASO_RC_NOTCONTROLLER: result = AsoRep[ASORetCode.ASO_RC_NOTCONTROLLER.ToString()]; break;
                case (int)ASORetCode.ASO_RC_LINE_CONNECT: result = AsoRep[ASORetCode.ASO_RC_LINE_CONNECT.ToString()]; break;
                case (int)ASORetCode.ASO_RC_NOFREECHANNEL: result = AsoRep[ASORetCode.ASO_RC_NOFREECHANNEL.ToString()]; break;
                case (int)ASORetCode.ASO_RC_NOCHANNELS: result = AsoRep[ASORetCode.ASO_RC_NOCHANNELS.ToString()]; break;
                case (int)ASORetCode.ASO_RC_NOLOADNEWMESSAGE: result = AsoRep[ASORetCode.ASO_RC_NOLOADNEWMESSAGE.ToString()]; break;
                case (int)ASORetCode.ASO_RC_ERRORLOADMESSAGE: result = AsoRep[ASORetCode.ASO_RC_ERRORLOADMESSAGE.ToString()]; break;
                case (int)ASORetCode.ASO_RC_ERRORSETCONNECT: result = AsoRep[ASORetCode.ASO_RC_ERRORSETCONNECT.ToString()]; break;
                case (int)ASORetCode.ASO_RC_NOMESSAGE: result = AsoRep[ASORetCode.ASO_RC_NOMESSAGE.ToString()]; break;
                case (int)ASORetCode.ASO_RC_INIT_ERR_SOUND_DEVICE: result = AsoRep[ASORetCode.ASO_RC_INIT_ERR_SOUND_DEVICE.ToString()]; break;
                case (int)ASORetCode.ASO_RC_INIT_NULL_DATA: result = AsoRep[ASORetCode.ASO_RC_INIT_NULL_DATA.ToString()]; break;
                case (int)ASORetCode.ASO_RC_INIT_LPT_DISABLE: result = AsoRep[ASORetCode.ASO_RC_INIT_LPT_DISABLE.ToString()]; break;
                case (int)ASORetCode.ASO_RC_INIT_ASODRV_DISABLE: result = AsoRep[ASORetCode.ASO_RC_INIT_ASODRV_DISABLE.ToString()]; break;
                case (int)ASORetCode.ASO_RC_INIT_LPT_BUSY: result = AsoRep[ASORetCode.ASO_RC_INIT_LPT_BUSY.ToString()]; break;
                case (int)ASORetCode.ASO_RC_INIT_INNER_ERROR: result = AsoRep[ASORetCode.ASO_RC_INIT_INNER_ERROR.ToString()]; break;
                case (int)ASORetCode.ASO_RC_INIT_HWCONNECT_ERROR: result = AsoRep[ASORetCode.ASO_RC_INIT_HWCONNECT_ERROR.ToString()]; break;
                case (int)ASORetCode.ASO_RC_ERROR_SERVER_DELIVERY: result = AsoRep[ASORetCode.ASO_RC_ERROR_SERVER_DELIVERY.ToString()]; break;
                case (int)ASORetCode.ASO_RC_ERROR_SERVER_LOGIN: result = AsoRep[ASORetCode.ASO_RC_ERROR_SERVER_LOGIN.ToString()]; break;
                case (int)ASORetCode.ASO_RC_ERROR_SERVER_CONNECT: result = AsoRep[ASORetCode.ASO_RC_ERROR_SERVER_CONNECT.ToString()]; break;
            }

            return result;
        }

        IEnumerable<LineInfoEx>? GetListItems
        {
            get
            {
                return line_list?.Where(x => SupportedConnect.Contains(x.device?.AsoTypeConnect ?? 0));
            }
        }

        string GetMsgTypeName(int typeMsg)
        {
            if (typeMsg == (int)MessageType.MessageSound)
                return $" [{AsoRep["MESSAGE_SOUND"]}]";
            else if (typeMsg == (int)MessageType.MessageText)
                return $" [{AsoRep["MESSAGE_TEXT"]}]";
            else
                return string.Empty;
        }

        public async ValueTask DisposeAsync()
        {
            if (IsStartLine)
                await StopTestLine();
            DisposeToken();
            await _HubContext.DisposeAsync();
        }
    }
}
