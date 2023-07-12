using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using SMDataServiceProto.V1;
using StaffDataProto.V1;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary;
using ServerLibrary.Extensions;
using ServerLibrary.HubsProvider;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SharedLibrary.PuSubModel;
using SharedLibrary.Utilities;
using Microsoft.Extensions.Options;
using RemoteConnectLibrary;

namespace DeviceConsole.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class RegistrationController : Controller
    {
        private readonly SMDataServiceProto.V1.SMDataService.SMDataServiceClient _SMData;
        private readonly StaffDataProto.V1.StaffData.StaffDataClient _StaffData;
        private readonly ILogger<RegistrationController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;
        private readonly SharedHub _hubContext;
        private readonly RemoteGateProvider _connectRemote;
        private readonly string ThisConnectGrpc;

        readonly static Dictionary<uint, CancellationTokenSource> ProssecingList = new();

        public RegistrationController(ILogger<RegistrationController> logger, SMDataServiceProto.V1.SMDataService.SMDataServiceClient SMData, StaffDataProto.V1.StaffData.StaffDataClient StaffData, WriteLog log, AuthorizedInfo userInfo, SharedHub hubContext, RemoteGateProvider connectRemote, IOptions<UriBuilder> connectSmGateSettings)
        {
            _logger = logger;
            _SMData = SMData;
            _Log = log;
            _userInfo = userInfo;
            _StaffData = StaffData;
            _hubContext = hubContext;
            _connectRemote = connectRemote;
            ThisConnectGrpc = connectSmGateSettings.Value.Uri.ToString();
        }

        enum CUType
        {
            MASTER = 2,
            SLAVE = 3,
            RESERV = 4,
            EQUAL = 5
        }
        /// <summary>
        /// Обнавляем параметры подключения к ПУ
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UpdateUser(CUpdateUser request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue s = new();
            try
            {
                s = await _StaffData.UpdateUserAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Создаем подключение к удаленном ПУ
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> DoRegistration(RemoteConnect request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            uint Id = 0;
            try
            {
                Id = IpAddressUtilities.StringToUint(request.IpAdress ?? "0.0.0.0");
                var t = GetToken(Id);
                var EventCode = 0;

                switch (request.TypeProssec)
                {
                    case RemoteCmdType.CMD_REGIST: EventCode = 199/*IDS_REG_REGISTR_INSERT*/; break;
                    case RemoteCmdType.CMD_UPREG: EventCode = 201/*IDS_REG_REGISTR_UPDATE*/; break;
                    case RemoteCmdType.CMD_DELREG: EventCode = 200/*IDS_REG_REGISTR_DELETE*/; break;
                }

                Metadata metaData = new();
                metaData.AddRequestToMetadata(Request);

                _ = StartProssecing(request, request.TypeProssec, metaData, t);
                await _Log.Write(Source: (int)GSOModules.StaffForms_Module, EventCode: EventCode, SubsystemID: SubsystemType.SUBSYST_GSO_STAFF, UserID: _userInfo.GetInfo?.UserID);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(Id);
        }

        private CancellationToken GetToken(uint Id)
        {
            if (ProssecingList.ContainsKey(Id))
            {
                ProssecingList[Id].Cancel();
                ProssecingList[Id].Dispose();
                ProssecingList.Remove(Id);
            }
            CancellationTokenSource token = new();
            ProssecingList.Add(Id, token);
            return token.Token;
        }

        /// <summary>
        /// Останавливаем процесс регистрции
        /// </summary>
        /// <param name="RelevatTask"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult StopTask([FromBody] uint RelevatTask)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                {
                    StatusCode = (int)StaffDataEnum.CU_Status.REG_ABORT
                });
                if (ProssecingList.ContainsKey(RelevatTask))
                {
                    ProssecingList[RelevatTask].Cancel();
                }
                else
                {
                    _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                    {
                        StatusCode = (int)StaffDataEnum.CU_Status.CMD_NO_ABORT
                    });
                    _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                    {
                        StatusCode = (int)StaffDataEnum.CU_Status.REG_END
                    });
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        private async Task StartProssecing(RemoteConnect request, RemoteCmdType typeProssecing, Metadata metaData, CancellationToken token)
        {
            try
            {
                _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                {
                    StatusCode = (int)StaffDataEnum.CU_Status.S_RDM_CONNBEG
                });

                if (string.IsNullOrEmpty(request.IpAdress))
                {
                    _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                    {
                        StatusCode = (int)StaffDataEnum.CU_Status.CMD_ERR
                    });
                }
                else
                {
                    string RemoteUnc = request.IpAdress;

                    if (request.IpAdress.IndexOf("http://") == -1)
                        request.IpAdress = "http://" + request.IpAdress;


                    var hrReceiver = await _connectRemote.AuthorizeRemote(request.IpAdress, request.UserName ?? "", AesEncrypt.EncryptString(request.Password ?? ""), token);

                    if (hrReceiver == null)
                    {
                        throw new Grpc.Core.RpcException(new Status(Grpc.Core.StatusCode.Unavailable, "Ошибка подключения к удаленному серверу"));
                    }
                    //выполнение команды
                    _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                    {
                        StatusCode = (int)StaffDataEnum.CU_Status.S_RDM_CONNECT
                    });

                    var r = await hrReceiver.GetRegInfoAllAsync(new Empty(), deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);

                    var Receiver = r.Array.FirstOrDefault();

                    var hrClient = _connectRemote.GetGateClient(ThisConnectGrpc, metaData);

                    if (hrClient == null)
                    {
                        throw new Grpc.Core.RpcException(new Status(Grpc.Core.StatusCode.Unavailable, "Ошибка подключения к локальному серверу"));
                    }

                    var rc = await hrClient.GetRegInfoAllAsync(new Empty(), deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);

                    var Caller = rc.Array.FirstOrDefault();

                    if (Receiver == null || Caller == null)
                    {
                        throw new Grpc.Core.RpcException(new Status(Grpc.Core.StatusCode.NotFound, "Нет данных"));
                    }
                    else
                    {
                        OBJ_Key CuID_C = new() { ObjID = Caller.OBJID };
                        OBJ_Key CuID_R = new() { ObjID = Receiver.OBJID };
                        switch (request.CuType)
                        {
                            case (int)CUType.SLAVE:
                            {   //ведомый
                                CuID_C.ObjType = (int)CUType.MASTER;
                                CuID_R.ObjType = (int)CUType.SLAVE;
                            }
                            break;

                            case (int)CUType.RESERV:
                            {
                                //соседний
                                CuID_C.ObjType = (int)CUType.RESERV;
                                CuID_R.ObjType = (int)CUType.RESERV;
                            }
                            break;

                            case (int)CUType.EQUAL:
                            {
                                //соседний
                                CuID_C.ObjType = (int)CUType.EQUAL;
                                CuID_R.ObjType = (int)CUType.EQUAL;
                            }
                            break;
                        }

                        Int32Value lSess_R = new();
                        Int32Value lSess_C = new();

                        bool isRegistration = false;

                        switch (typeProssecing)
                        {
                            case RemoteCmdType.CMD_REGIST:
                            {
                                if (Caller.OBJID.StaffID == Receiver.OBJID.StaffID)
                                {
                                    throw new Grpc.Core.RpcException(new Status(Grpc.Core.StatusCode.InvalidArgument, "Идентификаторы ПУ совпадают"));
                                }
                                else
                                {
                                    lSess_R = await hrReceiver.SetRegInfoAsync(new CSetRegInfoOnReceiver() { OBJKey = CuID_C, UnitName = Caller.CuName, UNC = Caller.UNC }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);

                                    if (lSess_R.Value > 0)
                                    {
                                        lSess_C = await hrClient.SetRegInfoAsync(new CSetRegInfoOnReceiver() { OBJKey = CuID_R, UnitName = Receiver.CuName, UNC = RemoteUnc }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);
                                        if (lSess_C.Value > 0)
                                        {
                                            var resultSetInfo = await hrClient.SetUserInfoAsync(new CSetUserInfo()
                                            {
                                                Login = request.UserName,
                                                Passw = request.Password,
                                                OBJID = CuID_R.ObjID
                                            }, cancellationToken: token);

                                            if (resultSetInfo != null && resultSetInfo.Value != -1)
                                            {
                                                isRegistration = true;
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                            case RemoteCmdType.CMD_UPREG:
                            {
                                if (request.ReceiverStaffID != CuID_R.ObjID.StaffID)
                                {
                                    throw new Grpc.Core.RpcException(new Status(Grpc.Core.StatusCode.DataLoss, ""));
                                }
                                else
                                {
                                    lSess_R = await hrReceiver.UpdateRegInfoAsync(new CSetRegInfoOnReceiver() { OBJKey = CuID_C, UnitName = Caller.CuName, UNC = Caller.UNC }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);

                                    if (lSess_R.Value > 0)
                                    {
                                        lSess_C = await hrClient.UpdateRegInfoAsync(new CSetRegInfoOnReceiver() { OBJKey = CuID_R, UnitName = Receiver.CuName, UNC = RemoteUnc }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);
                                        if (lSess_C.Value > 0)
                                        {
                                            _ = DoCuMatching(request.IpAdress, CuID_C.ObjID.StaffID, token);
                                            isRegistration = true;
                                        }
                                    }
                                }
                            }
                            break;
                            case RemoteCmdType.CMD_DELREG:
                            {

                                if (Receiver.OBJID.StaffID == request.ReceiverStaffID)
                                {
                                    lSess_R = await hrReceiver.DeleteRegInfoAsync(new Int32Value() { Value = Caller.OBJID.StaffID }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);
                                }
                                else
                                {
                                    if (request.ReceiverStaffID != null)
                                        Receiver.OBJID.StaffID = request.ReceiverStaffID ?? 0;
                                    else
                                    {
                                        throw new Grpc.Core.RpcException(new Status(Grpc.Core.StatusCode.NotFound, "Не задан идентификатор ПУ"));
                                    }
                                }
                                if (lSess_R.Value != -1)
                                {
                                    lSess_C = await hrClient.DeleteRegInfoAsync(new Int32Value() { Value = Receiver.OBJID.StaffID }, cancellationToken: token);

                                    if (lSess_C.Value != -1)
                                    {
                                        isRegistration = true;
                                    }
                                }
                            }
                            break;
                        }

                        //if (lSess_R.Value > 0)
                        //{
                        //    var isOk = await hrReceiver.RegCloseSessAsync(new RegCloseSessCu()
                        //    {
                        //        Sess = lSess_R.Value,
                        //        Commit = isRegistration
                        //    });

                        //    if (isRegistration)
                        //        isRegistration = isOk?.Value ?? false;
                        //}

                        //if (lSess_C.Value > 0)
                        //{
                        //    await hrClient.RegCloseSessAsync(new RegCloseSessCu()
                        //    {
                        //        Sess = lSess_C.Value,
                        //        Commit = isRegistration
                        //    });
                        //}

                        if (isRegistration)
                        {
                            if (typeProssecing == RemoteCmdType.CMD_UPREG || typeProssecing == RemoteCmdType.CMD_REGIST)
                            {
                                var rGeolocation = await hrReceiver.GetGeolocationAsync(new Int32Value() { Value = CuID_R.ObjID.StaffID }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);
                                var cGeolocation = await hrClient.GetGeolocationAsync(new Int32Value() { Value = CuID_C.ObjID.StaffID }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);

                                if (string.IsNullOrEmpty(rGeolocation.Value))
                                    rGeolocation.Value = "";

                                if (string.IsNullOrEmpty(cGeolocation.Value))
                                    cGeolocation.Value = "";

                                _ = hrReceiver.SetGeolocationAsync(new CSetGeolocation() { Geolocation = cGeolocation.Value, StaffId = CuID_C.ObjID.StaffID }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);
                                _ = hrClient.SetGeolocationAsync(new CSetGeolocation() { Geolocation = rGeolocation.Value, StaffId = CuID_R.ObjID.StaffID }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);

                                if (request.CuType == (int)CUType.RESERV)
                                {
                                    var reserv = await hrClient.FromReservAsync(new OBJ_Key() { ObjType = 1 }, cancellationToken: token);

                                    if (reserv.Array.Any())
                                    {
                                        //TODO
                                        //var bToReserv = await client.ToReserv
                                    }
                                }
                            }
                            _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                            {
                                StatusCode = (int)StaffDataEnum.CU_Status.CMD_SUCC,

                            });
                            _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_RemoteCuStaffID), new RemoteCuStaffID()
                            {
                                StaffID = CuID_R.ObjID.StaffID
                            });
                        }
                        else
                        {
                            string nameStaff = lSess_R.Value > 0 ? "локальному" : "удаленному";
                            throw new Grpc.Core.RpcException(new Status(Grpc.Core.StatusCode.Unavailable, $"Ошибка подключения к {nameStaff} пункту управления"));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (e is Grpc.Core.RpcException)
                {
                    var ex = e as Grpc.Core.RpcException;
                    if (ex?.StatusCode == Grpc.Core.StatusCode.Cancelled)
                    {
                        _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                        {
                            StatusCode = (int)StaffDataEnum.CU_Status.S_COMMAND_STOP,

                        });
                    }
                    else if (ex?.StatusCode == Grpc.Core.StatusCode.NotFound)
                    {
                        _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                        {
                            StatusCode = (int)StaffDataEnum.CU_Status.E_CONNPARAM
                        });
                    }
                    else if (ex?.StatusCode == Grpc.Core.StatusCode.DataLoss)
                    {
                        _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                        {
                            StatusCode = (int)StaffDataEnum.CU_Status.CMD_MISMATCH
                        });
                    }
                    else if (ex?.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
                    {
                        _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                        {
                            StatusCode = (int)StaffDataEnum.CU_Status.CMD_ID_EQUAL
                        });
                    }
                    else if (ex?.StatusCode == Grpc.Core.StatusCode.Unauthenticated)
                    {
                        _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                        {
                            StatusCode = (int)StaffDataEnum.CU_Status.E_RDM_LOGIN
                        });
                    }
                    else if (ex?.StatusCode == Grpc.Core.StatusCode.Unavailable)
                    {
                        _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                        {
                            StatusCode = (int)StaffDataEnum.CU_Status.E_RDM_NOANSW
                        });
                    }
                    else
                    {
                        _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                        {
                            StatusCode = (int)StaffDataEnum.CU_Status.E_RDM_CONNECT,
                            ErrorCode = (int?)ex?.Status.StatusCode ?? 0
                        });
                    }

                    _logger.LogError(ex?.Status.Detail);
                }
                else
                {
                    _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                    {
                        StatusCode = (int)StaffDataEnum.CU_Status.S_ERR_LO,
                    });
                    _logger.LogError(e.Message);
                }
            }
            _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
            {
                StatusCode = (int)StaffDataEnum.CU_Status.REG_END
            });

            if (ProssecingList.Any(x => x.Value.Token.Equals(token)))
            {
                var f = ProssecingList.First(x => x.Value.Token.Equals(token));
                f.Value.Dispose();
                ProssecingList.Remove(f.Key);
            }
        }
        private async Task DoCuMatching(string RemoteIp, int localStaffID, CancellationToken token)
        {
            try
            {
                var hrClient = _connectRemote.GetGateClient(ThisConnectGrpc);
                                
                if (localStaffID == 0)
                {
                    throw new Grpc.Core.RpcException(new Status(Grpc.Core.StatusCode.InvalidArgument, "Ошибка получения идентификатора локального ПУ"));
                }

                if (hrClient == null)
                {
                    throw new Grpc.Core.RpcException(new Status(Grpc.Core.StatusCode.InvalidArgument, "Ошибка подключения к локальному серверу"));
                }


                var CallerMsgID = await hrClient.WriteMessageAsync(new WriteMessageRequest()
                {
                    StaffID = localStaffID,
                    SubsystemID = SubsystemType.SUBSYST_GSO_STAFF
                }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);

                if (CallerMsgID.Value > 0)
                {
                    await hrClient.DeleteMsgAsync(new OBJ_ID()
                    {
                        ObjID = CallerMsgID.Value,
                        StaffID = localStaffID,
                        SubsystemID = SubsystemType.SUBSYST_GSO_STAFF
                    }, cancellationToken: token);
                }

                var hrReciver = _connectRemote.GetGateClient(RemoteIp);

                if (hrReciver == null)
                {
                    throw new Grpc.Core.RpcException(new Status(Grpc.Core.StatusCode.InvalidArgument, "Ошибка подключения к удаленному серверу"));
                }

                var ReciverMsgID = await hrReciver.WriteMessageAsync(new WriteMessageRequest()
                {
                    StaffID = localStaffID,
                    SubsystemID = SubsystemType.SUBSYST_GSO_STAFF
                }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);

                if (ReciverMsgID.Value > 0)
                {
                    await hrReciver.DeleteMsgAsync(new OBJ_ID()
                    {
                        ObjID = ReciverMsgID.Value,
                        StaffID = localStaffID,
                        SubsystemID = SubsystemType.SUBSYST_GSO_STAFF
                    }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);
                }

                if (CallerMsgID.Value < ReciverMsgID.Value && ReciverMsgID.Value > 0 && CallerMsgID.Value > 0)
                {
                    while (ReciverMsgID.Value > CallerMsgID.Value && !token.IsCancellationRequested)
                    {
                        CallerMsgID = await hrClient.WriteMessageAsync(new WriteMessageRequest()
                        {
                            StaffID = localStaffID,
                            SubsystemID = SubsystemType.SUBSYST_GSO_STAFF,
                            MsgName = "Утерянное сообщение",
                            MsgComm = RemoteIp
                        }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), new CmdStatus()
                {
                    StatusCode = (int)StaffDataEnum.CU_Status.CMD_MISMATCH
                });
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }


        /// <summary>
        /// Получаем список ПУ
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetItems_IRegistration(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            RegistrCmd s = new();

            try
            {
                s = await _StaffData.GetItems_IRegistrationAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s.Array);
        }

        /// <summary>
        /// Получаем информацию о пункте управления
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetRegInfo(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            Registration s = new();

            try
            {
                s = await _StaffData.GetRegInfoAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Получаем типы пунктов управления
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetCUType()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            CGetCUTypeArray s = new();
            try
            {
                s = await _StaffData.GetCUTypeAsync(new Null());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s.Array);
        }

        /// <summary>
        /// Проверка выходного дня
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> IsHoliday()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            IntResponse s = new();
            try
            {
                s = await _StaffData.IsHolidayAsync(new Null());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }


        /// <summary>
        /// Получаем информацию расписания дозвона зарегистрированного ПУ
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetStaffShedule(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            StaffConnParamsList s = new();
            try
            {
                s = await _StaffData.GetStaffSheduleAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s.Array);
        }

        /// <summary>
        /// Сохраняем параметры дозвона (ПУ)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> SetSheduleInfoStaff(CSheduleInfoExMsg request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue s = new() { Value = false };
            try
            {
                s = await _StaffData.SetSheduleInfoAsync(request);

                int EventCode = 197/*IDS_REG_SHEDULE_INSERT*/;
                if (request.CSheduleInfo?.Object?.ObjID > 0)
                    EventCode = 198/*IDS_REG_SHEDULE_UPDATE*/;

                if (request?.CSheduleInfo?.Object != null && request.CSheduleInfo.Object.StaffID > 0)
                    await _StaffData.SetNullUserAsync(new IntID() { ID = request.CSheduleInfo.Object.StaffID });

                await _Log.Write(Source: (int)GSOModules.StaffForms_Module, EventCode: EventCode, SubsystemID: SubsystemType.SUBSYST_GSO_STAFF, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Получаем список расписания дозвона ПУ
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetItems_IStaffAccess(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            CSheduleInfoList s = new();
            try
            {
                s = await _StaffData.GetItems_IStaffAccessAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s.Array);
        }

        /// <summary>
        /// Получение информации о локальном пункте управления
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetStaffInfo(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            StaffGetAccessor s = new();
            try
            {
                s = await _SMData.GetStaffInfoAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Получаем информацию о расписании дозвона ПУ
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetSheduleInfoStaff(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            CSheduleInfoExMsg s = new();
            try
            {
                s = await _StaffData.GetSheduleInfoAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Удаляем расписание дозвона ПУ
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteSheduleInfoStaff(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue response = new();
            try
            {
                response = await _StaffData.DeleteSheduleInfoAsync(request);

                int EventCode = 196/*IDS_REG_SHEDULE_DELETE*/;

                await _Log.Write(Source: (int)GSOModules.StaffForms_Module, EventCode: EventCode, SubsystemID: SubsystemType.SUBSYST_GSO_STAFF, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }


        /// <summary>
        /// Удаляем информацию о ПУ
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteRegInfo(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            SIntResponse s = new();
            try
            {
                s = await _StaffData.DeleteRegInfoAsync(request);

                if (s.SInt == 0)
                {
                    var staffInfo = await _SMData.GetStaffInfoAsync(request);
                    int EventCode = 200/*IDS_REG_REGISTR_DELETE*/;
                    await _Log.Write(Source: (int)GSOModules.StaffForms_Module, EventCode: EventCode, SubsystemID: SubsystemType.SUBSYST_GSO_STAFF, UserID: _userInfo.GetInfo?.UserID, Info: staffInfo?.Name);
                }
                else
                    return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }
    }
}
