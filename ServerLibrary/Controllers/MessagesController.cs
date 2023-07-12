using System.Web;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RemoteConnectLibrary;
using ReplaceLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;
using SyntezServiceProto.V1;
using static SMDataServiceProto.V1.SMDataService;
using static SMSSGsoProto.V1.SMSSGso;
using static StaffDataProto.V1.StaffData;
using static SyntezServiceProto.V1.SyntezService;

namespace ServerLibrary.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class MessagesController : Controller
    {
        private readonly SyntezServiceClient _TtsClient;
        private readonly SMSSGsoClient _SMSGso;
        private readonly StaffDataClient _StaffData;
        private readonly ILogger<MessagesController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;
        private readonly RemoteGateProvider _connectRemote;
        private readonly SMDataServiceClient _SMData;

        public MessagesController(ILogger<MessagesController> logger, RemoteGateProvider connectRemote, StaffDataClient staffData, SMSSGsoClient SMSGso, WriteLog log, AuthorizedInfo userInfo, SyntezServiceClient ttsClient, SMDataServiceClient sMData)
        {
            _logger = logger;
            _SMSGso = SMSGso;
            _Log = log;
            _userInfo = userInfo;
            _TtsClient = ttsClient;
            _connectRemote = connectRemote;
            _StaffData = staffData;
            _SMData = sMData;
        }

        /// <summary>
        /// Получаем параметры для синтеза речи
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetParamSyntez()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            SynthesisData param = new();
            try
            {
                var TTSFREQUENCY = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsSystem.TTSFREQUENCY) });

                int.TryParse(TTSFREQUENCY?.Value, out int r);

                param.Rate = r == 0 ? 8000 : r;

                var TTSVoice = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsSystem.TTSVoice) });

                var v = TTSVoice?.Value;

                param.VoiceIsMen = (v == "0" ? false : true);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(param);
        }


        [HttpGet]
        public async Task TextSynthesisStream([FromQuery] int Rate, bool VoiceIsMen, string Text)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                SynthesisDataStream request = new()
                {
                    Param = new()
                    {
                        Rate = Rate,
                        VoiceIsMen = VoiceIsMen,
                        Text = HttpUtility.HtmlDecode(Text)

                    }
                };
                var outputStream = Response.Body;
                var range = Request.GetRangeHeaderRequest();
                request.StartIndex = range.Item1;
                request.EndIndex = range.Item2;

                var call = _TtsClient.TextSynthesisStream(request);
                var headers = await call.ResponseHeadersAsync;
                string? FormatSound = headers.GetValue(MetaDataName.FormatSound);

                await outputStream.WriteAsync(Response.SetResponseHeaderSound(range, FormatSound));

                await foreach (var str in call.ResponseStream.ReadAllAsync(HttpContext.RequestAborted))
                {
                    var b = str.Value.ToByteArray();
                    if (b.Length > 0)
                        outputStream?.WriteAsync(b);
                }
                await outputStream.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }


        [HttpPost]
        public async Task TextSynthesis(SynthesisData request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var outputStream = Response.Body;
                var call = _TtsClient.TextSynthesis(request);
                var headers = await call.ResponseHeadersAsync;
                string? FormatSound = headers.GetValue(MetaDataName.FormatSound);
                Response.Headers.Add(MetaDataName.FormatSound, FormatSound);

                await foreach (var str in call.ResponseStream.ReadAllAsync(HttpContext.RequestAborted))
                {
                    var b = str.Value.Memory.ToArray();
                    if (b.Length > 0)
                        outputStream?.WriteAsync(b);
                }
                await outputStream.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpGet]
        public async Task GetSoundServer([FromQuery] int MsgId, int Staff, int System, int version)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                RequestSound request = new() { MsgId = new() };
                request.MsgId.ObjID = MsgId;
                request.MsgId.StaffID = Staff;
                request.MsgId.SubsystemID = System;
                var outputStream = Response.Body;

                var range = Request.GetRangeHeaderRequest();
                request.StartIndex = range.Item1;
                request.EndIndex = range.Item2;

                string? FormatSound = null;

                AsyncServerStreamingCall<BytesValue>? call = null;
                if (Staff == _userInfo.GetInfo?.LocalStaff)
                {
                    call = _SMSGso.GetSoundStream(request, null, deadline: DateTime.UtcNow.AddMinutes(5), HttpContext.RequestAborted);
                }
                else if (_userInfo.GetInfo != null)
                {
                    var cuInfo = await _StaffData.GetStaffAccessAsync(new IntID() { ID = Staff });
                    IpAddressUtilities.ParseEndPoint(cuInfo.UNC, out string? IpAdress, out int? Port);
                    if (!string.IsNullOrEmpty(IpAdress) && Port > 0)
                    {
                        var SMGate = await _connectRemote.AuthorizeRemote($"http://{IpAdress}:{Port}", cuInfo.Login, cuInfo.Passw, HttpContext.RequestAborted);

                        if (SMGate != null)
                        {
                            call = SMGate.GetSoundStream(request, null, deadline: DateTime.UtcNow.AddMinutes(5), HttpContext.RequestAborted);
                        }
                    }
                }
                if (call == null)
                    return;

                var headers = await call.ResponseHeadersAsync;
                FormatSound = headers.GetValue(MetaDataName.FormatSound);

                await outputStream.WriteAsync(Response.SetResponseHeaderSound(range, FormatSound));

                await foreach (var str in call.ResponseStream.ReadAllAsync(HttpContext.RequestAborted))
                {
                    var b = str.Value.ToByteArray();
                    if (b.Length > 0)
                        outputStream?.WriteAsync(b);
                }
                await outputStream.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        /// <summary>
        /// Получаем краткую информацию о сообщения(без звука)
        /// </summary>
        /// <param name="request"></param>
        /// <returns>MsgParam</returns>
        [HttpPost]
        public async Task<IActionResult> GetMessageShortInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSGso.GetMessageShortInfoAsync(request);
                return Ok(JsonFormatter.Default.Format(response));
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetObjects_IMessage(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSGso.GetObjects_IMessageAsync(request);
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> WriteMessages([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            OBJ_ID s = new OBJ_ID();
            try
            {
                MsgInfo request = MsgInfo.Parser.ParseJson(json);

                s = await _SMSGso.WriteMessagesAsync(request);

                int EventCode = (int)GsoEnum.IDS_REG_MESS_INSERT;
                if (request.Msg.ObjID != 0)
                    EventCode = (int)GsoEnum.IDS_REG_MESS_UPDATE;

                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: EventCode, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID);

                if (s.ObjID > 0 && request.Msg.ObjID == 0)
                {
                    await _SMSGso.SetMessageStatusAsync(new OBJ_ID(s) { SubsystemID = 0 });
                }
                else if (s.ObjID == 0)
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

    }
}
