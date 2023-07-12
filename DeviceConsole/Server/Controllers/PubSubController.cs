using Dapr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary.Extensions;
using ServerLibrary.HubsProvider;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Interfaces;
using SharedLibrary.PuSubModel;

namespace DeviceConsole.Server.Controllers
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

        #region Dapr Subscribe

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_CmdStatus)]
        [HttpPost]
        public async Task Fire_CmdStatus(CmdStatus request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_CmdStatus), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_RemoteCuStaffID)]
        [HttpPost]
        public async Task Fire_RemoteCuStaffID(RemoteCuStaffID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_RemoteCuStaffID), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }


        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateLine)]
        [HttpPost]
        public async Task Fire_UpdateLine([FromBody] long request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_UpdateLine), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateLocation)]
        [HttpPost]
        public async Task Fire_UpdateLocation([FromBody] ulong request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_UpdateLocation), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_UpdateLabels(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_UpdateLabels), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteLocation)]
        [HttpPost]
        public async Task Fire_InsertDeleteLocation([FromBody] ulong request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_InsertDeleteLocation), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteLine)]
        [HttpPost]
        public async Task Fire_InsertDeleteLine([FromBody] long request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_InsertDeleteLine), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateShedule)]
        [HttpPost]
        public async Task Fire_UpdateShedule([FromBody] ulong request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_UpdateShedule), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteShedule)]
        [HttpPost]
        public async Task Fire_InsertDeleteShedule([FromBody] ulong request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_InsertDeleteShedule), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateRegistration)]
        [HttpPost]
        public async Task Fire_UpdateRegistration([FromBody] ulong request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_UpdateRegistration), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteRegistration)]
        [HttpPost]
        public async Task Fire_InsertDeleteRegistration([FromBody] ulong request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_InsertDeleteRegistration), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateDepartment)]
        [HttpPost]
        public async Task Fire_UpdateDepartment([FromBody] uint request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_UpdateDepartment), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteDepartment)]
        [HttpPost]
        public async Task Fire_InsertDeleteDepartment([FromBody] uint request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_InsertDeleteDepartment), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateData)]
        [HttpPost]
        public async Task Fire_UpdateData(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_UpdateData), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteData)]
        [HttpPost]
        public async Task Fire_InsertDeleteData(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_InsertDeleteData), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }


        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_StopNl)]
        [HttpPost]
        public async Task Fire_StopNl(OnStopNL request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_StopNl), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_OnMessageSignal)]
        [HttpPost]
        public async Task Fire_OnMessageSignal([FromBody] AsoNlReplaceCode request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_OnMessageSignal), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_OnErrorSignal)]
        [HttpPost]
        public async Task Fire_OnErrorSignal([FromBody] string request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_OnErrorSignal), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_OnAnswerSignal)]
        [HttpPost]
        public async Task Fire_OnAnswerSignal(OnAnswerSignal request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_OnAnswerSignal), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_OnStateLineSignal)]
        [HttpPost]
        public async Task Fire_OnStateLineSignal(OnStateLineSignal request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_OnStateLineSignal), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_InsertDeleteTask)]
        [HttpPost]
        public async Task Fire_InsertDeleteTask([FromBody] uint request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_InsertDeleteTask), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }


        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_EndTask)]
        [HttpPost]
        public async Task Fire_EndTask([FromBody] uint request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_EndTask), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_StartTask)]
        [HttpPost]
        public async Task Fire_StartTask([FromBody] uint request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_StartTask), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_UpdateTask)]
        [HttpPost]
        public async Task Fire_UpdateTask([FromBody] uint request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_UpdateTask), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        #endregion
    }
}
