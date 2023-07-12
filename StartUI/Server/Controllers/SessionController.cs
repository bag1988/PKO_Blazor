using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using AsoDataProto.V1;
using SMSSGsoProto.V1;
using StaffDataProto.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using ReplaceLibrary;
using ServerLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static AsoDataProto.V1.AsoData;
using static SMDataServiceProto.V1.SMDataService;
using static SMP16XProto.V1.SMP16x;
using static SMSSGsoProto.V1.SMSSGso;
using static StaffDataProto.V1.StaffData;
using static UUZSDataProto.V1.UUZSData;
using static FiltersGSOProto.V1.FiltersGSO;

namespace StartUI.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class SessionController : Controller
    {
        private readonly StaffDataProto.V1.StaffData.StaffDataClient _StaffData;
        private readonly SMSSGsoProto.V1.SMSSGso.SMSSGsoClient _SMGso;
        private readonly SMDataServiceProto.V1.SMDataService.SMDataServiceClient _SMData;
        private readonly AsoDataProto.V1.AsoData.AsoDataClient _ASOData;
        private readonly UUZSDataProto.V1.UUZSData.UUZSDataClient _UUZSData;
        private readonly SMP16XProto.V1.SMP16x.SMP16xClient _SMP16x;
        private readonly ILogger<SessionController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;
        private readonly IStringLocalizer<SqlBaseReplace> SqlRep;

        public SessionController(ILogger<SessionController> logger, SMDataServiceProto.V1.SMDataService.SMDataServiceClient SMData, UUZSDataClient UUZSData, AsoDataClient ASOData, SMSSGsoClient SMGso, SMP16xClient SMP16x, WriteLog log, AuthorizedInfo userInfo, StaffDataClient StaffData, IStringLocalizer<SqlBaseReplace> sqlRep)
        {
            _logger = logger;
            _SMData = SMData;
            _Log = log;
            _userInfo = userInfo;
            _ASOData = ASOData;
            _SMGso = SMGso;
            _StaffData = StaffData;
            _UUZSData = UUZSData;
            SqlRep = sqlRep;
            _SMP16x = SMP16x;
        }

        [HttpGet]
        public async Task<IActionResult> GetFileByPhonogram([FromQuery] string filePath)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _ASOData.GetFileByPhonogramAsync(new SMDataServiceProto.V1.String() { Value = filePath }, cancellationToken: HttpContext.RequestAborted);

                string file_type = "audio/wav";
                string file_name = Path.GetFileName(filePath);
                return File(response.Value.ToByteArray(), file_type, file_name);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> GetPhonogramListBySess(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _ASOData.GetPhonogramListBySessAsync(request, cancellationToken: HttpContext.RequestAborted);

                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }


        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> GetSessionCountCallObjects(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                CountResponse countCall = new();
                CountResponse countUncall = new();

                countCall = await _ASOData.GetSessionCountCallObjectsAsync(request, cancellationToken: HttpContext.RequestAborted);
                countUncall = await _ASOData.GetSessionCountUncallObjectsAsync(request, cancellationToken: HttpContext.RequestAborted);

                return Ok(new CountCallASO() { CountCall = countCall?.Count ?? 0, CountUnCall = countUncall?.Count ?? 0 });
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        /// <summary>
        /// Получаем детальную статистику по дозвону
        /// </summary>
        /// <param name="request"></param>
        /// <returns>List<NotifyState></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> GetNotifyHistory(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _ASOData.GetNotifyHistoryAsync(request, cancellationToken: HttpContext.RequestAborted);
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        /// <summary>
        /// время последней сессии
        /// </summary>
        /// <param name="request"></param>
        /// <returns>CGetSessIdTime</returns>
        [HttpPost]
        public async Task<IActionResult> GetSessTime(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CGetSessIdTime response = new();
            try
            {
                if (_userInfo.GetInfo?.SubSystemID == SubsystemType.SUBSYST_ASO)
                    response = await _ASOData.GetSessTimeASOAsync(request);
                else if (_userInfo.GetInfo?.SubSystemID == SubsystemType.SUBSYST_GSO_STAFF)
                    response = await _StaffData.GetSessTimeAsync(request);
                else if (_userInfo.GetInfo?.SubSystemID == SubsystemType.SUBSYST_SZS)
                    response = await _UUZSData.GetSessTimeAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //CGetSessIdTime
            return Ok(response);
        }

        /// <summary>
        /// получить список сессий
        /// </summary>
        /// <param name="request">GetItemRequest</param>
        /// <returns>CSessions[]</returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> GetSessionList(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            SMSSGsoProto.V1.CSessionList models = new();
            try
            {
                models = await _SMGso.GetSessionListAsync(request, cancellationToken: HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<CSessions>
            return Ok(models.Array);
        }

        /// <summary>
        /// Информация по результатм оповещения
        /// </summary>
        /// <param name="request">SubSystemID</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetSessionTextResults(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            IntID r = new();
            StringValue response = new();
            try
            {
                if (request.ID == SubsystemType.SUBSYST_ASO)
                {
                    response = await _ASOData.GetSessionTextResultsAsync(r) ?? new();
                }
                else if (request.ID == SubsystemType.SUBSYST_SZS)
                {
                    response = await _UUZSData.GetSessionTextResultsAsync(r) ?? new();
                }
                else if (request.ID == SubsystemType.SUBSYST_GSO_STAFF)
                {
                    response = await _StaffData.GetSessionTextResultsAsync(r);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

            return Ok(response);

        }

        /// <summary>
        /// получить список сценриев отработанных в сеансе(ПУ)
        /// </summary>
        /// <param name="request">GetItemRequest</param>
        /// <returns>NotifySessModel[]</returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> GetItemsEx_INotifySess(GetItemRequest request)
        {
            if (HttpContext.RequestAborted.IsCancellationRequested)
            {
                return BadRequest();
            }

            using var activity = this.ActivitySourceForController()?.StartActivity();
            CUResultsListEx models = new();
            try
            {
                models = await _StaffData.GetItemsEx_INotifySessAsync(request, cancellationToken: HttpContext.RequestAborted) ?? new();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<CUResultsEx>
            return Ok(models.Array);
        }


        /// <summary>
        /// Получить список сценриев отработанных в сеансе
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> GetItems_INotifySess(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CResultsList r = new();
            try
            {
                switch (request.ObjID.SubsystemID)
                {
                    case SubsystemType.SUBSYST_ASO:
                    {
                        var result = await _ASOData.GetItems_INotifySessAsync(request, cancellationToken: HttpContext.RequestAborted);
                        //List<CResults>
                        return Ok(result.Array);
                    }
                    case SubsystemType.SUBSYST_SZS:
                    {
                        var result = await _UUZSData.GetItems_INotifySessAsync(request, cancellationToken: HttpContext.RequestAborted);
                        //List<CResults>
                        return Ok(result.Array);
                    }
                    case SubsystemType.SUBSYST_P16x:
                    {
                        var result = await _SMP16x.GetItems_INotifySessAsync(request, cancellationToken: HttpContext.RequestAborted);
                        //List<CSMP16xGetItemsINotifySess>
                        return Ok(result.Array);
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        /// <summary>
        /// Получить детальную статистику по сеансу
        /// </summary>
        /// <param name="request">GetItemRequest</param>
        /// <returns>CUExStats[]</returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> GetItems_ICUExResult(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                CGetResExStatList models = new();
                models = await _StaffData.GetItems_ICUExResultAsync(request, cancellationToken: HttpContext.RequestAborted) ?? new();
                //List<CGetResExStat>
                return Ok(models.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }


        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.DeleteResultNotify })]
        public async Task<IActionResult> DeleteSession(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue s = new() { Value = false };
            try
            {
                if (request.SubsystemID == SubsystemType.SUBSYST_GSO_STAFF)
                    await _SMGso.DeleteSessionStaffAsync(request);
                else
                    await _SMGso.DeleteSessionSMSAsync(request);
                s.Value = true;


                await _Log.Write(new WriteLog2Request() { Source = (int)GSOModules.StartUI_Module, EventCode = 182, SubsystemID = 0, UserID = _userInfo.GetInfo?.UserID ?? 0 });

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        [HttpGet]
        public async Task ReadSoundFromFile([FromQuery] string filename)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var outputStream = Response.Body;

                var range = Request.GetRangeHeaderRequest();

                RequestSoundFile request = new() { FilePath = filename, StartIndex = range.Item1, EndIndex = range.Item2 };

                var call = _SMGso.ReadSoundFromFileStream(request, null, deadline: DateTime.UtcNow.AddMinutes(5), HttpContext.RequestAborted);

                var headers = await call.ResponseHeadersAsync;
                string? FormatSound = headers.GetValue(MetaDataName.FormatSound);
                string? FileName = headers.GetValue(MetaDataName.FileName);

                Response.Headers.ContentDisposition = $"attachment;filename=\"{FileName}\"";

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
    }
}
