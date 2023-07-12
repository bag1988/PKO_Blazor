using System.Text.Json;
using Dapr;
using Dapr.Client;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GateServiceProto.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using ReplaceLibrary;
using ServerLibrary.Extensions;
using ServerLibrary.HubsProvider;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using WebPush;
using static SMDataServiceProto.V1.SMDataService;
using SMSSGsoProto.V1;
using SMDataServiceProto.V1;

namespace StartUI.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [AllowAnonymous]
    public class PushController : Controller
    {
        private readonly ILogger<PushController> _logger;

        private readonly DaprClient _daprClient;

        private readonly SharedHub _hubContext;

        private readonly FiltersGSOProto.V1.FiltersGSO.FiltersGSOClient _Filtr;

        private readonly SMSSGsoProto.V1.SMSSGso.SMSSGsoClient _SMSSgso;

        public PushController(ILogger<PushController> logger, SMSSGsoProto.V1.SMSSGso.SMSSGsoClient SMSSgso, DaprClient daprClient, SharedHub hubContext, FiltersGSOProto.V1.FiltersGSO.FiltersGSOClient filtr)
        {
            _logger = logger;
            _daprClient = daprClient;
            _hubContext = hubContext;
            _SMSSgso = SMSSgso;
            _Filtr = filtr;
        }

       
        [HttpPost]
        public async Task<IActionResult> ClearServiceLogs()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                await _SMSSgso.ClearServiceMessageAsync(new Google.Protobuf.WellKnownTypes.Empty());
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_AddLogs), string.Empty);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return BadRequest();
            }


            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> DeleteServiceLogs(List<IntID> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                foreach (var item in request)
                {
                    await _SMSSgso.DelServiceMessageAsync(item);
                }
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_AddLogs), string.Empty);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return BadRequest();
            }
            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> GetMessageByServiceMessage(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var messages = await _Filtr.GetMessageByServiceMessageAsync(request);
                return Ok(messages.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetItems_IServiceMessages(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var messages = await _SMSSgso.GetItems_IServiceMessagesAsync(request);
                return Ok(JsonFormatter.Default.Format(messages));
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return BadRequest();
            }
        }

    }
}
