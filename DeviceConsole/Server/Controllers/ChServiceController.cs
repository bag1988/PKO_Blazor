using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using SCSChLService.Protocol.Grpc.Proto.V1;
using ServerLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SMDataServiceProto.V1;
using static SCSChLService.Protocol.Grpc.Proto.V1.ChService;
using FiltersGSOProto.V1;
using LibraryProto.Helpers;
using System.Linq.Expressions;
using LibraryProto.Helpers.V1;
using System.Linq;
using SharedLibrary.Utilities;
using System.Net;
using System.Reflection;
using BlazorLibrary.Shared;

namespace DeviceConsole.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class ChServiceController : Controller
    {
        private readonly ChServiceClient _ChServ;

        private readonly SMDataServiceProto.V1.SMDataService.SMDataServiceClient _SMData;

        private readonly ILogger<ChServiceController> _logger;

        private readonly AuthorizedInfo _userInfo;

        private static string session_id = "";

        static bool IsAthChService = false;

        public ChServiceController(ILogger<ChServiceController> logger, SMDataServiceProto.V1.SMDataService.SMDataServiceClient SMData, AuthorizedInfo userInfo, ChServiceClient chServ)
        {
            _logger = logger;
            _ChServ = chServ;
            _SMData = SMData;
            _userInfo = userInfo;
        }

        /// <summary>
        /// Авторизация на SCSChLService
        /// </summary>
        /// <returns></returns>    
        private async Task<Metadata> DoLogin(CancellationToken token)
        {
            try
            {
                while (IsAthChService && !token.IsCancellationRequested)
                {
                    await Task.Delay(100);
                }
                IsAthChService = true;

                using var activity = this.ActivitySourceForController()?.StartActivity();
                activity?.AddTag("ID сессии", session_id);
                string nonce = "";
                try
                {
                    await _ChServ.PingAsync(new Empty(), new Metadata() { new(nameof(session_id), session_id) }, cancellationToken: token);
                }
                catch (Grpc.Core.RpcException ex)
                {
                    nonce = ex.Trailers.Get("nonce")?.Value ?? "";
                    activity?.AddTag("Nonce", nonce);
                    var loginReq = new SCSChLService.Protocol.Grpc.Proto.V1.LoginRequest();

                    string user = "devui", password = "Sensor2022";

                    loginReq.User = user;
                    loginReq.Nonce = nonce;
                    loginReq.Hash = ComputeHash(user, password, nonce);
                    var s = await _ChServ.DoLoginAsync(loginReq, cancellationToken: token);
                    session_id = s.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(DoLogin));
            }
            IsAthChService = false;
            return new Metadata() { new(nameof(session_id), session_id) };
        }

        string ComputeHash(string user, string password, string nonce, string realm = "SCSChLService2", string digestUri = "DoLogin")
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            var A1 = $"{user}:{realm}:{password}";
            var A2 = $"REQUEST:{digestUri}";
            activity?.AddTag("A1", A1);
            activity?.AddTag("A2", A2);

            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] mdA1 = md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(A1));
                byte[] mdA2 = md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(A2));

                string HA1 = BitConverter.ToString(mdA1).Replace("-", string.Empty).ToUpper();
                string HA2 = BitConverter.ToString(mdA2).Replace("-", string.Empty).ToUpper();
                activity?.AddTag("HA1", HA1);
                activity?.AddTag("HA2", HA2);

                var respSrc = $"{HA1}:{nonce}:{HA2}";
                activity?.AddTag("respSrc", respSrc);
                var mdResp = md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(respSrc));

                string resp = BitConverter.ToString(mdResp).Replace("-", string.Empty).ToUpper();
                activity?.AddTag("hash", resp);
                return resp;
            }
        }


        /// <summary>
        /// Прочитать и отдать настройки всех портов конфигурации (УУЗС)
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> ConfigReadAllPorts()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            PortsConfig s = new();
            try
            {
                s = await _ChServ.ConfigReadAllPortsAsync(new Empty(), await DoLogin(HttpContext.RequestAborted), cancellationToken: HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(JsonFormatter.Default.Format(s));
        }


        /// <summary>
        /// Получить информацию по всем каналам из драйвера
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetChannelsInfo(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var response = await _ChServ.GetChannelsInfoAsync(new Empty(), await DoLogin(HttpContext.RequestAborted), cancellationToken: HttpContext.RequestAborted);

                if (response == null || response.Vec.Count == 0)
                    return Ok(response?.Vec);

                var sortList = new Dictionary<int, string>() {
                {0, "port_no" },
                {1, "port_dev_idx" },
                {2, "dev_ver" } };

                var resultSortList = response.Vec.Where(x => x.DevType == request.NObjType).OrderBy(x => request.BFlagDirection == 0 ? ((IMessage)x).Descriptor.FindFieldByName(sortList[request.LSortOrder]).Accessor.GetValue(x) : false)
                    .ThenByDescending(x => request.BFlagDirection == 1 ? ((IMessage)x).Descriptor.FindFieldByName(sortList[request.LSortOrder]).Accessor.GetValue(x) : false).AsQueryable();

                try
                {
                    TestSzsRealDeviceFiltr FiltrModel = new();

                    if (!request.BstrFilter.TryBase64ToProto(out FiltrModel))
                    {
                        FiltrModel = new();
                    }
                    var modelType = Expression.Parameter(ChannelInfo.Descriptor.ClrType);

                    BinaryExpression? filter = null;
                    if (FiltrModel.Connect?.Count > 0)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(ChannelInfo.PortNo));
                        var uintToStringExp = Expression.Call(typeof(IpAddressUtilities), "UintToString", null, member);
                        filter = FiltrModel.Connect.CreateExpressionFromRepeatedString(uintToStringExp, filter);
                    }
                    if (FiltrModel.Serial?.Count > 0)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(ChannelInfo.DevSerNo));
                        var uintToStringExp = Expression.Call(typeof(ChServiceController), "UintToSireal", null, member);
                        filter = FiltrModel.Serial.CreateExpressionFromRepeatedString(uintToStringExp, filter);
                    }
                    if (FiltrModel.Number?.Count > 0)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(ChannelInfo.PortDevIdx));
                        filter = FiltrModel.Number.CreateExpressionFromRepeatedInt(member, filter);
                    }

                    if (FiltrModel.Version?.Count > 0)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(ChannelInfo.DevVer));
                        filter = FiltrModel.Version.CreateExpressionFromRepeatedInt(member, filter);
                    }

                    if (filter != null)
                    {
                        Expression<Func<ChannelInfo, bool>>? filtrExp = Expression.Lambda<Func<ChannelInfo, bool>>(filter, modelType);

                        resultSortList = resultSortList.Where(filtrExp);
                    }

                    if (request.CountData != 0)
                    {
                        resultSortList = resultSortList?.Skip(request.SkipItems).Take(request.CountData);
                    }

                    return Ok(resultSortList);
                }
                catch (Exception ex)
                {
                    _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                }

                return Ok(resultSortList);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        static string UintToSireal(uint value)
        {
            return $"{value} ({value.ToString("X4")}h)";
        }

        /// <summary>
        /// Получить список портов
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> EnumPortsEx()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            VecComPort s = new();
            try
            {
                if (_userInfo.GetInfo?.SubSystemID == SubsystemType.SUBSYST_ASO)
                {
                    var r = await _SMData.EnumPortsExAsync(new Null());
                    s.Vec.AddRange(r.Array.Select(x => new ComPort() { PortNum = (uint)x.Port, PortName = x.PortName, LinkName = x.LinkName }));
                }
                else
                    s = await _ChServ.EnumerateSystemComPortsAsync(new Empty(), await DoLogin(HttpContext.RequestAborted), cancellationToken: HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s.Vec);
        }

        /// <summary>
        /// Получить инфу по всем портам
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetPortsInfo(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var response = await _ChServ.GetPortsInfoAsync(new Empty(), await DoLogin(HttpContext.RequestAborted), cancellationToken: HttpContext.RequestAborted);

                if (response == null || response.Vec.Count == 0)
                    return Ok(response?.Vec);

                var sortList = new Dictionary<int, string>() {
                 {0, "port_no" },
                {1, "dev_count" }
                };

                TestSzsDeviceFiltr FiltrModel = new();

                if (!request.BstrFilter.TryBase64ToProto(out FiltrModel))
                {
                    FiltrModel = new();
                }
                var modelType = Expression.Parameter(PortInfoTag.Descriptor.ClrType);

                var resultSortList = response.Vec.OrderBy(x => request.BFlagDirection == 0 ? ((IMessage)x).Descriptor.FindFieldByName(sortList[request.LSortOrder]).Accessor.GetValue(x) : false)
                    .ThenByDescending(x => request.BFlagDirection == 1 ? ((IMessage)x).Descriptor.FindFieldByName(sortList[request.LSortOrder]).Accessor.GetValue(x) : false).AsQueryable();

                try
                {
                    BinaryExpression? filter = null;
                    if (FiltrModel.Connect?.Count > 0)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(PortInfoTag.PortName));
                        filter = FiltrModel.Connect.CreateExpressionFromRepeatedString(member, filter);

                    }
                    if (FiltrModel.Channel?.Count > 0)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(PortInfoTag.DevCount));
                        filter = FiltrModel.Channel.CreateExpressionFromRepeatedInt(member, filter);
                    }

                    if (filter != null)
                    {
                        Expression<Func<PortInfoTag, bool>>? filtrExp = Expression.Lambda<Func<PortInfoTag, bool>>(filter, modelType);

                        resultSortList = resultSortList.Where(filtrExp);
                    }

                    if (request.CountData != 0)
                    {
                        resultSortList = resultSortList?.Skip(request.SkipItems).Take(request.CountData);
                    }

                    return Ok(resultSortList);
                }
                catch (Exception ex)
                {
                    _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                }

                return Ok(resultSortList);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        /// <summary>
        /// Удалить конфигурацию из ChService
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> ConfigDeletePort(UInt32Value request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var m = await DoLogin(HttpContext.RequestAborted);
                await _ChServ.ConfigDeletePortAsync(request, new Metadata() { m.First() }, cancellationToken: HttpContext.RequestAborted);
                await _ChServ.ReinitializeAsync(new Empty(), new Metadata() { m.First() }, cancellationToken: HttpContext.RequestAborted);
            }
            catch (Grpc.Core.RpcException ex)
            {
                if (ex.StatusCode != Grpc.Core.StatusCode.NotFound)
                {
                    _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                    return ex.GetResultStatusCode();
                }
            }
            return Ok();
        }

        /// <summary>
        /// Установить тип управляющего устройства
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> ConfigAddOrReplacePort([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue response = new() { Value = false };
            try
            {
                var request = PortUniversalRecord.Parser.ParseJson(json);
                var m = await DoLogin(HttpContext.RequestAborted);
                await _ChServ.ConfigAddOrReplacePortAsync(request, new Metadata() { m.First() }, cancellationToken: HttpContext.RequestAborted);
                await _ChServ.ReinitializeAsync(new Empty(), new Metadata() { m.First() }, cancellationToken: HttpContext.RequestAborted);
                response.Value = true;
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        /// <summary>
        /// Получить тип управляющего устройства
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> ConfigReadPort(UInt32Value request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            PortUniversalRecord s = new();
            try
            {
                s = await _ChServ.ConfigReadPortAsync(request, await DoLogin(HttpContext.RequestAborted), cancellationToken: HttpContext.RequestAborted);
            }
            catch (Grpc.Core.RpcException ex)
            {
                if (ex.StatusCode != Grpc.Core.StatusCode.NotFound)
                {
                    _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                    return ex.GetResultStatusCode();
                }
            }
            return Ok(JsonFormatter.Default.Format(s));
        }

    }
}
