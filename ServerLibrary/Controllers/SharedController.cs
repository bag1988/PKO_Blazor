using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using SMDataServiceProto.V1;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using SyntezServiceProto.V1;
using static AsoDataProto.V1.AsoData;
using static SMDataServiceProto.V1.SMDataService;
using static SMSSGsoProto.V1.SMSSGso;
using static StaffDataProto.V1.StaffData;
using static UUZSDataProto.V1.UUZSData;

namespace ServerLibrary.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class SharedController : Controller
    {
        private readonly SMSSGsoClient _SMSGso;
        private readonly SMDataServiceClient _SMData;
        private readonly AsoDataClient _ASOData;
        private readonly ILogger<SharedController> _logger;
        private readonly UUZSDataClient _UUZSData;

        public SharedController(ILogger<SharedController> logger, SMSSGsoClient SMSGso, SMDataServiceClient SMData, AsoDataClient ASOData, UUZSDataClient UUZSData)
        {
            _logger = logger;
            _SMSGso = SMSGso;
            _SMData = SMData;
            _ASOData = ASOData;
            _UUZSData = UUZSData;
        }


        /// <summary>
        /// Получаем список линий
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetObjects_ILine(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _UUZSData.GetObjects_ILineAsync(request);
                //Line
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }


        /// <summary>
        /// Получить типы линий
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetLineTypeList(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            IntAndStrList response = new();
            try
            {
                response = await _SMData.GetLineTypeListAsync(new Null());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

            if (request.ID == SubsystemType.SUBSYST_ASO)
            {
                return Ok(response.Array.Where(x => (new int[] { 1, 6, 7, 9, 10, 11 }).Contains(x.Number)).ToList());
            }
            else if (request.ID == SubsystemType.SUBSYST_SZS)
            {
                return Ok(response.Array.Where(x => (new int[] { 1, 2, 3, 4, 5 }).Contains(x.Number)).ToList());
            }
            else if (request.ID == SubsystemType.SUBSYST_RDM)
            {
                return Ok(response.Array.Where(x => (new int[] { 1, 2 }).Contains(x.Number)).ToList());
            }


            return Ok(response.Array.OrderBy(x => x.Number));
        }

        /// <summary>
        /// Получить типы линий
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetConnTypeList()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            IntAndStrList response = new();
            try
            {
                response = await _ASOData.GetConnTypeListAsync(new Null());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }

        /// <summary>
        /// Получить характеристики линии
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetRestrictList(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            RestrictList response = new();
            try
            {
                response = await _SMData.GetRestrictListAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }

        /// <summary>
        /// Проверка параметров дозвона на уникальность
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> IsExistConnParam(CIsExistConnParam request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _ASOData.IsExistConnParamAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получаем список пользователей и их разрешения
        /// </summary>
        /// <returns>UserInfo[]</returns>
        [HttpPost]
        public async Task<IActionResult> GetGsoUserEx2()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            UserInfoExList response = new();

            List<SharedLibrary.Models.UserInfo> listInfo = new();

            try
            {
                response = await _SMSGso.GetGsoUserEx2Async(new Null());

                if (response != null)
                {
                    listInfo.AddRange(response.Array.Select(x => new SharedLibrary.Models.UserInfo()
                    {
                        Login = x.Login,
                        Passw = x.Passw,
                        Status = x.Status,
                        OBJID = x.OBJID,
                        SuperVision = x.SuperVision,
                        Permisions = new PermisionsUser()
                        {
                            PerAccAso = x.Permisions.PerAccAso.ToByteArray(),
                            PerAccCu = x.Permisions.PerAccCu.ToByteArray(),
                            PerAccFn = x.Permisions.PerAccFn.ToByteArray(),
                            PerAccP16 = x.Permisions.PerAccP16.ToByteArray(),
                            PerAccRdm = x.Permisions.PerAccRdm.ToByteArray(),
                            PerAccSec = x.Permisions.PerAccSec.ToByteArray(),
                            PerAccSzs = x.Permisions.PerAccSzs.ToByteArray()
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

            return Ok(listInfo);
        }

        /// <summary>
        /// Сохраняем отдельный параметр настройки
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> SetParams(SetParamRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            SMDataServiceProto.V1.String s = new();
            try
            {
                s = await _SMData.SetParamsAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }


        /// <summary>
        /// Получаем отдельный параметр настройки
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetParams(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            StringValue s = new();
            try
            {
                s = await _SMData.GetParamsAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }


        /// <summary>
        /// Получаем ID ПУ
        /// </summary>
        /// <param name="request"> OBJ_ID->ObjID=serNo </param>
        /// <returns>OBJ_ID</returns>
        [HttpPost]
        public async Task<IActionResult> GetControlUnitKey(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            OBJ_ID response = new OBJ_ID();
            try
            {
                response = await _SMSGso.GetControlUnitKeyAsync(request) ?? new();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }
    }
}
