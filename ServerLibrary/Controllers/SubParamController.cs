using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using SMSSGsoProto.V1;
using StaffDataProto.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ReplaceLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static AsoDataProto.V1.AsoData;
using static SMDataServiceProto.V1.SMDataService;
using static SMSSGsoProto.V1.SMSSGso;
using static StaffDataProto.V1.StaffData;

namespace ServerLibrary.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class SubParamController : Controller
    {
        private readonly SMSSGsoClient _SMSGso;
        private readonly SMDataServiceClient _SMData;
        private readonly AsoDataClient _ASOData;
        private readonly StaffDataClient _StaffData;
        private readonly ILogger<SubParamController> _logger;
        private readonly AuthorizedInfo _userInfo;
        private readonly WriteLog _Log;

        public SubParamController(ILogger<SubParamController> logger, SMSSGsoClient SMSGso, SMDataServiceClient SMData, AsoDataClient ASOData, StaffDataClient StaffData, AuthorizedInfo userInfo, WriteLog Log)
        {
            _logger = logger;
            _SMSGso = SMSGso;
            _SMData = SMData;
            _StaffData = StaffData;
            _userInfo = userInfo;
            _Log = Log;
            _ASOData = ASOData;
        }


        [HttpPost]
        public async Task<IActionResult> GetSndSettingEx(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            GetSndSettingExResponse? models;
            try
            {
                models = await _SMData.GetSndSettingExAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(JsonFormatter.Default.Format(models));
        }


        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> SetSndSettingEx([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue r = new BoolValue();
            try
            {
                var request = JsonParser.Default.Parse<SetSndSettingExRequest>(json);

                r = await _SMData.SetSndSettingExAsync(request);
                await _Log.Write(Source: request.OBJID?.SubsystemID == SubsystemType.SUBSYST_P16x ? (int)GSOModules.P16Forms_Module : (int)GSOModules.GsoForms_Module, EventCode: (int)GsoEnum.IDS_REG_PARAM_UPDATE, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(r);
        }


        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> UpdateVipTimeout(CVipTimeout request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue s = new BoolValue();
            try
            {
                s = await _SMSGso.UpdateVipTimeoutAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Получаем настройки по умолчанию
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetSubsystemParam(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            SubsystemParam s = new SubsystemParam();
            try
            {
                s = await _SMSGso.GetSubsystemParamAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Получаем настройки подсистемы "Система управления"
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetSubsystemParamStaff(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            StaffSubsystemParam s = new();
            try
            {
                s = await _StaffData.GetSubsystemParamAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Сохраняем настройки подсистемы "Система управления"
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> UpdateSubsystemParamStaff(StaffSubsystemParam request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue s = new BoolValue();
            try
            {
                s = await _StaffData.UpdateSubsystemParamAsync(request);
                await _Log.Write(Source: (int)GSOModules.StaffForms_Module, EventCode: 70/*IDS_REG_PARAM_UPDATE*/, SubsystemID: SubsystemType.SUBSYST_GSO_STAFF, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> UpdateSubsystemParam(SubsystemParam request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue s = new BoolValue();
            try
            {
                s = await _SMSGso.UpdateSubsystemParamAsync(request);
                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: (int)GsoEnum.IDS_REG_PARAM_UPDATE, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID);
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
