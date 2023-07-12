using Google.Protobuf.WellKnownTypes;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Mvc;
using ReplaceLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;
using static SMSSGsoProto.V1.SMSSGso;
using ServerLibrary;

namespace StartUI.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class MessagesController : Controller
    {
        private readonly SMSSGsoClient _SMSGso;
        private readonly ILogger<MessagesController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;

        public MessagesController(ILogger<MessagesController> logger, SMSSGsoClient SMSGso, WriteLog log, AuthorizedInfo userInfo)
        {
            _logger = logger;
            _SMSGso = SMSGso;
            _Log = log;
            _userInfo = userInfo;
        }

        /// <summary>
        /// Записывае пользовательское сообщение, идет запись сообщения
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> WriteMessagesCustom([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            OBJ_ID s = new OBJ_ID();
            try
            {
                MsgInfo request = MsgInfo.Parser.ParseJson(json);
                s = await _SMSGso.WriteMessagesAsync(request);

                int EventCode = (int)GsoEnum.IDS_REG_MESS_INSERT;
                _logger.LogInformation("{SubSystem}: Создаем сообщение в ручном режиме, наименование: {MsgName}", SubSystemName.Get(request.Msg.SubsystemID), request.Param.MsgName);
                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: EventCode, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Устанавливаем статус сообщения, 1-идет запись, 0-записано
        /// </summary>
        /// <param name="request">SubSystemID->status</param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> SetMessageStatus(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                _logger.LogInformation("{SubSystem}: Для сообщения № {MsgID}, устанавливаем статус: {State}", SubSystemName.Get(_userInfo.GetInfo?.SubSystemID), request.ObjID, request.SubsystemID);

                await _SMSGso.SetMessageStatusAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok();
        }

        /// <summary>
        /// Идет запись сообщения
        /// </summary>
        /// <param name="request"></param>
        /// <returns>BoolValue</returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> SetMessageParts(MessageParts request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue s = new();
            try
            {
                _logger.LogInformation("{SubSystem}: Записываем часть сообщения № {MsgID}, длина части сообщения: {SoundLength}", SubSystemName.Get(_userInfo.GetInfo?.SubSystemID), request.OBJID.ObjID, Convert.FromBase64String(request.Sound).Length);
                s = await _SMSGso.SetMessagePartsAsync(request);
            }
            catch (Exception ex)
            {
                s.Value = false;
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

    }
}
