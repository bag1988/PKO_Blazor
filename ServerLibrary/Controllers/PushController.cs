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
using Microsoft.Extensions.Logging;

namespace ServerLibrary.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [AllowAnonymous]
    public class PushController : Controller
    {
        private readonly ILogger<PushController> _logger;

        private readonly DaprClient _daprClient;

        public PushController(ILogger<PushController> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }

        [HttpPost]
        public async Task<IActionResult> CreateVAPIDKeys([FromBody] string ipClient)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                List<VapidDetails>? list = await _daprClient.GetStateAsync<List<VapidDetails>?>(StateNameConst.StateStore, StateNameConst.VapidDetails) ?? new();

                list.RemoveAll(x => x.Subject == ipClient);
                var keys = VapidHelper.GenerateVapidKeys();
                keys.Subject = ipClient;
                list.Add(keys);
                await _daprClient.SaveStateAsync(StateNameConst.StateStore, StateNameConst.VapidDetails, list);
                return Ok(keys.PublicKey);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveSettingSubscription(NotificationSubscription request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var response = await GetSubscription();

                if (response.Any(x => x.IpClient == request.IpClient))
                {
                    response.RemoveAll(x => x.IpClient == request.IpClient);
                }

                response.Add(request);

                await _daprClient.SaveStateAsync(StateNameConst.StateStore, StateNameConst.PushSetting, response);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return BadRequest();
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> GetSubscriptionSetting(NotificationSubscription request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            bool b = false;
            if (!string.IsNullOrEmpty(request.Url))
            {
                List<NotificationSubscription> response = await GetSubscription();

                var client = response.FirstOrDefault(x => x.IpClient == request.IpClient);
                if (client != null)
                {
                    if (client.Url == request.Url)
                    {
                        b = true;
                    }
                    else
                    {
                        response.RemoveAll(x => x.IpClient == request.IpClient);

                        await _daprClient.SaveStateAsync(StateNameConst.StateStore, StateNameConst.PushSetting, response);
                    }
                }
            }
            return Ok(b);
        }

        
        /// <summary>
        /// Получаем настройки push уведомления
        /// </summary>
        /// <returns></returns>
        private async Task<List<NotificationSubscription>> GetSubscription()
        {
            List<NotificationSubscription>? response = null;
            try
            {
                response = await _daprClient.GetStateAsync<List<NotificationSubscription>>(StateNameConst.StateStore, StateNameConst.PushSetting);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }

            return response ?? new();
        }
    }
}
