using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using SMDataServiceProto.V1;
using SMP16XProto.V1;
using SMSSGsoProto.V1;
using StaffDataProto.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using ReplaceLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using static AsoDataProto.V1.AsoData;
using static SMControlSysProto.V1.SMControlSys;
using static SMDataServiceProto.V1.SMDataService;
using static SMP16XProto.V1.SMP16x;
using static SMSSGsoProto.V1.SMSSGso;
using static StaffDataProto.V1.StaffData;
using static UUZSDataProto.V1.UUZSData;

namespace ServerLibrary.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class SitController : Controller
    {
        private readonly StaffDataClient _StaffData;
        private readonly SMSSGsoClient _SMSGso;
        private readonly AsoDataClient _ASOData;
        private readonly SMDataServiceClient _SMData;
        private readonly UUZSDataClient _UUZSData;
        private readonly SMControlSysClient _SMContr;
        private readonly SMP16xClient _SMP16x;

        private readonly ILogger<SitController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;

        private readonly IStringLocalizer<SqlBaseReplace> SqlRep;

        public SitController(ILogger<SitController> logger, SMSSGsoClient SMSGso, AsoDataClient ASOData, SMDataServiceClient SMData, SMP16xClient SMP16x, UUZSDataClient UUZSData, WriteLog log, StaffDataClient StaffData, SMControlSysClient SMContr, AuthorizedInfo userInfo, IStringLocalizer<SqlBaseReplace> sqlRep)
        {
            _logger = logger;
            _SMSGso = SMSGso;
            _ASOData = ASOData;
            _SMData = SMData;
            _SMContr = SMContr;
            _SMP16x = SMP16x;
            _Log = log;
            _StaffData = StaffData;
            _UUZSData = UUZSData;
            _userInfo = userInfo;
            SqlRep = sqlRep;
        }


        [HttpPost]
        public async Task<IActionResult> GetObjects_ISituationForFiltr(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSGso.GetObjects_ISituationForFiltrAsync(request);
                //List<Objects>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получаем инфо о сценарии
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Objects[]</returns>
        [HttpPost]
        public async Task<IActionResult> GetObjects_ISituation(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSGso.GetObjects_ISituationAsync(request);
                //List<Objects>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Проверка на наличие глобального номера сценария
        /// </summary>
        /// <param name="request"></param>
        /// <returns>CountResponse</returns>
        [HttpPost]
        public async Task<IActionResult> CheckMatchCodeName(OBJIDAndStr request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _StaffData.CheckMatchCodeNameAsync(request);
                //CountResponse
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// получаем список пунктов управления
        /// </summary>
        /// <returns>CUListsItem[]</returns>
        [HttpPost]
        public async Task<IActionResult> GetCULists()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _StaffData.GetCUListsAsync(new Null());
                //List<CUListsItem>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        /// <summary>
        /// получаем список подсистем
        /// </summary>
        /// <returns>CcommSubSystem[]</returns>
        [HttpPost]
        public async Task<IActionResult> GetSubSystemList()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMData.GetSubSystemListAsync(new Null());
                //List<CcommSubSystem>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        /// <summary>
        /// Список сформированных сообщений для оповещения
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SelectNotifyObjectWithMessages(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSGso.SelectNotifyObjectWithMessagesAsync(request);
                var requestProccess = new CNotifyObjectWithMessagesList();
                requestProccess.Array.AddRange(response.Array.Where(x => x.Message.Contains('%')));
                if (requestProccess.Array.Count > 0)
                {
                    var result = await _ASOData.ProcessTemplateMessageListAsync(requestProccess);
                    //List<CNotifyObjectWithMessages>
                    return Ok(result.Array.Select(x => new CNotifyObjectWithMessages(x.CNotifyObjectWithMessages) { Message = x.ProcessedTemplateMessage }));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Удалить объект из сценария
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> DeleteSitItem([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                BoolValue response = new();
                if (_userInfo.GetInfo?.SubSystemID == SubsystemType.SUBSYST_GSO_STAFF)
                {
                    var request = SMControlSysProto.V1.DeleteStaffSitItems.Parser.ParseJson(json);
                    response = await _SMContr.DeleteSitItemAsync(request, deadline: DateTime.UtcNow.AddSeconds(+600));
                }
                else if (_userInfo.GetInfo?.SubSystemID == SubsystemType.SUBSYST_P16x)
                {
                    var request = CStaffSitItemList.Parser.ParseJson(json);
                    response = await _SMP16x.DeleteSitItemAsync(request, deadline: DateTime.UtcNow.AddSeconds(+600));
                }
                else
                {
                    var request = CSitItemInfoList.Parser.ParseJson(json);
                    response = await _SMSGso.DeleteSitItemAsync(request, deadline: DateTime.UtcNow.AddSeconds(+600));
                }
                //BoolValue
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> UpdateSitTimeout(SitTimeout request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _ASOData.UpdateSitTimeoutAsync(request);
                //BoolValue
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> UpdateSituation([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                BoolValue response = new();
                if (_userInfo.GetInfo?.SubSystemID == SubsystemType.SUBSYST_P16x)
                {
                    var request = CStaffSitItemList.Parser.ParseJson(json);
                    response = await _SMP16x.UpdateSituationAsync(request, deadline: DateTime.UtcNow.AddSeconds(+600));
                }
                else
                {
                    var request = UpdateSituationRequest.Parser.ParseJson(json);
                    response = await _SMSGso.UpdateSituationAsync(request, deadline: DateTime.UtcNow.AddSeconds(+600));
                }
                //BoolValue
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
        /// <summary>
        /// добавляем ситуацию
        /// </summary>
        /// <param name="request"></param>
        /// <returns>OBJ_ID</returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> AddSituation(UpdateSituation request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                UpdateSituationRequest r = new();
                r.Info = request.Info;
                r.Items.AddRange(request.Items);
                r.UserSessID = _userInfo.GetInfo?.UserSessID ?? 0;
                OBJ_ID response = new();
                if (request.Info?.Sit?.SubsystemID == SubsystemType.SUBSYST_ASO)
                    response = await _ASOData.AddSituationAsync(r);
                else if (request.Info?.Sit?.SubsystemID == SubsystemType.SUBSYST_SZS)
                {
                    var key = await _SMSGso.S_AddSituationAsync(r);
                    response = key.ObjID;
                }
                else
                    return BadRequest();

                int EventCode = (int)GsoEnum.IDS_REG_SIT_INSERT;
                if (request.Info?.SitTypeID == 0)
                    EventCode = (int)GsoEnum.IDS_REG_NON_SIT_INSERT;
                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: EventCode, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID, Info: request.Info?.SitName);

                if (response?.ObjID > 0)
                    return Ok(response);
                else
                    return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        /// <summary>
        /// добавляем ситуацию Staff
        /// </summary>
        /// <param name="request"></param>
        /// <returns>OBJ_Key</returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> AddSituationStaff(UpdateSituationStaff request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                CInnerInsertSituation r = new();
                r.Info = request.Info;
                r.Items.AddRange(request.Items?.Select(x => new CStaffSitItemInfo()
                {
                    SitItem = new() { SitID = x.SitID, CmdID = new() { ObjID = request.Info?.Sit?.ObjID ?? 0, StaffID = request.Info?.Sit.StaffID ?? 0, SubsystemID = request.Info?.Sit.SubsystemID ?? 0 } },
                    ExInfoType = x.ExInfoType,
                    CustMsgID = x.CustMsg?.ObjID ?? 0,
                    CustMsgStaffID = x.CustMsg?.StaffID ?? 0,
                    DirectID = x.Direct?.ObjID ?? 0,
                    DirectStaffID = x.Direct?.StaffID ?? 0,
                    Status = 0
                }).ToList());

                r.UserSessID = request.UserSessId;

                OBJ_Key response = new();
                if (request.Info != null && request.Info.Sit != null)
                {
                    int EventCode = request.Info.Sit.ObjID == 0 ? (int)GsoEnum.IDS_REG_SIT_INSERT : (int)GsoEnum.IDS_REG_SIT_UPDATE;
                    if (request.Info.SitTypeID == 0)
                        EventCode = request.Info.Sit.ObjID == 0 ? (int)GsoEnum.IDS_REG_NON_SIT_INSERT : (int)GsoEnum.IDS_REG_NON_SIT_UPDATE;
                    await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: EventCode, SubsystemID: request.Info.Sit.SubsystemID, UserID: _userInfo.GetInfo?.UserID, Info: request.Info.SitName);

                    if (request.Info.Sit.ObjID == 0)
                    {
                        response = await _StaffData.S_AddSituationAsync(r);
                    }
                    else
                    {
                        var b = await _StaffData.UpdateSituationAsync(r);
                        if (b?.Value == false)
                        {
                            return BadRequest();
                        }
                        else
                            return Ok();
                    }

                }

                if (response?.ObjID?.ObjID > 0)
                    return Ok(response);
                else
                    return BadRequest();

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получаем сообщение для сценария(Staff)
        /// </summary>
        /// <param name="request"></param>
        /// <returns>SitMsgInfo</returns>
        [HttpPost]
        public async Task<IActionResult> GetSituationItemsStaff(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSGso.GetSituationItemsAsync(request);
                //CSitMsgInfo
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        /// <summary>
        /// Получаем объекты сценария для редактирования 
        /// </summary>
        /// <param name="request"></param>
        /// <returns>SitItem[]</returns>
        [HttpPost]
        public async Task<IActionResult> GetSituationItems(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                if (request.SubsystemID == SubsystemType.SUBSYST_ASO)
                {
                    var response = await _ASOData.GetSituationItemsAsync(request);
                    //CGetSitItemInfo
                    return Ok(response.Array);
                }
                else if (request.SubsystemID == SubsystemType.SUBSYST_SZS)
                {
                    var response = await _UUZSData.GetSituationItemsAsync(request);
                    //CGetSitItemInfo
                    return Ok(response.Array);
                }
                else if (request.SubsystemID == SubsystemType.SUBSYST_GSO_STAFF)
                {
                    var r = await _SMContr.GetSituationItemsAsync(request);
                    //List<SituationItem>
                    return Ok(r.Array);
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получаем таймауты
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetSitTimeout(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _ASOData.GetSitTimeoutAsync(request);
                //SitTimeout
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получаем информацию о сценарии
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetSituationInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                if (request.SubsystemID == SubsystemType.SUBSYST_P16x)
                {
                    var response = await _SMP16x.GetSituationInfoAsync(request);
                    // List<CGetSituationInfo>
                    return Ok(response.Array);
                }
                else
                {
                    var response = await _SMSGso.GetSituationInfoSMSSGsoAsync(request);
                    //SituationInfo
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetItems_ISituation(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _ASOData.GetItems_ISituationAsync(request);
                //List<SituationItem>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteSituation(OBJIDAndStr request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSGso.DeleteSituationAsync(request.OBJID) ?? new();

                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: (int)GsoEnum.IDS_REG_SIT_DELETE, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID, Info: request.Str);
                //BoolValue
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteSituationNONSIT(OBJIDAndStr request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSGso.DeleteSituationAsync(request.OBJID) ?? new();

                await _Log.Write(Source: (int)GSOModules.StartUI_Module, EventCode: 181/*IDS_REG_DELNONSIT*/, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID, Info: request.Str);
                //BoolValue
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckMatchSitName(OBJIDAndStr request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSGso.CheckMatchSitNameAsync(request);
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
