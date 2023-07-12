using AsoDataProto.V1;
using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using ReplaceLibrary;
using ServerLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using static AsoDataProto.V1.AsoData;
using static SMDataServiceProto.V1.SMDataService;
using static SMSSGsoProto.V1.SMSSGso;

namespace ARMsred.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.ViewLogs })]
    public class PuNotifyLogController : Controller
    {
        private readonly SMDataServiceProto.V1.SMDataService.SMDataServiceClient _SMData;
        private readonly SMSSGsoProto.V1.SMSSGso.SMSSGsoClient _SMSGso;
        private readonly AsoDataProto.V1.AsoData.AsoDataClient _ASOData;
        private readonly ILogger<PuNotifyLogController> _logger;
        private readonly IStringLocalizer<SqlBaseReplace> SqlRep;

        public PuNotifyLogController(ILogger<PuNotifyLogController> logger, SMDataServiceProto.V1.SMDataService.SMDataServiceClient data, AsoDataProto.V1.AsoData.AsoDataClient ASOData, SMSSGsoProto.V1.SMSSGso.SMSSGsoClient SMSGso, IStringLocalizer<SqlBaseReplace> sqlRep)
        {
            _logger = logger;
            _SMData = data;
            _ASOData = ASOData;
            _SMSGso = SMSGso;
            SqlRep = sqlRep;
        }
        /// <summary>
        /// Загрузка списка исходя из выбранного типа(приянтые(0))
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<P16xDeviceUnit[]> GetListGateLog()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();


            GetItemRequest request = new GetItemRequest() { ObjID = new OBJ_ID() };

            request.NObjType = 8;

            P16xDeviceUnitList models = await _ASOData.GetListGateLogAsync(request);

            return models.Array.ToArray();
        }


        /// <summary>
        /// Журнал событий(принятые)
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<P16xLog[]> GetP16xLog(IntID Id)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            P16xLogList models = await _SMData.GetP16xLogAsync(Id);

            return models.Array.ToArray();
        }

        /// <summary>
        /// Журнал событий(отправленные)
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<P16xLog[]> GetPRDLog(IntID Id)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            P16xLogList models = await _SMData.GetPRDLogAsync(Id);

            return models.Array.ToArray();
        }

    }
}
