using AsoDataProto.V1;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SMDataServiceProto.V1;
using static AsoDataProto.V1.AsoData;

namespace StartUI.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General, NameBitsPos.ViewChannel })]
    public class ChannelController : Controller
    {
        private readonly AsoDataProto.V1.AsoData.AsoDataClient _ASOData;

        private readonly ILogger<ChannelController> _logger;
        public ChannelController(ILogger<ChannelController> logger, AsoDataClient data)
        {
            _logger = logger;
            _ASOData = data;
        }

        /// <summary>
        /// Получаем информацию по каналам
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetChannelInfoList(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            ChannelContainerList s = new();
            try
            {
                s = await _ASOData.GetChannelInfoListAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s.Array);
        }

        /// <summary>
        /// Получить список групп для каналов
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetChannelGroupItem()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            ChannelGroupList s = new();
            try
            {
                s = await _ASOData.GetChannelGroupItemAsync(new Null());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s.Array);
        }
    }
}
