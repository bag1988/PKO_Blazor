using Google.Protobuf;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static AsoDataProto.V1.AsoData;
using Int32Value = Google.Protobuf.WellKnownTypes.Int32Value;

namespace ServerLibrary.Controllers.ASO
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class AbonentController : Controller
    {
        private readonly AsoDataClient _ASOData;
        private readonly ILogger<AbonentController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;

        public AbonentController(ILogger<AbonentController> logger, AsoDataClient ASOData, WriteLog log, AuthorizedInfo userInfo)
        {
            _logger = logger;
            _ASOData = ASOData;
            _Log = log;
            _userInfo = userInfo;
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> AddMsgParamList([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            Int32Value response = new();
            try
            {
                var request = JsonParser.Default.Parse<AbonMsgParamList>(json);
                response = await _ASOData.AddMsgParamListAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> DeleteSheduleInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            IntID response = new();
            try
            {
                response = await _ASOData.DeleteSheduleInfoAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        /// <summary>
        /// Получить расписание дозвона
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetSheduleInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            SheduleList response = new();
            try
            {
                response = await _ASOData.GetSheduleInfoAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array.ToList());
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> SetSheduleInfo(List<Shedule> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            var r = new SheduleList();
            r.Array.AddRange(request);
            try
            {
                await _ASOData.SetSheduleInfoAsync(r);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok();
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> ImportAbonent([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var request = JsonParser.Default.Parse<AbonInfoImportList>(json);

                //var response = await _ASOData.SetAbInfoAsync(request);


                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> SetAbInfo(AbonInfo request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();            
            try
            {
               var response = await _ASOData.SetAbInfoAsync(request);

                var EventCode = 337;/*IDS_REG_AB_INSERT*/
                if (request.Abon.ObjID != 0)
                    EventCode = 335;/*IDS_REG_AB_SAVE*/

                await _Log.Write(Source: (int)GSOModules.AsoForms_Module, EventCode: EventCode, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }            
        }

        [HttpPost]
        public async Task<IActionResult> GetAbInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            AbonInfo response = new();
            try
            {
                response = await _ASOData.GetAbInfoAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> GetAbStatusList()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            IntAndStrList s = new();
            try
            {
                s = await _ASOData.GetAbStatusListAsync(new Null());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<IntAndString>
            return Ok(s.Array);

        }

    }
}
