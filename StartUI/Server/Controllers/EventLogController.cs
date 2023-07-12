using Microsoft.AspNetCore.Mvc;
using ServerLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SMDataServiceProto.V1;
using static SMSSGsoProto.V1.SMSSGso;

namespace StartUI.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.ViewLogs })]
    public class EventLogController : Controller
    {
        private readonly ILogger<EventLogController> _logger;
        private readonly SMSSGsoClient _SMSGso;

        public EventLogController(ILogger<EventLogController> logger, SMSSGsoClient sMSGso)
        {
            _logger = logger;
            _SMSGso = sMSGso;
        }

        [HttpPost]
        public async Task<IActionResult> GetItems_IEventLog(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            
            try
            {
                var call = _SMSGso.GetItems_IEventLogAsync(request);
                var headers = await call.ResponseHeadersAsync;
                string? FormatSound = headers.GetValue(MetaDataName.TotalCount);
                Response.Headers.Add(MetaDataName.TotalCount, FormatSound);
                var response = await call.ResponseAsync;
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
    }
}
