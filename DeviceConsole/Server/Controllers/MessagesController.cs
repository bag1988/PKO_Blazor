using Microsoft.AspNetCore.Mvc;
using ReplaceLibrary;
using ServerLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static SMSSGsoProto.V1.SMSSGso;

namespace DeviceConsole.Server.Controllers
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

        [HttpPost]
        public async Task<IActionResult> GetItems_IMessage(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSGso.GetItems_IMessageAsync(request);
                //List<MessageItem>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        [HttpPost]
        public async Task<IActionResult> GetLinkObjects_IMessage(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSGso.GetLinkObjects_IMessageAsync(request);
                //List<string>
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
        public async Task<IActionResult> DeleteMsg(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSGso.DeleteMsgAsync(request);

                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: (int)GsoEnum.IDS_REG_MESS_DELETE, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID);

                //BoolValue
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
