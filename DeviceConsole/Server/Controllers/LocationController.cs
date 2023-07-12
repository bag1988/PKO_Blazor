using Google.Protobuf.WellKnownTypes;
using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ReplaceLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using static AsoDataProto.V1.AsoData;
using static SMDataServiceProto.V1.SMDataService;
using ServerLibrary;

namespace DeviceConsole.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class LocationController : Controller
    {
        private readonly SMDataServiceClient _SMData;
        private readonly AsoDataClient _ASOData;
        private readonly ILogger<LocationController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;

        public LocationController(ILogger<LocationController> logger, SMDataServiceClient SMData, AsoDataClient ASOData, WriteLog log, AuthorizedInfo userInfo)
        {
            _logger = logger;
            _SMData = SMData;
            _Log = log;
            _userInfo = userInfo;
            _ASOData = ASOData;
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteLocation(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue s = new();

            try
            {
                s = await _SMData.DeleteLocationAsync(request);
                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: (int)GsoEnum.IDS_REG_LOC_DELETE, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        [HttpPost]
        public async Task<IActionResult> GetItems_ILocation(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            LocationList s = new();

            try
            {
                s = await _SMData.GetItems_ILocationAsync(request);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s.Array);
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> SetLocationInfo(ActualizeLocationListItem request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue s = new();

            try
            {
                s = await _SMData.SetLocationInfoAsync(request);

                int EventCode = (int)GsoEnum.IDS_REG_LOC_INSERT;
                if (request.DwLocationID != 0)
                    EventCode = (int)GsoEnum.IDS_REG_LOC_UPDATE;


                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: EventCode, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID);//insert-65, update-66

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        [HttpPost]
        public async Task<IActionResult> ILocation_Aso_GetLinkObjects(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            StringArray s = new StringArray();
            try
            {
                s = await _ASOData.ILocation_Aso_GetLinkObjectsAsync(request);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<string>
            return Ok(s.Array);
        }

    }
}
