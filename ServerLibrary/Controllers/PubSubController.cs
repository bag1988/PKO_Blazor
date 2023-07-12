using Dapr;
using DocumentFormat.OpenXml.Vml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using ServerLibrary.Extensions;
using ServerLibrary.HubsProvider;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Interfaces;
using SMDataServiceProto.V1;
using Dapr.Client;

namespace ServerLibrary.Controllers
{
    [ApiController]
    [Route("/[action]")]
    [AllowAnonymous]
    public class PubSubController : Controller, IPubSubMethod
    {
        private readonly ILogger<PubSubController> _logger;

        private readonly SharedHub _hubContext;

        public PubSubController(ILogger<PubSubController> logger, SharedHub hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;
        }

        private async Task SetCookie<TData>(string NameNotify, TData ValueNotify)
        {
            try
            {
                await _hubContext.SendTopic(NameNotify, ValueNotify);

                //if (Request.Cookies.ContainsKey(NameNotify))
                //{
                //    Response.Cookies.Delete(NameNotify);
                //}
                //Response.Cookies.Append(NameNotify, JsonSerializer.Serialize(ValueNotify));
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        #region Dapr Subscribe

        ////[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateTermDevice)]
        //[HttpPost]
        //public async Task PkoTopics()
        //{
        //    using var activity = this.ActivitySourceForController()?.StartActivity();
        //    try
        //    {
        //        //await SetCookie(nameof(DaprMessage.Fire_UpdateTermDevice, value);
        //        var strm = new StreamReader(Request.Body);
        //        var str = strm.ReadToEnd();
        //        _logger.LogInformation(str);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
        //    }
        //}

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateSituation)]
        [HttpPost]
        public async Task Fire_UpdateSituation(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_UpdateSituation), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateTermDevice)]
        [HttpPost]
        public async Task Fire_UpdateTermDevice(long value)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_UpdateTermDevice), value);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteTermDevice)]
        [HttpPost]
        public async Task Fire_InsertDeleteTermDevice(long value)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_InsertDeleteTermDevice), value);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }


        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateTermDevicesGroup)]
        [HttpPost]
        public async Task Fire_UpdateTermDevicesGroup(long value)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_UpdateTermDevicesGroup), value);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteTermDevicesGroup)]
        [HttpPost]
        public async Task Fire_InsertDeleteTermDevicesGroup(long value)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_InsertDeleteTermDevicesGroup), value);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_AllUserLogout)]
        [HttpPost]
        public async Task Fire_AllUserLogout([FromBody] string request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_AllUserLogout), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateMessage)]
        [HttpPost]
        public async Task Fire_UpdateMessage([FromBody] ulong request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_UpdateMessage), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateList)]
        [HttpPost]
        public async Task Fire_UpdateList(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_UpdateList), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateAbonent)]
        [HttpPost]
        public async Task Fire_UpdateAbonent(byte[] AbonentItemByte)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_UpdateAbonent), AbonentItemByte);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateControllingDevice)]
        [HttpPost]
        public async Task Fire_UpdateControllingDevice([FromBody] long request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_UpdateControllingDevice), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteSituation)]
        [HttpPost]
        public async Task Fire_InsertDeleteSituation(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_InsertDeleteSituation), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteMessage)]
        [HttpPost]
        public async Task Fire_InsertDeleteMessage([FromBody] ulong request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_InsertDeleteMessage), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteList)]
        [HttpPost]
        public async Task Fire_InsertDeleteList(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_InsertDeleteList), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteAbonent)]
        [HttpPost]
        public async Task Fire_InsertDeleteAbonent([FromBody] byte[] AbonentItemByte)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_InsertDeleteAbonent), AbonentItemByte);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteControllingDevice)]
        [HttpPost]
        public async Task Fire_InsertDeleteControllingDevice([FromBody] long request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_InsertDeleteControllingDevice), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }


        ////[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateSubsystParam)]
        //[HttpPost]
        //public async Task Fire_UpdateSubsystParam([FromBody] uint request)
        //{
        //    using var activity = this.ActivitySourceForController()?.StartActivity();
        //    try
        //    {
        //        await SetCookie(nameof(DaprMessage.Fire_UpdateSubsystParam, request);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());

        //    }
        //}

        #endregion
    }
}
