using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Mvc;
using ReplaceLibrary;
using ServerLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using static AsoDataProto.V1.AsoData;
using static SMDataServiceProto.V1.SMDataService;
using static AsoNlServiceProto.V1.AsoNlService;
using AsoNlServiceProto.V1;
using Google.Protobuf;
using static UUZSDataProto.V1.UUZSData;

namespace DeviceConsole.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class LineController : Controller
    {
        private readonly SMDataServiceProto.V1.SMDataService.SMDataServiceClient _SMData;
        private readonly AsoDataProto.V1.AsoData.AsoDataClient _ASOData;
        private readonly AsoNlServiceProto.V1.AsoNlService.AsoNlServiceClient _AsoNl;
        private readonly UUZSDataProto.V1.UUZSData.UUZSDataClient _UUZSData;
        private readonly ILogger<LineController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;


        public LineController(ILogger<LineController> logger, SMDataServiceProto.V1.SMDataService.SMDataServiceClient SMData, UUZSDataClient UUZSData, AsoDataClient ASOData, AsoNlServiceClient AsoNl, WriteLog log, AuthorizedInfo userInfo)
        {
            _logger = logger;
            _SMData = SMData;
            _ASOData = ASOData;
            _UUZSData = UUZSData;
            _AsoNl = AsoNl;
            _Log = log;
            _userInfo = userInfo;
        }
        /// <summary>
        /// Получить список свободных линий
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> StartTestLine([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var request = JsonParser.Default.Parse<TestNotificationConfiguration>(json);
                await _AsoNl.StartTestNotificationAsync(request);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> StopTestLine()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                await _AsoNl.StopTestNotificationAsync(new Empty());
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetFreeLineList()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BindingLineList s = new();
            try
            {
                s = await _SMData.GetFreeLineListAsync(new Null());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //GetFreeLineListItem
            return Ok(s.Array);
        }


        /// <summary>
        /// Удалить линию
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteLine(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue s = new();

            try
            {
                s = await _SMData.DeleteLineAsync(request);
                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: (int)GsoEnum.IDS_REG_LINE_DELETE, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }


        [HttpPost]
        public async Task<IActionResult> GetMaxPhoneLine()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMData.GetMaxPhoneLineAsync(new Null());
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        /// <summary>
        /// Получить список линий
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetItems_ILine(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
               var response = await _SMData.GetItems_ILineAsync(request);
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }           
        }

        /// <summary>
        /// Добавить линию связи
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> AddLine(Line request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            IntID s = new();

            try
            {
                s = await _SMData.AddLineAsync(request);
                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: (int)GsoEnum.IDS_REG_LINE_INSERT, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Редактировать линию связи
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> EditLine(Line request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue s = new();

            try
            {
                s = await _SMData.EditLineAsync(request);
                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: (int)GsoEnum.IDS_REG_LINE_UPDATE, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Установить статус линии
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> SetLineStatus(CSetLineStatus request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue s = new();

            try
            {
                s = await _ASOData.SetLineStatusAsync(request);

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
        /// Получить информацию о линии
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetLineInfo(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            Line s = new();

            try
            {
                s = await _SMData.GetLineInfoAsync(request);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        [HttpPost]
        public async Task<IActionResult> GetLineInfoAso(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var response = await _ASOData.GetLineInfoAsync(request);
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Удалить параметр
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteRestrict(DeleteRestrictRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue response = new();
            try
            {
                response = await _SMData.DeleteRestrictAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        /// <summary>
        /// Добавить параметр
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> AddRestrict(AddRestrictRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue response = new();
            try
            {
                response = await _SMData.AddRestrictAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        /// <summary>
        /// Удалить связь с устройствам
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteLineBinding(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            SIntResponse response = new();
            try
            {
                if (_userInfo.GetInfo?.SubSystemID == SubsystemType.SUBSYST_ASO)
                    response = await _ASOData.DeleteLineBindingAsync(request);
                else if (_userInfo.GetInfo?.SubSystemID == SubsystemType.SUBSYST_SZS)
                {
                    var b = await _UUZSData.DeleteLineBindingAsync(request);
                    response.SInt = b.Value == true ? 0 : 1;
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
        /// Установить привязку линии
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> SetLineBinding(LineBinding request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            SIntResponse s = new SIntResponse();
            try
            {
                if (_userInfo.GetInfo?.SubSystemID == SubsystemType.SUBSYST_ASO)
                {
                    s = await _ASOData.SetLineBindingAsync(request);
                }
                else if (_userInfo.GetInfo?.SubSystemID == SubsystemType.SUBSYST_SZS)
                {
                    var b = await _UUZSData.SetLineBindingAsync(request);
                    s.SInt = b.Value == true ? 0 : 1;
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Установить расширение линии
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> SetLineExtParam(CSetLineExtParam request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue s = new BoolValue();
            try
            {
                s = await _SMData.SetLineExtParamAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }
        /// <summary>
        /// Получить свободные каналы
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetFreeChannelList()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BindingDeviceList response = new();
            try
            {
                if (_userInfo.GetInfo?.SubSystemID == SubsystemType.SUBSYST_ASO)
                    response = await _ASOData.GetFreeChannelListAsync(new Null());
                else
                    response = await _UUZSData.GetFreeChannelListAsync(new Null());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }

        /// <summary>
        /// Проверка абонентского номера на уникальность
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CheckExistPhone_2(CheckExistPhone_2Request request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CountResponse response = new();
            try
            {
                response = await _SMData.CheckExistPhone_2Async(request);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        /// <summary>
        /// Получить расширении линии
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetLineExtParam(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            SMDataServiceProto.V1.String response = new();
            try
            {
                response = await _SMData.GetLineExtParamAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        /// <summary>
        /// Получить привязки к устройствам
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetBindingDevice(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BindingDeviceList response = new();
            try
            {
                if (_userInfo.GetInfo?.SubSystemID == SubsystemType.SUBSYST_ASO)
                    response = await _ASOData.GetBindingDeviceAsync(request);
                else
                    response = await _UUZSData.GetBindingDeviceAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }

        /// <summary>
        /// Получить информацию о линии, подключенной к каналу
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetBindingLine(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BindingLine s = new();
            try
            {
                s = await _UUZSData.GetBindingLineAsync(request);
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
