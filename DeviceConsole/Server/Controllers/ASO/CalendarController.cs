using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static AsoDataProto.V1.AsoData;
using static SMDataServiceProto.V1.SMDataService;

namespace DeviceConsole.Server.Controllers.ASO
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class CalendarController : Controller
    {
        private readonly SMDataServiceClient _SMData;
        private readonly AsoDataClient _ASOData;
        private readonly ILogger<CalendarController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;

        public CalendarController(ILogger<CalendarController> logger, SMDataServiceClient SMData, WriteLog log, AuthorizedInfo userInfo, AsoDataClient ASOData)
        {
            _logger = logger;
            _SMData = SMData;
            _Log = log;
            _userInfo = userInfo;
            _ASOData = ASOData;
        }


        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteData(DeleteDataRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue s = new();

            try
            {
                s = await _ASOData.DeleteDataAsync(request);
                await _Log.Write(Source: (int)GSOModules.AsoForms_Module, EventCode: 342, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //BoolValue
            return Ok(s);
        }


        [HttpPost]
        public async Task<IActionResult> GetItems_ICalendar(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            CalendarList s = new();

            try
            {
                s = await _ASOData.GetItems_ICalendarAsync(request);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<CalendarItem>
            return Ok(s.Array);
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> SetCalendarInfo(CalendarItem request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            IntResponse s = new();

            try
            {
                s = await _ASOData.SetCalendarInfoAsync(request);

                var EventCode = 341;//IDS_REG_CALENDAR_INSERT

                await _Log.Write(Source: (int)GSOModules.AsoForms_Module, EventCode: EventCode, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //IntResponse
            return Ok(s);
        }

    }
}
