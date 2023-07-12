using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteConnectLibrary;
using ServerLibrary;
using ServerLibrary.Extensions;
using ServerLibrary.HubsProvider;
using SMDataServiceProto.V1;

namespace DispatchingConsole.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/chat/[action]")]
    [AllowAnonymous]
    public class CommunicationController : ControllerBase
    {
        private readonly ILogger<CommunicationController> _logger;
        private readonly ChatHub _hubContext;
        private readonly AuthorizedInfo _userInfo;
        private readonly GateServiceProto.V1.GateService.GateServiceClient _SMGate;
        private readonly RemoteGateProvider _connectRemote;
        public CommunicationController(ILogger<CommunicationController> logger, ChatHub hubContext, AuthorizedInfo userInfo, GateServiceProto.V1.GateService.GateServiceClient sMGate, RemoteGateProvider connectRemote)
        {
            _logger = logger;
            _hubContext = hubContext;
            _userInfo = userInfo;
            _SMGate = sMGate;
            _connectRemote = connectRemote;
        }

        [HttpPost]
        public async Task<IActionResult> GetItems_IRegistration(GetItemsRegistration request)
        {
            try
            {
                var requestItems = new GetItemRequest() { ObjID = new OBJ_ID() { StaffID = request.ChildStaff } };

                if (request.ChildStaff == _userInfo.GetInfo?.LocalStaff)
                {
                    var response = await _SMGate.GetItems_IRegistrationAsync(requestItems, deadline: DateTime.UtcNow.AddSeconds(10), cancellationToken: HttpContext.RequestAborted) ?? new RegistrCmd();
                    //List<RegistrCmd>
                    return Ok(response.Array);
                }
                else
                {
                    Registration? reg = null;

                    if (request.ParentStaff == _userInfo.GetInfo?.LocalStaff)
                    {
                        reg = await _SMGate.GetStaffAccessAsync(new IntID() { ID = request.ChildStaff }, deadline: DateTime.UtcNow.AddSeconds(10));
                    }
                    else
                    {
                        var url = $"http://{request.ParentUrl}";

                        var _gate = _connectRemote.GetGateClient(url);

                        if (_gate == null) return NoContent();

                        reg = await _gate.GetStaffAccessAsync(new IntID() { ID = request.ChildStaff }, deadline: DateTime.UtcNow.AddSeconds(10));
                    }

                    if (reg == null || reg.OBJID == null || string.IsNullOrEmpty(reg.UNC)) return BadRequest();

                    try
                    {
                        var r = await _connectRemote.AuthorizeRemote($"http://{reg.UNC}", reg.Login, reg.Passw, HttpContext.RequestAborted);

                        if (r == null) return NoContent();

                        var response = await r.GetItems_IRegistrationAsync(requestItems, deadline: DateTime.UtcNow.AddSeconds(10), cancellationToken: HttpContext.RequestAborted) ?? new RegistrCmd();
                        //List<RegistrCmd>
                        return Ok(response.Array);
                    }
                    catch
                    {
                        return NoContent();
                    }
                    
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
    }
}
