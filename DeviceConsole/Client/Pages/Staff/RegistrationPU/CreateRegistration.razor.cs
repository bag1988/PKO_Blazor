using System.ComponentModel;
using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using SMDataServiceProto.V1;
using StaffDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SharedLibrary.PuSubModel;
using SharedLibrary.Utilities;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;

namespace DeviceConsole.Client.Pages.Staff.RegistrationPU
{
    partial class CreateRegistration : IAsyncDisposable, IPubSubMethod
    {
        [Parameter]
        public int? IdStaff { get; set; }

        [Parameter]
        public bool? IsDelete { get; set; } = false;

        [Parameter]
        public EventCallback<bool?> CallBack { get; set; }

        private List<string>? InfoConnect = null;

        private Registration? Model = null;

        private string? IpAdress = null;
        private int? Port = null;

        private Registration? OldModel = null;

        private List<Restrict>? RestrictList = null;

        private List<Objects> LocationList = new();

        private List<IntAndString> LineTypes = new();

        private List<CGetCUType> CUTypeList = new();

        private List<IntAndString>? ConnTypeList = null;

        private List<StaffConnParams>? SheduleList = null;

        readonly CLineParams LineParams = new() { BaseType = (int)BaseLineType.LINE_TYPE_UNDEF };

        readonly CShConnParams ConnParams = new() { ConnType = (int)BaseLineType.LINE_TYPE_UNDEF, LocationID = new(), Prior = 1 };


        private bool IsStartProssecing = false;

        private int StaffId = 0;
        private int SubsystemID = 0;

        private uint m_RemoteCuStaffID = 0;
        private bool IsCreateConnect = false;
        private bool IsUpdateConnect = false;
        private bool IsAbort = false;

        private bool IsDeleting = false;
        private bool ViewInputPassword = false;
        readonly string EditPassw = ",scnhsthtrb";//быстрыереки
        private string UserPassw = "";

        private string ButtonName = "";

        private uint RelevantTask = 0;
        private enum SMBaseDlgVerbs
        {
            verbNew,
            verbEdit,
            verbDelete,
            verbView,
            verbList,
            verbEdit1
        };

        protected override async Task OnInitializedAsync()
        {
            if (IdStaff > 0)
            {
                if (IsDelete == false)
                {
                    ButtonName = GSOFormRep["IDS_STRING_APPROVE_CU"];
                }
                else
                {
                    ButtonName = GSOFormRep["IDS_STRING_DELETE_CU"];
                }
            }
            else
            {
                ButtonName = GSOFormRep["IDS_STRING_REGISTRATION_CU"];
            }

            StaffId = await _User.GetLocalStaff();
            SubsystemID = SubsystemType.SUBSYST_GSO_STAFF;

            //TitleError = GSOFormRep["IDS_STRING_REGISTRATION_CU"];

            ConnTypeList = new()
            {
                new IntAndString() { Number = 1, Str = GsoRep["Modem"] },//проводной модем
                new IntAndString() { Number = 2, Str = GsoRep["ISDN"] }//ISDN-адаптер
            };

            //await GetSubsystemParam();
            await GetLineTypeList();
            await GetLocationInfo();
            await GetCUType();
            await GetRestrictList();


            if (IdStaff != null)
            {
                await GetRegInfo();
            }
            else
            {
                Model = new();
                Model.OBJID = new OBJ_ID() { SubsystemID = SubsystemID };
            }

            OldModel = new Registration(Model);

            _ = _HubContext.SubscribeAsync(this);

        }

        [Description(DaprMessage.PubSubName)]
        public Task Fire_CmdStatus(CmdStatus request)
        {
            SetCommandStatus(request.StatusCode, request.ErrorCode);

            if (request.StatusCode == (int)StaffDataEnum.CU_Status.REG_END)
            {
                InfoConnect?.Add("-----");
                IsStartProssecing = false;
                StateHasChanged();
            }
            return Task.CompletedTask;
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_RemoteCuStaffID(RemoteCuStaffID request)
        {
            int m_Verb = IdStaff > 0 ? 1 : 0;
            if (IsDelete == true)
                m_Verb = 2;
            m_RemoteCuStaffID = (uint)request.StaffID;
            if ((int)SMBaseDlgVerbs.verbNew == m_Verb && m_RemoteCuStaffID > 0)
            {
                //Вывод сообщения об удачном подключении, предложение сохранить данные подключения
                IsCreateConnect = true;
                return;
            }
            else if ((int)SMBaseDlgVerbs.verbEdit == m_Verb)
            {
                if (Model == null)
                    return;

                //Вывод сообщения об удачном подключении, предложение обновить данные подключения
                if (!OldModel?.Equals(Model) ?? true)
                    IsUpdateConnect = true;
                return;
            }
            else if ((int)SMBaseDlgVerbs.verbDelete == m_Verb)
            {
                await CloseDelete();
            }

            IsStartProssecing = false;
        }

        private async Task UpdateUser()
        {
            if (Model != null && IdStaff > 0)
            {
                await Http.PostAsJsonAsync("api/v1/UpdateUser", new CUpdateUser() { Login = Model.Login, Passw = Model.Passw, StaffID = IdStaff.Value }, ComponentDetached).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        var b = await x.Result.Content.ReadFromJsonAsync<BoolValue>() ?? new();
                        if (b.Value == true)
                        {
                            MessageView?.AddMessage("", GSOFormRep["IDS_SUCCESSCAPTION"]);
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
                });


                await GetStaffShedule();

                bool shedule_present = false;

                if (SheduleList?.Any() ?? false)
                {
                    if (SheduleList.Any(x => x.RDMCLIENTCONNDESC != null && x.RDMCLIENTCONNDESC != null && x.NetBIOSName == Model.UNC && x.NetBIOSName != x.RDMCLIENTCONNDESC.Phone))
                        shedule_present = true;
                }

                if (!shedule_present)
                    MessageView?.AddMessage("", GSOFormRep["IDS_CORRECT_SHEDULE"]);
            }
            IsUpdateConnect = false;
            await CloseModal();

        }

        private async Task SetSheduleInfo()
        {
            CSheduleInfoExMsg request = new();

            request.CSheduleInfo = new()
            {
                Object = new() { ObjID = 0, StaffID = (int)m_RemoteCuStaffID, SubsystemID = SubsystemType.SUBSYST_GSO_STAFF },
                WeekDays = new CWeekDays() { DayType = 0, WeekDay = "1111111" },
                TimeRestr = new()
                {
                    Btime = Duration.FromTimeSpan(new TimeSpan(0, 0, 0)),
                    Etime = Duration.FromTimeSpan(new TimeSpan(23, 59, 59)),
                    TimeType = 0
                },
                ConnParams = ConnParams,
                LineParams = LineParams

            };

            var result = await Http.PostAsJsonAsync("api/v1/SetSheduleInfoStaff", request, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var b = await result.Content.ReadFromJsonAsync<BoolValue>() ?? new();
                if (b.Value == true)
                {
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
            IsCreateConnect = false;
            await CloseModal();
        }


        private async Task GetRegInfo()
        {
            if (IdStaff != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetRegInfo", new IntID() { ID = IdStaff.Value }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    Model = await result.Content.ReadFromJsonAsync<Registration>() ?? new();

                    IpAddressUtilities.ParseEndPoint(Model.UNC, out IpAdress, out Port);

                    if (Model.CUType == 2)
                    {
                        MessageView?.AddError("", GSOFormRep["IDS_CU_ACCESSDEN"]);
                        IsDeleting = true;
                    }
                    if (Model == null || Model.OBJID == null)
                    {
                        IsDeleting = true;
                        return;
                    }

                    OldModel = new Registration(Model);

                }
                else
                {
                    Model = new();
                    MessageView?.AddError("", GSOFormRep["IDS_EFAILGETREGINFO"]);
                }
            }
        }

        private async Task DeleteRegInfo()
        {
            if (ViewInputPassword == false)
            {
                ViewInputPassword = true;
                return;
            }

            if (!EditPassw.Equals(UserPassw))
            {
                MessageView?.AddError("", GSOFormRep["IDS_STRING_PASSWORD_INCORRECT"]);
                return;
            }


            if (IdStaff != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/DeleteRegInfo", new IntID() { ID = IdStaff.Value }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var b = await result.Content.ReadFromJsonAsync<SIntResponse>() ?? new();
                }
                else
                {
                    MessageView?.AddError("", StartUIRep["IDS_ERRORCAPTION"]);
                }
            }
            await CloseDelete();
        }

        private async Task CloseDelete()
        {
            IsDeleting = false;

            await CloseModal();
        }

        private async Task GetCUType()
        {
            var result = await Http.PostAsync("api/v1/GetCUType", null);
            if (result.IsSuccessStatusCode)
            {
                CUTypeList = await result.Content.ReadFromJsonAsync<List<CGetCUType>>() ?? new();
            }
            else
            {
                MessageView?.AddError("", GSOFormRep["IDS_EFAILGETCUTYPE"]);
            }
        }

        private async Task StartConnect()
        {
            if (Model == null)
                return;

            IsStartProssecing = true;

            bool IsOkMpdel = true;
            if (Model.CUType == 0)
            {
                IsOkMpdel = false;
                MessageView?.AddError("", GSOFormRep["IDS_E_CU_TYPE"]);
            }
            if (string.IsNullOrEmpty(Model.Login))
            {
                IsOkMpdel = false;
                MessageView?.AddError("", GSOFormRep["IDS_E_USER"]);
            }
            if (string.IsNullOrEmpty(Model.Passw))
            {
                IsOkMpdel = false;
                MessageView?.AddError("", GSOFormRep["IDS_E_NOPASSW"]);
            }


            var result = await Http.PostAsJsonAsync("api/v1/GetParams", new StringValue() { Value = nameof(ParamsSystem.LocalIpAddress) });
            if (result.IsSuccessStatusCode)
            {
                var g = await result.Content.ReadFromJsonAsync<StringValue>() ?? new();

                if (string.IsNullOrEmpty(g.Value))
                {
                    MessageView?.AddError("", DeviceRep["ERROR_LOCAL_IP"]);
                    IsOkMpdel = false;
                }
            }
            else
            {
                MessageView?.AddError("", DeviceRep["ERROR_GET_LOCAL_IP"]);
                IsOkMpdel = false;
            }


            Model.UNC = IpAdress + (Port > 0 ? $":{Port}" : "");
            IpAddressUtilities.ParseEndPoint(Model.UNC, out IpAdress, out Port);
            if (string.IsNullOrEmpty(IpAdress))
                Model.UNC = string.Empty;

            if (string.IsNullOrEmpty(Model.UNC))
            {
                IsOkMpdel = false;
                MessageView?.AddError("", GSOFormRep["IDS_E_NOUNC"]);
            }

            if (IdStaff > 0)
            {
                await GetStaffShedule();
            }
            else
            {
                ConnParams.ConnParam = Model.UNC;
                if (string.IsNullOrEmpty(ConnParams.ConnParam))
                {
                    IsOkMpdel = false;
                    MessageView?.AddError("", GSOFormRep["IDS_E_SAVE_NOCONNPARAM"]);
                }

                if (IsOkMpdel)
                {
                    StaffConnParams ConnInfo = new();
                    ConnInfo.MsgPack = 0;
                    ConnInfo.DateType = 0;
                    ConnInfo.DayWeek = "0000000";
                    ConnInfo.ETime = new DateTime(1900, 1, 1, 23, 59, 59).ToUniversalTime().ToTimestamp();
                    ConnInfo.BTime = new DateTime(1900, 1, 1).ToUniversalTime().ToTimestamp();

                    RDMCLIENTCONNDESC rdm = new();

                    rdm.Phone = ConnParams.ConnParam;
                    rdm.UserName = Model.Login;
                    rdm.Password = Model.Passw;
                    rdm.Domain = Model.Domain;
                    rdm.DeviceType = ConnParams.DeviceType;
                    rdm.LineType = LineParams.BaseType;//type line
                    rdm.LineID = ConnParams.LineID;
                    rdm.Location = ConnParams.LocationID.ObjID;
                    rdm.LocStaffID = StaffId;// LocationList?.FirstOrDefault(x => x.OBJID.ObjID == ConnParams.LocationID.ObjID)?.OBJID?.StaffID ?? 0;
                    rdm.GlobalRestrictMask = LineParams.GlobalType;
                    rdm.UserRestrictMask = LineParams.UserType;
                    rdm.ClientType = ConnParams.ConnType;

                    SheduleList = new()
                    {
                        new StaffConnParams()
                        {
                            RDMCLIENTCONNDESC = rdm,
                            DateType = 0,
                            DayWeek = "0000000",
                            BTime = new DateTime(1900, 1, 1).ToUniversalTime().ToTimestamp(),
                            ETime = new DateTime(1900, 1, 1, 23, 59, 59).ToUniversalTime().ToTimestamp(),
                            MsgPack = 0
                        }
                    };
                }
            }


            if (!IsOkMpdel)
            {
                IsStartProssecing = false;
                return;
            }


            if (!SheduleList?.Any() ?? true)
            {
                MessageView?.AddError("", GSOFormRep["IDS_E_SHEDULE"]);
                IsStartProssecing = false;
                return;
            }

            await CreateConnect();
            IsStartProssecing = false;
        }


        private async Task CreateConnect()
        {
            var m_psaShed = SheduleList?.FirstOrDefault();

            RemoteConnect request = new();
            request.ConnParams = m_psaShed;
            request.UserName = Model?.Login;
            request.Password = Model?.Passw;
            request.IpAdress = Model?.UNC;
            request.CuType = Model?.CUType;
            request.ReceiverStaffID = IdStaff;
            request.IsHoliday = await IsHoliday();

            if (IdStaff > 0)
            {
                if (IsDelete == false)
                {
                    request.TypeProssec = RemoteCmdType.CMD_UPREG;
                }
                else
                {
                    request.TypeProssec = RemoteCmdType.CMD_DELREG;
                }
            }
            else
            {
                request.TypeProssec = RemoteCmdType.CMD_REGIST;
            }

            var result = await Http.PostAsJsonAsync("api/v1/DoRegistration", request, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                RelevantTask = await result.Content.ReadFromJsonAsync<uint>();
            }

        }

        //private async Task DeleteRegistration(RemoteConnect request)
        //{
        //    await Http.PostAsJsonAsync("api/v1/DeleteRegistration", request).ContinueWith(async x =>
        //    {
        //        if (x.Result.IsSuccessStatusCode)
        //        {
        //            var b = await x.Result.Content.ReadFromJsonAsync<IntID>() ?? new();

        //            m_lCmdCookie = b.ID;
        //        }
        //    });
        //}

        //private async Task UpdateRegistration(RemoteConnect request)
        //{
        //    await Http.PostAsJsonAsync("api/v1/UpdateRegistration", request).ContinueWith(async x =>
        //    {
        //        if (x.Result.IsSuccessStatusCode)
        //        {
        //            var b = await x.Result.Content.ReadFromJsonAsync<IntID>() ?? new();

        //            m_lCmdCookie = b.ID;
        //        }
        //    });
        //}


        private async Task<bool> IsHoliday()
        {
            bool response = false;
            var result = await Http.PostAsync("api/v1/IsHoliday", null);
            if (result.IsSuccessStatusCode)
            {
                var b = await result.Content.ReadFromJsonAsync<IntResponse>() ?? new();
                response = b.Int == 1 ? true : false;
            }
            return response;

        }

        private async Task GetStaffShedule()
        {
            if (IdStaff != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetStaffShedule", new IntID() { ID = IdStaff.Value });
                if (result.IsSuccessStatusCode)
                {
                    SheduleList = await result.Content.ReadFromJsonAsync<List<StaffConnParams>>() ?? new();

                }
                else
                {
                    MessageView?.AddError("", GSOFormRep["IDS_E_GETSHEDULE"]);
                }
            }
        }

        //private async Task GetSubsystemParam()
        //{
        //    await Http.PostAsJsonAsync("api/v1/GetSubsystemParamStaff", new OBJ_ID() { StaffID = StaffId, SubsystemID = SubsystemID }).ContinueWith(async x =>
        //    {
        //        if (x.Result.IsSuccessStatusCode)
        //        {
        //            StaffParam = await x.Result.Content.ReadFromJsonAsync<StaffSubsystemParam>() ?? new();
        //        }
        //        else
        //        {
        //            MessageView?.AddError("", GSOFormRep["IDS_EGETSUBSYSTPARAMS"]);
        //        }
        //    });
        //}

        private async Task GetRestrictList()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetRestrictList", new IntID() { ID = (int)BaseLineType.LINE_TYPE_DIAL_UP });
            if (result.IsSuccessStatusCode)
            {
                RestrictList = await result.Content.ReadFromJsonAsync<List<Restrict>>() ?? new();
                RestrictList.RemoveAll(x => x.RestrictType > 1);

                StateHasChanged();
            }
        }

        private async Task GetLocationInfo()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetObjects_ILocation", new OBJ_ID() { StaffID = StaffId });
            if (result.IsSuccessStatusCode)
            {
                LocationList = await result.Content.ReadFromJsonAsync<List<Objects>>() ?? new();
            }
            else
            {
                LocationList = new();
                MessageView?.AddError("", GSOFormRep["IDS_EFAILGETLOCATIONLIST"]);
            }
        }

        private async Task GetLineTypeList()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetLineTypeList", new IntID() { ID = SubsystemID });
            if (result.IsSuccessStatusCode)
            {
                LineTypes = await result.Content.ReadFromJsonAsync<List<IntAndString>>() ?? new();

                LineTypes.RemoveAll(x => x.Number != (int)BaseLineType.LINE_TYPE_DIAL_UP && x.Number != (int)BaseLineType.LINE_TYPE_DEDICATED);

                LineTypes.Insert(0, new IntAndString() { Number = 0, Str = "ЛВС" });
            }
            else
            {
                LineTypes = new();
                MessageView?.AddError("", GSOFormRep["IDS_EFAILGETLINETYPE"]);
            }
        }

        private bool IsChecked(Restrict item)
        {
            bool isChecked = false;
            if (item.RestrictType == 0)
                isChecked = ((LineParams.GlobalType >> item.BitNumber) & 0x01) > 0;
            if (item.RestrictType == 1)
                isChecked = ((LineParams.UserType >> item.BitNumber) & 0x01) > 0;
            //if (item.RestrictType == 2)
            //    isChecked = ((PropertyMask >> item.BitNumber) & 0x01) > 0;
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
                if (((LineParams.GlobalType >> BitNumber) & 0x01) > 0)
                {
                    LineParams.GlobalType -= i;
                }
                else
                {
                    LineParams.GlobalType += i;
                }
            }
            else if (item.RestrictType == 1)
            {
                if (((LineParams.UserType >> BitNumber) & 0x01) > 0)
                {
                    LineParams.UserType -= i;
                }
                else
                {
                    LineParams.UserType += i;
                }
            }
            //else if (item.RestrictType == 2)
            //{
            //    if (((PropertyMask >> BitNumber) & 0x01) > 0)
            //    {
            //        PropertyMask -= i;
            //    }
            //    else
            //    {
            //        PropertyMask += i;
            //    }
            //}
        }

        private void SetCommandStatus(long Status, long? Code = null)
        {
            if (InfoConnect == null)
                InfoConnect = new();

            InfoConnect.Add(GetString((int)Status, Code));

            StateHasChanged();
        }

        private string GetString(int Status, long? Code = null)
        {
            string statusName = "";
            switch (Status)
            {
                case (int)StaffDataEnum.CU_Status.S_RDM_CONNBEG: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.S_RDM_CONNBEG]; break;//Устанавливается удаленное соединение...
                case (int)StaffDataEnum.CU_Status.E_RDM_CREATE: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.E_RDM_CREATE]; break;//Ошибка активизации Менеджера удаленного запуска...
                case (int)StaffDataEnum.CU_Status.S_ERR_LO: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.S_ERR_LO]; break;//Внутренняя ошибка...
                case (int)StaffDataEnum.CU_Status.E_RDM_TIMEOUT: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.E_RDM_TIMEOUT]; break;//Истек таймаут для установления соединения...
                case (int)StaffDataEnum.CU_Status.E_RDM_CONNECT: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.E_RDM_CONNECT]; break;//Удаленное соединение не установлено...
                case (int)StaffDataEnum.CU_Status.E_RDM_BUSY: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.E_RDM_BUSY]; break;//Линия занята...
                case (int)StaffDataEnum.CU_Status.E_RDM_VOICE: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.E_RDM_VOICE]; break;//Ответил голос...
                case (int)StaffDataEnum.CU_Status.E_RDM_NOANSW: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.E_RDM_NOANSW]; break;//Нет ответа...
                case (int)StaffDataEnum.CU_Status.E_RDM_NOCARRIER: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.E_RDM_NOCARRIER]; break;//Нет несущей...
                case (int)StaffDataEnum.CU_Status.E_RDM_NODIALTONE: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.E_RDM_NODIALTONE]; break;//Нет тона в линии...
                case (int)StaffDataEnum.CU_Status.E_RDM_LOGIN: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.E_RDM_LOGIN]; break;//Ошибка аутентификации пользователя...
                case (int)StaffDataEnum.CU_Status.S_RDM_OPENPORT: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.S_RDM_OPENPORT]; break;//Порт открыт...
                case (int)StaffDataEnum.CU_Status.E_CONNPARAM: statusName = GSOFormRep["IDS_S_" + StaffDataEnum.CU_Status.E_CONNPARAM]; break;//Ошибка получения параметров соединения...
                case (int)StaffDataEnum.CU_Status.S_RDM_CALL: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.S_RDM_CALL]; break;//Набор номера...
                case (int)StaffDataEnum.CU_Status.S_RDM_LOGIN: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.S_RDM_LOGIN]; break;//Проверка имени и пароля...
                case (int)StaffDataEnum.CU_Status.S_RDM_CONNECT: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.S_RDM_CONNECT]; break;//Выполнение команды...
                case (int)StaffDataEnum.CU_Status.S_RDM_CONNBREAK: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.S_RDM_CONNBREAK]; break;//Cоединение разорвано...
                case (int)StaffDataEnum.CU_Status.S_COMMAND_STOP: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.S_COMMAND_STOP]; break;//Выполнение команды прервано...
                case (int)StaffDataEnum.CU_Status.REG_END: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.REG_END]; break;//ПРОЦЕСС ВЫПОЛНЕНИЯ КОМАНДЫ ЗАВЕРШЕН.
                case (int)StaffDataEnum.CU_Status.WAIT_TRY: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.WAIT_TRY]; break;//Ожидание следующей попытки соединения...
                case (int)StaffDataEnum.CU_Status.REG_ABORT: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.REG_ABORT]; break;//Выполнение команды прерывается...
                case (int)StaffDataEnum.CU_Status.CMD_ERR: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.CMD_ERR]; break;//ОШИБКА при выполнении команды.
                case (int)StaffDataEnum.CU_Status.CMD_SUCC: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.CMD_SUCC]; break;//Команда выполнена УСПЕШНО.
                case (int)StaffDataEnum.CU_Status.CMD_ABORT: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.CMD_ABORT]; break;//Выполнение команды прерывается...
                case (int)StaffDataEnum.CU_Status.CMD_NO_ABORT: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.CMD_NO_ABORT]; break;//ПРЕРЫВАНИЕ выполнения команды НЕ ПРОИЗОШЛО: команда выполнилась раньше.
                case (int)StaffDataEnum.CU_Status.CMD_MISMATCH: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.CMD_MISMATCH]; break;//ОШИБКА СОГЛАСОВАНИЯ данных: вероятно, на удаленном ПУ было переустановлено ПО. УДАЛИТЕ эту регистрацию, а затем ЗАРЕГИСТРИРУЙТЕ этот ПУ снова.
                case (int)StaffDataEnum.CU_Status.CMD_ID_EQUAL: statusName = GSOFormRep["IDS_" + StaffDataEnum.CU_Status.CMD_ID_EQUAL]; break;//Идентификаторы ПУ совпадают. Регистрация не может быть выполнена.
                default: statusName = ""; break;
            }
            return $"{DateTime.Now.ToString("T")} -> {statusName} {(Code > 0 ? $" ({StartUIRep["Code"]} 0x{Code.Value.ToString("X8")}h)" : "")}";
        }

        private async Task CloseModal(bool? IsUpdate = false)
        {
            if (CallBack.HasDelegate)
                await CallBack.InvokeAsync(IsUpdate);
        }

        private async Task CancelOrClose()
        {
            if (IsStartProssecing)
            {
                IsAbort = true;
            }
            else
                await CloseModal();
        }

        private void AbortProssecing()
        {
            IsAbort = false;
            IsStartProssecing = false;
            _ = Http.PostAsJsonAsync("api/v1/StopTask", RelevantTask, ComponentDetached);
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
