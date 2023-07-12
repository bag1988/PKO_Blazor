using AsoDataProto.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GsoReporterProto.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using ReplaceLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using UUZSDataProto.V1;
using static GsoReporterProto.V1.GsoReporter;

namespace ServerLibrary.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class ReportController : Controller
    {
        private readonly GsoReporterClient _data;
        private readonly IStringLocalizer<SqlBaseReplace> SqlRep;

        private readonly ILogger<ReportController> _logger;
        public ReportController(ILogger<ReportController> logger, GsoReporterClient data, IStringLocalizer<SqlBaseReplace> sqlRep)
        {
            _logger = logger;
            _data = data;
            SqlRep = sqlRep;
        }


        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> SZSSessResult(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var result = await _data.SZSSessResultAsync(new CSZSSessResult() { SessID = request.ID, SessSubsystemID = SubsystemType.SUBSYST_SZS });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }



        [HttpPost]
        public async Task<IActionResult> SetFonts(SetFontModel request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                SetFontsRequest r = new();
                r.ObjId = request.Obj;
                r.Font = UnsafeByteOperations.UnsafeWrap(request.Font);

                var result = await _data.SetFontsAsync(r);
                //BoolValue
                return Ok(result);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }


        }

        //GSO.dbo.ReportItem
        /// <summary>
        /// Сохранение параметров отчета
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> CGsoReport(List<CGsoReportItem> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue s = new();

            CGsoReportRequest r = new();
            r.Array.AddRange(request);

            try
            {
                s = await _data.CGsoReportAsync(r);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

            //BoolValue
            return Ok(s);
        }

        /// <summary>
        /// Получить стиль отчета
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetReportFont(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            ArrayByte s = new();
            try
            {
                s = await _data.GetReportFontAsync(request);


                var str = s.Value.ToBase64();

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

            //ReporterFont
            return Ok(new ReporterFont(s.Value.ToByteArray()));
        }

        /// <summary>
        /// Детальный список столбцов отчета
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetReportColumnList(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            GetReportColumnListResponse s = new();
            try
            {
                s = await _data.GetReportColumnListAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<GetReportColumnListItem>
            return Ok(s.Array);
        }


        [HttpPost]
        public async Task<IActionResult> GetReportList(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            GetReportListResponse s = new();
            try
            {
                s = await _data.GetReportListAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<GetReportListItem>
            return Ok(s.Array);
        }

        /// <summary>
        /// Получить описание отчета
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetReportInfo(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            ReportInfo s = new ReportInfo();
            try
            {
                s = await _data.GetReportInfoAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Список столбцов отчета
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetColumnsEx(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            GetColumnsExResponse s = new GetColumnsExResponse();
            try
            {
                s = await _data.GetColumnsExAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s.Array);
        }

        /// <summary>
        /// Печать абонентов
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewObject })]
        public async Task<IActionResult> GetAbonReportAso(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _data.GetAbonReportAsoAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<AbonReport>
                return Ok(JsonFormatter.Default.Format(response));
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Печать результатов оповещения
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> GetINotifySessReportAso(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            AsoReportList response = new();
            try
            {                
                response = await _data.GetINotifySessReportAsoAsync(request);
                _logger.LogInformation("АСО: получение информации для отчета 'Результаты оповещения' по сессии {Session}", request.ObjID?.ObjID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> GetSitReportAso(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _data.GetSitReportAsoAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<SitReport>
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
