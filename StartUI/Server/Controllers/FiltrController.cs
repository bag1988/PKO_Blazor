using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using ReplaceLibrary;
using ServerLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SMDataServiceProto.V1;
using Google.Protobuf;
using FiltersGSOProto.V1;

namespace StartUI.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public partial class FiltrController : Controller
    {
        private readonly FiltersGSOProto.V1.FiltersGSO.FiltersGSOClient _Filtr;

        private readonly ILogger<FiltrController> _logger;
        
        private readonly IStringLocalizer<SqlBaseReplace> SqlRep;

        public FiltrController(ILogger<FiltrController> logger, IStringLocalizer<SqlBaseReplace> sqlRep, FiltersGSOProto.V1.FiltersGSO.FiltersGSOClient filtr)
        {
            _logger = logger;
            SqlRep = sqlRep;
            _Filtr = filtr;
        }


        [HttpPost]
        public async Task<IActionResult> GetStateForStatListCacheAso(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetStateForStatListCacheAsoAsync(request) ?? new();
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
        public async Task<IActionResult> GetObjForStatAso(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetObjForStatAsoAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSituationForStatAso(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetSituationForStatAsoAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetLineForStatAso(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetLineForStatAsoAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }



        [HttpPost]
        public async Task<IActionResult> GetStateForStatListCache(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetStateForStatListCacheAsync(request) ?? new();
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
        public async Task<IActionResult> GetObjForStatUuzs(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetObjForStatUuzsAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSituationForStatUuzs(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetSituationForStatUuzsAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetSituationForStatStaff(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetSituationForStatStaffAsync(request) ?? new();
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
        public async Task<IActionResult> GetObjForStatStaff(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetObjForStatStaffAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetAllStateCUForStat(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetAllStateCUForStatAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetAllStateCUForResultListCache(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetAllStateCUForResultListCacheAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetObjStaffForResultListCache(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetObjStaffForResultListCacheAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSitStaffForResultListCache(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetSitStaffForResultListCacheAsync(request) ?? new();
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
        public async Task<IActionResult> GetConnForResultListCache(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetConnForResultListCacheAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetStateForResultListCache(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetStateForResultListCacheAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetDepForResultListCache(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetDepForResultListCacheAsync(request) ?? new();
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
        public async Task<IActionResult> GetObjForResultListCache(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetObjForResultListCacheAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetSituationForResultListCache(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetSituationForResultListCacheAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получить из базы список линий для фильтра.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> GetNotifyHistoryFiltrLine(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetNotifyHistoryFiltrLineAsync(request) ?? new();
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
        public async Task<IActionResult> GetNotifyHistoryFiltrPhone(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetNotifyHistoryFiltrPhoneAsync(request) ?? new();
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
        public async Task<IActionResult> GetNotifyHistoryFiltrDep(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetNotifyHistoryFiltrDepAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получить из базы список объектов для фильтра
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> GetNotifyHistoryFiltrObj(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetNotifyHistoryFiltrObjAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получить из базы список сеансов для фильтра
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> GetNotifyHistoryFiltrSess(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetNotifyHistoryFiltrSessAsync(request) ?? new();
                return Ok(response.Array.Select(x => x.Int));
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получить из базы список ситуаций для фильтра
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.ViewArhive })]
        public async Task<IActionResult> GetNotifyHistoryFiltrSit(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetNotifyHistoryFiltrSitAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSituationForSitListCache(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetSituationForSitListCacheAsync(request) ?? new();
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetCuNameForICUExResult(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetCuNameForICUExResultAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetObjAddressForICUExResult(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetObjAddressForICUExResultAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetObjDefineForICUExResult(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetObjDefineForICUExResultAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetObjTypeForICUExResult(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetObjTypeForICUExResultAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
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
        public async Task<IActionResult> GetObjNameForICUExResult(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetObjNameForICUExResultAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSituationForICUExResult(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetSituationForICUExResultAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
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
        public async Task<IActionResult> GetUnitNameForNotifyLogStaff(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetUnitNameForNotifyLogStaffAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetSituationForNotifyLogStaff(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetSituationForNotifyLogStaffAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
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
        public async Task<IActionResult> GetDevNameForNotifyLog(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetDevNameForNotifyLogAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
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
        public async Task<IActionResult> GetStateNameForNotifyLog(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetStateNameForNotifyLogAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<CGetAllStateBySessId>
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
        public async Task<IActionResult> GetConnParamForNotifyLog(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetConnParamForNotifyLogAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetDepForNotifyLogAso(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetDepForNotifyLogAsoAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
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
        public async Task<IActionResult> GetAbonForNotifyLog(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetAbonForNotifyLogAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSubSystemForNotifyLogSmp(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetSubSystemForNotifyLogSmpAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetSituationForNotifyLogSmp(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetSituationForNotifyLogSmpAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSituationForNotifyLog(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetSituationForNotifyLogAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSituationForSessionLog(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetSituationForSessionLogAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<IntAndString>
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
        public async Task<IActionResult> GetStatusFromICUExResultBySessId(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetStatusFromICUExResultBySessIdAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<CGetAllStateBySessId>
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
        public async Task<IActionResult> GetAllStateCUBySessId(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetAllStateCUBySessIdAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<CGetAllStateBySessId>
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
        public async Task<IActionResult> GetAllStateFromNotifyhistory(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Filtr.GetAllStateFromNotifyhistoryAsync(request, cancellationToken: HttpContext.RequestAborted);
                //List<CGetAllStateBySessId>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetUserNameListFromEvenLog(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var models = await _Filtr.GetUserNameListFromEvenLogAsync(request) ?? new();
                //UserName[]
                return Ok(models.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetEventcodeListFromEvenLogBySource([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var request = JsonParser.Default.Parse<RequestCode>(json);

                var models = await _Filtr.GetEventcodeListFromEvenLogBySourceAsync(request) ?? new();
                //Eventcode[]
                return Ok(models.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSourceListFromEvenLog([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var request = JsonParser.Default.Parse<RequestCode>(json);
                var models = await _Filtr.GetSourceListFromEvenLogAsync(request) ?? new();
                //Source[]
                return Ok(models.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


    }
}
