//using Dapr.Client;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using RemoteConnectLibrary;
using ServerLibrary;
using ServerLibrary.Extensions;
using ServiceLibrary;
using ServiceLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static GateServiceProto.V1.GateService;
using static SMDataServiceProto.V1.SMDataService;
using static StaffDataProto.V1.StaffData;
using static StateServiceProto.V1.StateService;

namespace ViewState.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    public partial class StatesDeviceController : Controller
    {
        private readonly StateServiceClient _States;
        private readonly StaffDataClient _StaffData;
        private readonly SMDataServiceProto.V1.SMDataService.SMDataServiceClient _SMData;
        private readonly ILogger<StatesDeviceController> _logger;
        private readonly AuthorizedInfo _userInfo;
        private readonly RemoteGateProvider _connectRemote;
        public StatesDeviceController(StateServiceClient States, StaffDataClient StaffData, ILogger<StatesDeviceController> logger, AuthorizedInfo userInfo, RemoteGateProvider connectRemote, SMDataServiceProto.V1.SMDataService.SMDataServiceClient SMData)
        {
            _logger = logger;
            _States = States;
            _userInfo = userInfo;
            _connectRemote = connectRemote;
            _StaffData = StaffData;
            _SMData = SMData;
        }


        async Task<GateServiceClient?> GetGateClient(int staffId)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            activity?.AddTag("Staff ID", staffId);            
            var m_pIRegistration = await _StaffData.GetStaffAccessAsync(new IntID() { ID = staffId });

            activity?.AddTag("IP address", m_pIRegistration?.UNC);

            if (!string.IsNullOrEmpty(m_pIRegistration?.UNC))
            {
                if (m_pIRegistration.UNC.IndexOf("http://") == -1)
                    m_pIRegistration.UNC = "http://" + m_pIRegistration.UNC;
                var client = await _connectRemote.AuthorizeRemote(m_pIRegistration.UNC, m_pIRegistration.Login, m_pIRegistration.Passw, HttpContext.RequestAborted);
                if (client == null)
                    return null;
                _connectRemote.AddMetaData(m_pIRegistration.UNC, new Metadata().AddRequestToMetadata(Request));
                return client;
            }
            return null;
        }

        /// <summary>
        /// Состояние устройств
        /// </summary>
        /// <param name="tagOBJKey"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.General })]
        public async Task<IActionResult> GetResultList2()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            HardwareMonitorExList models = new();
            OBJ_Key request = new() { ObjID = new() { StaffID = _userInfo.GetInfo?.LocalStaff ?? 0 } };
            try
            {
                models = await _States.GetResultList2Async(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return BadRequest();
            }

            return Ok(JsonFormatter.Default.Format(models));
        }

        [HttpPost]
        public async Task<IActionResult> GetParamViewState()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            ParamViewState param = new();
            try
            {
                var IntervalUpdateState = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsSystem.IntervalUpdateState) });

                int.TryParse(IntervalUpdateState?.Value, out int r);
                if (r > 0)
                    param.IntervalUpdateState = r;

                var IsCheckCu = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsSystem.IsCheckCu) });
                param.IsCheckCu = (IsCheckCu?.Value == "1" ? true : false);

                var IsPOSIgnore = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsSystem.IsPOSIgnore) });
                param.IsPOSIgnore = (IsPOSIgnore?.Value == "1" ? true : false);

                var IsHistoryCommand = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsSystem.IsHistoryCommand) });
                param.IsHistoryCommand = (IsHistoryCommand?.Value == "1" ? true : false);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(param);
        }

        /// <summary>
        /// Состояние подчиненного устройств
        /// </summary>
        /// <param name="tagOBJKey"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.General })]
        public async Task<IActionResult> GetChildInfo([FromBody] int StaffID)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            HardwareMonitorExList models = new();
            try
            {
                var hrReceiver = await GetGateClient(StaffID);
                if (hrReceiver != null)
                {
                    OBJ_Key request = new() { ObjID = new() { StaffID = StaffID } };
                    models = await hrReceiver.GetResultList2Async(request, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: HttpContext.RequestAborted);
                    return Ok(JsonFormatter.Default.Format(models));
                }
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return BadRequest();
            }
        }

        /// <summary>
        /// Истроия состояния устройств
        /// </summary>
        /// <param name="FiltrModel"></param>
        /// <returns>HistoryResponse[]</returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.General })]
        public async Task<IActionResult> GetHistoryDevice(HistoryRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            HistoryList models = new();
            try
            {
                if (request.OBJKey?.ObjID?.StaffID == _userInfo.GetInfo?.LocalStaff)
                {
                    models = await _States.GetHistoryDeviceAsync(request);
                    return Ok(JsonFormatter.Default.Format(models));
                }
                else
                {
                    var hrReceiver = await GetGateClient(request.OBJKey?.ObjID.StaffID ?? 0);
                    if (hrReceiver != null)
                    {
                        models = await hrReceiver.GetHistoryDeviceAsync(request, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: HttpContext.RequestAborted);
                        return Ok(JsonFormatter.Default.Format(models));
                    }
                }
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
    }
}
