using Google.Protobuf.WellKnownTypes;
using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SkudProto.V1;
using ServerLibrary.Extensions;
using System.Text.Json;

namespace BBImport.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/skud/event/[action]")]
    public class SkudController : Controller
    {
        private readonly Skud.SkudClient _SKUDClient;
        private readonly ILogger<SkudController> _logger;

        public SkudController(ILogger<SkudController> logger, Skud.SkudClient sKUDClient)
        {
            _logger = logger;
            _SKUDClient = sKUDClient;
        }

        [HttpGet]
        public async Task<IActionResult> Add([FromQuery] SkudEntHttpMessage request)
        {
            try
            {
                _logger.LogInformation("Получено уведомление от СКУД, с параметрами: {@event}", request);
                if (string.IsNullOrEmpty(request.Sn) || string.IsNullOrEmpty(request.Ln) || string.IsNullOrEmpty(request.Fn))
                    return NoContent();
                await _SKUDClient.SetSkudNotificationAsync(request);
                return Ok("Ok");
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return BadRequest();
            }
        }
    }
}
