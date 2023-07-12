using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using GateServiceProto.V1;
using SMSSGsoProto.V1;
using StaffDataProto.V1;
using UUZSDataProto.V1;
//using SharedLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using ReplaceLibrary;
using ServerLibrary;
using ServerLibrary.Extensions;
using ServerLibrary.Utilities;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static AsoDataProto.V1.AsoData;
using static GateServiceProto.V1.GateService;
using static SMSSGsoProto.V1.SMSSGso;
using static StaffDataProto.V1.StaffData;
using static UUZSDataProto.V1.UUZSData;
using SituationList = SMSSGsoProto.V1.SituationList;
using SharedLibrary.Utilities;

namespace StartUI.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public partial class StartUIController : Controller
    {
        private readonly StaffDataProto.V1.StaffData.StaffDataClient _StaffData;
        private readonly AsoDataProto.V1.AsoData.AsoDataClient _AsoData;
        private readonly GateServiceProto.V1.GateService.GateServiceClient _SMGate;
        private readonly SMSSGsoProto.V1.SMSSGso.SMSSGsoClient _SMSSgso;

        private readonly UUZSDataClient _UUZSData;

        private readonly ILogger<StartUIController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;
        private readonly IStringLocalizer<SqlBaseReplace> SqlRep;

        public StartUIController(ILogger<StartUIController> logger, AsoDataProto.V1.AsoData.AsoDataClient AsoData, GateServiceProto.V1.GateService.GateServiceClient SMGate, SMSSGsoProto.V1.SMSSGso.SMSSGsoClient SMSSgso, IStringLocalizer<SqlBaseReplace> sqlRep, WriteLog log, AuthorizedInfo userInfo, StaffDataProto.V1.StaffData.StaffDataClient StaffData, UUZSDataClient UUZSData)
        {
            _SMGate = SMGate;
            _logger = logger;
            _AsoData = AsoData;
            _UUZSData = UUZSData;
            _SMSSgso = SMSSgso;
            _StaffData = StaffData;
            _Log = log;
            _userInfo = userInfo;
            SqlRep = sqlRep;
        }

        /// <summary>
        /// Получаем время запуска оповещения
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.StartNotify })]
        public async Task<IActionResult> GetStartCommandTime()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSSgso.GetStartCommandTimeAsync(new Null()) ?? new();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> ClearASOHistory(Timestamp request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _AsoData.ClearASOHistoryAsync(request) ?? new();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Проверка возможности запуска
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.StartNotify })]
        public async Task<IActionResult> CheckConfiguration([FromBody] RequestType request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                ConfState s = new();
                var configurationState = await _SMGate.CheckConfigurationAsync(new NotificationRequest
                {
                    RequestType = request
                });

                s.Errors.AddRange(configurationState.Errors);
                s.Warnings.AddRange(configurationState.Warnings);

                _logger.LogInformation("{SubSystem}: Проверка возможности {RequestType}, обнаружено {Error} ошибок и {Warning} предупреждений", SubSystemName.Get(_userInfo.GetInfo?.SubSystemID ?? 0), request switch
                {
                    RequestType.StartNotification => "запуска оповещения",
                    RequestType.StopNotification => "остановки оповещения",
                    RequestType.ContinueNotification => "дооповещения",
                    RequestType.CustomStartNotification => "запуска оповещения в ручном режиме",
                    _ => "ПАРАМЕТР НЕ ИЗВЕСТЕН"
                }, configurationState.Errors.Count, configurationState.Warnings.Count);
                return Ok(s);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Setting }, SubsystemType.SUBSYST_Setting)]
        public async Task<IActionResult> SaveSetting(SettingApp request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                string fileName = "settingApp.txt";
                await new FileReadWrite().WriteText(fileName, request);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GetSetting()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                string fileName = "settingApp.txt";
                var response = await new FileReadWrite().ReadFile<SettingApp>(fileName) ?? new();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получаем список нестандартных ситуаций
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetDrySitInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSSgso.GetDrySitInfoAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Список сценариев для запуска
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> S_CreateSitListCache(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSSgso.S_CreateSitListCacheAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        /// <summary>
        /// Получаем информацию по выбранному сценарию
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetSituationDryInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                StringValue response = new();
                switch (request.SubsystemID)
                {
                    case SubsystemType.SUBSYST_ASO:
                    {
                        response = await _AsoData.GetSituationDryInfoAsync(request);
                    }; break;
                    case SubsystemType.SUBSYST_GSO_STAFF:
                    {
                        response = await _StaffData.GetSituationDryInfoAsync(request);
                    }; break;
                    case SubsystemType.SUBSYST_SZS:
                    {
                        response = await _UUZSData.GetSituationDryInfoAsync(request) ?? new();
                    }; break;
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        /// <summary>
        /// Добавляем сценарии для запуска
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.StartNotify })]
        public async Task<IActionResult> AddSitSeqNum(List<OBJ_ID> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                foreach (var item in request)
                {
                    _logger.LogInformation("{SubSystem}: Добавление сценария в базу для запуска, сценарий № {SitID}", SubSystemName.Get(item.SubsystemID), item.ObjID);
                    await _SMSSgso.AddSitSeqNumAsync(item);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        /// <summary>
        /// Удаляем сценарии для запуска
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> RemoveSitSeqNum(List<OBJ_ID> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                foreach (var item in request)
                {
                    _logger.LogInformation("{SubSystem}: Удаляем сценарий из базы для запуска, сценарий № {SitID}", SubSystemName.Get(item.SubsystemID), item.ObjID);
                    await _SMSSgso.RemoveSitSeqNumAsync(item);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получаем кол-во объектов в сценарии
        /// </summary>
        /// <param name="SubSystemID"></param>
        /// <returns>IntID</returns>
        [HttpPost]
        public async Task<IActionResult> IsExistNotifyObject(IntID SubSystemID)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                IntID response = new();
                if (SubSystemID.ID == SubsystemType.SUBSYST_ASO)
                    response = await _AsoData.IsExistNotifyObjectAsync(new Null()) ?? new();
                else if (SubSystemID.ID == SubsystemType.SUBSYST_GSO_STAFF)
                {
                    var r = await _StaffData.IsExistNotifyObjectAsync(new Null());
                    if (r != null && r.Value == true)
                        response.ID = 1;
                }
                else if (SubSystemID.ID == SubsystemType.SUBSYST_SZS)
                {
                    var r = await _UUZSData.IsExistNotifyObjectAsync(new Null());
                    if (r != null && r.Count > 0)
                        response.ID = r.Count;
                }

                _logger.LogInformation("{SubSystem}: Проверяем наличие объектов для оповещения, найдено объектов: {Count}", SubSystemName.Get(SubSystemID.ID), response.ID);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        /// <summary>
        /// Получаем информацию о выбранных сценариях
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetSituationInfoList(List<OBJ_ID> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                List<SituationInfo> response = new();
                foreach (var item in request)
                {
                    var r = await _SMSSgso.GetSituationInfoSMSSGsoAsync(item);

                    _logger.LogInformation("{SubSystem}: Получена информация о запускаемом сценарии № {SitID} - {SitName}", SubSystemName.Get(item.SubsystemID), item.ObjID, r?.SitName);
                    if (r != null)
                        response.Add(r);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Активные сценарии
        /// </summary>
        /// <param name="request"></param>
        /// <returns>AppropriateNotify[]</returns>
        [HttpPost]
        public async Task<IActionResult> GetAppropriateNotifyInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                IntID r = new() { ID = request.ObjID };
                StringValue response = new();
                if (request.SubsystemID == SubsystemType.SUBSYST_ASO)
                {
                    response = await _AsoData.GetAppropriateNotifyInfoAsync(r);
                }
                else if (request.SubsystemID == SubsystemType.SUBSYST_GSO_STAFF)
                {
                    response = await _StaffData.GetAppropriateNotifyInfoAsync(r);
                }
                else if (request.SubsystemID == SubsystemType.SUBSYST_SZS)
                {
                    response = await _UUZSData.GetAppropriateNotifyInfoAsync(r);
                }

                _logger.LogInformation("{SubSystem}: Получение информации об активных сценариях: {Info}", SubSystemName.Get(request.SubsystemID), (string.IsNullOrEmpty(response.Value) ? "нет активных сценариев" : response.Value.Replace("\n", " ")));

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Запуск оповещения
        /// </summary>
        /// <param name="request"></param>
        /// <returns>NotificationResponse</returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.StartNotify })]
        public async Task<IActionResult> StartNotify(StartNotify request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var r = new NotificationRequest()
                {
                    UnitID = request.UnitID,
                    SessId = request.SessId,
                    RequestType = RequestType.StartNotification
                };
                r.ListOBJ.AddRange(request.ListOBJ);

                _logger.LogInformation("{SubSystem}: Запуск оповещения пользователем: {UserName}, по сценариям: {SitName}",
                   SubSystemName.Get(_userInfo.GetInfo?.SubSystemID), _userInfo.GetInfo?.UserName, string.Join(",", request.SitNameList));

                var response = await _SMGate.StartNotifyAsync(r);



                await _Log.Write(Source: (int)GSOModules.StartUI_Module, EventCode: 176, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID, Info: string.Join(",", request.SitNameList));

                if (response.ResponseCode == ResponseCode.LisenceNotExist)
                {
                    _logger.LogInformation("{SubSystem}: Отсутствует лицензия", SubSystemName.Get(_userInfo.GetInfo?.SubSystemID));
                    return StatusCode((int)System.Net.HttpStatusCode.SeeOther);
                }

                return Ok(response.SessionIds.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        /// <summary>
        /// Запуск оповещения в ручном режиме
        /// </summary>
        /// <param name="request"></param>
        /// <returns>NotificationResponse</returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.StartNotify })]
        public async Task<IActionResult> CustomStartNotify(StartNotify request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var r = new NotificationRequest()
                {
                    UnitID = request.UnitID,
                    SessId = request.SessId,
                    MsgId = request.MsgId,
                    RequestType = RequestType.CustomStartNotification
                };
                r.ListOBJ.AddRange(request.ListOBJ);

                _logger.LogInformation("{SubSystem}: Запуск оповещения в ручном режиме пользователем: {UserName}, по сценариям: {SitName}",
                  SubSystemName.Get(_userInfo.GetInfo?.SubSystemID), _userInfo.GetInfo?.UserName, string.Join(",", request.SitNameList));

                var response = await _SMGate.StartNotifyAsync(r);

                await _Log.Write(Source: (int)GSOModules.StartUI_Module, EventCode: 176, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID, Info: (string.Join(", ", request.SitNameList)));

                if (response.ResponseCode == ResponseCode.LisenceNotExist)
                {
                    _logger.LogInformation("{SubSystem}: Отсутствует лицензия", SubSystemName.Get(_userInfo.GetInfo?.SubSystemID));
                    return StatusCode((int)System.Net.HttpStatusCode.SeeOther);
                }
                return Ok(response.SessionIds.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.StartNotify })]
        public async Task<IActionResult> StopNotify(StartNotify request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var r = new NotificationRequest()
                {
                    UnitID = request.UnitID,
                    SessId = request.SessId,
                    RequestType = RequestType.StopNotification
                };
                r.ListOBJ.AddRange(request.ListOBJ);

                _logger.LogInformation("{SubSystem}: Остановка оповещения, сессия: {Session}, сценарии: {SitInfo}", SubSystemName.Get(_userInfo.GetInfo?.SubSystemID), request.SessId, string.Join(", ", request.ListOBJ.Select(x => $"№ {x.ObjID} ({SubSystemName.Get(x.SubsystemID)})")));

                var response = await _SMGate.StopNotifyAsync(r);

                await _Log.Write(Source: (int)GSOModules.StartUI_Module, EventCode: 178, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID);

                if (response.ResponseCode == ResponseCode.LisenceNotExist)
                {
                    _logger.LogInformation("{SubSystem}: Отсутствует лицензия", SubSystemName.Get(_userInfo.GetInfo?.SubSystemID));
                    return StatusCode((int)System.Net.HttpStatusCode.SeeOther);
                }
                return Ok(response.SessionIds.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Запускаем дооповещение
        /// </summary>
        /// <param name="request"></param>
        /// <returns>NotificationResponse</returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.StartNotify })]
        public async Task<IActionResult> ContinueNotify(StartNotify request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var r = new NotificationRequest()
                {
                    UnitID = request.UnitID
                  ,
                    SessId = request.SessId
                  ,
                    RequestType = RequestType.ContinueNotification
                };
                r.ListOBJ.AddRange(request.ListOBJ);

                _logger.LogInformation("{SubSystem}: Дооповещение, сессия: {Session}, сценарии: {SitInfo}", SubSystemName.Get(_userInfo.GetInfo?.SubSystemID), request.SessId, string.Join(", ", request.ListOBJ.Select(x => $"№ {x.ObjID} ({SubSystemName.Get(x.SubsystemID)})")));

                var response = await _SMGate.ContinueNotifyAsync(r);

                await _Log.Write(Source: (int)GSOModules.StartUI_Module, EventCode: 177, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID);

                if (response.ResponseCode == ResponseCode.LisenceNotExist)
                {
                    _logger.LogInformation("{SubSystem}: Отсутствует лицензия", SubSystemName.Get(_userInfo.GetInfo?.SubSystemID));
                    return StatusCode((int)System.Net.HttpStatusCode.SeeOther);
                }
                return Ok(response.SessionIds.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        /// <summary>
        /// ход оповещения ASO
        /// </summary>
        /// <param name="request">SessID</param>
        /// <returns>StatCache[]</returns>
        [HttpPost]
        public async Task<IActionResult> CreateStatListCache(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                if (request.ObjID?.SubsystemID == SubsystemType.SUBSYST_ASO)
                {
                    StatCacheList response = new();
                    response = await _AsoData.CreateStatListCacheAsync(request);
                    //List<StatCache>
                    return Ok(response.Array);
                }
                else if (request.ObjID?.SubsystemID == SubsystemType.SUBSYST_GSO_STAFF)
                {
                    CResultsListCache response = new();
                    response = await _StaffData.CreateStatListCacheAsync(request);
                    //List<CResultsCache>
                    return Ok(response.Array);
                }
                else if (request.ObjID?.SubsystemID == SubsystemType.SUBSYST_SZS)
                {
                    CLVNotifyList response = new();
                    response = await _UUZSData.CreateStatListCacheAsync(request);
                    //List<CLVNotify>
                    return Ok(response.Array);
                }
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateResultsListCache(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                if (request.ObjID?.SubsystemID == SubsystemType.SUBSYST_ASO)
                {
                    ResultCacheList response = new();
                    response = await _AsoData.CreateResultsListCacheAsync(request);
                    //List<ResultCache>
                    return Ok(response.Array);
                }
                else if (request.ObjID?.SubsystemID == SubsystemType.SUBSYST_GSO_STAFF)
                {
                    CResultsListCache response = new();
                    response = await _StaffData.CreateResultsListCacheAsync(request);
                    //List<CResultsCache>
                    return Ok(response.Array);
                }
                else if (request.ObjID?.SubsystemID == SubsystemType.SUBSYST_SZS)
                {
                    CLVResultList response = new();
                    response = await _UUZSData.CreateResultsListCacheAsync(request);
                    //List<CLVResult>
                    return Ok(response.Array);
                }
                return BadRequest();

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получаем статистику оповещения
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetResultsInfo([FromBody] int subSystemID)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                List<CStatistic> response = new();

                if (subSystemID == SubsystemType.SUBSYST_ASO)
                {
                    var r = await _AsoData.GetResultsInfoAsync(new Null()) ?? new();
                    response.Add(r);
                }
                else if (subSystemID == SubsystemType.SUBSYST_SZS)
                {
                    var r = await _UUZSData.GetResultsInfoAsync(new Null()) ?? new();
                    response.AddRange(r.Array);
                }
                else if (subSystemID == SubsystemType.SUBSYST_GSO_STAFF)
                {
                    var r = await _StaffData.GetResultsInfoAsync(new Null()) ?? new();
                    response.Add(r);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }


        }
        /// <summary>
        /// Получаем статистику кол-ва оповещенных абонентов
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetStaticsInfo([FromBody] int subSystemID)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                List<CStatistic> response = new();
                
                if (subSystemID == SubsystemType.SUBSYST_ASO)
                {
                    var r = await _AsoData.GetStaticsInfoAsync(new Null()) ?? new();
                    response.Add(r);
                }
                else if (subSystemID == SubsystemType.SUBSYST_SZS)
                {
                    var r = await _UUZSData.GetStaticsInfo_2Async(new Null()) ?? new();
                    response.AddRange(r.Array);
                }
                else if (subSystemID == SubsystemType.SUBSYST_GSO_STAFF)
                {
                    var r = await _StaffData.GetStaticsInfo_2Async(new Null()) ?? new();
                    response.Add(r);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
    }
}
