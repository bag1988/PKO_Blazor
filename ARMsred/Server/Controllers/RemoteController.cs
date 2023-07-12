
using System.Net;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GateServiceProto.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary;
using ServerLibrary.Extensions;
using ServiceLibrary;
using ServiceLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static GateServiceProto.V1.GateService;
using Metadata = Grpc.Core.Metadata;
using System.Text.Json;
using static Google.Rpc.Context.AttributeContext.Types;
using Microsoft.Extensions.Options;
using RemoteConnectLibrary;

namespace ARMsred.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/remote/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public partial class RemoteController : Controller
    {
        private readonly ILogger<RemoteController> _logger;
        private readonly GateServiceProto.V1.GateService.GateServiceClient _SMGate;
        private readonly RemoteGateProvider _connectRemote;
        private readonly AuthorizedInfo _userInfo;
        private readonly string ThisConnectGrpc;

        public RemoteController(ILogger<RemoteController> logger, GateServiceProto.V1.GateService.GateServiceClient sMGate, AuthorizedInfo userInfo, RemoteGateProvider connectRemote, IOptions<UriBuilder> connectSmGateSettings)
        {
            _logger = logger;
            _SMGate = sMGate;
            _connectRemote = connectRemote;
            _userInfo = userInfo;
            ThisConnectGrpc = connectSmGateSettings.Value.Uri.ToString();
        }

        GateServiceProto.V1.GateService.GateServiceClient GetSMGateClient
        {
            get
            {
                string UncRemoteCu = Request.Headers[CookieName.UncRemoteCu].FirstOrDefault() ?? "";
                if (!string.IsNullOrEmpty(UncRemoteCu))
                {
                    Metadata metaData = new();
                    metaData.AddRequestToMetadata(Request);

                    var client = _connectRemote.GetGateClient($"http://{UncRemoteCu}", metaData);

                    if (client == null)
                        throw new Grpc.Core.RpcException(new Status(Grpc.Core.StatusCode.InvalidArgument, "Ошибка подключения к удаленному серверу"));

                    return client;
                }
                else
                    return _SMGate;
            }
        }

        /// <summary>
        /// Запуск оповещения
        /// </summary>
        /// <param name="request"></param>
        /// <returns>NotificationResponse</returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.StartNotify })]
        public async Task<IActionResult> StartNotify([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var request = JsonParser.Default.Parse<NotificationRequest>(json);
                var response = await GetSMGateClient.StartNotifyAsync(request);
                if (response.ResponseCode == ResponseCode.LisenceNotExist)
                {
                    return StatusCode((int)System.Net.HttpStatusCode.SeeOther);
                }

                return Ok(response.SessionIds.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.StartNotify })]
        public async Task<IActionResult> StopNotify(StartNotify request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var r = new NotificationRequest()
                {
                    UnitID = request.UnitID,
                    SessId = request.SessId,
                    RequestType = RequestType.StopNotification
                };
                r.ListOBJ.AddRange(request.ListOBJ);

                var response = await GetSMGateClient.StopNotifyAsync(r);

                if (response.ResponseCode == ResponseCode.LisenceNotExist)
                {
                    return StatusCode((int)System.Net.HttpStatusCode.SeeOther);
                }
                return Ok(response.SessionIds.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSituationState(OBJ_Key request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await GetSMGateClient.GetSituationStateAsync(request, cancellationToken: HttpContext.RequestAborted);
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> S_GetSitGroupListLink(SitGroupLinkInfoRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await GetSMGateClient.S_GetSitGroupListLinkAsync(request, cancellationToken: HttpContext.RequestAborted);
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> S_GetSitGroupList(SitGroupInfoRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await GetSMGateClient.S_GetSitGroupListAsync(request, cancellationToken: HttpContext.RequestAborted);
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetAppPortInfo(BoolValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await GetSMGateClient.GetAppPortsAsync(request, cancellationToken: HttpContext.RequestAborted);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.StartNotify })]
        public async Task<IActionResult> P16xGateStartNotify4([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var request = JsonParser.Default.Parse<RequestStartNotify>(json);
                await GetSMGateClient.P16xGateStartNotify4Async(request, cancellationToken: HttpContext.RequestAborted);
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
        public async Task<IActionResult> Rem_CloseWriteManualCmd(UInt32Value request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await GetSMGateClient.Rem_CloseWriteManualCmdAsync(request, cancellationToken: HttpContext.RequestAborted);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json">UInt32AndBytes</param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> Rem_WriteManualGSMBuffer([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var request = UInt32AndBytes.Parser.ParseJson(json);
                await GetSMGateClient.Rem_WriteManualGSMBufferAsync(request);
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
        public async Task<IActionResult> Rem_WriteManualBuffer([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                UInt32AndBytes request = UInt32AndBytes.Parser.ParseJson(json);
                await GetSMGateClient.Rem_WriteManualBufferAsync(request, cancellationToken: HttpContext.RequestAborted);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json">CreateMsgInFile</param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> Rem_CreateMsgInFile([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var request = CreateMsgInFile.Parser.ParseJson(json);

                var s = await GetSMGateClient.Rem_CreateMsgInFileAsync(request, cancellationToken: HttpContext.RequestAborted);

                if (s.MessageId.ObjID == 0)
                {
                    return BadRequest();
                }
                return Ok(s);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        [HttpPost]
        public async Task<IActionResult> GetParams(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            StringValue s = new();
            try
            {
                s = await GetSMGateClient.GetParamsAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetSndSettingEx(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                if (User.Identity?.IsAuthenticated == false)
                    Console.WriteLine("No Authenticated");
                GetSndSettingExResponse? models;
                models = await GetSMGateClient.GetSndSettingExAsync(request, cancellationToken: HttpContext.RequestAborted);
                return Ok(JsonFormatter.Default.Format(models));
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> SetSndSettingEx([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                BoolValue r = new BoolValue();
                var request = JsonParser.Default.Parse<SetSndSettingExRequest>(json);
                r = await GetSMGateClient.SetSndSettingExAsync(request, cancellationToken: HttpContext.RequestAborted);
                return Ok(r);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        [HttpPost]
        public async Task<IActionResult> SetState(P16xGateDevice request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var response = await GetSMGateClient.SetStateAsync(request, cancellationToken: HttpContext.RequestAborted) ?? new BoolValue();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Загрузка списка исходя из выбранного типа(отправленные(1))
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetState()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var response = await GetSMGateClient.GetStateAsync(new Null(), cancellationToken: HttpContext.RequestAborted) ?? new P16xGateDeviceList();

                //List<P16xGateDevice>
                return Ok(response.Array.Where(x => x.SerNo != 0));
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> GetCuLocalName()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                Registration response = new();

                string userName = _userInfo.GetInfo?.UserName ?? "";
                string password = "";
                UserCookie? userCookie = Request.GetUserCookie();
                if (userCookie != null)
                {
                    password = userCookie.RefreshToken ?? string.Empty;

                    if (!string.IsNullOrEmpty(userName))
                    {
                        response.Login = userName;
                        response.Passw = password;
                        var r = await GetSMGateClient.GetRegInfoAllAsync(new Empty(), cancellationToken: HttpContext.RequestAborted) ?? new();

                        var Caller = r.Array.FirstOrDefault() ?? new();

                        response.CUName = Caller.CuName;
                        //response.UNC = Caller.UNC;
                        return Ok(response);
                    }
                }

                return new StatusCodeResult((int)HttpStatusCode.Unauthorized);


            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> AuthorizeUser(RequestLogin request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var user = await GetSMGateClient.BeginUserSessAsync(request, cancellationToken: HttpContext.RequestAborted);

                if (user?.IsAuthSuccessful == true)
                {
                    UserCookie newUser = new()
                    {
                        UserToken = user.Token,
                        RefreshToken = request.Password
                    };
                    Response.SetUserCookie(newUser);
                    return Ok(user.Token);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> RefreshUser([FromBody] string NameUser)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            AuthResponse? user = null;
            try
            {
                UserCookie? userCookie = Request.GetUserCookie();

                if (userCookie != null)
                {
                    user = await GetSMGateClient.BeginUserSessAsync(new RequestLogin()
                    {
                        User = NameUser,
                        Password = userCookie.RefreshToken ?? ""
                    }, cancellationToken: HttpContext.RequestAborted);

                    if (user?.IsAuthSuccessful == true)
                    {
                        userCookie.UserToken = user.Token;

                        Response.SetUserCookie(userCookie);

                        return Ok(user.Token);
                    }
                }
                Response.Cookies.Delete(Request.Host.GetTokenName());
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetItems_IRegistration()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                GetItemRequest request = new();

                request.ObjID = new() { StaffID = _userInfo.GetInfo?.LocalStaff ?? 0, SubsystemID = SubsystemType.SUBSYST_GSO_STAFF };

                var response = await GetSMGateClient.GetItems_IRegistrationAsync(request, cancellationToken: HttpContext.RequestAborted) ?? new RegistrCmd();
                //List<RegistrCmd>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetStaffAccess(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await GetSMGateClient.GetStaffAccessAsync(request, cancellationToken: HttpContext.RequestAborted) ?? new Registration();

                //CGetRegInfo
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        /// <summary>
        /// Список групп
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SGetGroupList(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await GetSMGateClient.S_GetGroupListAsync(request, cancellationToken: HttpContext.RequestAborted) ?? new P16xGroupList();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        /// <summary>
        /// Список команд для запуска
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetGroupCommandList(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await GetSMGateClient.GetGroupCommandListAsync(request, cancellationToken: HttpContext.RequestAborted) ?? new P16xGroupCommandList();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получает список состояний для ПР
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> S_GetGroupListLink(S_GetGroupListLinkRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                P16xGateDeviceList response = new();
                response = await GetSMGateClient.S_GetGroupListLinkAsync(request, cancellationToken: HttpContext.RequestAborted) ?? new P16xGateDeviceList();
                return Ok(response.Array.OrderBy(x => x.ObjectName));
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        /// <summary>
        /// Список пунктов управления
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetResultList(OBJ_Key request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await GetSMGateClient.GetResultListAsync(request, cancellationToken: HttpContext.RequestAborted) ?? new HardwareMonitorList();
                return Ok(JsonFormatter.Default.Format(response));
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
    }
}
