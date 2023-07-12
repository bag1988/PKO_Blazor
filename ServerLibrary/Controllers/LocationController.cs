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

namespace ServerLibrary.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class LocationController : Controller
    {
        private readonly SMDataServiceClient _SMData;
        private readonly ILogger<LocationController> _logger;

        public LocationController(ILogger<LocationController> logger, SMDataServiceClient SMData)
        {
            _logger = logger;
            _SMData = SMData;
        }

        [HttpPost]
        public async Task<IActionResult> GetLocationInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            ActualizeLocationListItem s = new();

            try
            {
                s = await _SMData.GetLocationInfoAsync(request);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        [HttpPost]
        public async Task<IActionResult> GetObjects_ILocation(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            ObjectsList response = new();
            try
            {
                response = await _SMData.GetObjects_ILocationAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array.ToList());
        }

    }
}
