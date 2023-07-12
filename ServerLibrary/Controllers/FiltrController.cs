using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using ReplaceLibrary;
using ServerLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SMDataServiceProto.V1;
using Google.Protobuf;
using FiltersGSOProto.V1;
using Microsoft.Extensions.Logging;

namespace ServerLibrary.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public partial class FiltrController : Controller
    {
        private readonly FiltersGSOProto.V1.FiltersGSO.FiltersGSOClient _Filtr;

        private readonly ILogger<FiltrController> _logger;

        public FiltrController(ILogger<FiltrController> logger, FiltersGSOProto.V1.FiltersGSO.FiltersGSOClient filtr)
        {
            _logger = logger;
            _Filtr = filtr;
        }
        //[CheckPermission(new int[] { NameBitsPos.ViewArhive })]

        [HttpPost]
        public async Task<IActionResult> GetAllLabelValueForNameList(ValueByNameAndEntity request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetAllLabelValueForNameListAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetLabelValueForNameList(ValueByNameAndEntity request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetLabelValueForNameListAsync(request) ?? new();
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
