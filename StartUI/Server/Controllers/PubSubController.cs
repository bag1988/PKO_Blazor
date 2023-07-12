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
using SharedLibrary.Interfaces;
using StaffDataProto.V1;
using AsoDataProto.V1;
using UUZSDataProto.V1;
using SharedLibrary.PuSubModel;
using SharedLibrary.Utilities;

namespace StartUI.Server.Controllers
{
    [ApiController]
    [Route("/[action]")]
    [AllowAnonymous]
    public class PubSubController : Controller, IPubSubMethod
    {
        private readonly ILogger<PubSubController> _logger;

        private readonly SharedHub _hubContext;

        private readonly DaprClient _daprClient;

        private readonly SMDataServiceProto.V1.SMDataService.SMDataServiceClient _SMData;

        private readonly SMSSGsoProto.V1.SMSSGso.SMSSGsoClient _SMSSgso;

        private readonly IStringLocalizer<GSOReplase> GsoRep;

        private readonly IStringLocalizer<SMDataReplace> SMDataRep;

        public PubSubController(ILogger<PubSubController> logger, SharedHub hubContext, SMSSGsoProto.V1.SMSSGso.SMSSGsoClient sMSSgso, IStringLocalizer<GSOReplase> gsoRep, IStringLocalizer<SMDataReplace> sMDataRep, SMDataServiceClient SMData, DaprClient daprClient)
        {
            _logger = logger;
            _hubContext = hubContext;
            _SMData = SMData;
            GsoRep = gsoRep;
            _daprClient = daprClient;
            SMDataRep = sMDataRep;
            _SMSSgso = sMSSgso;
        }

        /// <summary>
        /// Сохраняем сервисные сообщения в файл
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task SaveMessageToBase(ServiceMessage message)
        {
            try
            {
                await _SMSSgso.SetServiceMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        /// <summary>
        /// Отправляем нотификацию
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>        
        private async Task SendNotificationAsync(string message, CUStartSitInfo? info = null)
        {
            var s = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsSystem.NotifyStaff) });
            var b = s.Value == "1" ? true : false;
            if (b)
            {
                await SendPushAsync(message, info);
            }
        }

        private async Task SendPushAsync(string message, CUStartSitInfo? info = null)
        {
            try
            {
                await SaveMessageToBase(new ServiceMessage() { Date = DateTime.UtcNow.ToTimestamp(), Message = message, Info = info?.ToByteString() ?? ByteString.Empty });

                bool IsReplaceFile = false;

                List<VapidDetails>? listKeys = await _daprClient.GetStateAsync<List<VapidDetails>?>(StateNameConst.StateStore, StateNameConst.VapidDetails);

                if (listKeys == null || listKeys.Count == 0)
                    return;

                var subscription = await _daprClient.GetStateAsync<List<NotificationSubscription>?>(StateNameConst.StateStore, StateNameConst.PushSetting);

                if (subscription == null || !subscription.Any())
                    return;

                if (subscription.Any(x => x.CountError > 5))
                {
                    subscription.RemoveAll(x => x.CountError > 5);
                    IsReplaceFile = true;
                }

                var payload = JsonSerializer.Serialize(new
                {
                    message = message,
                    url = "ViewServiceMessage",
                });

                foreach (var item in subscription)
                {
                    if (string.IsNullOrEmpty(item.IpClient))
                        continue;

                    var vapidDetailsKey = listKeys.FirstOrDefault(x => x.Subject == item.IpClient);

                    if (vapidDetailsKey == null || string.IsNullOrEmpty(vapidDetailsKey.PublicKey) || string.IsNullOrEmpty(vapidDetailsKey.PrivateKey))
                        continue;

                    var pushSubscription = new PushSubscription(item.Url, item.P256dh, item.Auth);

                    var vapidDetails = new VapidDetails(item.IpClient, vapidDetailsKey.PublicKey, vapidDetailsKey.PrivateKey);
                    var webPushClient = new WebPushClient();
                    try
                    {
                        await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                        await webPushClient.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        item.CountError++;
                        IsReplaceFile = true;
                        _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                    }
                }

                //отправка нотификации
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_AddLogs), message);

                if (IsReplaceFile)
                {
                    await _daprClient.SaveStateAsync(StateNameConst.StateStore, StateNameConst.PushSetting, subscription);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        #region Dapr Subscribe


        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_StartNewSituation)]
        [HttpPost]
        public async Task Fire_StartNewSituation(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_StartNewSituation), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }


        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateSCUDresult)]
        [HttpPost]
        public async Task Fire_UpdateSCUDresult(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_UpdateSCUDresult), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }


        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_AddLogs)]
        [HttpPost]
        public async Task Fire_AddLogs([FromBody] string request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SendNotificationAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_StartSessionSubCu)]
        [HttpPost]
        public async Task Fire_StartSessionSubCu(CUStartSitInfo request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                string SubName = "";

                switch (request.SitID?.SubsystemID)
                {
                    case SubsystemType.SUBSYST_ASO:
                        SubName = SMDataRep["SUBSYST_ASO"]; break;
                    case SubsystemType.SUBSYST_SZS:
                        SubName = SMDataRep["SUBSYST_SZS"]; break;
                    case SubsystemType.SUBSYST_GSO_STAFF:
                        SubName = SMDataRep["SUBSYST_GSO_STAFF"]; break;
                    case SubsystemType.SUBSYST_P16x:
                        SubName = SMDataRep["SUBSYST_P16x"]; break;
                    default:
                        SubName = SMDataRep["SUBSYST_GSO_STAFF"]; break;
                }
                string message = $"{GsoRep["LAUNCH_COMTROL_CU"]} {request.StaffName} ({SubName})";

                _logger.LogInformation(message);

                await SendNotificationAsync(message, request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateExStatistics)]
        [HttpPost]
        public async Task Fire_UpdateExStatistics(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_UpdateExStatistics), request);

                try
                {
                    var result = CGetResExStat.Parser.ParseFrom(request);
                    if (result?.RecID > 0)
                    {
                        _logger.LogInformation("ПУ: Обновление детальной статистики для сценария: {SitName}, объект: {ObjName}, статус: {state}", result.SitName, result.ObjName, result.StatusName);
                    }
                }
                catch
                {
                    _logger.LogDebug("Ошибка конвертации данных");
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteExStatistics)]
        [HttpPost]
        public async Task Fire_InsertDeleteExStatistics(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_InsertDeleteExStatistics), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_AddEvent)]
        [HttpPost]
        public async Task Fire_AddEvent(byte[] CEventLogByte)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_AddEvent), CEventLogByte);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }


        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateChannels)]
        [HttpPost]
        public async Task Fire_UpdateChannels([FromBody] ulong request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_UpdateChannels), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }


        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_NotifySessEvents)]
        [HttpPost]
        public async Task Fire_NotifySessEvents([FromBody] ulong request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_NotifySessEvents), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }


        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_ErrorContinue)]
        [HttpPost]
        public async Task Fire_ErrorContinue([FromBody] ulong request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                _logger.LogInformation("Получено уведомление об ошибки дооповещения");
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_ErrorContinue), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }


        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_Redraw_LV)]
        [HttpPost]
        public async Task Fire_Redraw_LV(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_Redraw_LV), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }


        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_StartSession)]
        [HttpPost]
        public async Task Fire_StartSession(FireStartSession request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                _logger.LogInformation("{SubSystem}: Получено уведомление о запуске оповещения, сессия {Session}", SubSystemName.Get(request.Subsystem), request.IdSession);
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_StartSession), request);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_EndSession)]
        [HttpPost]
        public async Task Fire_EndSession(FireEndSession request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                _logger.LogInformation("{SubSystem}: Получено уведомление об окончании оповещения, сессии {Session}", SubSystemName.Get(request.Subsystem), request.IdSession);
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_EndSession), request);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());

            }
        }


        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_ErrorCreateNewSituation)]
        [HttpPost]
        public async Task Fire_ErrorCreateNewSituation([FromBody] ulong request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                _logger.LogInformation("Получено уведомление об ошибки запуска оповещения ({Session})", request);
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_ErrorCreateNewSituation), request);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());

            }
        }


        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateStatListCache)]
        [HttpPost]
        public async Task Fire_UpdateStatListCache(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_UpdateStatListCache), request);

                if (request.Length > 0)
                {
                    try
                    {
                        //для пу
                        var result = CUNotifyOut.Parser.ParseFrom(request);
                        if (result?.FromCUID?.ObjID?.ObjID > 0)
                        {
                            _logger.LogInformation("ПУ: Обновление статистики для сценария: {SitName}, статус: {state}, выполнено: {Succ}, не выполнено: {Fail}", result.SitName, result.Status, result.Succ, result.Fail);
                        }
                    }
                    catch
                    {
                        try
                        {
                            //для асо
                            var result = StatCache.Parser.ParseFrom(request);
                            if (result?.AsoAbon != null && result.AsoAbon.ObjID > 0 && result.Sit != null)
                            {
                                _logger.LogInformation("АСО: Обновление статистики для абонента: {AbonName}, статус: {state}, канал: {Line}, попыток: {Count}", result.AbName, result.SelectName, result.LineName, result.CountRealCall);
                            }
                        }
                        catch
                        {
                            try
                            {
                                //для уузс
                                var result = CLVNotify.Parser.ParseFrom(request);
                                if (result?.DevID > 0 && result.SitID != null)
                                {
                                    _logger.LogInformation("УУЗС: Обновление статистики для устройства: {DevName}, статус: {state}", result.DevName, result.StatusName);
                                }
                            }
                            catch
                            {
                                _logger.LogDebug("Ошибка конвертации данных");
                            }
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());

            }
        }

        #endregion
    }
}
