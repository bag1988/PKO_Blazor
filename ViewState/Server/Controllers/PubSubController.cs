using Dapr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary.Extensions;
using ServerLibrary.HubsProvider;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Interfaces;

namespace ViewState.Server.Controllers
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

        //[Topic(DaprMessage.PubSubName, DaprMessage.Fire_AddErrorState)]
        [HttpPost]
        public async Task Fire_AddErrorState(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.SendTopic(nameof(DaprMessage.Fire_AddErrorState), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }
    }
}
